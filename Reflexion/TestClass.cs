using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Reflex
{
    public class TestClass
    {
        public event EventHandler Fired;

        public enum MyEnum
        {
            [Description("hey")]
            Opt1,
            Opt2
        }

        [XmlAttribute("k")]
        public string Name { get; set; }
        private DateTime _id;
        public string name;
        protected int GetInt<V>(V v)
        {
            return 0;
        }
    }
}