using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Text;

public class Printer
{
    private string IP_ADDRESS { get; set; }
    private List<byte> BUFFER = new List<byte>();

    private const byte ESC = 27;
    private const byte FF = 12;
    private const byte SO = 14;
    private const byte LF = 10;
    private const byte CR = 13;
    private const byte GS = 29;
    private const byte HT = 9;

    public Printer (string ipAddress)
    {
        System.Net.IPAddress address;
        if (System.Net.IPAddress.TryParse(ipAddress, out address))
        {
            this.IP_ADDRESS = ipAddress;
            Reset();
        }
        else
            throw new ArgumentException("IPAddress is not valid!");
    }

    /// <summary>
    /// Print the current data buffer.
    /// </summary>
    public void Print()
    {
        byte[] buffer = BUFFER.ToArray();
        _networkSend(buffer);
    }
    /// <summary>
    /// Print the current data buffer and cut the paper.
    /// </summary>
    public void PrintAndCut()
    {
        FeedAndCut();
        byte[] buffer = BUFFER.ToArray();
        _networkSend(buffer);
    }
    /// <summary>
    /// Reset the data buffer and initializes the printer.
    /// </summary>
    public void Reset()
    {
        BUFFER.Add(ESC);
        BUFFER.Add(64);
    }

    #region Layout
    /// <summary>
    /// Set the spacing between characters.
    /// </summary>
    /// <param name="value">Value needs to be between 0 and 48 both included.</param>
    public void CharacterRightSpace(byte value)
    {
        if (value >= 0 && value <= 48)
        {
            BUFFER.Add(ESC);
            BUFFER.Add(23);
            BUFFER.Add(value);
        }
    }
    /// <summary>
    /// Move the printing cursor N tabs forward.
    /// </summary>
    /// <param name="value">Needs to be between 0 and 4 both included.</param>
    public void InLineTab(byte value)
    {
        //Each tab is 8 characters
        if (value >= 0 && value <= 4)
            for (int i = 0; i < value; i++)
                BUFFER.Add(HT);
    }
    /// <summary>
    /// Move the printing cursor one tab forward.
    /// </summary>
    public void InLineTab() 
    {
        InLineTab(1);
    }
    /// <summary>
    /// Print the array string divided by columns, be aware that longer text will be trimmed.
    /// </summary>
    /// <param name="text">Array of content.</param>
    /// <param name="columnNumber">Number of total columns.</param>
    public void PrintInColumn(string[] text, int columnNumber)
    {
        int maxCharsPerLine = 42; //When text is default
        int maxColumnChars = maxCharsPerLine / columnNumber;
        string finalLineString = null;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i].Length > maxColumnChars)
                text[i] = text[i].Substring(0, maxColumnChars);
            else
                text[i] = text[i].PadRight(maxColumnChars);
            finalLineString += text[i];
        }
        byte[] data = Encoding.UTF8.GetBytes(finalLineString + "\n");
        foreach (byte b in data)
            BUFFER.Add(b);
    }
    #endregion

    #region Alignment
    /// <summary>
    /// Align the following content to the left.
    /// </summary>
    public void AlignLeft()
    {
        BUFFER.Add(ESC);
        BUFFER.Add(97);
        BUFFER.Add(0);
    }
    /// <summary>
    /// Align the following content to the centre.
    /// </summary>
    public void AlignCenter()
    {
        BUFFER.Add(ESC);
        BUFFER.Add(97);
        BUFFER.Add(1);
    }
    /// <summary>
    /// Align the following content to the right.
    /// </summary>
    public void AlignRight()
    {
        BUFFER.Add(ESC);
        BUFFER.Add(97);
        BUFFER.Add(2);
    }
    #endregion

    #region Styles
    /// <summary>
    /// Enable/Disable underline for the following text.
    /// </summary>
    /// <param name="value">True to enable, False to disable.</param>
    public void SetUnderline(bool value)
    {
        BUFFER.Add(ESC);
        BUFFER.Add(45);
        if (value) BUFFER.Add(2);
        else BUFFER.Add(0);
    }
    /// <summary>
    /// Enable/Disable bold text printing on the following text.
    /// </summary>
    /// <param name="value">True to enable, False to disable.</param>
    public void SetBold(bool value)
    {
        BUFFER.Add(ESC);
        BUFFER.Add(69);
        if (value) BUFFER.Add(1);
        else BUFFER.Add(0);
    }
    /// <summary>
    /// Enable/Disable inverted printing (white on black) on the following text.
    /// </summary>
    /// <param name="value">True to enable, False to disable.</param>
    public void SetInvert(bool value)
    {
        BUFFER.Add(GS);
        BUFFER.Add(66);
        if (value) BUFFER.Add(1);
        else BUFFER.Add(0);
    }
    /// <summary>
    /// Enable/Disable upside down text, be aware that alignment will be mirrored.
    /// </summary>
    /// <param name="value">True to enable, False to disable.</param>
    public void SetUpsideDown(bool value)
    {
        BUFFER.Add(ESC);
        BUFFER.Add(123);
        if (value) BUFFER.Add(1);
        else BUFFER.Add(0);
    }
    #endregion

    #region Fonts
    /// <summary>
    /// Set default ESCPOS Font A as active.
    /// </summary>
    public void SetFontA()
    {
        BUFFER.Add(ESC);
        BUFFER.Add(33);
        BUFFER.Add(0);
    }
    /// <summary>
    /// Set default ESCPOS Font B as active.
    /// </summary>
    public void SetFontB()
    {
        BUFFER.Add(ESC);
        BUFFER.Add(33);
        BUFFER.Add(1);
    }
    /// <summary>
    /// Set default ESCPOS Font as active.
    /// </summary>
    public void SetFontDefault()
    {
        BUFFER.Add(ESC);
        BUFFER.Add(64);
    }
    /// <summary>
    /// Changes the size of the fonts depending on the passed arguments.
    /// </summary>
    /// <param name="doubleWidth">True to enable double width.</param>
    /// <param name="doubleHeight">True to enable double height.</param>
    public void SetSize(bool doubleWidth, bool doubleHeight)
    {
        BUFFER.Add(29);
        BUFFER.Add(33);
        if (doubleWidth && doubleHeight) BUFFER.Add(17);          //BIG
        else if (!doubleWidth && doubleHeight) BUFFER.Add(16);    //HIGH
        else if (doubleWidth && !doubleHeight) BUFFER.Add(1);     //WIDE
        else BUFFER.Add(0);                                       //NORMAL
    }
    #endregion

    #region Write
    /// <summary>
    /// Print the desired text and terminates the line.
    /// </summary>
    /// <param name="text">Text to print.</param>
    public void WriteLine(string text)
    {
        byte[] data = Encoding.UTF8.GetBytes(text + "\n");
        foreach (byte b in data)
            BUFFER.Add(b);
    }
    /// <summary>
    /// Print the desired text without terminating the line.
    /// </summary>
    /// <param name="text">Text to print.</param>
    public void Write(string text)
    {
        byte[] data = Encoding.UTF8.GetBytes(text);
        foreach (byte b in data)
            BUFFER.Add(b);
    }
    #endregion

    #region Other
    /// <summary>
    /// Print a simple horizontal line for the desired length.
    /// </summary>
    /// <param name="length">Length in dots, each dot is 0.125mm.</param>
    public void HorizontalLine(int length)
    {
        if (length > 0)
        {
            if (length > 42)
                length = 42;

            for (int i = 0; i < length; i++)
                BUFFER.Add(196);
            BUFFER.Add(10);
        }
    }
    /// <summary>
    /// Print a simple horizontal line throughout the paper.
    /// </summary>
    public void HorizontalLine()
    {
        HorizontalLine(42);
    }
    /// <summary>
    /// Get the printer buffer until this moment.
    /// </summary>
    /// <returns>Returns a string containing all the decimal bytes within the printer buffer.</returns>
    public string GetBufferString()
    {
        string buffer = null;
        foreach (byte b in BUFFER)
            buffer += b + " ";
        return buffer;
    }
    /// <summary>
    /// Send a raw decimal ESC/POS command.
    /// </summary>
    /// <param name="cmd">List of commands</param>
    public void SendRawDecimal(byte[] cmd)
    {
        _networkSend(cmd);
    }
    #endregion

    #region Feed
    /// <summary>
    /// Feed one simple line.
    /// </summary>
    public void FeedLine()
    {
        BUFFER.Add(FF);
    }
    /// <summary>
    /// Feed one simple line and cut the paper.
    /// </summary>
    public void FeedAndCut()
    {
        BUFFER.Add(GS);
        BUFFER.Add(86);
        BUFFER.Add(65);
        BUFFER.Add(0);
    }
    /// <summary>
    /// Feed the desired number of lines.
    /// </summary>
    /// <param name="value">This value must be between 1 and 255 both included.</param>
    public void FeedLines(byte value)
    {
        if (value >= 1 && value <= 255)
        {
            BUFFER.Add(ESC);
            BUFFER.Add(100);
            BUFFER.Add(value);
        }
    }
    #endregion

    #region Barcode/QR
    public enum BarcodeType
    {
        UPC_A = 0,
        UPC_E = 1,
        EAN13 = 2,
        EAN8 = 3,
        CODE39 = 4,
        I25 = 5,
        CODEBAR = 6,
        CODE93 = 7,
        CODE128 = 8,
        CODE11 = 9,
        MSI = 10
    }
    /// <summary>
    /// Prints a barcode.
    /// </summary>
    /// <param name="type">Specify the barcode type by using BarcodeType enumerator.</param>
    /// <param name="data">Specify the data of the barcode.</param>
    /// <param name="printToCenter">True to print the barcode centered.</param>
    public void PrintBarcode(BarcodeType type, string data, bool printToCenter)
    {
        if (printToCenter) InLineTab();

        byte[] originalBytes;
        byte[] outputBytes;
        if (type == BarcodeType.CODE93 || type == BarcodeType.CODE128)
        {
            originalBytes = System.Text.Encoding.UTF8.GetBytes(data);
            outputBytes = originalBytes;
        }
        else
        {
            originalBytes = System.Text.Encoding.UTF8.GetBytes(data.ToUpper());
            outputBytes = System.Text.Encoding.Convert(System.Text.Encoding.UTF8, System.Text.Encoding.GetEncoding("ibm850"), originalBytes);
        }
        switch (type)
        {
            case BarcodeType.UPC_A:
                if (data.Length == 11 || data.Length == 12)
                {
                    BUFFER.Add(29);
                    BUFFER.Add(107);
                    BUFFER.Add(0);
                }
                break;
            case BarcodeType.UPC_E:
                if (data.Length == 11 || data.Length == 12)
                {
                    BUFFER.Add(29);
                    BUFFER.Add(107);
                    BUFFER.Add(1);
                }
                break;
            case BarcodeType.EAN13:
                if (data.Length == 12 || data.Length == 13)
                {
                    BUFFER.Add(29);
                    BUFFER.Add(107);
                    BUFFER.Add(2);
                }
                break;
            case BarcodeType.EAN8:
                if (data.Length == 7 || data.Length == 8)
                {
                    BUFFER.Add(29);
                    BUFFER.Add(107);
                    BUFFER.Add(3);
                }
                break;
            case BarcodeType.CODE39:
                if (data.Length > 1)
                {
                    BUFFER.Add(29);
                    BUFFER.Add(107);
                    BUFFER.Add(4);
                }
                break;
            case BarcodeType.I25:
                if (data.Length > 1 || data.Length % 2 == 0)
                {
                    BUFFER.Add(29);
                    BUFFER.Add(107);
                    BUFFER.Add(5);
                }
                break;
            case BarcodeType.CODEBAR:
                if (data.Length > 1)
                {
                    BUFFER.Add(29);
                    BUFFER.Add(107);
                    BUFFER.Add(6);
                }
                break;
            case BarcodeType.CODE93: //todo: overload PrintBarcode method with a byte array parameter
                if (data.Length > 1)
                {
                    BUFFER.Add(29);
                    BUFFER.Add(107);
                    BUFFER.Add(7);
                }
                break;
            case BarcodeType.CODE128:
                if (data.Length > 1)
                {
                    BUFFER.Add(29);
                    BUFFER.Add(107);
                    BUFFER.Add(8);
                }
                break;
            case BarcodeType.CODE11:
                if (data.Length > 1)
                {
                    BUFFER.Add(29);
                    BUFFER.Add(107);
                    BUFFER.Add(9);
                }
                break;
            case BarcodeType.MSI:
                if (data.Length > 1)
                {
                    BUFFER.Add(29);
                    BUFFER.Add(107);
                    BUFFER.Add(10);
                }
                break;
        }
        foreach(byte b in outputBytes)
            BUFFER.Add(b);
        BUFFER.Add(0);
    }

    //QRCode currently not working
    public void PrintQRcode(string data)
    {
        int length = Encoding.Default.GetBytes(data).Length + 3;
        byte pH = (byte)(length % 256);
        byte pL = (byte)(length / 256);
        BUFFER.Add(GS);
        BUFFER.Add(40);
        BUFFER.Add(107);
        BUFFER.Add(pL);
        BUFFER.Add(pH);
        BUFFER.Add(49);
        BUFFER.Add(80);
        BUFFER.Add(48);
        foreach (byte b in Encoding.Default.GetBytes(data))
            BUFFER.Add(b);
        BUFFER.Add(LF);
    }
    #endregion

    #region Private methods
    private void _networkSend(byte[] data)
    {
        Socket s = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp);
        s.Connect(IP_ADDRESS, 9100);
        s.Send(data);
        s.Disconnect(false);
    }
    #endregion

}