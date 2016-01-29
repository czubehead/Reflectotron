using System;

namespace Reflex
{
    class Program
    {
        static void Main(string[] args)
        {
            TestClass tc=new TestClass {PubVirtProp = 0};

            Reflectotron reflectotron=new Reflectotron(tc);
            Console.WriteLine(reflectotron.Info);
            Console.ReadKey();
        }
    }
}
