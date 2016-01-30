using System;

namespace Reflex
{
    class Program
    {
        static void Main(string[] args)
        {
            int x, y = 0;

            TestClass<int,string> tc=new TestClass<int,string>(string.Empty, out x, ref y);

            Reflectotron reflectotron=new Reflectotron(tc);
            Console.WriteLine(reflectotron.Info);
            Console.ReadKey();
        }
    }
}
