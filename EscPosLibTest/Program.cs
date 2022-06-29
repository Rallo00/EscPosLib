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
            printer.Reset();
            /*
            //NORMALE
            printer.WriteLine("Niente di speciale.");
            printer.Write("Test di testo");
            printer.WriteLine(" CON APPEND");
            //GRASSETTO
            printer.SetBold(true);
            printer.WriteLine("Sono quello spesso.");
            printer.SetBold(false);
            //SOTTOLINEATO
            printer.SetUnderline(true);
            printer.WriteLine("Io sono importante.");
            printer.SetUnderline(false);
            //INVERSO
            printer.SetInvert(true);
            printer.WriteLine("Io sono africano.");
            printer.SetInvert(false);/*
            //SOPRASOTTO
            printer.SetUpsideDown(true);
            printer.AlignRight();
            printer.WriteLine("Mondo parallelo");
            printer.SetUpsideDown(false);
            printer.AlignLeft();
            //ALLINEAMENTO
            printer.HorizontalLine(42);
            printer.WriteLine("Braccio sinistro");
            printer.AlignCenter();
            printer.WriteLine("Petto");
            printer.AlignRight();
            printer.WriteLine("Braccio destro");
            printer.AlignLeft();
            //FONTS TYPE
            printer.HorizontalLine(42);
            printer.SetFontA();
            printer.WriteLine("Font A");
            printer.SetFontB();
            printer.WriteLine("Font B");
            printer.SetFontDefault();
            //FONT SIZE
            printer.HorizontalLine(42);
            printer.SetSize(true, false);
            printer.WriteLine("Testo alto");
            printer.SetSize(false, true);
            printer.WriteLine("Testo largo");
            printer.SetSize(true, true);
            printer.WriteLine("Testo grande");
            printer.SetSize(false, false);
            printer.WriteLine("Torno normale");
            //TAB
            printer.HorizontalLine(42);
            printer.WriteLine("STAMPA PER TAB:");
            printer.WriteLine("12345678");
            printer.InLineTab();
            printer.WriteLine("12345678");
            printer.InLineTab(2);
            printer.WriteLine("12345678");
            printer.InLineTab(3);
            printer.WriteLine("12345678");
            printer.InLineTab(4);
            printer.WriteLine("12345678");
            //TESTO ALLINEATO
            printer.HorizontalLine(42);
            printer.WriteLine("Testo allineato: max 42 chars per riga");
            printer.PrintInColumn(new string[2] { "Colonna1", "Colonna2" }, 2);
            printer.HorizontalLine(42);
            printer.PrintInColumn(new string[3] { "Colonna1", "Colonna2", "Colonna3" }, 3);
            printer.HorizontalLine(42);
            printer.PrintInColumn(new string[4] { "Colonna1", "Colonna2", "Colonna3", "Colonna4" }, 4);
            printer.HorizontalLine(42);
            //BARCODE
            printer.WriteLine("Barcode On Left");
            printer.WriteLine("1234567890123");
            printer.PrintBarcode(Printer.BarcodeType.EAN13, "1234567890123", false);
            printer.FeedLine();
            printer.WriteLine("Barcode Centered");
            printer.PrintBarcode(Printer.BarcodeType.EAN13, "1234567890123", true);*/
            //QRCODE
            printer.PrintQRcode("!!!!");
            

            printer.Print();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("PRINT BUFFER:");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(printer.GetBufferString());
            
            Console.ReadKey();
        }
    }
}
