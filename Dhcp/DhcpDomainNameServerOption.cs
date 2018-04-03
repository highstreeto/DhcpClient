using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DhcpClientNET.Dhcp
{
    public class DhcpDomainNameServerOption : DhcpOption
    {
        public DhcpDomainNameServerOption(IPAddress[] domainNameServers)
        {
            DomainNameServers = domainNameServers;
        }

        public IPAddress[] DomainNameServers { get; set; }

        public override DhcpOptionType Type => DhcpOptionType.DomainNameServer;

        public static DhcpDomainNameServerOption CreateFromRaw(byte[] rawData)
        {
            int addressCount = rawData.Length / 4;
            IPAddress[] addresses = new IPAddress[addressCount];
            for (int i = 0; i < addressCount; i++)
            {
                addresses[i] = new IPAddress(rawData.Skip(i * 4).Take(4).ToArray());
            }
            return new DhcpDomainNameServerOption(addresses);
        }

        public override byte[] Build()
        {
            return DomainNameServers
                .SelectMany(addr => addr.GetAddressBytes())
                .ToArray();
        }
    }
}
