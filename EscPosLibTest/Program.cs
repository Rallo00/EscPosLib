using System;
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
            Printer printer = new Printer("10.225.59.51");
            //printer.Region = Printer.Alphabet.Multilingual;

            printer.Reset();
            printer.ExampleReceipt();

            printer.PrintAndCut();
        }
    }
}
