using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhcpClientNET
{
    public interface IApplicationPacketFactory
    {
        IApplicationPacket CreateFromRaw(ushort port, byte[] rawPacket);
    }
}
