using System;

namespace Reflex
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var tc = new TestClass<int> {S=2};

            var reflectotron = new Reflectotron(tc);
            Console.WriteLine(reflectotron.Info);
            Console.ReadKey();
        }
    }
}