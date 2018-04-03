using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhcpClientNET
{
    public class HardwareAddress
    {
        public HardwareAddress(HardwareAddressType type, byte[] address)
        {
            Type = type;
            Address = address;
        }

        public HardwareAddressType Type { get; }
        public byte[] Address { get; }
    }

    public enum HardwareAddressType
    {
        Ethernet = 1
    }
}
