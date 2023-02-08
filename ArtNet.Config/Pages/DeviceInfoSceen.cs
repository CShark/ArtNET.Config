using ArtNet.Config.ArtNet;
using Haukcode.ArtNet;
using Haukcode.ArtNet.Packets;

namespace ArtNet.Config.Pages {
    internal class DeviceInfoScreen : IPage {
        private DeviceInfo _device;
        private byte? _bindIndex = null;
        private bool _canEditIp = false;
        private bool _canSwitchPort = false;
        private bool _canSwitchRDM = false;
        private bool _canSwitchArtNET = false;
        private bool _canSwitchMode = false;
        private bool _canSwitchFailover = false;

        private Dictionary<int, string> _portProtocols = new Dictionary<int, string>() {
            [0] = "DMX512",
            [1] = "MIDI",
            [2] = "Avab",
            [3] = "Colortran CMX",
            [4] = "ADB 62.5",
            [5] = "Art-Net"
        };

        public DeviceInfoScreen(IPage parent, DeviceInfo device) : base(parent) {
            _device = device;
            _canEditIp = (_device.Status2 & 0x01) != 0;
            _canSwitchArtNET = (_device.Status2 & (1 << 4)) != 0;
            _canSwitchMode = (_device.Status2 & (1 << 6)) != 0;
            _canSwitchRDM = (_device.Status2 & (1 << 7)) != 0;
            _canSwitchFailover = (_device.Status3 & (1 << 5)) != 0;
            _canSwitchPort = (_device.Status3 & (1 << 3)) != 0;

            if (_device.PortReplies.Count == 1) {
                _bindIndex = _device.PortReplies[0].BindIndex;
            }
        }

        public override bool AllowRefresh => true;

