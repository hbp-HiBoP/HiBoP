using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HBP.Core.Tools;
using HBP.Data.Module3D;

namespace HBP.Sync.Scene
{
    public enum V2CorrelationCommandKind : byte
    {
        Compute = 1,
        Load = 2
    }

    /// <summary>Immutable command snapshot. Load carries the typed result body, never a local file path.</summary>
    public sealed class V2CorrelationRequest
    {
        private readonly byte[] m_ResultBytes;
        public V2CorrelationCommandKind Kind { get; }
        public bool ExternalLoadingIndicator { get; }
        public Action<float, float, LoadingText> Progress { get; }
        public byte[] ResultBytes => (byte[])m_ResultBytes.Clone();

        private V2CorrelationRequest(V2CorrelationCommandKind kind, byte[] resultBytes, bool externalLoadingIndicator, Action<float, float, LoadingText> progress)
        {
            if (kind < V2CorrelationCommandKind.Compute || kind > V2CorrelationCommandKind.Load) throw new ArgumentOutOfRangeException(nameof(kind));
            if (kind == V2CorrelationCommandKind.Load && (resultBytes == null || resultBytes.Length == 0 || resultBytes.Length > CorrelationResultResource.MaximumBytes)) throw new ArgumentOutOfRangeException(nameof(resultBytes));
            if (kind == V2CorrelationCommandKind.Compute && resultBytes is { Length: > 0 }) throw new ArgumentException("Compute requests do not contain result data.", nameof(resultBytes));
            Kind = kind;
            m_ResultBytes = resultBytes == null ? Array.Empty<byte>() : (byte[])resultBytes.Clone();
            ExternalLoadingIndicator = externalLoadingIndicator;
            Progress = progress;
        }

        public static V2CorrelationRequest Compute(bool externalLoadingIndicator = false, Action<float, float, LoadingText> progress = null) => new(V2CorrelationCommandKind.Compute, null, externalLoadingIndicator, progress);
        public static V2CorrelationRequest Load(byte[] resultBytes, bool externalLoadingIndicator = false) => new(V2CorrelationCommandKind.Load, resultBytes, externalLoadingIndicator, null);
    }

    public static class V2CorrelationRequestCodec
    {
        public const int MaximumEncodedBytes = 7;
        private const ushort SchemaVersion = 1;
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("HBCR");

        public static byte[] EncodeComputeRequest(V2CorrelationRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Kind != V2CorrelationCommandKind.Compute) throw new InvalidDataException("A correlation control request cannot carry loaded result data.");
            using var stream = new MemoryStream(MaximumEncodedBytes);
            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            writer.Write(Magic);
            writer.Write(SchemaVersion);
            writer.Write((byte)request.Kind);
            writer.Flush();
            return stream.ToArray();
        }

        public static V2CorrelationRequest DecodeComputeRequest(byte[] bytes)
        {
            if (bytes == null || bytes.Length != MaximumEncodedBytes) throw new InvalidDataException("Invalid correlation compute command length.");
            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            if (!reader.ReadBytes(Magic.Length).AsSpan().SequenceEqual(Magic) || reader.ReadUInt16() != SchemaVersion || reader.ReadByte() != (byte)V2CorrelationCommandKind.Compute)
                throw new InvalidDataException("Unsupported correlation compute command.");
            return V2CorrelationRequest.Compute();
        }
    }

    public static class V2CorrelationRequestRouter
    {
        private static readonly object Gate = new();
        private static readonly Dictionary<Base3DScene, Registration> Registrations = new();

        public static IDisposable Register(Base3DScene scene, object owner, Func<V2CorrelationRequest, CancellationToken, Task<bool>> handler)
        {
            if (!scene) throw new ArgumentNullException(nameof(scene));
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            lock (Gate)
            {
                if (Registrations.TryGetValue(scene, out Registration existing) && !ReferenceEquals(existing.Owner, owner)) throw new InvalidOperationException("A correlation job handler is already registered for this scene.");
                Registrations[scene] = new Registration(owner, handler);
            }

            return new RegistrationScope(scene, owner);
        }

        public static bool TryGetHandler(Base3DScene scene, out Func<V2CorrelationRequest, CancellationToken, Task<bool>> handler)
        {
            lock (Gate)
            {
                if (scene && Registrations.TryGetValue(scene, out Registration registration))
                {
                    handler = registration.Handler;
                    return true;
                }
            }

            handler = null;
            return false;
        }

        private sealed class Registration
        {
            public object Owner { get; }
            public Func<V2CorrelationRequest, CancellationToken, Task<bool>> Handler { get; }

            public Registration(object owner, Func<V2CorrelationRequest, CancellationToken, Task<bool>> handler)
            {
                Owner = owner;
                Handler = handler;
            }
        }

        private sealed class RegistrationScope : IDisposable
        {
            private readonly Base3DScene m_Scene;
            private readonly object m_Owner;

            public RegistrationScope(Base3DScene scene, object owner)
            {
                m_Scene = scene;
                m_Owner = owner;
            }

            public void Dispose()
            {
                lock (Gate)
                    if (!ReferenceEquals(m_Scene, null) && Registrations.TryGetValue(m_Scene, out Registration registration) && ReferenceEquals(registration.Owner, m_Owner))
                        Registrations.Remove(m_Scene);
            }
        }
    }

    public static class V2CorrelationOfflineCapabilityGate
    {
        public static bool CanCompute(Base3DScene scene, out string explanation)
        {
            if (!scene)
            {
                explanation = "The offline scene is unavailable.";
                return false;
            }

            Column3DIEEG[] columns = scene.ColumnsIEEG.ToArray();
            if (columns.Length == 0)
            {
                explanation = "The scene has no iEEG columns.";
                return false;
            }

            foreach (Column3DIEEG column in columns)
            foreach (var site in column.Sites)
            {
                if (!site || site.State.IsBlackListed) continue;
                var data = site.Data;
                if (data == null || data.Trials == null || data.Trials.Length == 0 || data.Trials.Any(trial => trial?.Values == null))
                {
                    explanation = "Complete trial samples are not available for every unblacklisted site.";
                    return false;
                }
            }

            explanation = null;
            return true;
        }
    }
}
