using System;
using System.ComponentModel;

namespace Reflex
{
    public class TestClass
    {
        public event EventHandler Fired;

        public enum MyEnum
        {
            [Description]
            Opt1,
            Opt2
        }

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