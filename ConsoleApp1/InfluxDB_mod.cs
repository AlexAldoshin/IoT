using InfluxClient;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    static class InfluxDB_mod
    {
        public static async Task Go()
        {

            // To authenticate, create the InfluxManager with additional parameters:
            var mgr = new InfluxManager("http://192.168.100.7:8086", "farms", "farms", "farms");

           
            var ListM = new List<Measurement>();
            for (int i = 0; i < 3; i++)
            {
                Measurement m = new Measurement("unittest").AddField("count", i);
                m.Timestamp = DateTime.Now.AddSeconds(-i*10);
                ListM.Add(m);
            }
            // Write the measurement (notice that this is awaitable):
            var retval = await mgr.Write(ListM);

            var retval2 = await mgr.Query("select * from unittest");
            Console.WriteLine();

        }
    }
}
