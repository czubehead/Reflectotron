using System;

namespace Reflex
{
    class Program
    {
        static void Main(string[] args)
        {
            int x, y = 0;

            TestClass tc=new TestClass(string.Empty, out x, ref y);

            Reflectotron reflectotron=new Reflectotron(tc);
            Console.WriteLine(reflectotron.Info);
            Console.ReadKey();
        }
    }
}
