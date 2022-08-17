using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class EthPrinter : Printer
{
    public string IpAddress { get; set; }
    public int Port { get; set; }

    public EthPrinter(string ipAddress, int PORT)
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
            throw new ArgumentException("IPAddress not valid.");
    }
    public EthPrinter(string ipAddress) : this(ipAddress, 9100) { }

    public override void Send(byte[] data)
    {
        try
        {
            Socket s = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);
            s.Connect(IpAddress, Port);
            s.Send(data);
            s.Disconnect(false);
        }
        catch (SocketException) { throw new SocketException(404); }
    }
    private bool PingTest(string IPtoPing)
    {
        bool pingable = false;
        Ping pinger = null;

        try
        {
            pinger = new Ping();
            PingReply reply = pinger.Send(IPtoPing);
            pingable = reply.Status == IPStatus.Success;
        }
        catch { return false; }
        finally { if (pinger != null) pinger.Dispose(); }

        return pingable;
    }
}
