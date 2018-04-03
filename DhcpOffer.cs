using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DhcpClientNET.Dhcp;

namespace DhcpClientNET
{
    public class DhcpOffer
    {
        public IPAddress RelayAgnetAddress { get; set; }
        public IPAddress ClientAddress { get; set; }
        public IPAddress SubnetMask { get; set; }
        public IPAddress Gateway { get; set; }
        public IPAddress DnsServer { get; set; }
        public string Domain { get; set; }
        public IPAddress DhcpServer { get; set; }

        public static DhcpOffer FromDhcpPacket(DhcpPacket packet)
        {
            if (packet.MessageType != DhcpMessageType.Offer)
                return null;

            return new DhcpOffer
            {
                DhcpServer = packet.GetOption<DhcpServerIdentifierOption>().ServerAddress,
                ClientAddress = packet.YourClientAddress,
                Gateway = packet.GetOption<DhcpRouterOption>()?.Routers[0],
                DnsServer = packet.GetOption<DhcpDomainNameServerOption>()?.DomainNameServers[0],
                Domain = packet.GetOption<DhcpDomainNameOption>()?.DomainName,
                SubnetMask = packet.GetOption<DhcpSubnetMaskOption>()?.Mask,
                RelayAgnetAddress = packet.RelayAgentAddress
            };
        }

        protected bool Equals(DhcpOffer other)
        {
            return Equals(DhcpServer, other.DhcpServer);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DhcpOffer) obj);
        }

        public override int GetHashCode()
        {
            return (DhcpServer != null ? DhcpServer.GetHashCode() : 0);
        }
    }
}
