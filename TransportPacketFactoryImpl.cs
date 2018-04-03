using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DhcpClientNET
{
    public class TransportPacketFactoryImpl : ITransportPacketFactory
    {
        public ITransportPacket CreateFromRaw(ProtocolType type, byte[] rawPacket, IApplicationPacketFactory applicationPacketFactory)
        {
            switch (type)
            {
                case ProtocolType.Udp:
                    return UdpPacket.CreateFromRaw(applicationPacketFactory, rawPacket);
                default:
                    throw new NotSupportedException("Protocol type not supported!");
            }
        }
    }
}
