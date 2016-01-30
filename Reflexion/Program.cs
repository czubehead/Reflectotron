using System;
using System.Linq;

namespace Reflex
{
    class Program
    {
        static void Main(string[] args)
        {
            int x, y = 0;

            TestClass<int> tc=new TestClass<int>();

            Reflectotron reflectotron=new Reflectotron(tc);
            Console.WriteLine(reflectotron.Info);
            Console.ReadKey();
        }
    }
}