        protected override IPage? HandleInputInternal(string? input) {
            LastErr = "";
            var _pollReply = _device.PortReplies.FirstOrDefault(x => x.BindIndex == _bindIndex);

            if (_pollReply == null && byte.TryParse(input, out var idx) && idx >= 0 && idx <= 2 & !_device.BrokenLink) {
                if (_device.PortReplies.Any(x => x.BindIndex == idx)) {
                    _bindIndex = idx;
                } else {
                    LastErr = $"The group {idx} was not found";
                }
            } else {
                if (string.IsNullOrWhiteSpace(input)) {
                    if (_pollReply == null || _device.PortReplies.Count == 1) {
                        return Parent;
                    } else {
                        _bindIndex = null;
                    }
                } else {
                    switch (input) {
                        case "0":
                            return new EditDeviceName(this, _device, _pollReply);
                        case "1":
                            return new EditDeviceArtNet(this, _device, _pollReply);
                        case "2":
                            Console.WriteLine();
                            Console.WriteLine("Please enter a new ACN Priority between 0 and 200:");
                            Console.Write("> ");
                            if (!byte.TryParse(Console.ReadLine(), out var prio)) {
                                LastErr = "Unable to parse priority";
                            } else {
                                _device.NetIf.Send(new ArtAddressPacket(_pollReply) { AcnPriority = prio });
                            }
                            break;
                        case "3":
                            Console.WriteLine();
                            Console.WriteLine("Please enter a new Indicator State:");
                            Console.WriteLine("[l]\tLocate");
                            Console.WriteLine("[m]\tMute");
                            Console.WriteLine("[n]\tNormal");
                            Console.Write("> ");
                            switch (Console.ReadLine()) {
                                case "l":
                                    _device.NetIf.Send(new ArtAddressPacket(_pollReply) { Command = ArtAddressCommand.AcLedLocate });
                                    break;
                                case "m":
                                    _device.NetIf.Send(new ArtAddressPacket(_pollReply) { Command = ArtAddressCommand.AcLedMute });
                                    break;
                                case "n":
                                    _device.NetIf.Send(new ArtAddressPacket(_pollReply) { Command = ArtAddressCommand.AcLedNormal });
                                    break;
                                default:
                                    LastErr = "Unknown Indicator option";
                                    break;
                            }
                            break;
                        case "4":
                            Console.WriteLine();
                            Console.WriteLine("Please enter the new Failover mode:");
                            Console.WriteLine("[h]\tHold");
                            Console.WriteLine("[0]\tZero");
                            Console.WriteLine("[1]\tFull");
                            Console.WriteLine("[s]\tScene");
                            Console.WriteLine("[r]\tRecord Failover Scene");
                            Console.Write("> ");
                            switch (Console.ReadLine()) {
                                case "h":
                                    _device.NetIf.Send(new ArtAddressPacket(_pollReply) { Command = ArtAddressCommand.AcFailHold });
                                    break;
                                case "0":
                                    _device.NetIf.Send(new ArtAddressPacket(_pollReply) { Command = ArtAddressCommand.AcFailZero });
                                    break;
                                case "1":
                                    _device.NetIf.Send(new ArtAddressPacket(_pollReply) { Command = ArtAddressCommand.AcFailFull });
                                    break;
                                case "s":
                                    _device.NetIf.Send(new ArtAddressPacket(_pollReply) { Command = ArtAddressCommand.AcFailScene });
                                    break;
                                case "r":
                                    _device.NetIf.Send(new ArtAddressPacket(_pollReply) { Command = ArtAddressCommand.AcFailRecord });
                                    break;
                            }
                            break;
                        case "ip":
                            return new EditDeviceIp(this, _device);
                        case "-r":
                            _device.Serial?.RebootDevice();
                            break;
                        case "-b":
                            _device.Serial?.RebootBootloader();
                            break;
                        case "-c":
                            _device.Serial?.ResetConfig();
                            break;
                        case "-d":
                            // Edit DHCP Server
                            break;
                        case "-ip":
                            // Edit Static IP / Mode
                            break;
                        default:
                            var cmd = input.Substring(0, 1);
                            if (byte.TryParse(input.Substring(1), out idx)) {
                                if (idx >= 0 && idx < _pollReply.PortCount) {
                                    switch (cmd) {
                                        case "r":
                                            TogglePortRDM(idx);
                                            break;
                                        case "a":
                                            TogglePortArtNet(idx);
                                            break;
                                        case "d":
                                            if ((_pollReply.PortTypes[idx] & 0x80) != 0) { // Output
                                                TogglePortMode(idx);
                                            } else { // Input
                                                ToggleInput(idx);
                                            }
                                            break;
                                        case "i":
                                            SwitchPortType(idx, true);
                                            break;
                                        case "o":
                                            SwitchPortType(idx, false);
                                            break;
                                        default:
                                            LastErr = $"The command \"{input}\" was not recognized";
                                            break;
                                    }
                                } else {
                                    LastErr = $"The number \"{idx}\" was not a valid port";
                                }
                            }
                            break;
                    }
                }
            }

            return this;
        }

        public override async Task RenderInternal(object? state) {
            Console.WriteLine("ArtNet Device");
            if (_device.BrokenLink) {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"{_device.IpAddressStr()} — {_device.MacAddressStr()}");
                Console.WriteLine("— The device is on a different Network and cannot be reached");
                Console.ResetColor();
            } else {
                Console.WriteLine($"{_device.IpAddressStr()} — {_device.MacAddressStr()}");
            }

            var _pollReply = _device.PortReplies.FirstOrDefault(x => x.BindIndex == _bindIndex);

            if (_pollReply != null) {
                Console.WriteLine($"{_pollReply.ShortName} — {_pollReply.LongName}");
            }


            Console.WriteLine();
            var replies = _device.PortReplies;
            if (_pollReply != null) {
                replies = new List<ArtPollReplyPacket>() { _pollReply };
            }

