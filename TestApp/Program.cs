﻿using SharpIMPP;
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
            Console.Title = "SharpIMPP Test";
            Console.WriteLine(DnsSRV.GetSRVRecords("_impp._tcp.trillian.im").First());
            IMPPClient si = new IMPPClient();
            Console.Write("Username (tricia): ");
            string user = Console.ReadLine();
            if (user == "")
            {
                user = "tricia";
            }
            Console.Write("Password (password): ");
            string pass = Console.ReadLine();
            Console.Clear();
            if (pass == "")
            {
                pass = "password";
            }
            si.Connect(user,"trillian.im",pass);
            Console.ReadLine();
        }
    }
}
