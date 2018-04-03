using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DhcpClientNET.Dhcp
{
    public class DhcpServerIdentifierOption : DhcpOption
    {
        public DhcpServerIdentifierOption(IPAddress serverAddress)
        {
            this.serverAddress = serverAddress;
        }

        private IPAddress serverAddress;
        public IPAddress ServerAddress {
            get => serverAddress;
            set
            {
                if (value.AddressFamily != AddressFamily.InterNetwork)
                    throw new ArgumentException("Server identifier must be IPv4 address!");
                serverAddress = value;
            }
        }

        public override DhcpOptionType Type => DhcpOptionType.ServerIdentifier;

        public static DhcpServerIdentifierOption CreateFromRaw(byte[] rawData)
        {
            return new DhcpServerIdentifierOption(new IPAddress(rawData));
        }

        public override byte[] Build()
        {
            return ServerAddress.GetAddressBytes();
        }
    }
}
