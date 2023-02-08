using System.IO.Ports;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using ArtNet.Config.ArtNet;
using Haukcode.ArtNet;
using Haukcode.ArtNet.Packets;
using Haukcode.ArtNet.Sockets;
using Haukcode.Sockets;

namespace ArtNet.Config.Pages {
    internal class DeviceEnumeration : IPage {
        private List<ArtNetSocket> _sockets = new();
        private List<DeviceInfo> _devices = new();

        private void EnumerateDevices() {
            foreach (var socket in _sockets) {
                socket.Close();
            }

            _sockets.Clear();
            _devices.Clear();

            var nics = NetworkInterface.GetAllNetworkInterfaces().Where(x =>
                x.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                x.NetworkInterfaceType == NetworkInterfaceType.Wireless80211).ToArray();
            foreach (var nic in nics) {
                var addr = nic.GetIPProperties().UnicastAddresses
                    .FirstOrDefault(x => x.Address.AddressFamily == AddressFamily.InterNetwork);

                if (addr != null) {
                    try {
                        var soc = new ArtNetSocket {
                            EnableBroadcast = true
                        };
                        soc.NewPacket += (sender, args) => { ArtNetOnNewPacket(sender, args, soc); };
                        soc.Open(addr.Address, addr.IPv4Mask);

                        soc.Send(new ArtPollPacket(), new RdmEndPoint(IPAddress.Broadcast));

                        _sockets.Add(soc);
                    } catch (Exception ex) {

                    }
                }
            }

            // find management ports
            Task.Run(async () => {
                foreach (var port in SerialPort.GetPortNames()) {
                    var seriface = new SerialInterface(port);
                    if (await seriface.TryConnect()) {
                        var target = _devices.FirstOrDefault(x => x.ID.SequenceEqual(seriface.DeviceID));

                        if (target != null) {
                            target.Serial = seriface;
                            ActivePage.Refresh();
                        }
                    }
                }
            });
        }
        private void ArtNetOnNewPacket(object? sender, NewPacketEventArgs<ArtNetPacket> e, ArtNetSocket netif) {
            if (e.Packet.OpCode == ArtNetOpCodes.PollReply) {
                var reply = (ArtPollReplyPacket)e.Packet;

                var root = _devices.FirstOrDefault(x => Enumerable.SequenceEqual(x.MacAddress, reply.MacAddress));

                if (root == null) {
                    _devices.Add(new DeviceInfo(reply, netif));
                } else {
                    root.AppendReply(reply);
                }

                _devices = _devices.OrderBy(x => x.PortReplies[0].ShortName).ToList();

                ActivePage.Refresh();
            }
        }

        public override bool AllowRefresh => true;

        protected override IPage? HandleInputInternal(string? input) {
            LastErr = "";
            if (input != null) {
                if (input.ToLower() == "r") {
                    EnumerateDevices();
                    return this;
                } else if (int.TryParse(input, out var idx)) {
                    if (idx >= 0 && idx < _devices.Count) {
                        return new DeviceInfoScreen(this, _devices[idx]);
                    } else {
                        LastErr = $"The device {idx} does not exist";
                    }
                } else {
                    LastErr = $"The command \"{input}\" was not recognized";
                }
            }

            return this;
        }

        public override async Task RenderInternal(object? state) {
            Console.WriteLine("ArtNet Overview");
            Console.WriteLine();
            Console.WriteLine(
                $"{"Nr",-3} {"Short Name",-20} | {"Ip Address",-15} {"Ports",-5} | {"Squawk!",-7}");
            Console.WriteLine(new string('-', 100));

            for (var i = 0; i < _devices.Count; i++) {
                var dev = _devices[i];

                if (dev.BrokenLink) {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                }

                Console.WriteLine(
                    $"{i,-3} {dev.PortReplies[0].ShortName,-20} | {$"{dev.IpAddress[0]}.{dev.IpAddress[1]}.{dev.IpAddress[2]}.{dev.IpAddress[3]}",-15} {dev.Ports,-5} | {((dev.PortReplies.Any(x => (x.Status2 & 0x20) != 0)) ? "  ■■■  " : ""),-7}");

                Console.ResetColor();
            }

            Console.WriteLine(new string('-', 100));
            Console.WriteLine();
            Console.WriteLine("[<n>]:  Show device info");
            Console.WriteLine("[r]:    Refresh list");
        }

        public DeviceEnumeration(IPage parent) : base(parent) {
            EnumerateDevices();
        }
    }
}