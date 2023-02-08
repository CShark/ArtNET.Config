using ArtNet.Config.ArtNet;
using Haukcode.ArtNet.Packets;

namespace ArtNet.Config.Pages {
    internal class EditDeviceArtNet : IPage {
        private readonly DeviceInfo _device;
        private readonly ArtPollReplyPacket _reply;

        private int _net = -1;
        private int _sub = -1;
        private int[] _inputs = { -1, -1, -1, -1 };
        private int[] _outputs = { -1, -1, -1, -1 };

        public EditDeviceArtNet(IPage parent, DeviceInfo device, ArtPollReplyPacket reply) : base(parent) {
            _device = device;
            _reply = reply;
        }

        public override bool AllowRefresh => false;

        protected override IPage? HandleInputInternal(string? input) {
            _inputs = _inputs.Zip(_reply.SwIn, (a, b) => a != -1 ? a : b).ToArray();
            _outputs = _outputs.Zip(_reply.SwOut, (a, b) => a != -1 ? a : b).ToArray();

            if (input == "yes" || input == "y") {
                _device.NetIf.Send(new ArtAddressPacket(_reply) {
                    NetSwitch = (byte)(_net | 0x80),
                    SubSwitch = (byte)(_sub | 0x80),
                    SwIn = _inputs.Select(x => (byte)(x | 0x80)).ToArray(),
                    SwOut = _outputs.Select(x => (byte)(x | 0x80)).ToArray()
                });
            }

            return Parent;
        }

