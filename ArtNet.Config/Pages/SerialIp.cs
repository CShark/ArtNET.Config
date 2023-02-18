using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ArtNet.Config.ArtNet;

namespace ArtNet.Config.Pages {
    internal class SerialIp : IPage {
        private readonly DeviceInfo _device;
        private EIpMode _mode;
        private StaticIpConfig _ipConfig;

        public SerialIp(IPage parent, DeviceInfo device) : base(parent) {
            _device = device;
        }

        public override bool AllowRefresh => false;

        protected override IPage? HandleInputInternal(string? input) {
            if (input == "yes" || input == "y") {
                _device.Serial.SetMode(_mode);
                if (_ipConfig != null) {
                    _device.Serial.SetStaticIpConfig(_ipConfig);
                }
            }

            return Parent;
        }

        public override async Task RenderInternal(object? state) {
            Console.WriteLine("ArtNet Device — Change IP Configuration (Serial)");
            Console.WriteLine($"{_device.IpAddressStr()} — {_device.MacAddressStr()}");
            Console.WriteLine();

            if (_device.Serial == null) {
                WriteError("Device does not support serial access");
            } else {
                var mode = await _device.Serial.GetMode();
                var ip = await _device.Serial.GetStaticIpConfig();

                if (mode == EIpMode.Unknown || ip == null) {
                    WriteError("Failed to fetch mode and/or static ip configuration");
                    return;
                }

                Console.WriteLine("Current Mode:\t" + mode);
                Console.WriteLine($"Address:\t{ip.IPAddress}");
                Console.WriteLine($"Netmask:\t{ip.Netmask}");
                Console.WriteLine($"Gateway:\t{ip.DefaultGateway}");
                Console.WriteLine();

                Console.WriteLine();
                Console.WriteLine("Please select new mode:");
                Console.WriteLine("[0]\tAuto-IP");
                Console.WriteLine("[1]\tStatic IP");
                Console.WriteLine("[2]\tDHCP");
                Console.Write("> ");
                var newMode = await ReadInput();
                Console.WriteLine();
                if (!int.TryParse(newMode, out var intMode) || intMode < 0 || intMode > 2) {
                    WriteError($"Failed to parse mode {newMode}");
                    return;
                }

                if (intMode != 1) {
                    Console.WriteLine(
                        "The new mode does not require static ip configuration. Do you want to configure it anyway? [yes/no]");
                    Console.Write("> ");
                    var configure = await ReadInput();
                    Console.WriteLine();
                    if (configure != "yes" && configure != "y") {
                        _mode = (EIpMode)intMode;
                        Console.WriteLine("Confirm? [yes/no]");
                        return;
                    }
                }

                Console.WriteLine("Please enter an IP Address:");
                Console.Write("> ");
                var strAddress = await ReadInput();
                Console.WriteLine();

                Console.WriteLine("Please enter a Netmask:");
                Console.Write("> ");
                var strNetmask = await ReadInput();
                Console.WriteLine();

                Console.WriteLine("Please enter a default Gateway:");
                Console.Write("> ");
                var strGateway = await ReadInput();
                Console.WriteLine();

                if (!IPAddress.TryParse(strAddress, out var addr) ||
                    !IPAddress.TryParse(strNetmask, out var netmask) ||
                    !IPAddress.TryParse(strGateway, out var gateway)) {
                    WriteError("Failed to parse one of the IP Addresses");
                    return;
                }

                mode = (EIpMode)intMode;
                ip.IPAddress = addr;
                ip.Netmask = netmask;
                ip.DefaultGateway = gateway;

                _mode = mode;
                _ipConfig = ip;

                Console.WriteLine("New Settings:");
                Console.WriteLine("Current Mode:\t" + mode);
                Console.WriteLine($"Address:\t{ip.IPAddress}");
                Console.WriteLine($"Netmask:\t{ip.Netmask}");
                Console.WriteLine($"Gateway:\t{ip.DefaultGateway}");
                Console.WriteLine("Confirm? [yes/no]");
            }
        }
    }
}