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
    public class IPv4Packet
    {
        public byte Version { get; } = 4;

        public byte DSF { get; set; } = 0;

        public ushort Identifiation { get; set; }

        public byte Flags { get; set; } = 0;

        public byte FragmentOffset { get; set; } = 0;

        public byte TTL { get; set; } = 128;

        public ushort HeaderChecksum { get; private set; } = 0;

        private IPAddress source;
        public IPAddress Source {
            get => source;
            set {
                if (value.AddressFamily != AddressFamily.InterNetwork)
                    throw new ArgumentException("Only IPv4 addresses are allowed!");
                source = value;
            }
        }

        private IPAddress destination;
        public IPAddress Destination {
            get => destination;
            set {
                if (value.AddressFamily != AddressFamily.InterNetwork)
                    throw new ArgumentException("Only IPv4 addresses are allowed!");
                destination = value;
            }
        }

        public ITransportPacket Payload { get; set; }

        public static IPv4Packet CreateFromRaw(ITransportPacketFactory payloadFactory, IApplicationPacketFactory applicationPacketFactory, byte[] rawPacket)
        {
            IPv4Packet packet = new IPv4Packet();
            using (var reader = new BinaryReader(new MemoryStream(rawPacket)))
            {
                byte versionAndIhl = reader.ReadByte();
                byte version = (byte) (versionAndIhl >> 4);
                byte headerLength = (byte) ((versionAndIhl & 0x0F) * 4);
                if (version != 4)
                    throw new ArgumentException("Not a IPv4 Packet!");

                packet.DSF = reader.ReadByte();
                ushort totalLength = (ushort) IPAddress.NetworkToHostOrder(reader.ReadInt16());
                packet.Identifiation = (ushort) IPAddress.NetworkToHostOrder(reader.ReadInt16());
                packet.Flags = reader.ReadByte();
                packet.FragmentOffset = reader.ReadByte();
                packet.TTL = reader.ReadByte();
                ProtocolType payloadType = (ProtocolType) reader.ReadByte();
                ushort headerChecksum = (ushort) IPAddress.NetworkToHostOrder(reader.ReadInt16());
                ushort payloadLength = (ushort) (totalLength - headerLength);

                packet.Source = new IPAddress(reader.ReadBytes(4));
                packet.Destination = new IPAddress(reader.ReadBytes(4));

                packet.Payload = payloadFactory.CreateFromRaw(payloadType, reader.ReadBytes(payloadLength), applicationPacketFactory);
            }
            return packet;
        }

        public byte[] Build()
        {
            byte headerLength = 20;

            using (var mem = new MemoryStream())
            using (var writer = new BinaryWriter(mem))
            {
                byte[] payloadData = Payload.Build();

                byte versionAndIhl = (byte) (Version << 4 | (headerLength / 4));
                writer.Write(versionAndIhl);
                writer.Write(DSF);
                writer.Write(IPAddress.HostToNetworkOrder((short) (headerLength + payloadData.Length)));
                writer.Write(IPAddress.HostToNetworkOrder((short) Identifiation));
                writer.Write(Flags);
                writer.Write(FragmentOffset);
                writer.Write(TTL);
                writer.Write((byte) Payload.Type);
                writer.Write(IPAddress.HostToNetworkOrder((short) HeaderChecksum));

                writer.Write(source.GetAddressBytes());
                writer.Write(destination.GetAddressBytes());

                writer.Write(payloadData);

                return mem.ToArray();
            }
        }

        public void Send(Socket rawSocket)
        {
            rawSocket.SendTo(Build(), new IPEndPoint(IPAddress.None, 0));
        }

        public static IPv4Packet Recieve(Socket rawSocket, ITransportPacketFactory transportPacketFactory, IApplicationPacketFactory applicationPacketFactory)
        {
            try
            {
                byte[] buffer = new byte[10240];
                EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Broadcast, 0);
                int length = rawSocket.ReceiveFrom(buffer, ref remoteEndPoint);

                return CreateFromRaw(transportPacketFactory, applicationPacketFactory, buffer);
            }
            catch (SocketException)
            {
                return null;
            }
        }
    }
}
