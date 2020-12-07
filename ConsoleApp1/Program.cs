using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Task task = Task.Run(async () => {
                await InfluxDB_mod.Go();
                Console.WriteLine("Ok");
            });
            Console.ReadLine();
        }
    }
}
