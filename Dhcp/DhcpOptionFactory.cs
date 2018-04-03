using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhcpClientNET.Dhcp
{
    public class DhcpOptionFactory
    {
        public DhcpOption CreateFromRaw(DhcpOptionType type, byte[] rawData)
        {
            switch (type)
            {
                case DhcpOptionType.SubnetMask:
                    return DhcpSubnetMaskOption.CreateFromRaw(rawData);
                case DhcpOptionType.Router:
                    return DhcpRouterOption.CreateFromRaw(rawData);
                case DhcpOptionType.DomainNameServer:
                    return DhcpDomainNameServerOption.CreateFromRaw(rawData);
                case DhcpOptionType.DomainName:
                    return DhcpDomainNameOption.CreateFromRaw(rawData);
                case DhcpOptionType.ServerIdentifier:
                    return DhcpServerIdentifierOption.CreateFromRaw(rawData);
                case DhcpOptionType.ClientIdentifier:
                    return DhcpClientIdentifier.CreateFromRaw(rawData);
                default:
                    return null;
            }
        }
    }
}
