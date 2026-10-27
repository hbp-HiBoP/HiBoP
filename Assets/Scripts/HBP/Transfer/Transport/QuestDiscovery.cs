using System;
using System.Linq;

namespace HBP.Transfer.Transport
{
    public sealed class QuestDevice
    {
        public byte[] Pin;
        public string Name;
        public QuestEndpoint Endpoint;
        public string Id => Convert.ToBase64String(Pin);
        public string Label => Name + " · " + (Endpoint?.Path == QuestNetworkPath.Usb ? "USB" : Endpoint?.AddressText ?? "Network") + " · " + BitConverter.ToString(Pin, 0, 3).Replace("-", "");
    }

    // The TLS description command keeps its wire format; it is independent of discovery.
    public static class QuestDescription
    {
        private const string Prefix = "HiBoP-Quest-v1|";

        public static string Encode(byte[] pin, string name)
        {
            string safe = new string((name ?? "Quest").Where(c => !char.IsControl(c) && c != '|').Take(64).ToArray());
            return Prefix + Convert.ToBase64String(pin) + "|" + safe;
        }

        public static QuestDevice Decode(string message, string host)
        {
            if (message == null || message.Length > 256 || !message.StartsWith(Prefix, StringComparison.Ordinal)) return null;
            string[] parts = message.Split('|');
            if (parts.Length != 3 || parts[2].Length > 64 || parts[2].Any(char.IsControl)) return null;
            try
            {
                byte[] pin = Convert.FromBase64String(parts[1]);
                return pin.Length == 32 ? new QuestDevice { Pin = pin, Name = parts[2], Endpoint = QuestEndpoint.Parse(host) } : null;
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
