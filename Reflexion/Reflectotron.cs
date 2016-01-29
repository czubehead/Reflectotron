using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Reflex
{
    public class Reflectotron
    {
        public enum EAccMods
        {
            Public,
            Private,
            Virtual,
            Protected,
            Internal,
            Abstract,
            Static,
            Readonly,
            Override
        }

        private const BindingFlags AllBindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public |
                                             BindingFlags.Static|BindingFlags.DeclaredOnly;

        public string Info { get; }

        public Reflectotron(object obj)
        {
            StringBuilder sb = new StringBuilder();
            Type T = obj.GetType();
            var clMgr = new ClassManager(T.Namespace);//manages class names and namespaces
            List<string> properties = new List<string>();

            sb.AppendLine($"namespace {T.Namespace}");
            sb.AppendLine("{");

            Attributes attributes = new Attributes(T.GetCustomAttributes(), clMgr, 2);
            sb.Append(attributes);

            sb.Append($"  class {T.Name}");
            #region inheritance

            bool inherits = false;
            if (T.BaseType != null)
            {
                inherits = true;
                sb.Append($" : {clMgr.Write(T.BaseType)}");
            }
            TypeFilter interfaceFilter = InterfaceFilter;
            Type[] interfaces = T.FindInterfaces(interfaceFilter, T.BaseType);

            if (interfaces.Any())
            {
                sb.Append(!inherits ? " : " : ", ");

                for (int i = 0; i < interfaces.Length; i++)
                {
                    sb.Append(clMgr.Write(interfaces[i]));
                    if (i < interfaces.Length - 1)
                        sb.Append(", ");
                }
            }

            #endregion

            sb.AppendLine();
            sb.AppendLine("  {");

            #region properties

            foreach (var property in T.GetProperties(AllBindingFlags))
            {
                Attributes attrs = new Attributes(property.GetCustomAttributes(), clMgr, 4);

                sb.Append(attrs);
                sb.Append("    ");

                MethodInfo[] getterInfos = property.GetAccessors(true).Where(a => a.ReturnType != typeof(void)).ToArray();//setters don't have return types
                MethodInfo[] setterInfos = property.GetAccessors(true).Where(a => a.ReturnType == typeof(void)).ToArray();//getters do

                AccessModifiers setterMods = new AccessModifiers(setterInfos);
                AccessModifiers getterMods = new AccessModifiers(getterInfos);

                //those which are the same for both getter and setter
                AccessModifiers commonModifiers = new AccessModifiers();
                commonModifiers.AddRange(getterMods.Where(accessModifier => setterMods.Contains(accessModifier)));
                getterMods.RemoveAll(q => commonModifiers.Contains(q));
                setterMods.RemoveAll(q => commonModifiers.Contains(q));//they are redundant there

                if (getterMods.IsPublic && setterMods.IsPrivate)
                {
                    commonModifiers.Add(EAccMods.Public);
                    getterMods.Remove(EAccMods.Public);
                }
                else if (setterMods.IsPublic && getterMods.IsPrivate)//who uses this anyway?
                {
                    commonModifiers.Add(EAccMods.Public);
                    setterMods.Remove(EAccMods.Public);
                }

                sb.Append(commonModifiers + clMgr.Write(property.PropertyType) + " " + property.Name + " { ");
                if (property.CanRead)
                {
                    sb.Append(getterMods + "get; ");
                }
                if (property.CanWrite)
                {
                    sb.Append(setterMods + "set; ");
                }
                sb.Append("}");

                if (property.PropertyType.IsPrimitive)//properties values
                {
                    try
                    {
                        sb.Append(" = " + T.GetProperty(property.Name).GetValue(obj, null) + ";");
                    }
                    // ReSharper disable once EmptyGeneralCatchClause
                    catch (Exception)
                    { }
                }

                properties.Add(property.Name);
                sb.AppendLine();
            }

            #endregion

            sb.AppendLine();

            #region fields

            foreach (var field in T.GetFields(AllBindingFlags))
            {
                string name = field.Name;
                if (name.IndexOf('<') == 0)//backing field for auto-property => ignore
                {
                    continue;
                }

                Attributes attrs = new Attributes(field.GetCustomAttributes(), clMgr, 4);
                sb.Append(attrs);

                sb.Append("    ");
                AccessModifiers mods = new AccessModifiers(field);
                sb.Append($"{mods} {clMgr.Write(field.FieldType)} {field.Name}");
                if (field.FieldType.IsPrimitive)
                {
                    var val = mods.Contains(EAccMods.Static) ?
                        field.GetValue(null).ToString() : field.GetValue(obj).ToString();

                    sb.Append($" = {val}");
                }
                sb.AppendLine(";");
            }
            #endregion

            sb.AppendLine();
            sb.AppendLine();

            #region methods

            foreach (var method in T.GetMethods(AllBindingFlags))
            {
                if ((T.BaseType != null) && T.BaseType.GetMethods(AllBindingFlags).Contains(method))//no inherited methods
                    continue;

                if (properties.Any(p => method.Name == $"get_{p}")) //auto-getters                
                    continue;
                if (properties.Any(p => method.Name == $"set_{p}"))
                    continue;

                AccessModifiers mods = new AccessModifiers(method);
                if (interfaces.Any())//no virtual keywords for methods implemented by interface
                {
                    foreach (var @interface in interfaces)
                    {
                        if (@interface.GetMethods().Any(q => MethodsEqual(q, method)))//this method is implemented from inetrface
                        {
                            mods.Remove(EAccMods.Virtual);
                        }
                    }
                }

                sb.Append("    ");
                sb.Append(mods);
                sb.AppendLine($"{clMgr.Write(method.ReturnType)} {method.Name}(){{}}");
            }

            #endregion

            sb.AppendLine();
            sb.AppendLine("  }");
            sb.AppendLine("}");

            sb.Insert(0, clMgr.Usings + Environment.NewLine);//prepend using directives

            Info = sb.ToString();
        }

        private static bool MethodsEqual(MethodInfo m1, MethodInfo m2)
        {
            if (m1.Name != m2.Name)
                return false;

            ParameterInfo[] a1 = m1.GetParameters();
            ParameterInfo[] a2 = m2.GetParameters();
            if (a1.Length != a2.Length)
                return false;

            EqualityComparer<ParameterInfo> comparer = EqualityComparer<ParameterInfo>.Default;
            return !a1.Where((t, i) => !comparer.Equals(t, a2[i])).Any();
        }

        private static bool InterfaceFilter(Type typeObj, object criteriaObj)
        {
            Type baseClassType = (Type)criteriaObj;
            // Obtain an array of the interfaces supported by the base class A.
            Type[] interfacesArray = baseClassType.GetInterfaces();
            return interfacesArray.All(t => typeObj.ToString() != t.ToString());
        }

        #region helper classes
        private class AccessModifiers : List<EAccMods>
        {
            public AccessModifiers(IEnumerable<MethodInfo> infos)
            {
                foreach (var info in infos)
                {
                    if (info.IsPublic)
                        Add(EAccMods.Public);
                    if (info.IsPrivate)
                        Add(EAccMods.Private);
                    if (info.IsAssembly)
                        Add(EAccMods.Internal);
                    if (info.IsFamily)
                        Add(EAccMods.Protected);

                    if (info.IsStatic)
                        Add(EAccMods.Static);

                    if (info.IsAbstract)
                        Add(EAccMods.Abstract);

                    if (!info.GetBaseDefinition().Equals(info))
                        Add(EAccMods.Override);
                    else if (info.IsVirtual)//it is not both override and virtual
                        Add(EAccMods.Virtual);
                }
            }
            public AccessModifiers(MethodInfo info) : this(new[] { info })
            { }
            public AccessModifiers(FieldInfo field)
            {
                if (field.IsPublic)
                    Add(EAccMods.Public);
                if (field.IsPrivate)
                    Add(EAccMods.Private);
                if (field.IsFamily)
                    Add(EAccMods.Protected);
                if (field.IsAssembly)
                    Add(EAccMods.Internal);

                if (field.IsStatic)
                    Add(EAccMods.Static);
                if (field.IsInitOnly)
                    Add(EAccMods.Readonly);
            }
            public AccessModifiers() { }

            public override string ToString()
            {
                if (!this.Any())
                    return "";

                string res = this.Aggregate("", (current, str) => current + (str.Str() + " "));
                return res;
            }

            public bool IsPublic => Contains(EAccMods.Public);
            public bool IsPrivate => Contains(EAccMods.Private);
        }
        private class Attributes : List<object>
        {
            private readonly ClassManager _classManager;
            private readonly string _indent = "";
            public Attributes(IEnumerable<Attribute> attributes, ClassManager classManager, int indentSpaces)
            {
                _classManager = classManager;
                for (int i = 0; i < indentSpaces; i++)
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
                return this.Aggregate("", (prev, next) => $"{prev}{_indent}[{_classManager.Write(next.GetType())}]{Environment.NewLine}");
            }
        }
        #endregion
    }
}
