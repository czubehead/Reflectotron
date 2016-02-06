using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Reflex
{
    [Serializable]
    public class TestClass<T> : IDisposable
    {
        protected const int MaxValue = 100;

        private int _id;

        [My("hey", I = 1)]
        public List<T> List { get; set; }

        public DateTime DateTime { get; set; }

        public string this[[In] int index]
        {
            get { return "Itnetwork.cz"; }
            set { }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public static TestClass<T> operator +(TestClass<T> a, TestClass<T> b)
        {
            return a;
        }

        protected static V Method<V>(T thing)
        {
            throw new NotImplementedException();
        }
    }

    internal class MyAttribute : Attribute
    {
        public MyAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
        public int I { get; set; }
    }
}