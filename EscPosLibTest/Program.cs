﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EscPosLibTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.Write("IP: ");
            string ip = Console.ReadLine();
            EthPrinter printer = new EthPrinter(ip);
            printer.Region = Printer.Alphabet.Multilingual;
            printer.Reset();
            printer.ExampleReceipt();
            printer.PrintAndCut();
        }
    }
}
