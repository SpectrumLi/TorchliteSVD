// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace DataRace
{
    class Program
    {

        static List<string> list = new List<string>();
        static List<string> l2;
        static Program p1 = null;
        static void Main(string[] args)
        {
            Stopwatch sw = new Stopwatch();

            l2 = list;
            
            sw.Start();

            Test();

            sw.Stop();

            Console.WriteLine("Ellapsed time (ms): " + sw.ElapsedMilliseconds);
        }

        static void Test()
        {
            Thread t1 = new Thread(Request1);
            t1.Start();
            Thread t2 = new Thread(Request2);
            t2.Start();

            t1.Join();
            t2.Join();
        }

        static void Request1()
        {
            p1 = new Program();
            Console.WriteLine("Request1 Start");
            list.Add("Request 1");
            l2.Add("Request 2");
            list.Sort();
            Console.WriteLine("Request1 End");
        }

        static void Request2()
        {
            Thread.Sleep(250);
            Console.WriteLine("P1 to string " + p1.ToString());
            Console.WriteLine("Request2 Start");
            list.Add("Request 2");
            list.Sort();
            Console.WriteLine("Request2 End");
        }

    }
}
