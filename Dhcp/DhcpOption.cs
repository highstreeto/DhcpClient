using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhcpClientNET.Dhcp
{
    public abstract class DhcpOption
    {
        public abstract DhcpOptionType Type { get; }

        public abstract byte[] Build();
    }

    public enum DhcpOptionType : byte
    {
        Pad = 0,
        SubnetMask = 1,
        Router = 3,
        DomainNameServer = 6,
        DomainName = 15,
        RequestedIpAddress = 50,
        MessageType = 53,
        ServerIdentifier = 54,
        ClientIdentifier = 61,
        End = 255
    }
}
