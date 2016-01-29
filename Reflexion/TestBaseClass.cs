using System;

namespace Reflex
{
    public abstract class TestBaseClass:IComparable
    {
        public abstract DateTime PubAbstProp { get; set; }

        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
        }
    }
}