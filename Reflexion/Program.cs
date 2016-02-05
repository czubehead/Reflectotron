using System;

namespace Reflex
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var tc = new TestClass<DateTime>();

            var reflectotron = new Reflectotron(tc);
            Console.WriteLine(reflectotron.ReflectedInfo);
            Console.ReadKey();
        }
    }
}