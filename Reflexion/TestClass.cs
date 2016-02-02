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
            [Description()]
            Opt1,
            Opt2
        }

        [XmlAttribute("k")]
        public string Name { get; set; }
        private DateTime _id;
        protected int GetInt<V>(V v)
        {
            return 0;
        }
    }

    class NothingAttribute:Attribute
    {
        public NothingAttribute(string seed)
        {
            
        }
    }
}