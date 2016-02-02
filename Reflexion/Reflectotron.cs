//welocme to LINQ hell. If you aren't master in LINQ you should probably leave!

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Reflex
{
    public class Reflectotron
    {
        /// <summary>
        ///     Keywords for modifiyng all stuff in classed
        /// </summary>
        public enum EKeyWords
        {
            Public,
            Private,
            Virtual,
            Protected,
            Internal,
            Abstract,
            Static,
            Readonly,
            Override,
            Sealed,
            Operator,
            Implicit,
            Explicit,
            Async
        }

        /// <summary>
        ///     <see cref="BindingFlags" /> that are used to obtain all useful stuff
        /// </summary>
        private const BindingFlags AllBindingFlags =
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public |
            BindingFlags.Static | BindingFlags.DeclaredOnly;

        /// <summary>
        ///     Instatializes a new <see cref="Reflectotron" /> class.
        /// </summary>
        /// <param name="obj">Object to be reflected, its estimated reprasentation will be saved in <see cref="Info" /></param>
        public Reflectotron(object obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            var sb = new StringBuilder();
            var T = obj.GetType();

            var clMgr = new ClassManager(T.Namespace); //manages class names and namespaces
            var properties = new List<string>(); //properties, used to filter out auto getters and setters from methods

            sb.AppendLine($"namespace {T.Namespace}");
            sb.AppendLine("{");

            #region class attributes

            var classAttributes = new Attributes(T.GetCustomAttributes(), clMgr, 2);
            sb.Append(classAttributes);

            #endregion

            //load all keywords that define given class
            var classMods = new AccessModifiers(T);
            sb.Append($"  {classMods}class {clMgr.Write(T)}");

            #region inheritance

            var ancestors = new List<string>();

            if ((T.BaseType != null) && (T.BaseType != typeof(object)))
            //inherits from something else than only object class
            {
                ancestors.Add(clMgr.Write(T.BaseType, true));
            }

            var interfaces = T.FindInterfaces(InterfaceFilter, T.BaseType); //find interfaces this inherits from
            ancestors.AddRange(interfaces.Select(q => clMgr.Write(q, true)));

            if (ancestors.Any())
            {
                sb.Append(" : " + string.Join(", ", ancestors));
            }

            #endregion

            sb.AppendLine();
            sb.AppendLine("  {");

            #region properties

            sb.AppendLine("#region properties");

            foreach (var property in T.GetProperties(AllBindingFlags))
            {
                var attrs = new Attributes(property.GetCustomAttributes(), clMgr, 4);

                sb.Append(attrs);
                sb.Append("    ");

                #region access modifiers for getters and setters

                var getterInfos =
                    property.GetAccessors(true).Where(a => a.ReturnType != typeof(void)).ToArray();
                //setters don't have return types
                var setterInfos =
                    property.GetAccessors(true).Where(a => a.ReturnType == typeof(void)).ToArray(); //getters do

                var setterMods = new AccessModifiers(setterInfos);
                var getterMods = new AccessModifiers(getterInfos);

                //those which are the same for both getter and setter
                var commonModifiers = new AccessModifiers();
                commonModifiers.AddRange(getterMods.Where(accessModifier => setterMods.Contains(accessModifier)));
                getterMods.RemoveAll(q => commonModifiers.Contains(q));
                setterMods.RemoveAll(q => commonModifiers.Contains(q)); //they are redundant there

                if (getterMods.IsPublic && setterMods.IsPrivate) //one is more restraining than other
                {
                    commonModifiers.Add(EKeyWords.Public);
                    getterMods.Remove(EKeyWords.Public);
                }
                else if (setterMods.IsPublic && getterMods.IsPrivate) //who uses this anyway?
                {
                    commonModifiers.Add(EKeyWords.Public);
                    setterMods.Remove(EKeyWords.Public);
                }
                #endregion

                if (property.CanRead && !property.CanWrite)//read-only property
                {
                    commonModifiers.AddRange(getterMods);
                    getterMods.Clear();
                }
                else if (property.CanWrite && !property.CanRead)//set-only property (does it even exist?)
                {
                    commonModifiers.AddRange(setterMods);
                    setterMods.Clear();
                }

                string type = clMgr.Write(property.PropertyType, true,T,q=>q.GetProperty(property.Name).PropertyType);
                bool isGeneric = false;

                #region generic property type

                if (T.IsGenericType) //may be a property with generic type
                {
                    Type genProp = T.GetGenericTypeDefinition().GetProperty(property.Name).PropertyType;
                    if (genProp.IsGenericParameter)
                    {
                        isGeneric = true;
                    }
                }

                #endregion

                sb.Append($"{commonModifiers}{type} {property.Name} {{ ");

                if (property.CanRead) //getter
                {
                    sb.Append(getterMods + "get; ");
                }
                if (property.CanWrite) //setter
                {
                    sb.Append(setterMods + "set; ");
                }
                sb.Append("}");

                if (!isGeneric)
                {
                    Type[] numberTypes = { typeof(int), typeof(long), typeof(short), typeof(sbyte) };
                    if (numberTypes.Contains(property.PropertyType))
                    {
                        if (property.GetValue(obj).ToString() != "0")
                            sb.Append($" = {property.GetValue(obj)};");
                    }
                    else if (property.PropertyType == typeof(string))
                    {
                        sb.Append($" = \"{property.GetValue(obj)}\"");
                    }
                }

                properties.Add(property.Name); //save for later use
                sb.AppendLine();
            }
            sb.AppendLine("#endregion");

            #endregion

            sb.AppendLine();

            #region fields

            sb.AppendLine("#region fields");
            foreach (var field in T.GetFields(AllBindingFlags))
            {
                var name = field.Name;
                if (name.IndexOf('<') == 0) //backing field for auto-property => ignore
                {
                    continue;
                }
                
                var attrs = new Attributes(field.GetCustomAttributes(), clMgr, 4);
                sb.Append(attrs);

                sb.Append("    ");
                var mods = new AccessModifiers(field);
                sb.Append($"{mods}{clMgr.WriteReturnType(field.FieldType,T,q=>q.GetField(field.Name).FieldType)} {field.Name}");

                if (field.FieldType.IsPrimitive) //the value can be safey assigned
                {
                    var val = mods.Contains(EKeyWords.Static)
                        ? field.GetValue(null).ToString()
                        : field.GetValue(obj).ToString();

                    sb.Append($" = {val}");
                }
                sb.AppendLine(";");
            }
            sb.AppendLine("#endregion");

            #endregion

            sb.AppendLine();

            #region constructors

            sb.AppendLine("#region constructors");
            foreach (var constructor in T.GetConstructors(AllBindingFlags))
            {
                var mods = new AccessModifiers(constructor);
                sb.AppendLine(
                    $"    {mods}{ClassManager.TrimTypeName(T.Name)}({ProcessParameters(constructor.GetParameters(), clMgr)}){{}}");
            }
            sb.AppendLine("#endregion");

            #endregion

            sb.AppendLine();

            #region methods

            sb.AppendLine("#region methods");
            foreach (var method in T.GetMethods(AllBindingFlags))
            {
                var mods = new AccessModifiers(method);
                var name = method.Name; //name to be used. is edited for the advanced stuff

                if (method.IsSpecialName) //auto-generated method
                {
                    if (properties.Any(p => method.Name == $"get_{p}")) //auto-getters                
                        continue;
                    if (properties.Any(p => method.Name == $"set_{p}"))
                        continue;

                    #region operators, conversions

                    if (method.Name.IndexOf("op_", StringComparison.Ordinal) == 0) //operators have prefix "op_"
                    {
                        var tempName = method.Name.Remove(0, 3);
                        switch (tempName)
                        {
                            case "Implicit": //implicit conversion
                                name = "";
                                mods.Add(EKeyWords.Implicit);
                                mods.Add(EKeyWords.Operator);
                                break;
                            case "Addition":
                                name = "operator + ";
                                break;
                            case "Subtraction":
                                name = "operator - ";
                                break;
                            case "Multiply":
                                name = "operator * ";
                                break;
                            case "Division":
                                name = "operator / ";
                                break;
                        }
                    }

                    #endregion
                }

                foreach (var @interface in interfaces) //no virtual keywords for methods implemented by interface
                    if (@interface.GetMethods().Any(q => MethodsEqual(q, method)))
                        //this method is implemented from inetrface                            
                        mods.Remove(EKeyWords.Virtual); //therefore it is not virtual

                #region async methods

                var asyncAttribType = typeof(AsyncStateMachineAttribute); //async methods have these
                var asyncAttrib = method.GetCustomAttribute(asyncAttribType) as AsyncStateMachineAttribute;
                if (asyncAttrib != null)
                    mods.Add(EKeyWords.Async); //it is async

                if ((method.Name.IndexOf('<') == 0) && (method.ReturnType == typeof(int)))
                    //async helper method, auto generated                    
                    if (T.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                        .Any(m => method.Name.IndexOf(m.Name, StringComparison.Ordinal) == 1))
                        //looks like this: private int <asyncmethod>__x_()
                        continue; //skip it

                #endregion

                sb.Append("    ");
                sb.Append(mods);
                sb.Append($"{clMgr.WriteReturnType(method.ReturnType,T,q=>q.GetMethod(method.Name).ReturnType)} {name}");

                #region generics

                if (method.IsGenericMethod)
                {
                    var genMethArgs =
                        method.GetGenericMethodDefinition().GetGenericArguments().Select(q => q.Name).ToList();
                    sb.Append($"<{string.Join(", ", genMethArgs)}>"); //generic arguments' names separated by commas
                }

                #endregion

                sb.AppendLine($"({ProcessParameters(method.GetParameters(), clMgr)}){{}}");
            }
            sb.AppendLine("#endregion");

            #endregion

            sb.AppendLine();
            sb.AppendLine("  }");
            sb.AppendLine("}");

            sb.Insert(0, clMgr.Usings + Environment.NewLine); //prepend using directives

            Info = sb.ToString();
        }

        /// <summary>
        ///     Estimated representation of object given to constructor
        /// </summary>
        public string Info { get; }

        /// <summary>
        ///     Process parameters into string without parenthess
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="mgr">for managing correct class names</param>
        /// <returns></returns>
        private static string ProcessParameters(IEnumerable<ParameterInfo> parameters, ClassManager mgr)
        {
            var sb = new StringBuilder();

            var parameterInfos = parameters as ParameterInfo[] ?? parameters.ToArray();
            for (var i = 0; i < parameterInfos.Length; i++)
            {
                var param = parameterInfos.ElementAt(i);

                if (param.IsOut)
                {
                    sb.Append("out ");
                }
                else if (param.ParameterType.IsByRef)
                {
                    sb.Append("ref ");
                }

                var typeName = param.ParameterType.FullName == null
                    ? param.ParameterType.Name
                    : mgr.Write(param.ParameterType); //generic parameter name or regular type

                sb.Append($"{typeName} {param.Name}");
                if (param.IsOptional)
                {
                    var val = "null";
                    if (param.ParameterType == typeof(int))
                    {
                        val = param.RawDefaultValue.ToString();
                    }
                    else if (param.ParameterType == typeof(string))
                    {
                        val = "\"" + param.RawDefaultValue + "\"";
                    }
                    sb.Append($" = {val}");
                }

                if (i < parameterInfos.Length - 1)
                    sb.Append(", ");
            }
            return sb.ToString();
        }

        /// <summary>
        ///     check if methods are equal. No default equals is not good enough
        /// </summary>
        /// <param name="m1"></param>
        /// <param name="m2"></param>
        /// <returns></returns>
        private static bool MethodsEqual(MethodBase m1, MethodBase m2)
        {
            if (m1.Name != m2.Name)
                return false;

            var a1 = m1.GetParameters();
            var a2 = m2.GetParameters();
            if (a1.Length != a2.Length)
                return false;

            var comparer = EqualityComparer<ParameterInfo>.Default;
            return !a1.Where((t, i) => !comparer.Equals(t, a2[i])).Any();
        }

        /// <summary>
        ///     filter method
        /// </summary>
        /// <param name="typeObj"></param>
        /// <param name="criteriaObj"></param>
        /// <returns></returns>
        private static bool InterfaceFilter(Type typeObj, object criteriaObj)
        {
            var baseClassType = (Type)criteriaObj;
            // Obtain an array of the interfaces supported by the base class
            var interfacesArray = baseClassType.GetInterfaces();
            return interfacesArray.All(t => typeObj.ToString() != t.ToString());
        }

        #region helper classes

        private class AccessModifiers : List<EKeyWords>
        {
            public AccessModifiers(IEnumerable<MethodInfo> infos)
            {
                foreach (var info in infos)
                {
                    if (info.IsPublic)
                        Add(EKeyWords.Public);
                    if (info.IsPrivate)
                        Add(EKeyWords.Private);
                    if (info.IsAssembly)
                        Add(EKeyWords.Internal);
                    if (info.IsFamily)
                        Add(EKeyWords.Protected);

                    if (info.IsStatic)
                        Add(EKeyWords.Static);

                    if (info.IsAbstract)
                        Add(EKeyWords.Abstract);

                    if (!info.GetBaseDefinition().Equals(info))
                        Add(EKeyWords.Override);
                    else if (info.IsVirtual) //it is not both override and virtual
                        Add(EKeyWords.Virtual);
                }
            }

            public AccessModifiers(MethodInfo info) : this(new[] { info })
            {
            }

            public AccessModifiers(FieldInfo field)
            {
                if (field.IsPublic)
                    Add(EKeyWords.Public);
                if (field.IsPrivate)
                    Add(EKeyWords.Private);
                if (field.IsFamily)
                    Add(EKeyWords.Protected);
                if (field.IsAssembly)
                    Add(EKeyWords.Internal);

                if (field.IsStatic)
                    Add(EKeyWords.Static);
                if (field.IsInitOnly)
                    Add(EKeyWords.Readonly);
            }

            public AccessModifiers(ConstructorInfo info)
            {
                if (info.IsPublic)
                    Add(EKeyWords.Public);
                if (info.IsPrivate)
                    Add(EKeyWords.Private);
                if (info.IsAssembly)
                    Add(EKeyWords.Internal);
                if (info.IsFamily)
                    Add(EKeyWords.Protected);

                if (info.IsStatic)
                {
                    Add(EKeyWords.Static);
                    Remove(EKeyWords.Public);
                    Remove(EKeyWords.Private);
                    Remove(EKeyWords.Protected); //static constructors don't have these
                }

                if (info.IsAbstract)
                    Add(EKeyWords.Abstract);

                if (info.IsVirtual)
                    Add(EKeyWords.Virtual);
            }

            public AccessModifiers(Type info)
            {
                if (info.IsPublic)
                    Add(EKeyWords.Public);
                if (info.IsAbstract)
                    Add(EKeyWords.Abstract);
                if (info.IsAbstract && info.IsSealed)
                    Add(EKeyWords.Static);
                if (info.IsSealed)
                    Add(EKeyWords.Sealed);
            }

            public AccessModifiers()
            {
            }

            public bool IsPublic => Contains(EKeyWords.Public);
            public bool IsPrivate => Contains(EKeyWords.Private);


            public override string ToString()
            {
                if (!this.Any())
                    return "";

                return string.Join(" ", this.Select(q => q.Str())) + " ";
            }
        }

        private class Attributes : List<object>
        {
            private readonly ClassManager _classManager;
            private readonly string _indent = "";

            public Attributes(IEnumerable<Attribute> attributes, ClassManager classManager, int indentSpaces)
            {
                _classManager = classManager;
                for (var i = 0; i < indentSpaces; i++)
                {
                    _indent += " ";
                }
                foreach (var attribute in attributes)
                {
                    Add(attribute);
                }
            }

            public override string ToString()
            {
                if (!this.Any())
                    return "";
                return this.Aggregate("",
                    (prev, next) => $"{prev}{_indent}[{_classManager.Write(next.GetType())}]{Environment.NewLine}");
            }
        }

        #endregion
    }
}