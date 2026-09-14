using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HBP.Transfer.Transport;

namespace HBP.UI.Quest
{
    /// <summary>Uses the user's already-authorized ADB USB transport; forwards only the HiBoP receiver port.</summary>
    public sealed class QuestUsbDiscovery
    {
        private readonly string adb;
        private readonly Dictionary<string, string> forwards = new Dictionary<string, string>();

        public QuestUsbDiscovery(string adb)
        {
            this.adb = adb;
        }

        public async Task<List<QuestDevice>> FindAsync(CancellationToken stop)
        {
            var result = new List<QuestDevice>();
            if (!File.Exists(adb)) return result;
            string devices = await RunAsync("devices -l", stop).ConfigureAwait(false);
            foreach (string line in devices.Split('\n'))
            {
                var match = Regex.Match(line.Trim(), @"^(\S+)\s+device\s+.*usb:");
                // Windows ADB omits usb: for some drivers; TCP serials contain ':' or _tcp.
                if (!match.Success) match = Regex.Match(line.Trim(), @"^([A-Za-z0-9_-]+)\s+device\s+.*product:");
                if (!match.Success) continue;
                string serial = match.Groups[1].Value;
                if (!Regex.IsMatch(serial, @"^[A-Za-z0-9_-]+$")) continue;
                try
                {
                    // Recreate only if our mapping disappeared (unplugging removes ADB forwards).
                    string mappings = await RunAsync("forward --list", stop).ConfigureAwait(false);
                    if (!forwards.TryGetValue(serial, out string port) || !mappings.Split('\n').Any(x => x.Trim() == serial + " tcp:" + port + " tcp:" + QuestPairing.Port))
                    {
                        port = (await RunAsync("-s " + serial + " forward tcp:0 tcp:" + QuestPairing.Port, stop).ConfigureAwait(false)).Trim();
                        if (!int.TryParse(port, out int number) || number < 1 || number > 65535) continue;
                        forwards[serial] = port;
                    }

                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stop);
                    timeout.CancelAfter(2000);
                    var candidate = await QuestPairing.DescribeAsync("127.0.0.1:" + port, timeout.Token).ConfigureAwait(false);
                    candidate.UsbSerial = serial;
                    result.Add(candidate);
                }
                catch (Exception) when (!stop.IsCancellationRequested)
                {
                    /* A USB device with no HiBoP receiver is not a candidate. */
                }
            }

            return result;
        }

        private Task<string> RunAsync(string arguments, CancellationToken stop) => QuestAdbProcess.RunAsync(adb, arguments, stop);
    }
}
