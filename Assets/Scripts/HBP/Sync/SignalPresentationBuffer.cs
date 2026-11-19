using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HBP.Sync
{
    /// <summary>Window-scoped exact data, with atomic revision publication and a hard allocation budget.</summary>
    public sealed class SignalPresentationBuffer
    {
        private readonly Dictionary<string, float[]> resources = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> received = new(StringComparer.Ordinal);
        private SignalSnapshot pending;
        private long revision = -1, reservedTextures, reservedMetadata;
        public Guid Generation { get; private set; }
        public long BudgetBytes { get; }
        public SignalSnapshot Current { get; private set; }
        public IReadOnlyDictionary<string, float[]> Resources => resources;
        public long ResourceBytes => resources.Values.Sum(v => (long)v.Length * 4);
        public event Action Progress;
        public event Action<bool> Loading;
        public event Action<SignalSnapshot> Published;
        public event Action<string> Failed;

        public SignalPresentationBuffer(Guid generation, long budgetBytes = SignalSnapshot.DefaultBudgetBytes)
        {
            if (generation == Guid.Empty || budgetBytes < 16 * 1024 * 1024 || budgetBytes > SignalSnapshot.DefaultBudgetBytes) throw new ArgumentOutOfRangeException(nameof(budgetBytes));
            Generation = generation;
            BudgetBytes = budgetBytes;
        }

        public void Receive(SignalMessage message)
        {
            if (message.Generation != Generation || message.Revision < revision) return;
            Progress?.Invoke();
            switch (message.Kind)
            {
                case SignalMessageKind.Begin:
                    foreach (string key in received.Keys.ToArray()) resources.Remove(key);
                    received.Clear();
                    pending = null;
                    revision = message.Revision;
                    if (message.Key == "content")
                    {
                        Current = null;
                        reservedTextures = reservedMetadata = 0;
                    }

                    Loading?.Invoke(message.Key == "content");
                    break;
                case SignalMessageKind.Manifest:
                    if (message.Revision != revision) return;
                    reservedMetadata = checked((long)message.Body.Length * 16);
                    EnsureCapacity(0);
                    pending = SignalSnapshotCodec.Decode(message.Body);
                    if (pending.Revision != revision) throw new InvalidDataException("Mismatched presentation revision.");
                    var needed = Required(pending).ToHashSet(StringComparer.Ordinal);
                    foreach (string key in resources.Keys.Where(k => !needed.Contains(k)).ToArray()) resources.Remove(key);
                    reservedTextures = EstimateTextures(pending);
                    EnsureCapacity(0);
                    break;
                case SignalMessageKind.Resource:
                    if (message.Revision != revision || pending == null) return;
                    if (message.Key.Length != 64 || !Required(pending).Contains(message.Key) || message.Total % 4 != 0 || message.Offset % 4 != 0 || message.Body.Length % 4 != 0 || message.Body.Length > SignalWire.PartBytes || message.Offset > message.Total - message.Body.Length) throw new InvalidDataException("Invalid signal resource part.");
                    if (message.Offset == 0)
                    {
                        if (resources.ContainsKey(message.Key)) throw new InvalidDataException("Duplicate signal resource.");
                        EnsureCapacity(checked(2L * message.Total));
                        resources.Add(message.Key, new float[message.Total / 4]);
                        received.Add(message.Key, 0);
                    }

                    if (!received.TryGetValue(message.Key, out int offset) || offset != message.Offset || resources[message.Key].Length * 4 != message.Total) throw new InvalidDataException("Out-of-order signal resource.");
                    Buffer.BlockCopy(message.Body, 0, resources[message.Key], offset, message.Body.Length);
                    if (offset + message.Body.Length == message.Total) received.Remove(message.Key);
                    else received[message.Key] += message.Body.Length;
                    break;
                case SignalMessageKind.Commit:
                    if (message.Revision != revision || pending == null) return;
                    if (received.Count != 0 || Required(pending).Any(key => !resources.ContainsKey(key))) throw new InvalidDataException("Signal presentation is incomplete.");
                    foreach (var cell in pending.Matrices.SelectMany(row => row.Cells))
                        if (cell.Resource.Length > 0 && resources[cell.Resource].LongLength != (long)cell.Rows * cell.Columns)
                            throw new InvalidDataException("Mismatched matrix resource dimensions.");
                    long vertices = Curves(pending).Where(c => c.Resource.Length > 0 && c.Enabled).Sum(c => (long)resources[c.Resource].Length / 3 * (c.HasShape ? 8 : 4));
                    foreach (var curve in Curves(pending))
                        if (curve.Resource.Length > 0 && resources[curve.Resource].Length % 3 != 0)
                            throw new InvalidDataException("Invalid curve resource dimensions.");
                    EnsureCapacity(checked(vertices * 96));
                    Current = pending;
                    pending = null;
                    Published?.Invoke(Current);
                    break;
                case SignalMessageKind.Error:
                    ClearData();
                    Failed?.Invoke(message.Key);
                    break;
            }
        }

        public void ClearData()
        {
            resources.Clear();
            received.Clear();
            pending = Current = null;
            reservedTextures = reservedMetadata = 0;
        }

        private void EnsureCapacity(long extra)
        {
            // Reserve the multiplexed transport reassembly window as well as managed/native render copies.
            if (extra < 0 || checked(2 * ResourceBytes + reservedTextures + reservedMetadata + extra + 16L * 1024 * 1024) > BudgetBytes) throw new InvalidDataException("Signals exceed the 128 MiB capacity. Reduce the displayed content or smoothing on Desktop.");
        }

        public static IEnumerable<SignalCurve> Curves(SignalSnapshot s) => s.Graphs.SelectMany(g => Flatten(g.Curves));

        private static IEnumerable<SignalCurve> Flatten(IEnumerable<SignalCurve> roots)
        {
            foreach (var root in roots)
            {
                yield return root;
                foreach (var child in Flatten(root.Children)) yield return child;
            }
        }

        public static IEnumerable<string> Required(SignalSnapshot s) => Curves(s).Select(c => c.Resource).Concat(s.Matrices.SelectMany(row => row.Cells).Select(c => c.Resource)).Where(k => k.Length > 0).Distinct(StringComparer.Ordinal);

        private static long EstimateTextures(SignalSnapshot s)
        {
            long bytes = 0;
            foreach (var cell in s.Matrices.SelectMany(row => row.Cells).Where(c => c.Rows > 0 && c.Columns > 0))
            {
                long width = s.Smooth ? checked((cell.Columns - 1L) * s.SmoothingFactor + 1) : cell.Columns;
                long height = s.Smooth && s.Smooth2D ? checked((cell.Rows - 1L) * s.SmoothingFactor + 1) : cell.Rows;
                // Conservative halos and simultaneous Color32 upload buffers plus native textures.
                bytes = checked(bytes + (width + 2 * ((width + 253) / 254)) * (height + 2 * ((height + 253) / 254)) * 12);
            }

            return bytes;
        }
    }
}
