//welocme to LINQ hell. If you aren't master in LINQ you should probably leave!

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Reflex
{
    public class Reflectotron
    {
        public string Indent { get; }

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
        public Reflectotron(object obj) : this(obj, 0)
        {

        }

        private Reflectotron(object obj, int indent)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            var sb = new StringBuilder();
            var T = obj.GetType();

            var clMgr = new ClassManager(T.Namespace); //manages class names and namespaces
            var properties = new List<string>(); //properties, used to filter out auto getters and setters from methods
            var ignoredMembers = new List<string>();//list of members to be ignored (obviously)
            Indent = "";
            for (int i = 0; i < indent; i++)
            {
                Indent += " ";
            }

            sb.AppendLine($"namespace {T.Namespace}");
            sb.AppendLine("{");

            #region class attributes

            var classAttributes = new Attributes(T.GetCustomAttributes(), clMgr, 2);
            sb.Append(classAttributes);

            #endregion

            //load all keywords that define given class
            var classMods = new AccessModifiers(T);
            sb.Append($"{Indent}  {classMods}class {clMgr.Write(T)}");

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
            sb.AppendLine($"{Indent}  {{");

            #region enums

            List<Type> enums = T.GetNestedTypes().Where(q => q.IsEnum).ToList();
            if (enums.Any())
            {
                foreach (var @enum in enums)
                {
                    string mod = @enum.IsPublic ? "public" : "private";
                    sb.AppendLine($"{Indent}    {mod} enum {ClassManager.TrimTypeName(@enum.Name)}");
                    sb.AppendLine($"{Indent}    {{");
                    foreach (var enumName in @enum.GetEnumNames())
                    {
                        MemberInfo info = @enum.GetMember(enumName)[0];
                        Attributes attrib = new Attributes(info.GetCustomAttributes(), clMgr, 6);
                        if (attrib.ToString() != "")
                            sb.Append(attrib.ToString());
                        sb.AppendLine($"{Indent}      {enumName},");
                    }
                    sb.AppendLine($"{Indent}    }}");
                }
            }


            #endregion

            sb.AppendLine();

            #region properties

            if (T.GetProperties(AllBindingFlags).Any())
            {
                sb.AppendLine("#region properties");

                foreach (var property in T.GetProperties(AllBindingFlags))
                {
                    var attrs = new Attributes(property.GetCustomAttributes(), clMgr, 4 + Indent.Length);

                    sb.Append(attrs);
                    sb.Append($"{Indent}    ");

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
                    if (property.CanRead && !property.CanWrite) //read-only property
                    {
                        commonModifiers.AddRange(getterMods);
                        getterMods.Clear();
                    }
                    else if (property.CanWrite && !property.CanRead) //set-only property (does it even exist?)
                    {
                        commonModifiers.AddRange(setterMods);
                        setterMods.Clear();
                    }

                    #endregion

                    string type = clMgr.WriteReturnType(property.PropertyType, T,
                        q => q.GetProperty(property.Name, AllBindingFlags).PropertyType);
                    bool isSpecial = false;

                    #region generic property type

                    if (T.IsGenericType) //may be a property with generic type
                    {
                        Type genProp = T.GetGenericTypeDefinition().GetProperty(property.Name).PropertyType;
                        if (genProp.IsGenericParameter)
                        {
                            isSpecial = true;
                        }
                    }

                    #endregion

                    sb.Append($"{commonModifiers}{type} {property.Name} {{ ");

                    if (property.CanRead) //getter
                    {
                        sb.Append(getterMods + "get; ");
                        ignoredMembers.Add($"get_{property.Name}");
                    }
                    if (property.CanWrite) //setter
                    {
                        sb.Append(setterMods + "set; ");
                        ignoredMembers.Add($"set_{property.Name}");
                    }
                    sb.Append("}");

                    if (!isSpecial && !property.IsSpecialName)
                    {
                        string value = GetMemberValue(property, obj);
                        if (value != "")
                        {
                            sb.Append($" = {value};");
                        }
                    }

                    properties.Add(property.Name); //save for later use
                    sb.AppendLine();
                }
                sb.AppendLine("#endregion");
            }

            #endregion

            sb.AppendLine();

            #region events

            if (T.GetEvents(AllBindingFlags).Any())
            {
                sb.AppendLine("#region events");
                foreach (var @event in T.GetEvents())
                {
                    sb.Append($"{Indent}    ");
                    sb.Append($"public event {clMgr.WriteReturnType(@event.EventHandlerType, T, q => q.GetEvent(@event.Name).EventHandlerType)} {@event.Name};");
                    //no way to get a proper access modifier
                    sb.AppendLine();
                    ignoredMembers.Add($"add_{@event.Name}");
                    ignoredMembers.Add($"remove_{@event.Name}");
                    ignoredMembers.Add(@event.Name);
                }
                sb.AppendLine("#endregion");
            }

            #endregion

            sb.AppendLine();

            #region fields

            if (T.GetFields(AllBindingFlags).Any())
            {
                sb.AppendLine("#region fields");
                foreach (var field in T.GetFields(AllBindingFlags).Where(q => !ignoredMembers.Contains(q.Name)))
                {
                    var name = field.Name;
                    if (name.IndexOf('<') == 0) //backing field for auto-property => ignore
                    {
                        continue;
                    }

                    var attrs = new Attributes(field.GetCustomAttributes(), clMgr, 4);
                    sb.Append(attrs);

                    sb.Append($"{Indent}    ");
                    var mods = new AccessModifiers(field);
                    sb.Append(
                        $"{mods}{clMgr.WriteReturnType(field.FieldType, T, q => q.GetField(field.Name, AllBindingFlags).FieldType)} {field.Name}");

                    #region value

                    string value = GetMemberValue(field, obj);
                    if (value != "")
                    {
                        sb.Append($" = {value}");
                    }

                    #endregion

                    sb.AppendLine(";");
                }
                sb.AppendLine("#endregion");
            }

            #endregion

            sb.AppendLine();

            #region constructors

            sb.AppendLine("#region constructors");
            foreach (var constructor in T.GetConstructors(AllBindingFlags))
            {
                var mods = new AccessModifiers(constructor);
                sb.AppendLine(
                    $"{Indent}    {mods}{ClassManager.TrimTypeName(T.Name)}({ProcessParameters(constructor.GetParameters(), clMgr)}){{}}");
            }
            sb.AppendLine("#endregion");

            #endregion

            sb.AppendLine();

            #region methods

            if (T.GetMethods(AllBindingFlags).Any())
            {
                sb.AppendLine("#region methods");
                foreach (var method in T.GetMethods(AllBindingFlags).Where(q => !ignoredMembers.Contains(q.Name)))
                {
                    var mods = new AccessModifiers(method);
                    var name = method.Name; //name to be used. is edited for the advanced stuff


                    if (method.IsSpecialName) //auto-generated method
                    {
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

                    sb.Append($"{Indent}    ");
                    sb.Append(mods);
                    sb.Append(
                        $"{clMgr.WriteReturnType(method.ReturnType, T, q => q.GetMethod(method.Name, AllBindingFlags).ReturnType)} {name}");

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
            }

            #endregion

            sb.AppendLine();
            sb.AppendLine($"{Indent}  }}");//class
            sb.AppendLine($"{Indent}}}");//namespace

            InfoNoUsings = sb.ToString();
            Usings = clMgr.Usings;
        }

        /// <summary>
        ///     Estimated representation of object given to constructor
        /// </summary>
        public string Info => $"{Usings}{Environment.NewLine}{InfoNoUsings}";

        /// <summary>
        /// Using directives for reflected type
        /// </summary>
        public string Usings { get; }

        /// <summary>
        /// Estimated class structure without using directives
        /// </summary>
        public string InfoNoUsings { get; }

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

        /// <summary>
        /// Get a string representation of value of something
        /// </summary>
        /// <param name="member">property, field etc.</param>
        /// <param name="reflectedType"></param>
        /// <returns></returns>
        private static string GetMemberValue(MemberInfo member, object reflectedType)
        {
            try
            {
                Type memberType;
                object value;
                var propertyInfo = member as PropertyInfo;
                var fieldInfo = member as FieldInfo;
                if (propertyInfo != null)
                {
                    memberType = propertyInfo.PropertyType;
                    value = propertyInfo.GetValue(reflectedType);
                }
                else if (fieldInfo != null)
                {
                    memberType = fieldInfo.FieldType;
                    value = fieldInfo.GetValue(reflectedType);
                }
                else
                {
                    return "";
                }

                Type[] numberTypes = { typeof(int), typeof(long), typeof(short), typeof(sbyte) };
                if (numberTypes.Contains(memberType))
                {
                    if (value.ToString() != "0")
                        return value.ToString();
                }
                else if (memberType == typeof(string))
                {
                    if (value.ToString() != "")
                        return $"\"{value}\"";
                }
                else if (memberType == typeof(bool))
                {
                    return (bool)value ? "true" : "false";
                }
                return "";
            }
            catch
            {
                return "";
            }
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

                List<string> lines = new List<string>();
                foreach (var attribute in this)
                {
                    var T = attribute.GetType();

                    Dictionary<string, string> props = new Dictionary<string, string>();//property,value
                    object def = Activator.CreateInstance(T);//default instance to match properties with

                    foreach (var property in T.GetProperties())
                    {
                        try
                        {
                            if (property.GetValue(attribute).Equals(property.GetValue(def)))//the property's value is not default
                                continue;

                            string value = GetMemberValue(property, attribute);

                            if (value != "")
                            {
                                props.Add(property.Name, value);
                            }
                        }
                        catch
                        {
                            //ignore
                        }
                    }

                    StringBuilder line = new StringBuilder($"{_indent}[{_classManager.Write(T, true)}");
                    if (props.Any())//some properties have not default values                        
                    {
                        line.Append("(");
                        line.Append(string.Join(", ", props.Select(q => $"{q.Key} = {q.Value}")));
                        line.Append(")");
                    }
                    line.Append("]");
                    lines.Add(line.ToString());
                }
                return string.Join(Environment.NewLine, lines) + Environment.NewLine;
            }
        }

        #endregion
    }
}