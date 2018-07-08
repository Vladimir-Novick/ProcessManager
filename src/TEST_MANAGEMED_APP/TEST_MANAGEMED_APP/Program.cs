using System;
using System.Threading.Tasks;

namespace TEST_MANAGEMED_APP
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            String TaskName = "Test APP";
            if (args.Length > 0) {
                TaskName = args[0];
            }
            for (int i=1; i <10; i++)
            {
                Console.WriteLine($"TEST_MANAGEMED_APP - {i} , {TaskName}");
                System.Threading.Thread.Sleep(2000);
            }
        }
    }
}
