using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DhcpClientNET.Dhcp
{
    public class DhcpSubnetMaskOption : DhcpOption
    {
        public DhcpSubnetMaskOption(IPAddress mask)
        {
            Mask = mask;
        }

        private IPAddress mask;
        public IPAddress Mask {
            get => mask;
            set {
                if (value.AddressFamily != AddressFamily.InterNetwork)
                    throw new ArgumentException("Mask must be IPv4!");
                mask = value;
            }
        }

        public override DhcpOptionType Type => DhcpOptionType.SubnetMask;

        public static DhcpSubnetMaskOption CreateFromRaw(byte[] rawData)
        {
            return new DhcpSubnetMaskOption(new IPAddress(rawData));
        }

        public override byte[] Build()
        {
            return Mask.GetAddressBytes();
        }
    }
}