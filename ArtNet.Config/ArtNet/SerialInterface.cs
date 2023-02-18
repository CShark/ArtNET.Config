using System.IO.Ports;
using System.Net;
using System.Text;

namespace ArtNet.Config.ArtNet {
    public enum EIpMode : byte {
        Unknown = 0xFF,
        AutoIp = 0,
        Static,
        Dhcp,
    }

    public class StaticIpConfig {
        public IPAddress IPAddress { get; set; }
        public IPAddress Netmask { get; set; }
        public IPAddress DefaultGateway { get; set; }

        public StaticIpConfig(IPAddress ipAddress, IPAddress netmask, IPAddress defaultGateway) {
            IPAddress = ipAddress;
            Netmask = netmask;
            DefaultGateway = defaultGateway;
        }
    }

    public class DhcpServerConfig {
        public bool EnableServer { get; set; }
        public IPAddress ServerAddress { get; set; }
        public IPAddress ClientAddress { get; set; }
        public IPAddress Netmask { get; set; }

        public DhcpServerConfig(bool enableServer, IPAddress serverAddress, IPAddress clientAddress,
            IPAddress netmask) {
            EnableServer = enableServer;
            ServerAddress = serverAddress;
            ClientAddress = clientAddress;
            Netmask = netmask;
        }
    }

    internal class SerialInterface {
        private SerialPort _serial;

        public string Port { get; }

        public bool? IsValid { get; private set; } = null;

        public byte[] DeviceID { get; private set; } = Array.Empty<byte>();

        public SerialInterface(string port) {
            Port = port;
        }

        public async Task<bool> TryConnect() {
            try {
                _serial = new SerialPort(Port, 115200, Parity.None);
                _serial.ReadTimeout = 500;
                _serial.Open();

                if (_serial.IsOpen) {
                    _serial.ReadExisting();
                    _serial.DataReceived += SerialOnDataReceived;
                    try {
                        _serial.DtrEnable = true;
                    } catch {
                    }

                    await Task.Delay(500);
                    _serial.Close();
                    if (IsValid == true) {
                        return true;
                    } else {
                        IsValid = false;
                        return false;
                    }
                } else {
                    return false;
                }
            } catch (Exception ex) {
                return false;
            }
        }

        private void SerialOnDataReceived(object sender, SerialDataReceivedEventArgs e) {
            if (_serial.DtrEnable) {
                // Get serid
                var id = _serial.ReadExisting();

                if (id.Length > 12) id = id.Substring(0, 12);
                DeviceID = Encoding.ASCII.GetBytes(id);
                IsValid = true;

                try {
                    _serial.DtrEnable = false;
                } catch {
                }
            }
        }

        public void RebootDevice() {
            try {
                _serial.Open();
                _serial.Write(new byte[] { 0xF1 }, 0, 1);
            } finally {
                if (_serial.IsOpen)
                    _serial.Close();
            }
        }

        public void RebootBootloader() {
            try {
                _serial.Open();
                _serial.Write(new byte[] { 0xF0 }, 0, 1);
            } finally {
                if (_serial.IsOpen)
                    _serial.Close();
            }
        }

        public void ResetConfig() {
            try {
                _serial.Open();
                _serial.Write(new byte[] { 0xF2 }, 0, 1);
            } finally {
                if (_serial.IsOpen)
                    _serial.Close();
            }
        }

        public async Task<EIpMode> GetMode() {
            try {
                _serial.Open();
                _serial.ReadExisting();
                _serial.Write(new byte[] { 0xB0 }, 0, 1);

                await Task.WhenAny(Task.Delay(500), Task.Run(() => {
                    while (_serial.BytesToRead < 1) {
                    }
                }));

                var result = _serial.ReadExisting();
                if (result.Length == 1) {
                    return (EIpMode)result[0];
                } else {
                    return EIpMode.Unknown;
                }
            } catch {
                return EIpMode.Unknown;
            } finally {
                if (_serial.IsOpen)
                    _serial.Close();
            }
        }

        public void SetMode(EIpMode mode) {
            try {
                _serial.Open();
                _serial.Write(new byte[] { 0xA0, (byte)mode }, 0, 2);
            } finally {
                if (_serial.IsOpen)
                    _serial.Close();
            }
        }

        public async Task<StaticIpConfig?> GetStaticIpConfig() {
            try {
                _serial.Open();
                _serial.ReadExisting();
                _serial.Write(new byte[] { 0xB2 }, 0, 1);

                await Task.WhenAny(Task.Delay(500), Task.Run(() => {
                    while (_serial.BytesToRead < 12) {
                    }
                }));

                var result = new byte[12];
                _serial.Read(result, 0, 12);
                if (result.Length == 12) {
                    return new StaticIpConfig(new IPAddress(result.Take(4).ToArray()),
                        new IPAddress(result.Skip(4).Take(4).ToArray()),
                        new IPAddress(result.Skip(8).Take(4).ToArray()));
                } else {
                    return null;
                }
            } catch {
                return null;
            } finally {
                if (_serial.IsOpen)
                    _serial.Close();
            }
        }

        public void SetStaticIpConfig(StaticIpConfig config) {
            try {
                _serial.Open();
                _serial.Write(
                    new byte[] { 0xA2 }
                        .Concat(config.IPAddress.GetAddressBytes().Take(4))
                        .Concat(config.Netmask.GetAddressBytes().Take(4))
                        .Concat(config.DefaultGateway.GetAddressBytes().Take(4))
                        .ToArray(), 0, 13);
            } finally {
                if (_serial.IsOpen)
                    _serial.Close();
            }
        }

        public async Task<DhcpServerConfig?> GetDhcpConfig() {
            try {
                _serial.Open();
                _serial.ReadExisting();
                _serial.Write(new byte[] { 0xB1 }, 0, 1);

                await Task.WhenAny(Task.Delay(500), Task.Run(() => {
                    while (_serial.BytesToRead < 13) {
                    }
                }));

                var result = new byte[13];
                _serial.Read(result, 0, 13);
                if (result.Length == 13) {
                    return new DhcpServerConfig(
                        result[0] != 0,
                        new IPAddress(result.Skip(1).Take(4).ToArray()),
                        new IPAddress(result.Skip(5).Take(4).ToArray()),
                        new IPAddress(result.Skip(9).Take(4).ToArray()));
                } else {
                    return null;
                }
            } catch {
                return null;
            } finally {
                if (_serial.IsOpen)
                    _serial.Close();
            }
        }

        public void SetDhcpConfig(DhcpServerConfig dhcpConfig) {
            try {
                _serial.Open();
                _serial.Write(new byte[] { 0xA1, (byte)(dhcpConfig.EnableServer ? 0x01 : 0x00) }
                    .Concat(dhcpConfig.ServerAddress.GetAddressBytes().Take(4))
                    .Concat(dhcpConfig.ClientAddress.GetAddressBytes().Take(4))
                    .Concat(dhcpConfig.Netmask.GetAddressBytes().Take(4))
                    .ToArray(), 0, 14);
            } finally {
                if (_serial.IsOpen)
                    _serial.Close();
            }
        }
    }
}