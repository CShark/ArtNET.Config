using ArtNet.Config.ArtNet;
using Haukcode.ArtNet.ArtNet.Packets;
using Haukcode.ArtNet.Packets;

namespace ArtNet.Config.Pages {
    internal class EditDeviceName : IPage {
        private DeviceInfo _device;
        private ArtPollReplyPacket _reply;
        private string _shortName = "";
        private string _longName = "";
        private byte _boundIndex = 0;

        public EditDeviceName(IPage parent, DeviceInfo device, ArtPollReplyPacket reply) : base(parent) {
            _device = device;
            _reply = reply;
        }

        public override bool AllowRefresh => false;

        protected override IPage? HandleInputInternal(string? input) {
            if (input == "yes" || input == "y") {
                _device.NetIf.Send(new ArtAddressPacket {
                    ShortName = _shortName,
                    LongName = _longName,
                    BindIndex = _boundIndex
                });
            }

            return Parent;
        }

        public override async Task RenderInternal(object? state) {
            Console.Clear();
            Console.WriteLine("ArtNet Device — Change Name");
            Console.WriteLine($"{_device.IpAddressStr()} — {_device.MacAddressStr()}");
            Console.WriteLine();

            Console.WriteLine(_reply.ShortName + " — " + _reply.LongName);

            Console.WriteLine();
            _boundIndex = _reply.BindIndex;
            Console.WriteLine("Enter a short name (max 18 characters), leave blank for no change:");
            _shortName = await ReadInput();
            Console.WriteLine();
            Console.WriteLine("Enter a long name (max 64 characters), leave blank for no change:");
            _longName = await ReadInput();

            _shortName = _shortName.Trim();
            _longName = _longName.Trim();

            if (_shortName.Length > 18) {
                _shortName = _shortName.Substring(0, 18);
            }

            if (_longName.Length > 64) {
                _longName = _longName.Substring(0, 64);
            }

            Console.WriteLine();
            Console.WriteLine("The device names will be changed to:");
            Console.WriteLine($"{"Short Name:",-15}{_shortName}");
            Console.WriteLine($"{"Long Name:",-15}{_longName}");
            Console.WriteLine("Confirm? [yes/no]");
        }
    }
}
