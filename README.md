# What is this library?
This library allows ethernet printing on ESC/POS printers using C#.

# The Idea
- I could not find a simple C# library to print stuff on thermal printer via Ethernet, so I created this one.


This latest version allows:
- Layout personalisation (some very basic functions, will be improved);
- Font selection (by using standard provided fonts);
- Styles selection (Bold, underlined, ...);
- Justification;
- Barcode (1D) printing;
- QR Code printing (not available yet);
- Image printing (not available yet);

# Requisites
- .Net Framework SDK v4.8

# How to use
- Add the provided DLL file as Reference into your C# project;
- Add using EscPosLib; at the top of your Project code-behind file;

Create a Printer object as below by pointing out the thermal printer IP address:
```cs
Printer yourThermalPrinter = new Printer("192.168.1.2"); //Your printer IP
yourThermalPrinter.Reset();                              //This resets and also initializes the printer
```

The library is very simple and straight forward.
It is documented internally with ```<summary> ```
