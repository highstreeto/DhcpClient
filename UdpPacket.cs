using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DhcpClientNET
{
    public class UdpPacket : ITransportPacket
    {
        public ProtocolType Type => ProtocolType.Udp;

        public ushort SourcePort { get; set; }

        public ushort DestinationPort { get; set; }

        public IApplicationPacket Payload { get; set; }

        public static UdpPacket CreateFromRaw(IApplicationPacketFactory applicationPacketFactory, byte[] rawPacket)
        {
            UdpPacket packet = new UdpPacket();
            using (var reader = new BinaryReader(new MemoryStream(rawPacket)))
            {
                packet.SourcePort = (ushort) IPAddress.NetworkToHostOrder(reader.ReadInt16());
                packet.DestinationPort = (ushort) IPAddress.NetworkToHostOrder(reader.ReadInt16());
                ushort totalLength = (ushort) IPAddress.NetworkToHostOrder(reader.ReadInt16());
                ushort checksum = (ushort) IPAddress.NetworkToHostOrder(reader.ReadInt16());
                ushort payloadLength = (ushort) (totalLength - 8);
                byte[] payloadData = reader.ReadBytes(payloadLength);

                packet.Payload = applicationPacketFactory.CreateFromRaw(packet.DestinationPort, payloadData);
            }
            return packet;
        }

        public byte[] Build()
        {
            byte headerLength = 8;

            using (var mem = new MemoryStream())
            using (var writer = new BinaryWriter(mem))
            {
                byte[] payloadData = Payload.Build();

                writer.Write(IPAddress.HostToNetworkOrder((short) SourcePort)); // source port
                writer.Write(IPAddress.HostToNetworkOrder((short) DestinationPort)); // destination port
                writer.Write(IPAddress.HostToNetworkOrder((short) (headerLength + payloadData.Length))); // length
                writer.Write((short) 0); // checksum

                writer.Write(payloadData);

                return mem.ToArray();
            }
        }
    }
}
