using System.Net;
using ArtNet.Config.ArtNet;
using Haukcode.ArtNet;
using Haukcode.ArtNet.Packets;
using Haukcode.Sockets;

namespace ArtNet.Config.Pages {
    internal class EditDeviceIp : IPage {
        private readonly DeviceInfo _device;
        private ArtIpProgReplyPacket? _ipConfig;

        private bool _dhcp;
        private bool _reset;
        private byte[]? _ip;
        private byte[]? _netmask;
        private byte[]? _gateway;

        public EditDeviceIp(IPage parent, DeviceInfo device) : base(parent) {
            _device = device;
            if (_ipConfig == null) {
                _device.NetIf.Send(new ArtIpProgPacket(), new RdmEndPoint(new IPAddress(_device.IpAddress)));
            }
        }

        public override bool AllowRefresh => false;

        protected override IPage? HandleInputInternal(string? input) {
            if (input == "yes" || input == "y") {
                var prog = new ArtIpProgPacket();
                prog.Command = ArtIpProgCommand.EnableProgramming;
                if (_reset) {
                    prog.Command |= ArtIpProgCommand.ResetToDefault;
                }else if (_dhcp) {
                    prog.Command |= ArtIpProgCommand.EnableDHCP;
                } else {
                    if (_ip != null) {
                        prog.Command |= ArtIpProgCommand.IpAddress;
                        prog.IpAddress = _ip;
                    }

                    if (_netmask != null) {
                        prog.Command |= ArtIpProgCommand.Netmask;
                        prog.Netmask = _netmask;
                    }

                    if (_gateway != null) {
                        prog.Command |= ArtIpProgCommand.DefaultGateway;
                        prog.DefaultGateway = _gateway;
                    }
                }

                _device.NetIf.Send(prog, new RdmEndPoint(new IPAddress(_device.IpAddress)));
                _device.NetIf.Send(new ArtPollPacket(), new RdmEndPoint(IPAddress.Broadcast));
            }

            return Parent;
        }

        public override async Task RenderInternal(object? state) {
            if (state is ArtIpProgReplyPacket reply) {
                _ipConfig = reply;
            }

            Console.WriteLine("ArtNet Device — Change IP Configuration");
            Console.WriteLine($"{_device.IpAddressStr()} — {_device.MacAddressStr()}");
            Console.WriteLine();

            if (_ipConfig == null) {
                WriteError("Device does not support IP-Programming");
            } else {
                Console.WriteLine($"{"Mode:",-20}{((_device.Status2 & 0x02) != 0 ? "DHCP" : "Manual")}");
                Console.WriteLine($"{"IP-Address:",-20}{_ipConfig.IpAddress[0]}.{_ipConfig.IpAddress[1]}.{_ipConfig.IpAddress[2]}.{_ipConfig.IpAddress[3]}");
                Console.WriteLine($"{"Netmask:",-20}{_ipConfig.Netmask[0]}.{_ipConfig.Netmask[1]}.{_ipConfig.Netmask[2]}.{_ipConfig.Netmask[3]}");
                Console.WriteLine($"{"Default Gateway:",-20}{_ipConfig.DefaultGateway[0]}.{_ipConfig.DefaultGateway[1]}.{_ipConfig.DefaultGateway[2]}.{_ipConfig.DefaultGateway[3]}");
                Console.WriteLine();

                Console.WriteLine();
                Console.WriteLine("Enable DHCP? [yes/no]");

                var dhcpIn = await ReadInput();
                if (dhcpIn == "yes" || dhcpIn == "y") {
                    Console.WriteLine();
                    _dhcp = true;
                } else {
                    Console.WriteLine();
                    Console.WriteLine("Set new IP-Address or leave empty for no change:");
                    var ipv4 = await ReadInput();

                    Console.WriteLine();
                    Console.WriteLine("Set new Netmask or leave empty for no change:");
                    var netmask = await ReadInput();

                    Console.WriteLine();
                    Console.WriteLine("Set new Default Gateway or leave empty for no change:");
                    var gateway = await ReadInput();

                    try {
                        _ip = ipv4.Split('.').Select(byte.Parse).ToArray();
                    } catch (Exception ex) { }

                    try {
                        _netmask = netmask.Split('.').Select(byte.Parse).ToArray();
                    } catch (Exception ex) { }

                    try {
                        _gateway = gateway.Split('.').Select(byte.Parse).ToArray();
                    } catch (Exception ex) { }

                    if(_ip?.Length != 4) _ip = null;
                    if(_netmask?.Length != 4) _netmask = null;
                    if(_gateway?.Length != 4) _gateway = null;

                    Console.WriteLine();

                    if (_ip == null && _netmask == null && _gateway == null) {
                        Console.WriteLine("No changes were made. Do you want to reset the device? [yes/no]");
                        var reset = await ReadInput();

                        if (reset == "yes" || reset == "y") {
                            _reset = true;
                        }
                    }

                    
                }

                Console.WriteLine("New Settings:");
                if (_reset) {
                    Console.WriteLine($"{"Mode:",-20}<Reset>");
                    Console.WriteLine($"{"IP-Address:",-20}<Reset>");
                    Console.WriteLine($"{"Netmask:",-20}<Reset>");
                    Console.WriteLine($"{"Default Gateway:",-20}<Reset>");
                } else if (_dhcp) {
                    Console.WriteLine($"{"Mode:",-20}DHCP");
                    Console.WriteLine($"{"IP-Address:",-20}<DHCP>");
                    Console.WriteLine($"{"Netmask:",-20}<DHCP>");
                    Console.WriteLine($"{"Default Gateway:",-20}<DHCP>");
                } else {
                    Console.WriteLine($"{"Mode:",-20}Manual");
                    if (_ip != null) {
                        Console.WriteLine($"{"IP-Address:",-20}{_ip[0]}.{_ip[1]}.{_ip[2]}.{_ip[3]}");
                    } else {
                        Console.WriteLine($"{"IP-Address:",-20}<unchanged>");
                    }

                    if (_netmask != null) {
                        Console.WriteLine($"{"Netmask:",-20}{_netmask[0]}.{_netmask[1]}.{_netmask[2]}.{_netmask[3]}");
                    } else {
                        Console.WriteLine($"{"Netmask:",-20}<unchanged>");
                    }

                    if (_gateway != null) {
                        Console.WriteLine($"{"Gateway:",-20}{_gateway[0]}.{_gateway[1]}.{_gateway[2]}.{_gateway[3]}");
                    } else {
                        Console.WriteLine($"{"Gateway:",-20}<unchanged>");
                    }
                }
                Console.WriteLine("Confirm? [yes/no]");
            }
        }

        public override void EnterPage() {
            _device.NetIf.NewPacket += NetIfOnNewPacket;
        }

        private void NetIfOnNewPacket(object? sender, NewPacketEventArgs<ArtNetPacket> e) {
            if (_ipConfig == null) {
                if (e.Packet.OpCode == ArtNetOpCodes.IpProgReply) {
                    _device.NetIf.NewPacket -= NetIfOnNewPacket;
                    var reply = (ArtIpProgReplyPacket)e.Packet;
                    Refresh(reply, true);
                }
            } else {
                _device.NetIf.NewPacket -= NetIfOnNewPacket;
            }
        }

        public override void ExitPage() {
            _device.NetIf.NewPacket -= NetIfOnNewPacket;
        }
    }
}
