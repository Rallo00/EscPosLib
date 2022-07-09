using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Linq;

public class Printer
{
    //--- NETWORK SETTINGS ---
    public string IpAddress { get; set; }
    public int Port { get; set; }

    //--- ALPHABET/REGION/COUNTRY ---
    public Alphabet Region { get; set; }

    //--- PRINT BUFFER ---
    private List<byte> BUFFER = new List<byte>();

    //--- COMMANDS ---
    private const byte ESC = 27;
    private const byte FF = 12;
    private const byte SO = 14;
    private const byte LF = 10;
    private const byte CR = 13;
    private const byte GS = 29;
    private const byte HT = 9;

    // --- CONSTANTS ---
    private readonly int MAX_IMAGE_WIDTH = 300, MAX_IMAGE_HEIGHT = 300, LOGO_FIXED_WIDTH = 500;

    public Printer (string ipAddress, int PORT)
    {
        System.Net.IPAddress address;
        if (System.Net.IPAddress.TryParse(ipAddress, out address))
        {
            this.IpAddress = ipAddress;
            this.Port = PORT;
            this.Region = 0;
            Reset();
        }
        else
            throw new ArgumentException("IPAddress is not valid!");
    }
    public Printer (string ipAddress) : this (ipAddress, 9100) { }

    #region Region
    public enum Alphabet
    {
        Not_Specified = 0,
        Arabic = 1,
        Canadian = 2,
        Cyrillic = 3,
        Farsi = 4,
        French = 5,
        Greek = 6,
        Hebrew = 7,
        Hiragana = 8,
        Icelandic = 9,
        Kanji_I = 10,
        Kanji_II = 11,
        Katakana = 12,
        Kazakhstan = 13,
        Latin = 14,
        Lithuanian = 15,
        Multilingual = 16,
        Nordic = 17,
        Portuguese = 18,
        Turkish = 19,
        Ukrainian = 20,
        Vietnamese = 21
    }
    private void SetRegion(Alphabet a)
    {
        BUFFER.Add(ESC);
        BUFFER.Add(116);
        if (a == Alphabet.Arabic) BUFFER.Add(37);
        else if (a == Alphabet.Canadian) BUFFER.Add(4);
        else if (a == Alphabet.Cyrillic) BUFFER.Add(34);
        else if (a == Alphabet.Farsi) BUFFER.Add(41);
        else if (a == Alphabet.French) BUFFER.Add(4);
        else if (a == Alphabet.Greek) BUFFER.Add(15);
        else if (a == Alphabet.Hebrew) BUFFER.Add(36);
        else if (a == Alphabet.Hiragana) BUFFER.Add(6);
        else if (a == Alphabet.Icelandic) BUFFER.Add(35);
        else if (a == Alphabet.Kanji_I) BUFFER.Add(7);
        else if (a == Alphabet.Kanji_II) BUFFER.Add(8);
        else if (a == Alphabet.Katakana) BUFFER.Add(1);
        else if (a == Alphabet.Kazakhstan) BUFFER.Add(53);
        else if (a == Alphabet.Latin) BUFFER.Add(40);
        else if (a == Alphabet.Lithuanian) BUFFER.Add(43);
        else if (a == Alphabet.Multilingual) BUFFER.Add(2);
        else if (a == Alphabet.Nordic) BUFFER.Add(5);
        else if (a == Alphabet.Portuguese) BUFFER.Add(3);
        else if (a == Alphabet.Turkish) BUFFER.Add(12);
        else if (a == Alphabet.Ukrainian) BUFFER.Add(44);
        else if (a == Alphabet.Vietnamese) BUFFER.Add(52);
        else BUFFER.Add(2); //Multilingual
    }
    #endregion

