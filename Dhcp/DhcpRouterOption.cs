using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DhcpClientNET.Dhcp
{
    public class DhcpRouterOption : DhcpOption
    {
        public DhcpRouterOption(IPAddress[] routers)
        {
            Routers = routers;
        }

        public IPAddress[] Routers { get; set; }

        public override DhcpOptionType Type => DhcpOptionType.Router;

        public static DhcpRouterOption CreateFromRaw(byte[] rawData)
        {
            int addressCount = rawData.Length / 4;
            IPAddress[] addresses = new IPAddress[addressCount];
            for (int i = 0; i < addressCount; i++)
            {
                addresses[i] = new IPAddress(rawData.Skip(i * 4).Take(4).ToArray());
            }
            return new DhcpRouterOption(addresses);
        }

        public override byte[] Build()
        {
            return Routers
                .SelectMany(addr => addr.GetAddressBytes())
                .ToArray();
        }
    }
}
