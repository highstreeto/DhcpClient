using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DhcpClientNET.Dhcp
{
    public class DhcpRequestedIpAddressOption : DhcpOption
    {
        public DhcpRequestedIpAddressOption(IPAddress requestedAddress)
        {
            RequestedAddress = requestedAddress;
        }

        public IPAddress RequestedAddress { get; set; }

        public override DhcpOptionType Type => DhcpOptionType.RequestedIpAddress;

        public static DhcpRequestedIpAddressOption CreateFromRaw(byte[] rawData)
        {
            return new DhcpRequestedIpAddressOption(new IPAddress(rawData));
        }

        public override byte[] Build()
        {
            return RequestedAddress.GetAddressBytes();
        }
    }
}
