using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HBP.Transfer.Transport
{
    /// <summary>Local connection history; credentials retain their existing protected file format.</summary>
    public sealed class PairingAssociations
    {
        private readonly string directory;
        public PairingAssociations(string directory) => this.directory = directory;
        public string CredentialPath(byte[] pin) => Path.Combine(directory, BitConverter.ToString(pin).Replace("-", "") + ".pair");

        public Dictionary<string, long> ReadHistory()
        {
            var result = new Dictionary<string, long>(StringComparer.Ordinal);
            if (!Directory.Exists(directory)) return result;
            foreach (string path in Directory.EnumerateFiles(directory, "*.pair"))
            {
                byte[] credential = null;
                try
                {
                    string hex = Path.GetFileNameWithoutExtension(path);
                    if (hex.Length != 64) continue;
                    byte[] pin = Enumerable.Range(0, 32).Select(i => Convert.ToByte(hex.Substring(i * 2, 2), 16)).ToArray();
                    credential = PairingStorage.Read(path);
                    if (credential?.Length != 32) continue;
                    string key = Convert.ToBase64String(pin);
                    result[key] = 0;
                    byte[] date = PairingStorage.Read(path + ".last");
                    if (date?.Length == 8) result[key] = Math.Max(0, BitConverter.ToInt64(date, 0));
                }
                catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is FormatException || error is System.ComponentModel.Win32Exception)
                {
                }
                finally
                {
                    if (credential != null) Array.Clear(credential, 0, credential.Length);
                }
            }

            return result;
        }

        public void RecordSuccess(byte[] pin, long utcTicks) => PairingStorage.Write(CredentialPath(pin) + ".last", BitConverter.GetBytes(utcTicks));

        public static QuestDevice Choose(IEnumerable<QuestDevice> available, IReadOnlyDictionary<string, long> history, ISet<string> excluded) => available.Where(device => history.ContainsKey(device.Id) && !excluded.Contains(device.Id)).OrderByDescending(device => history[device.Id]).ThenBy(device => device.Id, StringComparer.Ordinal).FirstOrDefault();
    }
}
