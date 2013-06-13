using SharpIMPP.DNS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(DnsSRV.GetSRVRecords("_impp._tcp.trillian.im").First());
            Console.ReadLine();
        }
    }
}
