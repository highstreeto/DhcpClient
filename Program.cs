using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using DhcpClientNET.Dhcp;

namespace DhcpClientNET
{
    class Program
    {
        static void Main(string[] args)
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var item in interfaces.Select((i, idx) => new { nr = idx + 1, inter = i }))
            {
                Console.WriteLine($"-{item.nr}- {item.inter.Name}");
            }
            Console.Write("> Choose a interface: ");
            int selection = int.Parse(Console.ReadLine());
            var mainInterface = interfaces[selection - 1];

            var random = new Random();
            ushort packetId = (ushort) random.Next();
            uint dhcpTransactionId = (uint) random.Next();

            ITransportPacketFactory transportPacketFactory = new TransportPacketFactoryImpl();
            IApplicationPacketFactory applicationPacketFactory = new ApplicationPacketFactoryImpl();

            var dhcpDiscover = BuildDhcpDiscoverMessage(mainInterface, packetId, dhcpTransactionId);

            bool running = true;
            bool rcvAck = false;
            bool rebinding = false;
            DhcpOffer selectedOffer = null;
            var socket = SetupPromiscousSocket(mainInterface);

            Console.WriteLine("Send DHCP Discover...");
            dhcpDiscover.Send(socket);

            ISet<DhcpOffer> offers = new HashSet<DhcpOffer>();
            var recieverThread = new Thread(() =>
            {
                while (running)
                {
                    IPv4Packet rcvPacket = IPv4Packet.Recieve(socket, transportPacketFactory, applicationPacketFactory);

                    if (rcvPacket != null && rcvPacket.Payload is UdpPacket udpPacket)
                    {
                        if (udpPacket.Payload is DhcpPacket responseDhcpPacket)
                        {
                            switch (responseDhcpPacket.MessageType)
                            {
                                case DhcpMessageType.Offer:
                                    DhcpOffer offer = DhcpOffer.FromDhcpPacket(responseDhcpPacket);
                                    offers.Remove(offer); // remove old offer from server if necassary
                                    offers.Add(offer);
                                    Console.WriteLine($"New Offer from {rcvPacket.Source}");
                                    break;
                                case DhcpMessageType.Ack:
                                    if (!rcvAck && !rebinding)
                                    {
                                        Console.WriteLine("OK from server - ready to launch!");
                                        ProcessStartInfo netshStart = new ProcessStartInfo()
                                        {
                                            FileName = "netsh",
                                            Arguments = $"interface ipv4 set address name=\"{mainInterface.Name}\" static" +
                                                        $" {selectedOffer.ClientAddress} {selectedOffer.SubnetMask} {selectedOffer.Gateway} 1",
                                            UseShellExecute = false
                                        };

                                        Process netshProc = Process.Start(netshStart);
                                        netshProc.WaitForExit();
                                        Console.WriteLine($"netsh exit code - {netshProc.ExitCode}");

                                        rebinding = true;

                                        System.Timers.Timer rebindTimer = new System.Timers.Timer(
                                            TimeSpan.FromSeconds(30).TotalMilliseconds
                                        );
                                        rebindTimer.Elapsed += (sender, eventArgs) =>
                                        {
                                            Console.WriteLine("Send DHCP rebind");

                                            dhcpTransactionId = (uint) random.Next();
                                            IPv4Packet dhcpRebind = BuildDhcpRebindMessage(selectedOffer, mainInterface, ++packetId, dhcpTransactionId);
                                            dhcpRebind.Send(socket);
                                        };
                                        Console.WriteLine("Starting rebind timer");
                                        rebindTimer.Start();
                                    }
                                    else if (rebinding)
                                    {
                                        Console.WriteLine("Re-OK from server - ready to launch!");
                                    }
                                    rcvAck = true;
                                    break;
                            }
                        }
                    }
                }
            });
            recieverThread.Start();

            Thread.Sleep(500);
            if (offers.Count == 0)
            {
                Console.WriteLine("Resending DHCP Discover...");
                dhcpDiscover.Send(socket);
            }

            Console.WriteLine("> Press any key to stop adding new offers\n");
            Console.ReadKey();

            Console.WriteLine();
            Console.WriteLine("-- Select DHCP server offer --");
            DhcpOffer[] finalOffers = offers.ToArray();
            foreach (var item in finalOffers.Select((o, idx) => new { nr = idx + 1, offer = o }))
            {
                Console.WriteLine($"-{item.nr}- Offer from {item.offer.DhcpServer}");
                Console.WriteLine($"   Client address: {item.offer.ClientAddress}");
                Console.WriteLine($"   Gateway address: {item.offer.Gateway}");
                Console.WriteLine($"   DNS server: {item.offer.DnsServer}");
                Console.WriteLine($"   Domain: {item.offer.Domain}");
            }

            Console.Write("> Choose a offer: ");
            selection = int.Parse(Console.ReadLine());
            selectedOffer = finalOffers[selection - 1];

