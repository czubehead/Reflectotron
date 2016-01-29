using System;
using System.Xml.Serialization;

namespace Reflex
{
    [Serializable]
    public class TestClass:TestBaseClass,IDisposable,ICloneable
    {
        public virtual int PubVirtProp { get; set; }
        [XmlIgnore]
        public string PubGetOnlyProp {get;private set; }
        private int PrivProp { get; set; }
        public static string PubStatProp { get; set; }
        public static int PrivStatProp { get; set; }
        public int PublicField;
        private string _privateField;
        private static int _privateStaticField;
        private static int _privateStaticField_value=0;
        private readonly int _privReadField;
        protected readonly int _ProtReadField;

        protected string ProtProp { get; set; }

        public override DateTime PubAbstProp { get; set; }
        
        public void PubVoidMeth() { }
        private void PivVoidMeth() { }
        public int PubIntMeth_noparam()
        {return 0;}
        private int PrivIntMeth_noparam()
        { return 0; }

        public static string PubStatStr_noparam()
        {
            return null;
        }
        private static string PrivStatStr_noparam()
        {
            return null;
        }


        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public object Clone()
        {
            return this;
        }
    }
}