            if (replies.Any(x => x.PortTypes.Any(y => (y & 0x80) != 0))) {
                WriteCentered("--[ Outputs ]--");
                Console.WriteLine(
                    $"{"Nr",-4} | {"Addr",-5} {"Network",-9} {"Protocol",-15} {"Mode",-8} {"RDM",-3} | {"Group Name"}");
                Console.WriteLine(new string('-', 100));

                for (var ix = 0; ix < replies.Count; ix++) {
                    var reply = replies[ix];
                    var net = (reply.SubSwitch & 0x7F00) >> 8;
                    var sub = reply.SubSwitch & 0x000F;
                    var id = "";
                    bool groupSet = false;

                    for (int i = 0; i < reply.PortTypes.Length; i++) {
                        var protocol = "<unknown>";

                        if (_portProtocols.ContainsKey(reply.PortTypes[i] & 0x1F)) {
                            protocol = _portProtocols[reply.PortTypes[i] & 0x1F];
                        }

                        if (_pollReply == null) {
                            id = !groupSet ? reply.BindIndex.ToString() : "";
                        } else {
                            id = i.ToString();
                        }

                        if ((reply.PortTypes[i] & 0x80) != 0) {
                            if ((reply.GoodOutputA[i] & 0x04) != 0) {
                                Console.ForegroundColor = ConsoleColor.DarkRed;
                            } else if ((reply.GoodOutputA[i] & 0x80) != 0) {
                                Console.ForegroundColor = ConsoleColor.DarkGreen;
                            }

                            var addr = (net << 8) + (sub << 4) + reply.SwOut[i];
                            Console.WriteLine(
                                $"{id,-4} | {addr,-5} {$"{net}:{sub}:{reply.SwOut[i]}",-9} {protocol,-15} {((reply.GoodOutputA[i] & 0x01) != 0 ? "sACN" : "ArtNET"),-6}{((reply.GoodOutputB[i] & 0x40) != 0 ? " ~" : " Δ")} {((reply.GoodOutputB[i] & 0x80) != 0 ? " ■" : " "),-3} | {(!groupSet ? reply.ShortName : "")}");
                            groupSet = true;

                            Console.ResetColor();
                        }
                    }
                }

                Console.WriteLine(new string('-', 100));
                Console.WriteLine();
            }

            if (replies.Any(x => x.PortTypes.Any(y => (y & 0x40) != 0))) {
                WriteCentered("--[ Inputs ]--");
                Console.WriteLine(
                    $"{"Nr",-4} | {"Addr",-5} {"Network",-9} {"Protocol",-15} {"Enabled",-7} | {"Group Name"}");
                Console.WriteLine(new string('-', 100));
                for (var ix = 0; ix < replies.Count; ix++) {
                    var reply = replies[ix];
                    var net = (reply.SubSwitch & 0x7F00) >> 8;
                    var sub = reply.SubSwitch & 0x000F;
                    var id = "";
                    bool groupSet = false;

                    for (int i = 0; i < reply.PortTypes.Length; i++) {
                        var protocol = "<unknown>";

                        if (_portProtocols.ContainsKey(reply.PortTypes[i] & 0x1F)) {
                            protocol = _portProtocols[reply.PortTypes[i] & 0x1F];
                        }

                        if (_pollReply == null) {
                            id = !groupSet ? reply.BindIndex.ToString() : "";
                        } else {
                            id = i.ToString();
                        }

                        if ((reply.PortTypes[i] & 0x40) != 0) {
                            if ((reply.GoodInput[i] & 0x04) != 0) {
                                Console.ForegroundColor = ConsoleColor.DarkRed;
                            } else if ((reply.GoodInput[i] & 0x80) != 0) {
                                Console.ForegroundColor = ConsoleColor.DarkGreen;
                            }

                            var addr = (net << 8) + (sub << 4) + reply.SwIn[i];
                            Console.WriteLine(
                                $"{id,-4} | {addr,-5} {$"{net}:{sub}:{reply.SwIn[i]}",-9} {protocol,-15} {((reply.GoodInput[i] & 0x08) != 0 ? "" : "   ■"),-7} | {(!groupSet ? reply.ShortName : "")}");
                            groupSet = true;

                            Console.ResetColor();
                        }
                    }
                }

                Console.WriteLine(new string('-', 100));
                Console.WriteLine();
            }

            WriteCentered("--[ Version ]--");
            Console.WriteLine($"{"Firmware Version:",-30}{replies[0].FirmwareVersion}");
            Console.WriteLine($"{"UBEA Version:",-30}{replies[0].UbeaVersion}");
            Console.WriteLine($"{"RDMnet & LLRP UID:",-30}{replies[0].RespUID.Select(x => $"{x:X2}").Aggregate("", (a, b) => a + b)}");
            Console.WriteLine($"");
            Console.WriteLine();

