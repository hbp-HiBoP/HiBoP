using System;
using System.Collections.Generic;
using System.Linq;
using Makaretu.Dns;

namespace HBP.Transfer.Transport
{
    /// <summary>DNS records are scoped to the receiving local interface, never to a peer's zone id.</summary>
    public sealed class QuestMdnsCache
    {
        public const string ServiceType = "_hibop-quest._tcp.local";
        private readonly Dictionary<string, (ResourceRecord Record, int Interface, double Expires)> records = new();
        private static string Name(DomainName name) => name.ToString().TrimEnd('.').ToLowerInvariant();

        public void Add(Message message, int localInterface, double now)
        {
            if (localInterface <= 0) return;
            foreach (var expired in records.Where(p => p.Value.Expires <= now).Select(p => p.Key).ToArray()) records.Remove(expired);
            var packetRecords = message.Answers.Concat(message.AdditionalRecords).Take(128).ToArray();
            var targets = records.Values.Where(r => r.Interface == localInterface).Select(r => r.Record).OfType<SRVRecord>().Select(r => r.Target).Concat(packetRecords.OfType<SRVRecord>().Where(r => Name(r.Name).EndsWith("." + ServiceType, StringComparison.Ordinal)).Select(r => r.Target)).ToArray();
            // Other Bonjour devices must not fill HiBoP's bounded cache on a busy LAN.
            var incoming = packetRecords.Where(r => r is PTRRecord && Name(r.Name) == ServiceType || r.Type is DnsType.SRV or DnsType.TXT && Name(r.Name).EndsWith("." + ServiceType, StringComparison.Ordinal) || r is AddressRecord && targets.Contains(r.Name)).ToArray();
            foreach (var record in incoming)
            {
                string key = localInterface + "|" + Name(record.Name) + "|" + record.Type + "|" + Convert.ToBase64String(record.GetData());
                if (((ushort)record.Class & 0x8000) != 0)
                    foreach (var old in records.Where(p => p.Value.Interface == localInterface && p.Value.Record.Name == record.Name && p.Value.Record.Type == record.Type && !incoming.Contains(p.Value.Record)).Select(p => p.Key).ToArray())
                    {
                        var value = records[old];
                        records[old] = (value.Record, value.Interface, Math.Min(value.Expires, now + 1));
                    }

                if (record.TTL == TimeSpan.Zero)
                {
                    if (records.TryGetValue(key, out var old)) records[key] = (old.Record, localInterface, Math.Min(old.Expires, now + 1));
                }
                else if (records.Count < 2048 || records.ContainsKey(key)) records[key] = (record, localInterface, now + Math.Min(4500, record.TTL.TotalSeconds));
            }
        }

        public IReadOnlyList<QuestDevice> Devices(double now, long generation)
        {
            var devices = new Dictionary<string, QuestDevice>();
            foreach (var group in records.Values.Where(r => r.Expires > now).GroupBy(r => r.Interface))
            {
                var available = group.Select(r => r.Record).ToArray();
                foreach (var pointer in available.OfType<PTRRecord>().Where(r => Name(r.Name) == ServiceType))
                {
                    var srv = available.OfType<SRVRecord>().FirstOrDefault(r => r.Name == pointer.DomainName);
                    var txt = available.OfType<TXTRecord>().FirstOrDefault(r => r.Name == pointer.DomainName);
                    if (srv == null || txt == null || srv.Port != QuestPairing.Port) continue;
                    var properties = new Dictionary<string, string>(StringComparer.Ordinal);
                    foreach (string entry in txt.Strings)
                    {
                        int separator = entry.IndexOf('=');
                        if (separator > 0) properties[entry.Substring(0, separator)] = entry.Substring(separator + 1);
                    }

                    if (!properties.TryGetValue("v", out string version) || version != "1" || !properties.TryGetValue("id", out string id) || !properties.TryGetValue("path", out string path) || path is not ("usb" or "lan")) continue;
                    byte[] pin;
                    try
                    {
                        pin = Convert.FromBase64String(id);
                    }
                    catch (FormatException)
                    {
                        continue;
                    }

                    if (pin.Length != 32) continue;
                    string name = properties.TryGetValue("name", out var label) ? label : "Quest";
                    if (name.Length > 64 || name.Any(char.IsControl)) continue;
                    foreach (var address in available.OfType<AddressRecord>().Where(r => r.Name == srv.Target))
                    {
                        try
                        {
                            var endpoint = new QuestEndpoint(address.Address, srv.Port, path == "usb" ? QuestNetworkPath.Usb : QuestNetworkPath.LocalNetwork, group.Key, generation);
                            var device = new QuestDevice { Pin = pin, Name = name, Endpoint = endpoint };
                            devices[device.Id + "|" + endpoint.Key] = device;
                        }
                        catch (ArgumentException)
                        {
                            /* An invalid address is never a usable route. */
                        }
                    }
                }
            }

            return devices.Values.Take(128).ToArray();
        }

        public IEnumerable<DomainName> Unresolved(double now) => records.Values.Where(r => r.Expires > now).GroupBy(r => r.Interface).SelectMany(group => group.Select(r => r.Record).OfType<PTRRecord>().Where(r => Name(r.Name) == ServiceType).Select(r => r.DomainName).Concat(group.Select(r => r.Record).OfType<SRVRecord>().Where(srv => !group.Select(r => r.Record).OfType<AddressRecord>().Any(a => a.Name == srv.Target)).Select(srv => srv.Target))).Distinct().Take(64);
    }
}