            Console.WriteLine("Send DHCP Request");
            IPv4Packet dhcpRequest = BuildDhcpRequestMessage(selectedOffer, mainInterface, ++packetId, dhcpTransactionId);
            dhcpRequest.Send(socket);

            Console.WriteLine("> Press any key to close");
            Console.ReadKey();
            running = false;
            socket.Close();
        }

        private static IPv4Packet BuildDhcpDiscoverMessage(NetworkInterface inter, ushort packetId, uint dhcpTransactionId)
        {
            var hardwareAddress = new HardwareAddress(HardwareAddressType.Ethernet,
                inter.GetPhysicalAddress().GetAddressBytes());

            var dhcpPacket = new DhcpPacket();
            dhcpPacket.Operation = DhcpOperation.Request;
            dhcpPacket.MessageType = DhcpMessageType.Discover;
            dhcpPacket.TransactionId = dhcpTransactionId;
            dhcpPacket.HardwareAddress = hardwareAddress;

            var dhcpUdpPacket = new UdpPacket();
            dhcpUdpPacket.SourcePort = 68;
            dhcpUdpPacket.DestinationPort = 67;
            dhcpUdpPacket.Payload = dhcpPacket;

            var dhcpIpPacket = new IPv4Packet();
            dhcpIpPacket.Source = IPAddress.Any;
            dhcpIpPacket.Destination = IPAddress.Broadcast;
            dhcpIpPacket.Identifiation = packetId;
            dhcpIpPacket.Payload = dhcpUdpPacket;
            return dhcpIpPacket;
        }

        private static IPv4Packet BuildDhcpRequestMessage(DhcpOffer offer, NetworkInterface inter, ushort packetId, uint dhcpTransactionId)
        {
            var hardwareAddress = new HardwareAddress(HardwareAddressType.Ethernet,
                inter.GetPhysicalAddress().GetAddressBytes());

            var dhcpPacket = new DhcpPacket();
            dhcpPacket.Operation = DhcpOperation.Request;
            dhcpPacket.MessageType = DhcpMessageType.Request;
            dhcpPacket.TransactionId = dhcpTransactionId;
            dhcpPacket.HardwareAddress = hardwareAddress;

            dhcpPacket.Options.Add(new DhcpServerIdentifierOption(offer.DhcpServer));
            dhcpPacket.Options.Add(new DhcpClientIdentifier(hardwareAddress));
            dhcpPacket.Options.Add(new DhcpRequestedIpAddressOption(offer.ClientAddress));

            var dhcpUdpPacket = new UdpPacket();
            dhcpUdpPacket.SourcePort = 68;
            dhcpUdpPacket.DestinationPort = 67;
            dhcpUdpPacket.Payload = dhcpPacket;

            var dhcpIpPacket = new IPv4Packet();
            dhcpIpPacket.Source = IPAddress.Any;
            dhcpIpPacket.Destination = IPAddress.Broadcast;
            dhcpIpPacket.Identifiation = packetId;
            dhcpIpPacket.Payload = dhcpUdpPacket;

            return dhcpIpPacket;
        }

        private static IPv4Packet BuildDhcpRebindMessage(DhcpOffer offer, NetworkInterface inter, ushort packetId, uint dhcpTransactionId)
        {
            var hardwareAddress = new HardwareAddress(HardwareAddressType.Ethernet,
                inter.GetPhysicalAddress().GetAddressBytes());

            var dhcpPacket = new DhcpPacket();
            dhcpPacket.Operation = DhcpOperation.Request;
            dhcpPacket.MessageType = DhcpMessageType.Request;
            dhcpPacket.TransactionId = dhcpTransactionId;
            dhcpPacket.HardwareAddress = hardwareAddress;
            dhcpPacket.ClientAddress = offer.ClientAddress;

            dhcpPacket.Options.Add(new DhcpClientIdentifier(hardwareAddress));

            var dhcpUdpPacket = new UdpPacket();
            dhcpUdpPacket.SourcePort = 68;
            dhcpUdpPacket.DestinationPort = 67;
            dhcpUdpPacket.Payload = dhcpPacket;

            var dhcpIpPacket = new IPv4Packet();
            dhcpIpPacket.Source = offer.ClientAddress;
            dhcpIpPacket.Destination = offer.DhcpServer;
            dhcpIpPacket.Identifiation = packetId;
            dhcpIpPacket.Payload = dhcpUdpPacket;

            return dhcpIpPacket;
        }

        private static Socket SetupPromiscousSocket(NetworkInterface inter)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Udp);
            IPAddress listeningAddress = inter.GetIPProperties().UnicastAddresses
                .Where(info => info.Address.AddressFamily == AddressFamily.InterNetwork)
                .Select(info => info.Address)
                .First();
            Console.WriteLine($" Binding to address {listeningAddress}");
            socket.Bind(new IPEndPoint(listeningAddress, 0));
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, 1);

            byte[] byOn = BitConverter.GetBytes(1);
            socket.IOControl(IOControlCode.ReceiveAll, byOn, BitConverter.GetBytes(0));
            socket.EnableBroadcast = true;
            return socket;
        }
    }
}
