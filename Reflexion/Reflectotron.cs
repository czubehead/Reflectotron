﻿//welocme to LINQ hell. If you aren't master in LINQ you should probably leave!

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Reflex
{
    /// <summary>
    ///     Takes an instance of a class and tries its best to estimate the class structure. Designed by czubehead
    /// </summary>
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
            Async,
            Const
        }

        /// <summary>
        ///     <see cref="BindingFlags" /> that are used to obtain all useful stuff
        /// </summary>
        private const BindingFlags AllBindingFlags =
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public |
            BindingFlags.Static | BindingFlags.DeclaredOnly;

        /// <summary>
        ///     Instatializes a new <see cref="Reflectotron" /> class from an instance
        /// </summary>
        /// <param name="obj">
        ///     Object to be reflected, its estimated reprasentation will be saved in <see cref="ReflectedInfo" />
        /// </param>
        public Reflectotron(object obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            Reflect(obj.GetType(), obj, 0);
        }

        /// <summary>
        ///     Instatializes a new <see cref="Reflectotron" /> class from a static class type
        /// </summary>
        /// <param name="T">Type of the static class. Use typeof() operator</param>
        public Reflectotron(Type T)
        {
            Reflect(T, null, 0);
        }

        /// <summary>
        ///     Indentation at the beginning of most lines. Intended for future use.
        /// </summary>
        public string Indent { get; private set; }

        /// <summary>
        ///     Estimated representation of object given to constructor
        /// </summary>
        public string ReflectedInfo => $"{Usings}{Environment.NewLine}{ReflectNoUsings}";

        /// <summary>
        ///     Using directives for reflected type
        /// </summary>
        public string Usings { get; private set; }

        /// <summary>
        ///     Estimated class structure without using directives
        /// </summary>
        public string ReflectNoUsings { get; private set; }

        /// <summary>
        ///     Processes a given type with respect to object into <see cref="ReflectedInfo" />
        /// </summary>
        /// <param name="T">Type to reflect</param>
        /// <param name="obj">Object whose properies' values should be used</param>
        /// <param name="indent">Indentation. 0 is default</param>
        private void Reflect(Type T, object obj, int indent)
        {
            var sb = new StringBuilder();

            var clMgr = new ClassManager(T.Namespace); //manages class names and namespaces
            var ignoredMembers = new List<string>(); //list of members to be ignored (obviously)
            Indent = "";
            for (var i = 0; i < indent; i++)
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

            if ((T.BaseType != null) && (T.BaseType != typeof (object)))
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

            var enums = T.GetNestedTypes().Where(q => q.IsEnum).ToList();
            if (enums.Any())
            {
                foreach (var @enum in enums)
                {
                    var mod = @enum.IsPublic ? "public" : "private";
                    sb.AppendLine($"{Indent}    {mod} enum {ClassManager.TrimTypeName(@enum.Name)}");
                    sb.AppendLine($"{Indent}    {{");
                    foreach (var enumName in @enum.GetEnumNames())
                    {
                        var info = @enum.GetMember(enumName)[0];
                        var attrib = new Attributes(info.GetCustomAttributes(), clMgr, 6);
                        if (attrib.ToString() != "")
                            sb.Append(attrib);
                        sb.AppendLine($"{Indent}      {enumName},");
                    }
                    sb.AppendLine($"{Indent}    }}");
                }
                sb.AppendLine();
            }

            #endregion

            #region properties, indexers

            var properties = T.GetProperties(AllBindingFlags).ToList();

            if (properties.Any())
            {
                sb.AppendLine("#region properties");

                foreach (var property in properties)
                {
                    var attrs = new Attributes(property.GetCustomAttributes(), clMgr, 4 + Indent.Length);
                    var name = property.Name; //to be edited for indexer

                    sb.Append(attrs);
                    sb.Append($"{Indent}    ");

                    #region access modifiers for getters and setters

                    var getterInfos =
                        property.GetAccessors(true).Where(a => a.ReturnType != typeof (void)).ToArray();
                    //setters don't have return types
                    var setterInfos =
                        property.GetAccessors(true).Where(a => a.ReturnType == typeof (void)).ToArray(); //getters do

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

                    var type = clMgr.WriteReturnType(property.PropertyType, T,
                        q => q.GetProperty(property.Name, AllBindingFlags).PropertyType);
                    var isSpecial = false;

                    #region generic property type

                    if (T.IsGenericType) //may be a property with generic type
                    {
                        var genProp = T.GetGenericTypeDefinition().GetProperty(property.Name).PropertyType;
                        if (genProp.IsGenericParameter)
                        {
                            isSpecial = true;
                        }
                    }

                    #endregion

                    #region indexer

                    if (property.GetIndexParameters().Any()) //this is in fact an indexer, not a property
                    {
                        isSpecial = true;

                        var setter = property.GetSetMethod(true);
                        var getter = property.GetGetMethod(true);
                        var parameters = ""; //there will always be at least 1 accessor and parameter

                        if (setter != null) //has setter
                        {
                            var par = setter.GetParameters().ToList();
                            if (par.Any()) //this should be always true but better safe than sorry
                                par.RemoveAt(par.Count - 1); //the last is value parameter

                            parameters = ProcessParameters(par, clMgr);
                        }
                        if (getter != null)
                        {
                            parameters = ProcessParameters(getter.GetParameters(), clMgr);
                        }
                        name = $"this[{parameters}]";
                    }

                    #endregion

                    sb.Append($"{commonModifiers}{type} {name} {{ ");

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
                        var value = GetMemberValue(property, obj);
                        if (value != "")
                        {
                            sb.Append($" = {value};");
                        }
                    }

                    sb.AppendLine();
                }
                sb.AppendLine("#endregion");
                sb.AppendLine();
            }

            #endregion

            #region events

            if (T.GetEvents(AllBindingFlags).Any())
            {
                sb.AppendLine("#region events");
                foreach (var @event in T.GetEvents())
                {
                    sb.Append($"{Indent}    ");
                    sb.Append(
                        $"public event {clMgr.WriteReturnType(@event.EventHandlerType, T, q => q.GetEvent(@event.Name).EventHandlerType)} {@event.Name};");
                    //no way to get a proper access modifier
                    sb.AppendLine();
                    ignoredMembers.Add($"add_{@event.Name}");
                    ignoredMembers.Add($"remove_{@event.Name}");
                    ignoredMembers.Add(@event.Name);
                }
                sb.AppendLine("#endregion");
                sb.AppendLine();
            }

            #endregion

            #region fields

            var fields = T.GetFields(AllBindingFlags).Where(q => !ignoredMembers.Contains(q.Name))
                .Where(q => q.Name.IndexOf('<') != 0).ToList();
            if (fields.Any())
            {
                sb.AppendLine("#region fields");
                foreach (var field in fields)
                {
                    var attrs = new Attributes(field.GetCustomAttributes(), clMgr, 4);
                    sb.Append(attrs);

                    sb.Append($"{Indent}    ");
                    var mods = new AccessModifiers(field);
                    sb.Append(
                        // ReSharper disable once PossibleNullReferenceException
                        $"{mods}{clMgr.WriteReturnType(field.FieldType, T, q => q.GetField(field.Name, AllBindingFlags).FieldType)} {field.Name}");

                    #region value

                    var value = GetMemberValue(field, obj);
                    if (value != "")
                    {
                        sb.Append($" = {value}");
                    }

                    #endregion

                    sb.AppendLine(";");
                }
                sb.AppendLine("#endregion");
                sb.AppendLine();
            }

            #endregion

            #region constructors

            var constructors = T.GetConstructors(AllBindingFlags);
            if (constructors.Any())
            {
                sb.AppendLine("#region constructors");
                foreach (var constructor in constructors)
                {
                    var mods = new AccessModifiers(constructor);
                    sb.AppendLine(
                        $"{Indent}    {mods}{ClassManager.TrimTypeName(T.Name)}({ProcessParameters(constructor.GetParameters(), clMgr)});");
                }
                sb.AppendLine("#endregion");
                sb.AppendLine();
            }

            #endregion

            #region methods

            var methods = T.GetMethods(AllBindingFlags)
                .Where(q => !ignoredMembers.Contains(q.Name)).ToList();
            if (methods.Any())
            {
                sb.AppendLine("#region methods");
                foreach (var method in methods)
                {
                    var mods = new AccessModifiers(method);
                    var name = method.Name; //name to be used. is edited for the advanced stuff
                    var parametersPrefix = ""; //changes for extension methods to "this "

                    var attrsRaw = method.GetCustomAttributes().ToList();
                    if (attrsRaw.Any(q => q is ExtensionAttribute))
                    {
                        attrsRaw.RemoveAll(q => q is ExtensionAttribute);
                        parametersPrefix = method.GetParameters().Any() ? "this " : "";
                    }

                    var attrs = new Attributes(attrsRaw, clMgr, indent + 4);

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

                    var asyncAttribType = typeof (AsyncStateMachineAttribute); //async methods have these
                    var asyncAttrib = method.GetCustomAttribute(asyncAttribType) as AsyncStateMachineAttribute;
                    if (asyncAttrib != null)
                        mods.Add(EKeyWords.Async); //it is async

                    if ((method.Name.IndexOf('<') == 0) && (method.ReturnType == typeof (int)))
                        //async helper method, auto generated                    
                        if (T.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                            .Any(m => method.Name.IndexOf(m.Name, StringComparison.Ordinal) == 1))
                            //looks like this: private int <asyncmethod>__x_()
                            continue; //skip it

                    #endregion

                    sb.Append($"{attrs}{Indent}    ");
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

                    sb.AppendLine($"({parametersPrefix}{ProcessParameters(method.GetParameters(), clMgr)});");
                }
                sb.AppendLine("#endregion");
                sb.AppendLine();
            }

            #endregion

            sb.AppendLine($"{Indent}  }}"); //class
            sb.AppendLine($"{Indent}}}"); //namespace

            ReflectNoUsings = sb.ToString();
            Usings = clMgr.Usings;
        }

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

                var attrs = new Attributes(param.GetCustomAttributes(), mgr, 0);
                //yep, even parameters may have attributes
                if (attrs.Any())
                {
                    var a = attrs.ToString();
                    a = a.Remove(a.Length - Environment.NewLine.Length);
                    sb.Append(a);
                }

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
                    if (param.ParameterType == typeof (int))
                    {
                        val = param.RawDefaultValue.ToString();
                    }
                    else if (param.ParameterType == typeof (string))
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
            var baseClassType = (Type) criteriaObj;
            // Obtain an array of the interfaces supported by the base class
            var interfacesArray = baseClassType.GetInterfaces();
            return interfacesArray.All(t => typeObj.ToString() != t.ToString());
        }

        /// <summary>
        ///     Get a string representation of value of something. Returns an empty string for unknown types or default values
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

                Type[] numberTypes = {typeof (int), typeof (long), typeof (short), typeof (sbyte)};
                if (numberTypes.Contains(memberType))
                {
                    if (value.ToString() != "0")
                        return value.ToString();
                }
                else if (memberType == typeof (string))
                {
                    if (value.ToString() != "")
                        return $"\"{value}\"";
                }
                else if (memberType == typeof (bool))
                {
                    return (bool) value ? "true" : "false";
                }
                return "";
            }
            catch
            {
                return "";
            }
        }

        #region helper classes

        /// <summary>
        ///     Self explaining, right?
        ///     Has useful constructors and Tostring
        /// </summary>
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

            public AccessModifiers(MethodInfo info) : this(new[] {info})
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
                else if (field.IsLiteral)
                {
                    Add(EKeyWords.Const);
                    Remove(EKeyWords.Static);
                }
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
                if (info.IsSealed)
                    Add(EKeyWords.Sealed);
                if (info.IsAbstract && info.IsSealed)
                {
                    Add(EKeyWords.Static);
                    Remove(EKeyWords.Abstract);
                    Remove(EKeyWords.Sealed);
                }
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

        /// <summary>
        ///     Represents attribues, surprisingly. Useful constructor
        /// </summary>
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

            /// <summary>
            ///     name of properties whose names partialy match parameter's name
            /// </summary>
            /// <param name="cons">Construcor whose parameters should be matched</param>
            /// <param name="T">type that has the properties desired</param>
            /// <returns></returns>
            private static IEnumerable<string> PropsMatchingParams(ConstructorInfo cons, Type T)
            {
                return cons.GetParameters()
                    .Where(par => T.GetProperties()
                        .Any(property =>
                            string.Equals(property.Name, par.Name, StringComparison.CurrentCultureIgnoreCase) &&
                            //case-insensitive name match
                            (property.PropertyType == par.ParameterType))) //type match
                    .Select(param => T.GetProperties().First(property =>
                        string.Equals(property.Name, param.Name, StringComparison.CurrentCultureIgnoreCase)).Name);
                    //select property's name
            }

            public override string ToString()
            {
                if (!this.Any())
                    return "";

                var lines = new List<string>();
                foreach (var attribute in this)
                {
                    var T = attribute.GetType();

                    var line = new StringBuilder($"{_indent}[{_classManager.Write(T, true)}");

                    if (T.HasParamlessConstructor()) //parameterless constructor is needed to
                    {
                        try
                        {
                            var props = new Dictionary<string, string>();
                                //property,value; which don't have default values
                            var def = Activator.CreateInstance(T);

                            if (def != null)
                            {
                                foreach (var property in T.GetProperties())
                                {
                                    if (property.GetValue(attribute).Equals(property.GetValue(def)))
                                        //the property's value is not default
                                        continue;

                                    var value = GetMemberValue(property, attribute);

                                    if (string.IsNullOrEmpty(value))
                                    {
                                        props.Add(property.Name, value);
                                    }
                                }
                            }
                            if (props.Any()) //some properties have not default values                        
                            {
                                line.Append("(");
                                line.Append(string.Join(", ", props.Select(q => $"{q.Key} = {q.Value}")));
                                line.Append(")");
                            }
                        }
                        catch
                        {
                            //ignore
                        }
                    }
                    else //has parametrized constructors only
                    {
                        var matchingProps = new Dictionary<ConstructorInfo, int>();
                            //constr, number of matching properties
                        //constructors whose parameters match some properties
                        foreach (var constructor in T.GetConstructors())
                        {
                            var match = //number of parametrs whose names matches attribute's properties
                                PropsMatchingParams(constructor, T).Count();
                            if (match > 0)
                                matchingProps.Add(constructor, match);
                        }
                        if (matchingProps.Any())
                        {
                            var bestConstr = matchingProps.OrderByDescending(q => q.Value).First().Key;
                            var paramPropNames = PropsMatchingParams(bestConstr, T).ToList();
                            //list of properties' names to be assigned as constructor params
                            var values =
                                paramPropNames.Select(param => GetMemberValue(T.GetProperty(param), attribute)).ToList();
                            //list of values of parameters
                            line.Append($"({string.Join(", ", values)}");

                            var otherProperties =
                                T.GetProperties().Where(q => !paramPropNames.Contains(q.Name)).ToList();
                            //properties which haven't been assigned in constructor

                            foreach (var property in otherProperties)
                            {
                                var value = GetMemberValue(property, attribute);
                                if (!string.IsNullOrEmpty(value)) //not default value
                                {
                                    line.Append($", {property.Name}={value}");
                                }
                            }

                            line.Append(")");
                        }
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