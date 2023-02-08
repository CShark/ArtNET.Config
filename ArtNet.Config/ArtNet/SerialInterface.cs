using System.IO.Ports;
using System.Text;

namespace ArtNet.Config.ArtNet {
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
                _serial.Open();

                if (_serial.IsOpen) {
                    _serial.ReadExisting();
                    _serial.DataReceived += SerialOnDataReceived;
                    try {
                        _serial.DtrEnable = true;
                    } catch { }

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
                } catch { }
            }
        }

        public void RebootDevice() {
            try {
                _serial.Open();
                _serial.Write(new byte[] { 0xF1 }, 0, 1);
                _serial.Close();
            }catch{}
        }

        public void RebootBootloader() {
            try {
                _serial.Open();
                _serial.Write(new byte[] { 0xF0 }, 0, 1);
                _serial.Close();
            } catch { }
        }

        public void ResetConfig() {
            try {
                _serial.Open();
                _serial.Write(new byte[] {0xF2},0, 1);
                _serial.Close();
            } catch { }
        }
    }
}