        public override async Task RenderInternal(object? state) {
            Console.Clear();
            Console.WriteLine("ArtNet Device — Change ArtNet-Address");
            Console.WriteLine($"{_device.IpAddressStr()} — {_device.MacAddressStr()}");
            Console.WriteLine();

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine($"{"Index",-5} | {"I/O",-3} {"Address",-8} {"Network",-9}");
            Console.WriteLine(new string('-', 100));
            int ix = 0;
            _net = (_reply.SubSwitch & 0x7F00) >> 8;
            _sub = _reply.SubSwitch & 0x000F;

            for (int i = 0; i < _reply.PortTypes.Length; i++) {
                if ((_reply.PortTypes[i] & 0x80) != 0) {
                    var addr = (_net << 8) + (_sub << 4) + _reply.SwOut[i];
                    Console.WriteLine(
                        $"{ix,-4} | {"Out",-4} {addr,-8} {$"{_net}:{_sub}:{_reply.SwOut[i]}",-9}");
                    ix++;
                }

                if ((_reply.PortTypes[i] & 0x40) != 0) {
                    var addr = (_net << 8) + (_sub << 4) + _reply.SwIn[i];
                    Console.WriteLine(
                        $"{ix,-4} | {"In",-4} {addr,-8} {$"{_net}:{_sub}:{_reply.SwIn[i]}",-9}");
                    ix++;
                }
            }
            Console.WriteLine(new string('-', 100));


            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("[0]\tSet Universe in calculated format");
            Console.WriteLine("[1]\tConfigure Network, Subnet and Universe separately");
            Console.Write("> ");

            if (!int.TryParse(await ReadInput(), out var idx)) {
                Console.WriteLine("Invalid option");
            }

            if (idx < 0 || idx >= 2) {
                Console.WriteLine("Invalid option");
            }

            _net = -1;
            _sub = -1;
            int min = 0;
            int max = 0x7FFF;

            switch (idx) {
                case 0:
                    for (int i = 0; i < 8; i++) {
                        if (i % 2 == 0 && (_reply.PortTypes[i / 2] & 0x80) != 0) {
                            Console.WriteLine($"Please enter a universe for output port {i / 2} between {min} and {max}, or leave empty for no change:");
                        } else if (i % 2 == 1 && (_reply.PortTypes[i / 2] & 0x40) != 0) {
                            Console.WriteLine($"Please enter a universe for input port {i / 2} between {min} and {max}, or leave empty for no change:");
                        } else {
                            continue;
                        }

                        Console.Write("> ");

                        var response = await ReadInput();
                        if (!string.IsNullOrWhiteSpace(response)) {
                            if (!int.TryParse(response, out var universe)) {
                                Console.WriteLine("\nInvalid input");
                                i--;
                                continue;
                            }

                            if (universe < min || universe > max) {
                                Console.WriteLine("\nUniverse out of range");
                                i--;
                                continue;
                            }

                            if (i % 2 == 0) {
                                _outputs[i / 2] = universe & 0x000F;
                            } else {
                                _inputs[i / 2] = universe & 0x000F;
                            }

                            _net = (universe & 0x7F00) >> 8;
                            _sub = (universe & 0x00F0) >> 4;

                            min = (_net << 8) | (_sub << 4);
                            max = (_net << 8) | (_sub << 4) | 0x000F;
                        }

                        Console.WriteLine();
                    }
                    break;
                case 1:
                    Console.WriteLine($"Please enter a network for the group between 0 and {0x7F}, or leave empty for no change:");
                    Console.Write("> ");
                    int.TryParse(await ReadInput(), out _net);

                    if (_net < 0 || _net > 0x7F) {
                        Console.WriteLine("Network out of range, ignoring input");
                        _net = -1;
                    }

                    Console.WriteLine($"\nPlease enter a subnet for the group between 0 and {0xF}, or leave empty for no change:");
                    Console.Write("> ");
                    int.TryParse(await ReadInput(), out _sub);

                    if (_sub < 0 || _sub > 0xF) {
                        Console.WriteLine("Subnet out of range, ignoring input");
                        _sub = -1;
                    }

                    for (int i = 0; i < 4; i++) {
                        if ((_reply.PortTypes[i] & 0x80) != 0) {
                            Console.WriteLine($"Please enter a universe for output {i} between 0 and {0xF}, or leave empty for no change:");
                            Console.Write("> ");
                            int.TryParse(await ReadInput(), out _outputs[i]);

                            if (_outputs[i] < 0 || _outputs[i] > 0xF) {
                                Console.WriteLine("Universe out of range, ingoring input");
                                _outputs[i] = -1;
                            }

                            Console.WriteLine();
                        }

                        if ((_reply.PortTypes[i] & 0x40) != 0) {
                            Console.WriteLine($"Please enter a universe for input {i} between 0 and {0xF}, or leave empty for no change:");
                            Console.Write("> ");
                            int.TryParse(await ReadInput(), out _inputs[i]);

                            if (_inputs[i] < 0 || _inputs[i] > 0xF) {
                                Console.WriteLine("Universe out of range, ingoring input");
                                _inputs[i] = -1;
                            }

                            Console.WriteLine();
                        }
                    }
                    break;
            }

            // Results
            Console.WriteLine();
            Console.WriteLine("The ArtNet-Addressing will be changed as follows:");
            Console.WriteLine($"{"Index",-5} | {"I/O",-3} {"Address",-8} {"Network",-9}");
            Console.WriteLine(new string('-', 100));
            ix = 0;

            if (_net == -1) {
                _net = (_reply.SubSwitch & 0x7F00) >> 8;
            }
            if (_sub == -1) {
                _sub = _reply.SubSwitch & 0x000F;
            }

            for (int i = 0; i < _reply.PortTypes.Length; i++) {
                if ((_reply.PortTypes[i] & 0x80) != 0) {
                    if (_outputs[i] == -1) {
                        _outputs[i] = _reply.SwOut[i];
                    }

                    var addr = (_net << 8) + (_sub << 4) + _outputs[i];
                    Console.WriteLine(
                        $"{ix,-4} | {"Out",-4} {addr,-8} {$"{_net}:{_sub}:{_outputs[i]}",-9}");
                    ix++;
                }

                if ((_reply.PortTypes[i] & 0x40) != 0) {
                    if (_inputs[i] == -1) {
                        _inputs[i] = _reply.SwIn[i];
                    }

                    var addr = (_net << 8) + (_sub << 4) + _inputs[i];
                    Console.WriteLine(
                        $"{ix,-4} | {"In",-4} {addr,-8} {$"{_net}:{_sub}:{_inputs[i]}",-9}");
                    ix++;
                }

                // If there are no dual input/output ports, copy the address to SwIn/SwOut as well
                // as some devices just parse one of them...
                if ((_reply.PortTypes[i] & 0x80) == 0 ||
                    (_reply.PortTypes[i] & 0x40) == 0) {

                    if ((_reply.PortTypes[i] & 0x80) != 0) {
                        _inputs[i] = _outputs[i];
                    }else if ((_reply.PortTypes[i] & 0x40) != 0) {
                        _outputs[i] = _inputs[i];
                    }
                }
            }

            Console.WriteLine(new string('-', 100));
            Console.WriteLine();
            Console.WriteLine("Confirm? [yes/no]");
        }
    }
}
