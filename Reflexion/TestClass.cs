using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Reflex
{
    [Serializable]
    public class TestClass<T>:IDisposable
    {
        protected const int MaxValue = 100;

        public List<T> List { get; set; }
        public DateTime DateTime { get; set; }

        private int _id;

        public static TestClass<T> operator +(TestClass<T> a, TestClass<T> b)
        {
            return a;
        }

        public string this[[In]int index]
        {
            get { return "Itnetwork.cz"; }
            set { }
        }

        protected static V Method<V>(T thing)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}