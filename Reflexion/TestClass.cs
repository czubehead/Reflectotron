using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Reflex
{
    [Serializable]
    public class TestClass<T,TX>:IDisposable,ICloneable
    {
        public virtual int PubVirtProp { get; set; }
        [XmlIgnore]
        public string PubGetOnlyProp {get;private set; }
        private int PrivProp { get; set; }
        //public override DateTime PubAbstProp { get; set; }

        protected readonly int _ProtReadField;

        public TestClass(string normal,out int out_int,ref int ref_int,string opt="empty")
        {
            out_int = 0;
        }
        public TestClass() { }

        static TestClass()
        {
            
        } 

        public void TestMethod(string normal, out int out_int, ref int ref_int, string opt = "empty")
        {
            out_int = 0;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public object Clone()
        {
            return this;
        }

        //public static TestClass operator +(TestClass a, TestClass b)
        //{
        //    return a;
        //}
        //public static TestClass operator -(TestClass a, TestClass b)
        //{
        //    return a;
        //}
        //public static TestClass operator *(TestClass a, TestClass b)
        //{
        //    return a;
        //}
        //public static TestClass operator /(TestClass a, TestClass b)
        //{
        //    return a;
        //}

        //public static implicit operator string(TestClass tc)
        //{
        //    return "";
        //}

        //public static implicit operator TestClass(string s)
        //{
        //    return new TestClass();
        //}

        public void PubGENERIC<Tm0,Tm1>(ref string a, Tm0 paramerter,int i=0)
        {

        }
        //public async void PubAsyncVoid()
        //{
        //    await Task.Run((() => PrivProp++));
        //}
    }
}
