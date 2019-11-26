using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestsGenerator;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            var tg = new NUnitTestsGenerator();
            Console.WriteLine(Process.GetCurrentProcess().Threads.Count);
            var r = tg.GenerateTests(new[]
            {
                @"D:\bsuir\C#\projects\ConsoleApp1\ConsoleApp2\Program.cs",
                @"D:\bsuir\C#\projects\ConsoleApp1\ConsoleApp2\Program.cs",
                @"D:\bsuir\C#\projects\ConsoleApp1\ConsoleApp2\Program.cs",
                @"D:\bsuir\C#\projects\ConsoleApp1\ConsoleApp2\Program.cs",
                @"D:\bsuir\C#\projects\ConsoleApp1\ConsoleApp2\Program.cs",
                @"D:\bsuir\C#\projects\ConsoleApp1\ConsoleApp2\Program.cs",
                @"D:\bsuir\C#\projects\ConsoleApp1\ConsoleApp2\Program.cs",
                @"D:\bsuir\C#\projects\ConsoleApp1\ConsoleApp2\Program.cs",
                @"D:\bsuir\C#\projects\ConsoleApp1\ConsoleApp2\Program.cs",
                @"D:\bsuir\C#\projects\ConsoleApp1\ConsoleApp2\Program.cs",
                @"D:\bsuir\C#\projects\ConsoleApp1\ConsoleApp2\BaseCls.cs"
            }, @"D:\bsuir\C#\projects\foo", 2, 2, 2);
            Console.WriteLine(Process.GetCurrentProcess().Threads.Count);
            //while (r.Status != TaskStatus.Canceled) { }
            r.Wait();
            Console.WriteLine(Process.GetCurrentProcess().Threads.Count);
            Console.ReadKey();
        }
    }
}