            if (_pollReply != null) {
                WriteCentered("--[ Macros & Triggers ]--");

                Console.WriteLine($"{"",-7} | 1  2  3  4  5  6  7  8");
                Console.WriteLine(new string('-', 9 + 3 * 8));
                Console.Write("Macro   |");
                for (int i = 0; i < 8; i++) {
                    Console.Write($"{((_pollReply.SwMacro & (1 << i)) != 0 ? " ■ " : "   ")}");
                }
                Console.WriteLine();

                Console.Write("Trigger |");
                for (int i = 0; i < 8; i++) {
                    Console.Write($"{((_pollReply.SwRemote & (1 << i)) != 0 ? " ■ " : "   ")}");
                }
                Console.WriteLine();
            }
            Console.WriteLine();

            var addrAuth = (replies[0].Status & 0x30) switch {
                (0 << 4) => "Front Panel & Network",
                (1 << 4) => "Front Panel",
                (2 << 4) => "Network",
                _ => "<unknown>"
            };
            var indicator = (replies[0].Status & 0xC0) switch {
                (0 << 6) => "Unknown",
                (1 << 6) => "Locate",
                (2 << 6) => "Mute",
                (3 << 6) => "Normal",
                _ => "<unknown>"
            };
            var failsafe = (replies[0].Status3 & 0xC0) switch {
                (0 << 6) => "Hold",
                (1 << 6) => "Zero",
                (2 << 6) => "Full",
                (3 << 6) => "Scene",
                _ => "<unknown>"
            };


            WriteCentered("--[ Status ]--");
            if (_pollReply == null && (_device.PortReplies.Any(x => x.Status != _device.PortReplies[0].Status) ||
                                       _device.PortReplies.Any(x => x.Status2 != _device.PortReplies[0].Status2) ||
                                       _device.PortReplies.Any(x => x.Status3 != _device.PortReplies[0].Status3) ||
                                       _device.PortReplies.Any(x => x.AcnPriority != replies[0].AcnPriority))) {
                WriteCentered("<Status differs per group, only first group shown>", ConsoleColor.Yellow);
            }
            var status1 = new ColumnRenderer();
            status1.AddEntry($"{"Indicator State:",-30}{indicator}");
            status1.AddEntry($"{"Address-Authority:",-30}{addrAuth}");
            status1.AddEntry($"{"Boot source:",-30}{((replies[0].Status & 0x04) != 0 ? "ROM" : "Flash")}");
            status1.AddEntry($"{"Supports RDM:",-30}" + ((replies[0].Status & 0x02) != 0 ? "■" : "-"), null, 1);
            status1.AddEntry($"{"UBEA:",-30}{((replies[0].Status & 0x01) != 0 ? "■" : "-")}", null, 1);
            status1.Render();
            Console.WriteLine();

            var status2 = new ColumnRenderer();
            status2.AddEntry($"{"Web configurable:",-30}" + (_canEditIp ? "■" : "-"));
            status2.AddEntry($"{"Uses DHCP currently:",-30}" + ((replies[0].Status2 & 0x02) != 0 ? "■" : "-"));
            status2.AddEntry($"{"Supports DHCP:",-30}" + ((replies[0].Status2 & 0x04) != 0 ? "■" : "-"));
            status2.AddEntry($"{"Uses 15-bit Port Adressing:",-30}" + ((replies[0].Status2 & 0x08) != 0 ? "■" : "-"), null, 1);
            status2.AddEntry($"{"Squawking:",-30}" + ((replies[0].Status2 & 0x20) != 0 ? "■" : "-"));
            status2.AddEntry($"{"sACN and ArtNET:",-30}" + ((replies[0].Status2 & 0x40) != 0 ? "■" : "-"), null, 1);
            status2.AddEntry($"{"RDM switchable:",-30}" + ((replies[0].Status2 & 0x80) != 0 ? "■" : "-"), null, 1);
            status2.Render();
            Console.WriteLine();

