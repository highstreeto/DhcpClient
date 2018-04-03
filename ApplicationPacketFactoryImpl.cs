using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhcpClientNET
{
    public class ApplicationPacketFactoryImpl : IApplicationPacketFactory
    {
        public IApplicationPacket CreateFromRaw(ushort port, byte[] rawPacket)
        {
            switch (port)
            {
                case 68:
                    return DhcpPacket.CreateFromRaw(rawPacket);
                default:
                    return new UnknownPayload(rawPacket);
            }
        }
    }

    public class UnknownPayload : IApplicationPacket
    {
        public UnknownPayload(byte[] data)
        {
            Data = data;
        }

        public byte[] Data { get; }

        public byte[] Build() => Data;
    }
}