    #region Printer
    /// <summary>
    /// Reset the data buffer and initializes the printer.
    /// </summary>
    public void Reset()
    {
        BUFFER.Clear();
        BUFFER.Add(ESC);
        BUFFER.Add(64);
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
    /// Get the printer buffer until this moment.
    /// </summary>
    /// <returns>Returns a string containing all the decimal bytes within the printer buffer.</returns>
    public string GetCurrentBuffer()
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
    public void RawDecimalSend(byte[] cmd)
    {
        _networkSend(cmd);
    }
    /// <summary>
    /// Add a raw decimal ESC/POS command to buffer
    /// </summary>
    /// <param name="cmd">>List of commands</param>
    public void RawDecimalToBuffer(byte[] cmd)
    {
        foreach (byte b in cmd)
            BUFFER.Add(b);
    }
    #endregion

    #region Layout
    /// <summary>
    /// Print a simple horizontal line for the desired length.
    /// </summary>
    /// <param name="length">Length in dots, each dot is 0.125mm. Max value is 42.</param>
    public void HorizontalLine(int length)
    {
        //Checking argument
        if (length < 0 || length > 42)
            length = 42;

        for (int i = 0; i < length; i++)
            BUFFER.Add(Encoding.Unicode.GetBytes("-")[0]);
        BUFFER.Add(10);
    }
    /// <summary>
    /// Print a simple horizontal line throughout the paper.
    /// </summary>
    public void HorizontalLine()
    {
        HorizontalLine(42);
    }
    /// <summary>
    /// Set the spacing between characters.
    /// </summary>
    /// <param name="value">Value needs to be between 0 and 48 both included.</param>
    public void SetCharSpacing(byte value)
    {
        if (value >= 0 && value <= 48)
        {
            BUFFER.Add(ESC);
            BUFFER.Add(23);
            BUFFER.Add(value);
        }
        else throw new ArgumentException("Argument is not valid, must be between 0 and 48 both included.");
    }
    /// <summary>
    /// Move the printing cursor N tabs forward.
    /// </summary>
    /// <param name="value">Needs to be between 0 and 4 both included.</param>
    public void IdentTabs(byte value)
    {
        //Each tab is 8 characters
        if (value >= 0 && value <= 4)
            for (int i = 0; i < value; i++)
                BUFFER.Add(HT);
        else throw new ArgumentException("Argument is not valid, must be between 0 and 4 both included.");
    }
    /// <summary>
    /// Move the printing cursor one tab forward.
    /// </summary>
    public void IdentTab() 
    {
        IdentTabs(1);
    }
    /// <summary>
    /// Print the array string divided by columns, be aware that longer text will be trimmed.
    /// </summary>
    /// <param name="text">Array of content.</param>
    /// <param name="columnNumber">Number of total columns. Max value is 5.</param>
    public void PrintInColumn(string[] text, int columnNumber)
    {
        //Checking argument
        if (columnNumber < 0) throw new ArgumentException("Argument is not valid, must be between 1 and 5 both included.");
        if (columnNumber > 5) columnNumber = 5;

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
    /// <summary>
    /// Print total of the receipt in big characters
    /// </summary>
    /// <param name="totalDescription">Description of the total amount</param>
    /// <param name="amount">Total amount</param>
    public void PrintTotal(string totalDescription, string amount)
    {
        int maxCharsPerLine = 21; //When text is default
        int maxColumnChars = maxCharsPerLine / 2;
        totalDescription = totalDescription.PadRight(maxCharsPerLine-amount.Length);
        string finalLineString = totalDescription + amount;
        byte[] data = Encoding.UTF8.GetBytes(finalLineString + "\n");
        foreach (byte b in data)
            BUFFER.Add(b);
    }
    /// <summary>
    /// Print the content of a receipt by padding the text content
    /// </summary>
    /// <param name="rcl">ReceiptContentLine object containing each product line content (quantity, description, price)</param>
    public void ReceiptContentLayout(List<ReceiptContentLine> rcl)
    {
        int MAX_CHARS_QTY = 5, MAX_CHARS_DESCRIPTION = 30, MAX_CHARS_PRICE = 7;
        SetBold(true);
        WriteLine("Qty  " + "Description                  " + "   Price");
        SetBold(false);
        string finalLineString = null;
        //Identing based on content text length
        for(int i = 0; i < rcl.Count; i++)
        {
            string Qty = rcl[i].Qty, Description = rcl[i].Description, Price = rcl[i].Price;
            if (Qty.Length < 4) Qty = Qty.PadRight(MAX_CHARS_QTY, ' ');
            if (Description.Length > MAX_CHARS_DESCRIPTION) Description = Description.Substring(0, MAX_CHARS_DESCRIPTION);
            else if (Description.Length < MAX_CHARS_DESCRIPTION) Description = Description.PadRight(MAX_CHARS_DESCRIPTION, ' ');
            if (Price.Length < MAX_CHARS_PRICE) Price = Price.PadLeft(MAX_CHARS_PRICE, ' ');
            finalLineString += Qty + Description + Price + '\n';
        }
        WriteLine(finalLineString);
    }
    public struct ReceiptContentLine
    {
        public string Qty { get; set; }
        public string Description { get; set; }
        public string Price { get; set; }
        public ReceiptContentLine(string q, string d, string p) { Qty = q; Description = d; Price = p; }
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
    public void WriteLine(string text) => Write(text + '\n');
    /// <summary>
    /// Print the desired text without terminating the line.
    /// </summary>
    /// <param name="text">Text to print.</param>
    public void Write(string text)
    {
        //Check region
        if (Region == Alphabet.Not_Specified) SetRegion(Alphabet.Multilingual);
        else SetRegion(Region);

        foreach (byte b in Encoding.Unicode.GetBytes(text))
            BUFFER.Add(b);
    }
    /// <summary>
    /// Print some boxed text in the center
    /// </summary>
    /// <param name="textLines">Array containing lines to print</param>
    /// <param name="boxChar">Decorative character used as box</param>
    public void WriteBox(string[] textLines, char boxChar)
    {
        AlignCenter();
        int maxLength = textLines.OrderByDescending(s => s.Length).First().Length;
        //Handling first and last box line
        string boxHeaderFooter = null;
        for (int i = 0; i < maxLength + 4; i++)
            boxHeaderFooter += boxChar;
        Write(boxHeaderFooter);
        //Handling content
        for (int i = 0; i < textLines.Length; i++)
        {
            int contentLength = textLines[i].Length;
            textLines[i] = $"\n{boxChar} {textLines[i].PadLeft((maxLength + 3)/2)}";
            textLines[i] = $"{textLines[i].PadRight((maxLength + 3))} {boxChar}";
            Write(textLines[i]);
        }
        Write("\n" + boxHeaderFooter);
        AlignLeft();
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
    /// Feed the desired number of lines.
    /// </summary>
    /// <param name="value">This value must be between 1 and 255 both included.</param>
    public void FeedLines(byte value)
    {
        if (value <= 1 || value > 255) throw new ArgumentException("Argument is not valid, must be between 1 and 255 both included.");
        else
        {
            BUFFER.Add(ESC);
            BUFFER.Add(100);
            BUFFER.Add(value);
        }
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
    #endregion

    #region Barcode
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
    /// <param name="printValue">True to print the barcode value.</param>
    public void PrintBarcode(BarcodeType type, string data, bool printToCenter, bool printValue)
    {
        if (printToCenter) AlignCenter();

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
        BUFFER.Add(GS);
        BUFFER.Add(107);
        switch (type)
        {
            case BarcodeType.UPC_A:
                if (data.Length == 11 || data.Length == 12) BUFFER.Add(0);
                break;
            case BarcodeType.UPC_E:
                if (data.Length == 11 || data.Length == 12) BUFFER.Add(1);
                break;
            case BarcodeType.EAN13:
                if (data.Length == 12 || data.Length == 13) BUFFER.Add(2);
                break;
            case BarcodeType.EAN8:
                if (data.Length == 7 || data.Length == 8) BUFFER.Add(3);
                break;
            case BarcodeType.CODE39:
                if (data.Length > 1) BUFFER.Add(4);
                break;
            case BarcodeType.I25:
                if (data.Length > 1 || data.Length % 2 == 0) BUFFER.Add(5);
                break;
            case BarcodeType.CODEBAR:
                if (data.Length > 1) BUFFER.Add(6);
                break;
            case BarcodeType.CODE93: //todo: overload PrintBarcode method with a byte array parameter
                if (data.Length > 1) BUFFER.Add(7);
                break;
            case BarcodeType.CODE128:
                if (data.Length > 1) BUFFER.Add(8);
                break;
            case BarcodeType.CODE11:
                if (data.Length > 1) BUFFER.Add(9);
                break;
            case BarcodeType.MSI:
                if (data.Length > 1) BUFFER.Add(10);
                break;
        }
        foreach(byte b in outputBytes)
            BUFFER.Add(b);
        BUFFER.Add(0);
        if (printValue) WriteLine(data);
        if (printToCenter) AlignLeft();
    }
    #endregion

    #region QRCode
    /// <summary>
    /// This function allows QR-Code printing up to 127 characters. It only supports standard 7 bit ASCII characters.
    /// </summary>
    /// <param name="data">QR-Code text data</param>
    /// <param name="alignCenter">Align QR-Code to center</param>
    public void PrintQRCode(string data, bool alignCenter) => PrintQRCode(data, 5, alignCenter);
    /// <summary>
    /// This function allows QR-Code printing up to 127 characters. It only supports standard 7 bit ASCII characters.
    /// </summary>
    /// <param name="data">QR-Code text data</param>
    /// <param name="size">Size in dots. Each dot is 0.125mm. Max Value is 15.</param>
    /// <param name="alignCenter">Align QR-Code to center</param>
    public void PrintQRCode(string data, byte size, bool alignCenter)
    {
        if (size <= 0 || size > 15)
            throw new ArgumentException("Size is not valid. Must be between 1 and 15 both included.");
        else
        {
            if (alignCenter) AlignCenter();
            int length = data.Length + 3;
            byte pL = (byte)(length % 256);
            byte pH = (byte)(length / 256);

            byte[] QrCodeSettings = new byte[]
            {
            GS, 40, 107, 4, 0, 49, 65, 50, 0,                          //<Function 165> select the model (2 is widely supported)
            GS, 40, 107, 3, 0, 49, 67, size,                           //<Function 167> Set the size of the module
            GS, 40, 107, 3, 0, 49, 69, 48,                             //<Function 169> Select lever of error correction (printer-dependent)
            GS, 40, 107, pL, pH, 49, 80, 48                            //<Function 080> Send your data to the image storage area in the printer        
            };
            byte[] QrCodeContent = Encoding.UTF8.GetBytes(data);
            byte[] QrCodePrinting = new byte[]
            {
            GS, 40, 107, 3, 0, 49, 81, 48,                             //<Function 081> Print the symbol data in the symbol storage area
            GS, 40, 107, 3, 0, 49, 82, 48                              //<Function 082> Transmit the size information of the symbol data in the symbol storage area
            };
            byte[] QrCodeBuffer = QrCodeSettings.Concat(QrCodeContent).Concat(QrCodePrinting).ToArray();
            foreach (byte b in QrCodeBuffer)
                BUFFER.Add(b);
            BUFFER.Add(LF);
            if (alignCenter) AlignLeft();
        }
    }
    #endregion

    #region Images
    public void PrintImage(string fileName, bool printToCenter)
    {
        //Checking valid path
        if (!File.Exists(fileName))
            throw new FileNotFoundException("File not found! Check path.");
        //Valid no-transparency format
        if (Path.GetExtension(fileName).ToLower() == ".jpg" || Path.GetExtension(fileName).ToLower() == ".jpeg" || Path.GetExtension(fileName).ToLower() == ".gif")
            PrintImage(new Bitmap(fileName), printToCenter);
        //Png/transparency format
        else if (Path.GetExtension(fileName).ToLower() == "png") { /*Convert*/ }
    }
    public void PrintImage(Bitmap bmp, bool printToCenter)
    {
        //Checking resizing
        if (bmp.Height > MAX_IMAGE_HEIGHT || bmp.Width > MAX_IMAGE_WIDTH) bmp = ResizeImage(bmp);
        //Check if need to print in center
        if (printToCenter) bmp = CenterImage(bmp);
        PrintImage(bmp);
    }
    public string GetAsciiArt(string fileName)
    {
        if (!File.Exists(fileName))
            throw new ArgumentException("Argument value is not valid!");
        else
            return GetAsciiArt(new Bitmap(fileName));
    }
    public string GetAsciiArt(Bitmap bmp)
    {
        //GET RESIZED IMAGE
        int asciiHeight = 300, asciiWidth = 300;
        //Calculate the new Height of the image from its width
        asciiHeight = (int)Math.Ceiling((double)bmp.Height * asciiWidth / bmp.Width);
        //Create a new Bitmap and define its resolution
        Bitmap resizedImage = new Bitmap(asciiWidth, asciiHeight);
        Graphics g = Graphics.FromImage((Image)resizedImage);
        //The interpolation mode produces high quality images
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.DrawImage(bmp, 0, 0, asciiWidth, asciiHeight);
        g.Dispose();
        //CONVERT TO ASCII
        string[] _AsciiChars = { "#", "#", "@", "%", "=", "+", "*", ":", "-", ".", " " };
        Boolean toggle = false;
        StringBuilder sb = new StringBuilder();
        for (int h = 0; h < resizedImage.Height; h++)
        {
            for (int w = 0; w < resizedImage.Width; w++)
            {
                Color pixelColor = resizedImage.GetPixel(w, h);
                //Average out the RGB components to find the Gray Color
                int red = (pixelColor.R + pixelColor.G + pixelColor.B) / 3;
                int green = (pixelColor.R + pixelColor.G + pixelColor.B) / 3;
                int blue = (pixelColor.R + pixelColor.G + pixelColor.B) / 3;
                Color grayColor = Color.FromArgb(red, green, blue);
                //Use the toggle flag to minimize height-wise stretch
                if (!toggle)
                {
                    int index = (grayColor.R * 10) / 255;
                    sb.Append(_AsciiChars[index]);
                }
            }
            if (!toggle)
            {
                sb.Append('\n');
                toggle = true;
            }
            else toggle = false;
        }
        return sb.ToString();
    }
    private BitmapData GetBitmapData(Bitmap bmp)
    {
        using (var bitmap = bmp)
        {
            var threshold = 127;
            var index = 0;
            double multiplier = 570; // this depends on your printer model. for Beiyang you should use 1000
            //double scale = (double)(multiplier / (double)bitmap.Width); //This prints in original 100% resolution
            double scale = 1;
            int xheight = (int)(bitmap.Height * scale);
            int xwidth = (int)(bitmap.Width * scale);
            var dimensions = xwidth * xheight;
            var dots = new System.Collections.BitArray(dimensions);

            for (var y = 0; y < xheight; y++)
            {
                for (var x = 0; x < xwidth; x++)
                {
                    var _x = (int)(x / scale);
                    var _y = (int)(y / scale);
                    var color = bitmap.GetPixel(_x, _y);
                    var luminance = (int)(color.R * 0.3 + color.G * 0.59 + color.B * 0.11);
                    dots[index] = (luminance < threshold);
                    index++;
                }
            }

            return new BitmapData()
            {
                Dots = dots,
                Height = (int)(bitmap.Height * scale),
                Width = (int)(bitmap.Width * scale)
            };
        }
    }
    private Bitmap ResizeImage(Bitmap bmp)
    {
        var destRect = new Rectangle(0, 0, MAX_IMAGE_WIDTH, MAX_IMAGE_HEIGHT);
        var destImage = new Bitmap(MAX_IMAGE_WIDTH, MAX_IMAGE_HEIGHT);

        destImage.SetResolution(destImage.HorizontalResolution, destImage.VerticalResolution);

        using (var graphics = Graphics.FromImage(destImage))
        {
            graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

            using (var wrapMode = new System.Drawing.Imaging.ImageAttributes())
            {
                wrapMode.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
                graphics.DrawImage(bmp, destRect, 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, wrapMode);
            }
        }
        return destImage;
    }
    private Bitmap CenterImage(Bitmap bmp)
    {
        Bitmap logo = new Bitmap(LOGO_FIXED_WIDTH, MAX_IMAGE_HEIGHT);
        //Blank rectangle
        using (Graphics graph = Graphics.FromImage(logo))
        {
            Rectangle ImageSize = new Rectangle(0, 0, LOGO_FIXED_WIDTH, MAX_IMAGE_HEIGHT);
            graph.FillRectangle(Brushes.White, ImageSize);
        }
        int widthStartPos = (LOGO_FIXED_WIDTH / 2) - (bmp.Width / 2);
        //Image on top of rectangle
        Graphics g = Graphics.FromImage(logo);
        g.DrawImage(bmp, widthStartPos, 0, bmp.Width, bmp.Height);
        return logo;
    }
    private void PrintImage(Bitmap bmp)
    {
        string logo = "";
        BitmapData data = GetBitmapData(bmp);
        System.Collections.BitArray dots = data.Dots;
        byte[] width = BitConverter.GetBytes(data.Width);

        int offset = 0;
        MemoryStream stream = new MemoryStream();
        BinaryWriter bw = new BinaryWriter(stream);

        bw.Write((char)0x1B);
        bw.Write('@');

        bw.Write((char)0x1B);
        bw.Write('3');
        bw.Write((byte)24);

        while (offset < data.Height)
        {
            bw.Write((char)0x1B);
            bw.Write('*');         // bit-image mode
            bw.Write((byte)33);    // 24-dot double-density
            bw.Write(width[0]);  // width low byte
            bw.Write(width[1]);  // width high byte

            for (int x = 0; x < data.Width; ++x)
            {
                for (int k = 0; k < 3; ++k)
                {
                    byte slice = 0;
                    for (int b = 0; b < 8; ++b)
                    {
                        int y = (((offset / 8) + k) * 8) + b;
                        // Calculate the location of the pixel we want in the bit array.
                        // It'll be at (y * width) + x.
                        int i = (y * data.Width) + x;

                        // If the image is shorter than 24 dots, pad with zero.
                        bool v = false;
                        if (i < dots.Length)
                        {
                            v = dots[i];
                        }
                        slice |= (byte)((v ? 1 : 0) << (7 - b));
                    }

                    bw.Write(slice);
                }
            }
            offset += 24;
            bw.Write((char)0x0A);
        }
        // Restore the line spacing to the default of 30 dots.
        bw.Write((char)0x1B);
        bw.Write('3');
        bw.Write((byte)30);

        bw.Flush();
        byte[] bytes = stream.ToArray();
        string finalString = logo + Encoding.Default.GetString(bytes);
        foreach (byte b in Encoding.Default.GetBytes(finalString))
            BUFFER.Add(b);
    }
    #endregion

    #region Private to class
    private class BitmapData
    {
        public System.Collections.BitArray Dots
        {
            get;
            set;
        }
        public int Height
        {
            get;
            set;
        }
        public int Width
        {
            get;
            set;
        }
    }
    private void _networkSend(byte[] data)
    {
        Socket s = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp);
        s.Connect(IpAddress, Port);
        s.Send(data);
        s.Disconnect(false);
    }
    #endregion

    #region Examples
    public void ExampleReceipt()
    {
        //Logo
        byte[] logo = { 27, 64, 27, 64, 27, 51, 24, 27, 42, 33, 244, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 7, 255, 192, 15, 255, 192, 15, 255, 192, 31, 255, 192, 31, 255, 192, 31, 255, 192, 31, 255, 192, 31, 255, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 240, 192, 31, 255, 192, 31, 255, 192, 31, 255, 192, 31, 255, 192, 31, 255, 192, 31, 255, 192, 15, 255, 192, 7, 255, 192, 1, 255, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 192, 0, 0, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, 27, 42, 33, 244, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 255, 0, 7, 255, 0, 15, 255, 0, 15, 255, 0, 15, 255, 0, 15, 255, 0, 15, 255, 0, 15, 255, 0, 15, 251, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 240, 0, 15, 248, 0, 15, 255, 0, 15, 255, 0, 15, 255, 0, 15, 255, 0, 15, 255, 0, 15, 255, 0, 7, 255, 0, 1, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, 27, 42, 33, 244, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 3, 0, 0, 3, 0, 0, 3, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 60, 0, 0, 126, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 126, 0, 0, 60, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 255, 128, 7, 255, 192, 7, 255, 192, 7, 255, 192, 7, 255, 192, 7, 255, 192, 7, 255, 192, 7, 255, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 224, 7, 63, 255, 255, 63, 255, 255, 63, 255, 255, 63, 255, 255, 63, 255, 255, 63, 255, 255, 63, 255, 255, 63, 255, 255, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 224, 7, 63, 255, 255, 63, 255, 255, 63, 255, 255, 63, 255, 255, 63, 255, 255, 63, 255, 255, 63, 255, 255, 63, 255, 255, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 63, 192, 7, 255, 192, 7, 255, 192, 7, 255, 192, 7, 255, 192, 7, 255, 192, 7, 255, 192, 7, 255, 192, 7, 255, 128, 7, 254, 0, 7, 0, 0, 7, 0, 0, 3, 0, 0, 3, 0, 0, 3, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, 27, 42, 33, 244, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 31, 0, 15, 255, 0, 255, 255, 3, 255, 255, 7, 255, 255, 31, 255, 255, 63, 255, 255, 127, 255, 255, 127, 255, 248, 255, 248, 0, 255, 192, 0, 255, 128, 0, 255, 0, 0, 254, 0, 0, 252, 0, 0, 252, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 15, 0, 3, 31, 128, 15, 63, 192, 15, 63, 192, 15, 63, 192, 15, 63, 192, 15, 63, 192, 15, 63, 192, 15, 63, 192, 15, 63, 192, 15, 63, 192, 15, 63, 192, 15, 63, 192, 15, 63, 192, 15, 63, 192, 15, 63, 192, 15, 63, 192, 15, 63, 192, 15, 63, 192, 15, 63, 192, 15, 31, 128, 15, 15, 0, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 127, 248, 0, 255, 248, 1, 255, 248, 1, 255, 248, 1, 255, 248, 1, 255, 248, 1, 255, 248, 1, 255, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 254, 248, 1, 255, 248, 1, 255, 248, 1, 255, 248, 1, 255, 248, 1, 255, 248, 1, 255, 248, 0, 255, 248, 0, 127, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 248, 0, 0, 252, 0, 0, 252, 0, 0, 254, 0, 0, 255, 0, 0, 255, 128, 0, 255, 192, 0, 255, 248, 0, 255, 255, 248, 127, 255, 255, 63, 255, 255, 31, 255, 255, 7, 255, 255, 3, 255, 255, 0, 255, 255, 0, 15, 255, 0, 0, 31, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, 27, 42, 33, 244, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 31, 0, 31, 255, 31, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 240, 255, 248, 0, 248, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 124, 0, 0, 254, 0, 0, 254, 0, 0, 255, 0, 1, 255, 0, 0, 255, 0, 1, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 253, 255, 0, 0, 255, 0, 1, 255, 0, 1, 255, 0, 1, 255, 0, 1, 255, 0, 1, 255, 0, 1, 255, 0, 1, 255, 0, 1, 255, 0, 1, 255, 192, 1, 255, 224, 1, 255, 240, 1, 255, 240, 1, 255, 240, 1, 255, 240, 1, 255, 240, 1, 255, 240, 1, 255, 240, 1, 255, 240, 1, 255, 240, 1, 255, 240, 1, 255, 240, 1, 255, 240, 1, 255, 240, 1, 255, 240, 1, 255, 240, 1, 255, 240, 1, 255, 240, 1, 255, 240, 1, 255, 224, 1, 255, 192, 1, 255, 0, 1, 255, 0, 1, 255, 0, 1, 255, 0, 1, 255, 0, 1, 255, 0, 1, 255, 0, 1, 255, 0, 0, 255, 0, 1, 255, 0, 0, 255, 0, 1, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 254, 0, 0, 254, 0, 0, 124, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 248, 0, 0, 255, 248, 0, 255, 255, 240, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 31, 255, 255, 0, 31, 255, 0, 0, 31, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, 27, 42, 33, 244, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 63, 0, 63, 255, 63, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 224, 255, 240, 0, 240, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 3, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 254, 0, 7, 255, 0, 7, 255, 128, 7, 255, 128, 7, 255, 128, 7, 255, 128, 7, 255, 128, 7, 255, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 3, 127, 128, 1, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 1, 127, 128, 3, 127, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 3, 127, 128, 1, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 1, 127, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 7, 127, 128, 7, 255, 128, 7, 255, 128, 7, 255, 128, 7, 255, 128, 7, 255, 128, 7, 255, 128, 7, 255, 0, 7, 254, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 7, 0, 0, 3, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 240, 0, 0, 255, 240, 0, 255, 255, 224, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 63, 255, 255, 0, 63, 255, 0, 0, 63, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, 27, 42, 33, 244, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 127, 0, 127, 255, 63, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 224, 255, 224, 0, 224, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 254, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 248, 0, 127, 248, 0, 127, 248, 0, 127, 248, 0, 127, 248, 0, 127, 248, 0, 127, 248, 0, 127, 248, 0, 127, 248, 0, 127, 248, 0, 127, 248, 0, 127, 248, 0, 127, 248, 0, 127, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 254, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 254, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 248, 0, 127, 248, 0, 127, 248, 0, 127, 248, 0, 127, 248, 0, 127, 248, 0, 127, 248, 0, 127, 248, 0, 127, 248, 0, 127, 248, 0, 127, 248, 0, 127, 248, 0, 127, 248, 128, 127, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 254, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 254, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 248, 0, 127, 248, 0, 127, 248, 0, 127, 248, 0, 127, 248, 0, 127, 248, 0, 127, 248, 0, 127, 248, 0, 127, 248, 0, 127, 248, 0, 127, 248, 0, 127, 248, 0, 127, 253, 255, 127, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 254, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 224, 0, 0, 255, 224, 0, 255, 255, 224, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 63, 255, 255, 0, 127, 255, 0, 0, 127, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, 27, 42, 33, 244, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 0, 255, 255, 127, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 192, 255, 192, 0, 192, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 255, 128, 7, 255, 128, 15, 255, 128, 15, 255, 128, 15, 255, 128, 15, 255, 128, 15, 255, 128, 15, 255, 128, 15, 248, 128, 15, 240, 128, 15, 240, 128, 15, 240, 128, 15, 240, 128, 15, 240, 128, 15, 240, 128, 15, 240, 128, 15, 240, 128, 15, 240, 128, 15, 240, 128, 15, 240, 128, 15, 248, 128, 15, 255, 128, 15, 255, 128, 15, 255, 128, 15, 255, 128, 15, 255, 128, 7, 255, 0, 7, 255, 0, 1, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 255, 128, 7, 255, 128, 15, 255, 128, 15, 255, 128, 15, 255, 128, 15, 255, 128, 15, 255, 128, 15, 255, 128, 15, 248, 128, 15, 240, 128, 15, 240, 128, 15, 240, 128, 15, 240, 128, 15, 240, 128, 15, 240, 128, 15, 240, 128, 15, 240, 128, 15, 240, 128, 15, 240, 128, 15, 240, 128, 15, 248, 128, 15, 255, 128, 15, 255, 128, 15, 255, 128, 15, 255, 128, 15, 255, 128, 7, 255, 0, 7, 255, 0, 1, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 255, 128, 7, 255, 128, 15, 255, 128, 15, 255, 128, 15, 255, 128, 15, 255, 128, 15, 255, 128, 15, 255, 128, 15, 248, 128, 15, 240, 128, 15, 240, 128, 15, 240, 128, 15, 240, 128, 15, 240, 128, 15, 240, 128, 15, 240, 128, 15, 240, 128, 15, 240, 128, 15, 240, 128, 15, 240, 128, 15, 251, 128, 15, 255, 128, 15, 255, 128, 15, 255, 128, 15, 255, 128, 15, 255, 128, 7, 255, 0, 7, 255, 0, 1, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 192, 0, 0, 255, 192, 0, 255, 255, 192, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 127, 255, 255, 0, 255, 255, 0, 0, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, 27, 42, 33, 244, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 255, 1, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 128, 255, 128, 0, 128, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 254, 0, 255, 255, 0, 255, 255, 128, 255, 255, 128, 255, 255, 128, 255, 255, 128, 255, 255, 128, 255, 255, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 255, 128, 255, 255, 128, 255, 255, 128, 255, 255, 128, 255, 255, 128, 255, 255, 128, 255, 255, 128, 255, 255, 0, 255, 252, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 254, 0, 255, 255, 0, 255, 255, 128, 255, 255, 128, 255, 255, 128, 255, 255, 128, 255, 255, 128, 255, 255, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 255, 128, 255, 255, 128, 255, 255, 128, 255, 255, 128, 255, 255, 128, 255, 255, 128, 255, 255, 128, 255, 255, 0, 255, 252, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 254, 0, 255, 255, 0, 255, 255, 128, 255, 255, 128, 255, 255, 128, 255, 255, 128, 255, 255, 128, 255, 255, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 0, 127, 128, 254, 255, 128, 255, 255, 128, 255, 255, 128, 255, 255, 128, 255, 255, 128, 255, 255, 128, 255, 255, 128, 255, 255, 0, 255, 252, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 128, 0, 0, 255, 128, 0, 255, 255, 128, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 1, 255, 255, 0, 1, 255, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, 27, 42, 33, 244, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 0, 129, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 1, 254, 0, 129, 254, 0, 255, 255, 0, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 1, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, 27, 42, 33, 244, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 15, 240, 0, 127, 254, 1, 255, 255, 3, 255, 255, 7, 255, 255, 15, 255, 255, 31, 255, 255, 31, 255, 255, 63, 252, 63, 63, 240, 15, 127, 224, 7, 127, 192, 3, 127, 128, 1, 255, 128, 1, 255, 128, 1, 255, 128, 1, 255, 128, 1, 255, 128, 1, 127, 128, 1, 127, 192, 3, 127, 224, 7, 63, 240, 15, 63, 252, 63, 31, 255, 255, 31, 255, 255, 15, 255, 255, 7, 255, 255, 3, 255, 255, 1, 255, 255, 0, 127, 254, 0, 15, 240, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, 27, 42, 33, 244, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 240, 0, 255, 254, 0, 255, 255, 128, 255, 255, 192, 255, 255, 224, 255, 255, 240, 255, 255, 248, 255, 255, 252, 0, 31, 254, 0, 7, 254, 0, 3, 255, 0, 1, 255, 0, 1, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 128, 0, 255, 192, 0, 255, 224, 0, 255, 240, 0, 255, 248, 0, 255, 248, 0, 255, 252, 0, 255, 252, 0, 255, 254, 0, 255, 254, 0, 255, 254, 0, 255, 254, 0, 255, 255, 0, 255, 255, 0, 255, 255, 0, 255, 254, 0, 255, 254, 0, 255, 254, 0, 255, 254, 0, 255, 252, 0, 255, 252, 0, 255, 248, 0, 255, 248, 0, 255, 240, 0, 255, 224, 0, 255, 192, 0, 255, 128, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 1, 255, 0, 1, 255, 0, 3, 255, 0, 7, 254, 0, 31, 254, 255, 255, 252, 255, 255, 248, 255, 255, 240, 255, 255, 224, 255, 255, 192, 255, 255, 128, 255, 254, 0, 255, 240, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, 27, 42, 33, 244, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 240, 0, 0, 48, 0, 0, 48, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 128, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 16, 0, 0, 48, 0, 255, 240, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, 27, 51, 30 };
        RawDecimalToBuffer(logo);
        //Text
        AlignCenter();
        SetSize(true, false);
        WriteLine("Shop name");
        SetSize(false, false);
        WriteLine("Company name");
        WriteLine("Address, street number");
        WriteLine("ZipCode - City");
        WriteLine("## Company registration number ##");
        AlignLeft();
        FeedLines(2);
        HorizontalLine();
        List<ReceiptContentLine> receiptContent = new List<ReceiptContentLine>();
        receiptContent.Add(new ReceiptContentLine("1", "Guest(s)", "1.00"));
        receiptContent.Add(new ReceiptContentLine("10", "Afternoon tea", "125.00"));
        receiptContent.Add(new ReceiptContentLine("100", "T-Shirts L size", "1125.00"));
        ReceiptContentLayout(receiptContent);
        FeedLine();
        HorizontalLine();
        SetBold(true);
        PrintInColumn(new string[3] { "Sub-total", "VAT", "Amount" }, 3);
        SetBold(false);
        PrintInColumn(new string[3] { "E990.90", "E110.10", "E1101.00" }, 3);
        HorizontalLine();
        SetSize(true, true);
        PrintTotal("Total", "E1101.00");
        SetSize(false, false);
        HorizontalLine();
        WriteLine("Does not fit properly? No problem.\nCome back and we'll change it for you.\nShow the assistant this receipt");
        FeedLines(2);
        PrintBarcode(BarcodeType.EAN13, "1234567890123", true, true);
        HorizontalLine();
        AlignCenter();
        WriteLine("Please review us!");
        PrintQRCode("WebAddressLinkQrCodeTest", true);
    }
    #endregion
}