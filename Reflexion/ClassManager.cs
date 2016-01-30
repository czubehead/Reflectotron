using System;
using System.Collections.Generic;
using System.Linq;

namespace Reflex
{
    /// <summary>
    /// Manages class names and their namespaces
    /// </summary>
    internal class ClassManager
    {
        /// <summary>
        /// Alias,Namespace
        /// </summary>
        private readonly Dictionary<string, string> _classes;

        /// <summary>
        /// Usinf directives
        /// </summary>
        public string Usings
        {
            get
            {
                List<string> namespaces = new List<string>();
                foreach (var pair in _classes.Where(pair => !namespaces.Contains(pair.Value)))
                {
                    namespaces.Add(pair.Value);//merge duplicate namespaces
                }

                return namespaces.Aggregate("", (prev, next) => $"{prev}using {next};{Environment.NewLine}");
            }
        }

        private readonly string _namespace;

        /// <param name="namespaceScope">Namespace of class reflected</param>
        public ClassManager(string namespaceScope)
        {
            _classes = new Dictionary<string, string>();
            _namespace = namespaceScope;
        }

        /// <summary>
        /// Returns a simplified class name from Namespace.Class with respect to usings
        /// </summary>
        /// <param name="usetypes">whether to use class names for generics instead of parameter names</param>
        /// <param name="genericClassArguments">list of generic arguments that won't be affected by <see cref="usetypes"/></param>
        /// <returns></returns>
        public string Write(Type type,bool usetypes=false,IEnumerable<string> genericClassArguments=null)
        {
            string fullname = type.FullName;
            if (fullname == null)
            {
                return TrimTypeName(type.Name);
            }

            string nameSpace = type.Namespace;
            string shortName = TrimTypeName(type.Name);

            if ((nameSpace != _namespace) && _classes.ContainsKey(shortName) && (_classes[shortName] != nameSpace))//class name conflict, use full name
                return $"{nameSpace}.{shortName}{GetGenericPart(type,usetypes,genericClassArguments)}";

            if (!_classes.ContainsKey(shortName))//unknown class with no conflicts            
                _classes.Add(shortName, nameSpace);//remember it

            return $"{shortName}{GetGenericPart(type,usetypes,genericClassArguments)}";
        }

        /// <summary>
        /// For generic types returns "&lt;T1, T2...&gt;"
        /// </summary>
        /// <param name="type"></param>
        /// <param name="usetype">if true, T1 will be replaced with actual class name</param>
        /// <param name="exclude"></param>
        /// <returns></returns>
        private string GetGenericPart(Type type, bool usetype=false,IEnumerable<string> exclude=null)
        {
            if (!type.IsGenericType) return "";

            exclude = exclude?.ToList() ?? new List<string>();

            List<string> genericArguments=new List<string>();
            if(usetype)
            {
                List<int> repeatIndexes=new List<int>();
                for (int i = 0; i < type.GetGenericArguments().Length; i++)
                {
                    var genericArgument = type.GetGenericArguments()[i];

                    if (!exclude.Contains(Write(genericArgument)))
                        genericArguments.Add(Write(genericArgument));
                    else //T1 should be used insted of type
                        repeatIndexes.Add(i);
                }
                for (int index = 0; index < type.GetGenericTypeDefinition().GetGenericArguments().Length; index++)
                    //repeat for indexes which are T1 and not classes
                {
                    if (!repeatIndexes.Contains(index)) continue;

                    var genericArgument = type.GetGenericTypeDefinition().GetGenericArguments()[index];
                    genericArguments.Add(genericArgument.Name);
                }
            }
            else
            {
                genericArguments= type.GetGenericTypeDefinition().GetGenericArguments().Select(q => Write(q,true)).ToList();
            }

            return $"<{string.Join(", ", genericArguments)}>";
        }

        /// <summary>
        /// convert Int32 to int etc
        /// </summary>
        /// <param name="shortname"></param>
        /// <returns></returns>
        public static string TrimTypeName(string shortname)
        {
            if (shortname.EndsWith("&"))//ref types have & at the end it is not needed
            {
                shortname = shortname.Remove(shortname.Length - 1);
            }
            if (shortname.Contains("`"))//generics
            {
                shortname = shortname.Remove(shortname.LastIndexOf("`", StringComparison.Ordinal));
            }

            switch (shortname)
            {
                case "Void":
                    return "void";

                case "Object":
                    return "object";

                case "Boolean":
                    return "bool";

                case "Int32":
                    return "int";

                case "String":
                    return "string";

                default:
                    return shortname;
            }
        }
    }
}