            var status3 = new ColumnRenderer();
            status3.AddEntry($"{"Failsafe state:",-30}" + failsafe);
            status3.AddEntry($"{"Supports fail-over:",-30}" + ((replies[0].Status3 & 0x20) != 0 ? "■" : "-"));
            status3.AddEntry($"{"Supports LLRP:",-30}" + ((replies[0].Status3 & 0x10) != 0 ? "■" : "-"), null, 1);
            status3.AddEntry($"{"Ports reconfigurable:",-30}" + ((replies[0].Status3 & 0x08) != 0 ? "■" : "-"), null, 1);
            status3.Render();
            Console.WriteLine();

            Console.WriteLine($"{"ACN-Priority:",-30}{replies[0].AcnPriority}");
            Console.WriteLine();

            WriteCentered("--[ Node Reports ]--");
            foreach (var grp in replies) {
                Console.WriteLine($"[{grp.ShortName}]");
                Console.WriteLine(grp.NodeReport);
                Console.WriteLine();
            }

            Console.WriteLine();
            Console.WriteLine(new string('-', 100));
            Console.WriteLine();

            ConsoleColor? color = null;
            if (_device.BrokenLink) {
                color = ConsoleColor.DarkGray;
            }

            var commands = new ColumnRenderer();
            if (_pollReply != null) {
                commands.AddEntry("[0]     Edit Name", color);
                commands.AddEntry("[1]     Edit ArtNet", color);
                commands.AddEntry("[2]     Edit ACN Priority", color);
                commands.AddEntry("[3]     Change Indicator State", color);
                commands.AddEntry("[4]     Change Failover Mode", color);
                commands.AddEntry("[ip]    Edit IP", color);
            } else {
                commands.AddEntry("[<n>]   Select group");
                commands.AddEntry("[ip]    Edit IP", color);
            }

            if (_device.Serial != null) {
                commands.AddEntry("[-r]\tPower-Cycle Device", ConsoleColor.DarkCyan, 1);
                commands.AddEntry("[-b]\tEnter Bootloader", ConsoleColor.DarkCyan, 1);
                commands.AddEntry("[-c]\tReset Configuration", ConsoleColor.DarkCyan, 1);
                commands.AddEntry("[-d]\tConfigure DHCP Server", ConsoleColor.DarkCyan, 1);
                commands.AddEntry("[-ip]\tConfigure Static IP & IP Mode", ConsoleColor.DarkCyan, 1);
            }

            commands.Render();
            Console.WriteLine();

            if (_pollReply != null) {
                WriteColoredLine("[r<n>]\tToggle Output RDM", _canSwitchRDM ? null : ConsoleColor.DarkGray);
                WriteColoredLine("[a<n>]\tToggle Output Protocol", _canSwitchArtNET ? null : ConsoleColor.DarkGray);
                WriteColoredLine("[d<n>]\tToggle Output Delta Mode", _canSwitchMode ? null : ConsoleColor.DarkGray);
                WriteColoredLine("[d<n>]\tToggle input disabled");
                WriteColoredLine("[i<n>]\tSet Port <n> to input", _canSwitchPort ? null : ConsoleColor.DarkGray);
                WriteColoredLine("[o<n>]\tSet Port <n> to output", _canSwitchPort ? null : ConsoleColor.DarkGray);
                Console.WriteLine();
            }

            if (_device.PortReplies.Count > 1 && _pollReply != null) {
                Console.WriteLine("[ ]\tShow all groups");
            } else {
                Console.WriteLine("[ ]\tReturn to overview");
            }

            Console.WriteLine();
        }


        private void SwitchPortType(int port, bool input) {
            if (_bindIndex == null) return;

            var pollReply = _device.PortReplies.FirstOrDefault(x => x.BindIndex == _bindIndex.Value);
            if (pollReply == null) return;

            var cmd = new ArtAddressPacket(pollReply);
            cmd.Command = input switch {
                true => port switch {
                    0 => ArtAddressCommand.AcDirectionRx0,
                    1 => ArtAddressCommand.AcDirectionRx1,
                    2 => ArtAddressCommand.AcDirectionRx2,
                    3 => ArtAddressCommand.AcDirectionRx3,
                    _ => ArtAddressCommand.AcNone
                },
                false => port switch {
                    0 => ArtAddressCommand.AcDirectionTx0,
                    1 => ArtAddressCommand.AcDirectionTx1,
                    2 => ArtAddressCommand.AcDirectionTx2,
                    3 => ArtAddressCommand.AcDirectionTx3,
                    _ => ArtAddressCommand.AcNone
                }
            };

            _device.NetIf.Send(cmd);
        }

