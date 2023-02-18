using ArtNet.Config.ArtNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ArtNet.Config.Pages {
    internal class SerialDhcp : IPage {
        private readonly DeviceInfo _device;
        private DhcpServerConfig _dhcpConfig;

        public SerialDhcp(IPage parent, DeviceInfo device) : base(parent) {
            _device = device;
        }

        public override bool AllowRefresh => false;

        protected override IPage? HandleInputInternal(string? input) {
            if (input == "yes" || input == "y") {
                _device.Serial.SetDhcpConfig(_dhcpConfig);
            }

            return Parent;
        }

        public override async Task RenderInternal(object? state) {
            Console.WriteLine("ArtNet Device — Change DHCP Server Configuration (Serial)");
            Console.WriteLine($"{_device.IpAddressStr()} — {_device.MacAddressStr()}");
            Console.WriteLine();

            if (_device.Serial == null) {
                WriteError("Device does not support serial access");
            } else {
                var ip = await _device.Serial.GetDhcpConfig();

                if (ip == null) {
                    WriteError("Failed to fetch DHCP server configuration");
                    return;
                }
                _dhcpConfig = ip;

                Console.WriteLine("DHCP Server Configuration:");
                Console.WriteLine($"DHCP Server enabled:\t{(ip.EnableServer ? "■" : "")}");
                Console.WriteLine($"Server Address:\t\t{ip.ServerAddress}");
                Console.WriteLine($"Client Address:\t\t{ip.ClientAddress}");
                Console.WriteLine($"Netmask:\t\t{ip.Netmask}");

                Console.WriteLine();
                Console.WriteLine("Enable DHCP Server? [yes/no]");
                Console.Write("> ");
                var dhcpEnable = await ReadInput() switch {
                    "y" => true,
                    "yes" => true,
                    _ => false
                };
                Console.WriteLine();
                ip.EnableServer = dhcpEnable;

                if (!dhcpEnable) {
                    Console.WriteLine(
                        "The new mode does not require to configure the dhcp server. Do you want to do it anyway? [yes/no]");
                    Console.Write("> ");
                    var configure = await ReadInput();
                    Console.WriteLine();
                    if (configure != "yes" && configure != "y") {
                        Console.WriteLine("Confirm? [yes/no]");
                        return;
                    }
                }

                Console.WriteLine("Please enter a Server Address:");
                Console.Write("> ");
                var strAddrServer = await ReadInput();
                Console.WriteLine();

                Console.WriteLine("Please enter a Client Address:");
                Console.Write("> ");
                var strAddrClient = await ReadInput();
                Console.WriteLine();

                Console.WriteLine("Please enter a Netmask:");
                Console.Write("> ");
                var strNetmask = await ReadInput();
                Console.WriteLine();

                if (!IPAddress.TryParse(strAddrServer, out var addrServer) ||
                    !IPAddress.TryParse(strNetmask, out var netmask) ||
                    !IPAddress.TryParse(strAddrClient, out var addrClient)) {
                    WriteError("Failed to parse one of the IP Addresses");
                    return;
                }

                ip.ServerAddress = addrServer;
                ip.Netmask = netmask;
                ip.ClientAddress = addrClient;

                Console.WriteLine("New Settings:");
                Console.WriteLine($"DHCP Server enabled:\t{(ip.EnableServer ? "■" : "")}");
                Console.WriteLine($"Server Address:\t\t{ip.ServerAddress}");
                Console.WriteLine($"Client Address:\t\t{ip.ClientAddress}");
                Console.WriteLine($"Netmask:\t\t{ip.Netmask}");
                Console.WriteLine("Confirm? [yes/no]");
            }
        }
    }
}