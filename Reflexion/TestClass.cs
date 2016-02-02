using System.Collections.Generic;

namespace Reflex
{
    public class TestClass <T> where T : new()
    {
        public T Prop { get; set; }
        public int S { get; set; }
        public List<T> Genmeth => null; 

        public T GetT()
        {
            return new T();
        }
    }
}