using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Reflex
{
    public class TestClass<T>
    {
        public T this[int index]
        {
            set { }
        }
    }
}