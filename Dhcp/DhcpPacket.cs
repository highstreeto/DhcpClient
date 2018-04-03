using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DhcpClientNET.Dhcp;

namespace DhcpClientNET
{
    public class DhcpPacket : IApplicationPacket
    {
        public const int MagicCookie = 0x63825363;

        public DhcpOperation Operation { get; set; }
        public HardwareAddress HardwareAddress { get; set; }
        public byte Hops { get; set; } = 0;
        public uint TransactionId { get; set; }
        public ushort SecondsElapsed { get; set; } = 0;
        public ushort Flags { get; set; } = 0;
        public IPAddress ClientAddress { get; set; } = new IPAddress(0);
        public IPAddress YourClientAddress { get; set; } = new IPAddress(0);
        public IPAddress NextServerAddress { get; set; } = new IPAddress(0);
        public IPAddress RelayAgentAddress { get; set; } = new IPAddress(0);
        public String ServerName { get; set; } = "";
        public String BootFileName { get; set; } = "";
        public DhcpMessageType MessageType { get; set; }
        public IList<DhcpOption> Options { get; } = new List<DhcpOption>();

        public T GetOption<T>() where T : DhcpOption
        {
            return Options.OfType<T>().SingleOrDefault();
        }

        public static DhcpPacket CreateFromRaw(byte[] rawPacket)
        {
            DhcpPacket packet = new DhcpPacket();
            using (var reader = new BinaryReader(new MemoryStream(rawPacket)))
            {
                packet.MessageType = (DhcpMessageType) reader.ReadByte();
                HardwareAddressType hardwareAddressType = (HardwareAddressType) reader.ReadByte();
                byte hardwareAddressLength = reader.ReadByte();
                packet.Hops = reader.ReadByte();
                packet.TransactionId = (uint) IPAddress.NetworkToHostOrder(reader.ReadInt32());
                packet.SecondsElapsed = (ushort) IPAddress.NetworkToHostOrder(reader.ReadInt16());
                packet.Flags = (ushort) IPAddress.NetworkToHostOrder(reader.ReadInt16());
                packet.ClientAddress = new IPAddress(reader.ReadBytes(4));
                packet.YourClientAddress = new IPAddress(reader.ReadBytes(4));
                packet.NextServerAddress = new IPAddress(reader.ReadBytes(4));
                packet.RelayAgentAddress = new IPAddress(reader.ReadBytes(4));
                packet.HardwareAddress = new HardwareAddress(hardwareAddressType,
                    reader.ReadBytes(hardwareAddressLength));
                reader.ReadBytes(16 - hardwareAddressLength); // padding
                packet.ServerName = Encoding.ASCII.GetString(reader.ReadBytes(64));
                packet.BootFileName = Encoding.ASCII.GetString(reader.ReadBytes(128));

                int packetMagicCookie = IPAddress.NetworkToHostOrder(reader.ReadInt32());
                if (packetMagicCookie != MagicCookie)
                    throw new InvalidDataException("Magic cookie not found!");

                byte messageType = reader.ReadByte();
                if (messageType != 53)
                    throw new InvalidDataException("Options should start with Message type!");
                reader.ReadByte(); // length
                packet.MessageType = (DhcpMessageType) reader.ReadByte();

                var optionFactory = new DhcpOptionFactory();
                byte optionType = reader.ReadByte();
                while (optionType != 255)
                {
                    DhcpOptionType type = (DhcpOptionType) optionType;
                    byte length = reader.ReadByte();
                    byte[] optionData = reader.ReadBytes(length);

                    DhcpOption option = optionFactory.CreateFromRaw(type, optionData);
                    if (option != null)
                        packet.Options.Add(option);
                    else
                        Console.WriteLine($"WARN: Unsupported option type: {type}");
                    optionType = reader.ReadByte();
                }
            }
            return packet;
        }

        public byte[] Build()
        {
            using (var mem = new MemoryStream())
            using (var writer = new BinaryWriter(mem))
            {
                writer.Write((byte) Operation);
                writer.Write((byte) HardwareAddress.Type);
                writer.Write((byte) HardwareAddress.Address.Length);
                writer.Write(Hops);
                writer.Write(IPAddress.HostToNetworkOrder((int) TransactionId));
                writer.Write(IPAddress.HostToNetworkOrder((short) SecondsElapsed));
                writer.Write(IPAddress.HostToNetworkOrder((short) Flags));
                writer.Write(ClientAddress.GetAddressBytes());
                writer.Write(YourClientAddress.GetAddressBytes());
                writer.Write(NextServerAddress.GetAddressBytes());
                writer.Write(RelayAgentAddress.GetAddressBytes());
                writer.Write(HardwareAddress.Address);
                for (int i = 0; i < 16 - HardwareAddress.Address.Length; i++)
                {
                    writer.Write((byte) 0);
                }

                byte[] nameBytes = Encoding.ASCII.GetBytes(ServerName);
                if (nameBytes.Length > 63)
                {
                    nameBytes[63] = 0;
                }
                writer.Write(nameBytes);
                for (int i = 0; i < 64 - nameBytes.Length; i++)
                {
                    writer.Write((byte) 0);
                }

                byte[] fileBytes = Encoding.ASCII.GetBytes(ServerName);
                if (fileBytes.Length > 127)
                {
                    fileBytes[127] = 0;
                }
                writer.Write(fileBytes);
                for (int i = 0; i < 128 - fileBytes.Length; i++)
                {
                    writer.Write((byte) 0);
                }

                writer.Write(IPAddress.HostToNetworkOrder(MagicCookie));

                writer.Write((byte) DhcpOptionType.MessageType); // Message Type option
                writer.Write((byte) 1); // length
                writer.Write((byte) MessageType);

                foreach (var option in Options)
                {
                    writer.Write((byte) option.Type);
                    byte[] data = option.Build();
                    writer.Write((byte) data.Length);
                    writer.Write(data);
                }

                writer.Write((byte) DhcpOptionType.End); // END option

                return mem.ToArray();
            }
        }
    }

    public enum DhcpOperation : byte
    {
        Request = 1,
        Response = 2
    }

    public enum DhcpMessageType : byte
    {
        Discover = 1,
        Offer = 2,
        Request = 3,
        Decline = 4,
        Ack = 5,
        Nak = 6,
        Release = 7,
        Inform = 8
    }
}
