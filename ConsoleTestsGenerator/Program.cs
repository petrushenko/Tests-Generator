using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TestsGenerator;

namespace ConsoleTestsGenerator
{
    public static class Program
    {
        private const string PathToSave = @"..\..\Tests\";
        public static void Main()
        {
            Task waiter = null;
            try
            {
                Console.WriteLine("Write list of test separating them by spaces");
                var testFiles = Console.ReadLine().Split(' ').ToList();
                Console.WriteLine("Write max amount of streams for reading");
                var maxInputStreams = int.Parse(Console.ReadLine());
                Console.WriteLine("Write max amount of streams for writing");
                var maxOutStreams = int.Parse(Console.ReadLine());
                Console.WriteLine("Write max amount of streams for generating tests");
                var maxMainStreams = int.Parse(Console.ReadLine());
                waiter =  new NUnitTestsGenerator().GenerateTests(testFiles, PathToSave, maxInputStreams, maxOutStreams,
                    maxMainStreams);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }

            waiter?.Wait();
            Console.WriteLine("Finished, check tests in [{0}] folder", Path.GetFullPath(PathToSave));
            Console.ReadLine();
        }
    }
}
