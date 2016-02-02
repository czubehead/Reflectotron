using System;

namespace Reflex
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var tc = new TestClass {Name = "big",name="small"};

            var reflectotron = new Reflectotron(tc);
            Console.WriteLine(reflectotron.Info);
            Console.ReadKey();
        }
    }
}