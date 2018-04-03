using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhcpClientNET.Dhcp
{
    public class DhcpClientIdentifier : DhcpOption
    {
        public DhcpClientIdentifier(HardwareAddress clientHardwareAddress)
        {
            ClientHardwareAddress = clientHardwareAddress;
        }

        public HardwareAddress ClientHardwareAddress { get; set; }

        public override DhcpOptionType Type => DhcpOptionType.ClientIdentifier;

        public static DhcpClientIdentifier CreateFromRaw(byte[] rawData)
        {
            var address = new HardwareAddress((HardwareAddressType) rawData[0],
                rawData.Skip(1).ToArray());
            return new DhcpClientIdentifier(address);
        }

        public override byte[] Build()
        {
            return new List<byte> { (byte) ClientHardwareAddress.Type }
                .Concat(ClientHardwareAddress.Address)
                .ToArray();
        }
    }
}