        private void TogglePortMode(int port) {
            if (_bindIndex == null) return;

            var pollReply = _device.PortReplies.FirstOrDefault(x => x.BindIndex == _bindIndex.Value);
            if (pollReply == null) return;

            var cmd = new ArtAddressPacket(pollReply);
            cmd.Command = (pollReply.GoodOutputB[port]) switch {
                (var x) when (x & 0x40) != 0 => port switch {
                    0 => ArtAddressCommand.AcStyleDelta0,
                    1 => ArtAddressCommand.AcStyleDelta1,
                    2 => ArtAddressCommand.AcStyleDelta2,
                    3 => ArtAddressCommand.AcStyleDelta3,
                    _ => ArtAddressCommand.AcNone
                },
                _ => port switch {
                    0 => ArtAddressCommand.AcStyleConst0,
                    1 => ArtAddressCommand.AcStyleConst1,
                    2 => ArtAddressCommand.AcStyleConst2,
                    3 => ArtAddressCommand.AcStyleConst3,
                    _ => ArtAddressCommand.AcNone
                }
            };

            _device.NetIf.Send(cmd);
        }

        private void TogglePortArtNet(int port) {
            if (_bindIndex == null) return;

            var pollReply = _device.PortReplies.FirstOrDefault(x => x.BindIndex == _bindIndex.Value);
            if (pollReply == null) return;

            var cmd = new ArtAddressPacket(pollReply);
            cmd.Command = (pollReply.GoodOutputA[port]) switch {
                (var x) when (x & 0x01) != 0 => port switch {
                    0 => ArtAddressCommand.AcArtNetSel0,
                    1 => ArtAddressCommand.AcArtNetSel1,
                    2 => ArtAddressCommand.AcArtNetSel2,
                    3 => ArtAddressCommand.AcArtNetSel3,
                    _ => ArtAddressCommand.AcNone
                },
                _ => port switch {
                    0 => ArtAddressCommand.AcAcnSel0,
                    1 => ArtAddressCommand.AcAcnSel1,
                    2 => ArtAddressCommand.AcAcnSel2,
                    3 => ArtAddressCommand.AcAcnSel3,
                    _ => ArtAddressCommand.AcNone
                }
            };

            _device.NetIf.Send(cmd);
        }

        private void TogglePortRDM(int port) {
            if (_bindIndex == null) return;

            var pollReply = _device.PortReplies.FirstOrDefault(x => x.BindIndex == _bindIndex.Value);
            if (pollReply == null) return;

            var cmd = new ArtAddressPacket(pollReply);
            cmd.Command = (pollReply.GoodOutputB[port]) switch {
                (var x) when (x & 0x80) != 0 => port switch {
                    0 => ArtAddressCommand.AcRdmEnable0,
                    1 => ArtAddressCommand.AcRdmEnable1,
                    2 => ArtAddressCommand.AcRdmEnable2,
                    3 => ArtAddressCommand.AcRdmEnable3,
                    _ => ArtAddressCommand.AcNone
                },
                _ => port switch {
                    0 => ArtAddressCommand.AcRdmDisable0,
                    1 => ArtAddressCommand.AcRdmDisable1,
                    2 => ArtAddressCommand.AcRdmDisable2,
                    3 => ArtAddressCommand.AcRdmDisable3,
                    _ => ArtAddressCommand.AcNone
                }
            };

            _device.NetIf.Send(cmd);
        }

        private void ToggleInput(int port) {
            if (_bindIndex == null) return;

            var pollReply = _device.PortReplies.FirstOrDefault(x => x.BindIndex == _bindIndex.Value);
            if (pollReply == null) return;

            var cmd = new ArtInputPacket();
            cmd.BindIndex = _bindIndex.Value;
            cmd.NumPorts = pollReply.PortCount;
            cmd.Inputs = pollReply.GoodInput.Select((x, i) => (byte)(((x & (1 << 3)) >> 3) ^ (i == port ? 1 : 0))).ToArray();

            _device.NetIf.Send(cmd);
        }
    }
}