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
        private readonly Dictionary<string,string> _classes;

        /// <summary>
        /// Usinf directives
        /// </summary>
        public string Usings
        {
            get
            {
                List<string> namespaces=new List<string>();
                foreach (var pair in _classes.Where(pair => !namespaces.Contains(pair.Value)))
                {
                    namespaces.Add(pair.Value);//merge duplicate namespaces
                }

                return namespaces.Aggregate("",(prev, next) => $"{prev}using {next};{Environment.NewLine}");
            }
        }

        private readonly string _namespace;
        
        /// <param name="namespaceScope">Namespace of class reflected</param>
        public ClassManager(string namespaceScope)
        {
            _classes=new Dictionary<string, string>();
            _namespace = namespaceScope;
        }

        /// <summary>
        /// Returns a simplified class name from Namespace.Class with respect to usings
        /// </summary>
        /// <param name="fullname">e.g. System.int</param>
        /// <returns></returns>
        public string Write(string fullname)
        {
            if (fullname == null)
            {
                return "";
            }
            int lastDotPos = fullname.LastIndexOf('.');
            if (lastDotPos < 0)
                return fullname;//no namespace specified

            string nameSpace = fullname.Substring(0, lastDotPos);
            string shortName = Shorten(fullname.Substring(lastDotPos + 1));

            if (nameSpace==_namespace)
                return Shorten(shortName);//class is in same namespace as class reflected

            if (_classes.ContainsKey(shortName) && _classes.ContainsValue(nameSpace))
                return Shorten(shortName);

            if (!_classes.ContainsKey(shortName))
            {
                _classes.Add(shortName, nameSpace);
                return Shorten(shortName); //this would cause some conflicts
            }

            return fullname;
        }

        public string Write(Type type)
        {
            return Write(type.FullName);
        }

        /// <summary>
        /// convert Int32 to int etc
        /// </summary>
        /// <param name="shortname"></param>
        /// <returns></returns>
        public static string Shorten(string shortname)
        {
            
            if (shortname.EndsWith("&"))//ref types have & at the end it is not needed
            {
                shortname= shortname.Remove(shortname.Length - 1);
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
