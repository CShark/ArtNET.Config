using System.Text;
using Haukcode.ArtNet.Packets;
using Haukcode.ArtNet.Sockets;

namespace ArtNet.Config.ArtNet {
    internal class DeviceInfo {
        private List<ArtPollReplyPacket> _replies = new();
        private ArtNetSocket _netif;

        public DeviceInfo(ArtPollReplyPacket reply, ArtNetSocket netif) {
            _replies.Add(reply);
            _netif = netif;

            var lines = reply.NodeReport.Split('\n');
            if (lines.Any(x => x.StartsWith("ID:"))) {
                ID = Encoding.ASCII.GetBytes(lines.First(x => x.StartsWith("ID:")).Substring(3));
            }
        }

        public void AppendReply(ArtPollReplyPacket reply) {
            if (_replies.Any(x => x.BindIndex == reply.BindIndex)) {
                _replies.RemoveAll(x => x.BindIndex == reply.BindIndex);
            }
            _replies.Add(reply);
            _replies = _replies.OrderBy(x => x.BindIndex).ToList();
        }

        public byte[] ID { get; private set; } = Array.Empty<byte>();

        public SerialInterface? Serial { get; set; }

        public bool BrokenLink => !IpAddress.Zip(_netif.LocalSubnetMask.GetAddressBytes(), (x, y) => x & y).SequenceEqual(_netif.LocalIP.GetAddressBytes().Zip(_netif.LocalSubnetMask.GetAddressBytes(), (x, y) => x & y));

        public ArtNetSocket NetIf => _netif;
        public int Ports => _replies.Sum(x => x.PortCount);
        public byte[] IpAddress => _replies[0].IpAddress;
        public byte[] MacAddress => _replies[0].MacAddress;

        public IReadOnlyList<ArtPollReplyPacket> PortReplies => _replies.AsReadOnly();

        public byte Status2 => _replies[0].Status2;
        public byte Status1 => (byte)_replies[0].Status;
        public byte Status3 => _replies[0].Status3;

        public string IpAddressStr() => $"{IpAddress[0]}.{IpAddress[1]}.{IpAddress[2]}.{IpAddress[3]}";

        public string MacAddressStr() =>
            $"{MacAddress[0]:X2}:{MacAddress[1]:X2}:{MacAddress[2]:X2}:{MacAddress[3]:X2}:{MacAddress[4]:X2}:{MacAddress[5]:X2}";
    }
}