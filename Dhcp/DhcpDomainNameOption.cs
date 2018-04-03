using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhcpClientNET.Dhcp
{
    public class DhcpDomainNameOption : DhcpOption
    {
        public DhcpDomainNameOption(string domainName)
        {
            DomainName = domainName;
        }

        public string DomainName { get; set; }

        public override DhcpOptionType Type => DhcpOptionType.DomainName;

        public static DhcpDomainNameOption CreateFromRaw(byte[] rawData)
        {
            return new DhcpDomainNameOption(Encoding.ASCII.GetString(rawData));
        }

        public override byte[] Build()
        {
            return Encoding.ASCII.GetBytes(DomainName);
        }
    }
}