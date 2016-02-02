using System;
using System.Collections.Generic;
using System.Linq;

namespace Reflex
{
    /// <summary>
    ///     Manages class names and their namespaces
    /// </summary>
    internal class ClassManager
    {
        /// <summary>
        ///     Class alias,Namespace
        /// </summary>
        private readonly Dictionary<string, string> _classes;

        public delegate Type GetReturnType(Type reflectedType);

        /// <summary>
        ///     working namespace
        /// </summary>
        private readonly string _namespace;
        
        /// <param name="namespaceScope">Namespace of class reflected</param>
        public ClassManager(string namespaceScope)
        {
            _classes = new Dictionary<string, string>();
            _namespace = namespaceScope;
        }

        /// <summary>
        ///     Using directives
        /// </summary>
        public string Usings
        {
            get
            {
                var namespaces = new List<string>();
                foreach (var pair in _classes.Where(pair => !namespaces.Contains(pair.Value)))
                {
                    namespaces.Add(pair.Value); //merge duplicate namespaces
                }

                return namespaces.Aggregate("", (prev, next) => $"{prev}using {next};{Environment.NewLine}");
            }
        }

        public string WriteReturnType(Type type, Type reflectedType, GetReturnType deleg)
        {
            return Write(type,true, reflectedType, deleg);
        }

        /// <summary>
        ///     Returns a simplified class name from Namespace.Class with respect to usings
        /// </summary>
        /// <param name="type"></param>
        /// <param name="usetypes">whether to use class names for generics instead of parameter names</param>
        /// <returns></returns>
        public string Write(Type type, bool usetypes = false,Type reflectedType=null,GetReturnType deleg=null)
        {
            var fullname = type.FullName;
            if (fullname == null)
            {
                return TrimTypeName(type.Name);
            }
            
            if ((reflectedType != null)&&(deleg!=null))
            {
                if (reflectedType.IsGenericType)
                {
                    Type returnType = deleg(reflectedType.GetGenericTypeDefinition());
                    if (returnType.IsGenericParameter)
                        return returnType.Name;
                }
            }

            var nameSpace = type.Namespace;
            var shortName = TrimTypeName(type.Name);

            if ((nameSpace != _namespace) && _classes.ContainsKey(shortName) && (_classes[shortName] != nameSpace))
                //class name conflict, use full name
                return $"{nameSpace}.{shortName}{GetGenericPart(type, usetypes)}";

            if (!_classes.ContainsKey(shortName)) //unknown class with no conflicts            
                _classes.Add(shortName, nameSpace); //remember it

            return $"{shortName}{GetGenericPart(type, usetypes)}";
        }

        /// <summary>
        ///     For generic types returns "&lt;T1, T2...&gt;"
        /// </summary>
        /// <param name="type"></param>
        /// <param name="usetype">if true, T1 will be replaced with actual class name</param>
        /// <param name="exclude"></param>
        /// <returns></returns>
        private string GetGenericPart(Type type, bool usetype = false)
        {
            if (!type.IsGenericType) return "";
            
            List<string> genericArguments;
            if (usetype)
            {
                genericArguments = type.GetGenericArguments().Select(q => Write(q)).ToList();
            }
            else
            {
                genericArguments =
                    type.GetGenericTypeDefinition().GetGenericArguments().Select(q => Write(q, true)).ToList();
            }

            return $"<{string.Join(", ", genericArguments)}>";
        }

        /// <summary>
        ///     convert Int32 to int etc
        /// </summary>
        /// <param name="shortname"></param>
        /// <returns></returns>
        public static string TrimTypeName(string shortname)
        {
            if (shortname.EndsWith("&")) //ref types have & at the end it is not needed
            {
                shortname = shortname.Remove(shortname.Length - 1);
            }
            if (shortname.Contains("`")) //generics
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