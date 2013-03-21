using System;
using System.Linq;
using System.Net.Sockets;

namespace Com.Dianping.Cat
{
    using System.Net;

    public class NetworkInterfaceManager
    {
        public static string GetLocalHostName()
        {
            return Dns.GetHostName();
        }

        public static string GetLocalHostAddress()
        {
            IPHostEntry host = Dns.GetHostEntry(GetLocalHostName());

            foreach (IPAddress ip in host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork))
            {
                return ip.ToString();
            }

            throw new NotSupportedException("No IP address found");
        }

        public static byte[] GetAddressBytes()
        {
            IPHostEntry host = Dns.GetHostEntry(GetLocalHostName());

            foreach (IPAddress ip in host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork))
            {
                return ip.GetAddressBytes();
            }

            throw new NotSupportedException("No IP address found");
        }
    }
}