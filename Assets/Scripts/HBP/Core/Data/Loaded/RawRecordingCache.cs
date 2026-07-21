using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HBP.Core.Data
{
    internal sealed class EEGRecordingSource
    {
        public DLL.EEG.File.FileType FileType { get; }
        public string[] ReaderFiles { get; }
        public string[] IdentityFiles { get; }

        private EEGRecordingSource(DLL.EEG.File.FileType fileType, IEnumerable<string> readerFiles, IEnumerable<string> identityFiles = null)
        {
            FileType = fileType;
            ReaderFiles = readerFiles.ToArray();
            IdentityFiles = (identityFiles ?? ReaderFiles).Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        public static EEGRecordingSource From(DataInfo dataInfo)
        {
            if (dataInfo.DataContainer is Container.BrainVision brainVision)
            {
                string[] identityFiles = new[] { brainVision.Header }.Concat(GetBrainVisionReferencedFiles(brainVision.Header)).ToArray();
                return new EEGRecordingSource(DLL.EEG.File.FileType.BrainVision, new[] { brainVision.Header }, identityFiles);
            }
            if (dataInfo.DataContainer is Container.EDF edf)
                return new EEGRecordingSource(DLL.EEG.File.FileType.EDF, new[] { edf.File });
            if (dataInfo.DataContainer is Container.Elan elan)
                return new EEGRecordingSource(DLL.EEG.File.FileType.ELAN, new[] { elan.EEG, elan.POS, elan.Notes }, new[] { elan.EEG, elan.EEGHeader, elan.POS, elan.Notes });
            if (dataInfo.DataContainer is Container.Micromed micromed)
                return new EEGRecordingSource(DLL.EEG.File.FileType.Micromed, new[] { micromed.Path });
            if (dataInfo.DataContainer is Container.FIF fif)
                return new EEGRecordingSource(DLL.EEG.File.FileType.FIF, new[] { fif.File });

            throw new Exception("Invalid data container type");
        }

        private static IEnumerable<string> GetBrainVisionReferencedFiles(string headerPath)
        {
            if (string.IsNullOrWhiteSpace(headerPath) || !System.IO.File.Exists(headerPath))
                return Array.Empty<string>();

            string directory = Path.GetDirectoryName(headerPath) ?? string.Empty;
            try
            {
                return System.IO.File.ReadLines(headerPath)
                    .Select(line => line.Trim())
                    .Where(line => line.StartsWith("DataFile=", StringComparison.OrdinalIgnoreCase) || line.StartsWith("MarkerFile=", StringComparison.OrdinalIgnoreCase))
                    .Select(line => line.Substring(line.IndexOf('=') + 1).Trim())
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .Select(path => Path.IsPathRooted(path) ? path : Path.Combine(directory, path))
                    .ToArray();
            }
            catch (IOException)
            {
                return Array.Empty<string>();
            }
            catch (UnauthorizedAccessException)
            {
                return Array.Empty<string>();
            }
        }
    }

    internal readonly struct RawRecordingSourceKey : IEquatable<RawRecordingSourceKey>
    {
        private readonly string m_Value;

        internal RawRecordingSourceKey(string value)
        {
            m_Value = value ?? string.Empty;
        }

        public static RawRecordingSourceKey From(EEGRecordingSource source)
        {
            string files = string.Join("|", source.IdentityFiles.Select(GetFileIdentity));
            return new RawRecordingSourceKey($"{source.FileType}|{files}");
        }

        private static string GetFileIdentity(string path)
        {
            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(path);
            }
            catch (Exception exception) when (exception is ArgumentException || exception is NotSupportedException || exception is PathTooLongException)
            {
                fullPath = path ?? string.Empty;
            }

            try
            {
                FileInfo file = new(fullPath);
                return file.Exists
                    ? $"{fullPath}:{file.Length}:{file.LastWriteTimeUtc.Ticks}"
                    : $"{fullPath}:missing";
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                return $"{fullPath}:unavailable";
            }
        }

        public bool Equals(RawRecordingSourceKey other) => StringComparer.OrdinalIgnoreCase.Equals(m_Value, other.m_Value);
        public override bool Equals(object obj) => obj is RawRecordingSourceKey other && Equals(other);
        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(m_Value ?? string.Empty);
    }

    internal sealed class RawRecordingCache
    {
        private readonly ConcurrentDictionary<RawRecordingSourceKey, Lazy<DynamicData>> m_Entries = new();
        private readonly ConcurrentDictionary<RawRecordingSourceKey, int> m_PinCounts = new();
        private readonly MemoryCacheBudget m_Budget;

        public RawRecordingCache(MemoryCacheBudget budget = null)
        {
            m_Budget = budget;
        }

        public int Count => m_Entries.Count;

        public DynamicData GetOrLoad(RawRecordingSourceKey key, Func<DynamicData> loader)
        {
            Lazy<DynamicData> candidate = new(loader, true);
            Lazy<DynamicData> entry = m_Entries.GetOrAdd(key, candidate);
            try
            {
                DynamicData data = entry.Value;
                if (m_Budget != null)
                {
                    bool pinned = m_PinCounts.TryGetValue(key, out int pinCount) && pinCount > 0;
                    m_Budget.Register(key, MemoryCacheCategory.RawRecording, EstimateBytes(data), pinned, () => Remove(key, entry));
                }
                return data;
            }
            catch
            {
                if (m_Entries.TryGetValue(key, out Lazy<DynamicData> current) && ReferenceEquals(current, entry))
                    ((ICollection<KeyValuePair<RawRecordingSourceKey, Lazy<DynamicData>>>)m_Entries).Remove(new KeyValuePair<RawRecordingSourceKey, Lazy<DynamicData>>(key, entry));
                throw;
            }
        }

        public void Clear()
        {
            if (m_Budget != null)
            {
                foreach (RawRecordingSourceKey key in m_Entries.Keys)
                    m_Budget.Unregister(key);
            }
            m_Entries.Clear();
            m_PinCounts.Clear();
        }

        public void Pin(RawRecordingSourceKey key)
        {
            m_PinCounts.AddOrUpdate(key, 1, (_, count) => count + 1);
            m_Budget?.SetPinned(key, true);
        }

        public void Unpin(RawRecordingSourceKey key)
        {
            int count = m_PinCounts.AddOrUpdate(key, 0, (_, current) => Math.Max(0, current - 1));
            if (count == 0)
            {
                m_PinCounts.TryRemove(key, out _);
                m_Budget?.SetPinned(key, false);
            }
        }

        private void Remove(RawRecordingSourceKey key, Lazy<DynamicData> expected)
        {
            if (m_Entries.TryGetValue(key, out Lazy<DynamicData> current) && ReferenceEquals(current, expected))
                ((ICollection<KeyValuePair<RawRecordingSourceKey, Lazy<DynamicData>>>)m_Entries).Remove(new KeyValuePair<RawRecordingSourceKey, Lazy<DynamicData>>(key, expected));
        }

        private static long EstimateBytes(DynamicData data)
        {
            long bytes = 0;
            foreach (KeyValuePair<string, float[]> pair in data.ValuesByChannel)
            {
                bytes += pair.Value?.LongLength * sizeof(float) ?? 0;
                bytes += pair.Key?.Length * sizeof(char) ?? 0;
            }
            foreach (KeyValuePair<string, string> pair in data.UnitByChannel)
            {
                bytes += pair.Key?.Length * sizeof(char) ?? 0;
                bytes += pair.Value?.Length * sizeof(char) ?? 0;
            }
            return bytes;
        }
    }
}
