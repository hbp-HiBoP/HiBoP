using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CRNL.HiBoP.Contracts;

namespace CRNL.HiBoP.Protocol
{
    [Flags]
    public enum ProtocolCapabilities : ulong
    {
        None = 0,
        TransactionalSnapshot = 1UL << 0,
        OrderedDeltas = 1UL << 1,
        SequencedCommands = 1UL << 2,
        Resume = 1UL << 3,
        RedactedDiagnostics = 1UL << 4,
    }

    public enum DeviceClass : byte
    {
        Unknown = 0,
        DesktopReference = 1,
        Quest = 2,
    }

    public enum CompatibilityDecision : byte
    {
        Unknown = 0,
        Accepted = 1,
        ProtocolIncompatible = 2,
        SchemaIncompatible = 3,
        CapabilitiesIncompatible = 4,
        AuthFailed = 5,
        SessionBusy = 6,
    }

    public readonly struct ProtocolVersion : IComparable<ProtocolVersion>, IEquatable<ProtocolVersion>
    {
        public ProtocolVersion(ushort major, ushort minor)
        {
            if (major == 0)
                throw new ArgumentOutOfRangeException(nameof(major));

            Major = major;
            Minor = minor;
        }

        public ushort Major { get; }

        public ushort Minor { get; }

        public bool IsValid => Major != 0;

        public static ProtocolVersion V1 => new(1, 0);

        public int CompareTo(ProtocolVersion other)
        {
            int major = Major.CompareTo(other.Major);
            return major != 0 ? major : Minor.CompareTo(other.Minor);
        }

        public bool Equals(ProtocolVersion other) => Major == other.Major && Minor == other.Minor;

        public override bool Equals(object obj) => obj is ProtocolVersion other && Equals(other);

        public override int GetHashCode() => (Major << 16) | Minor;

        public override string ToString() => $"ProtocolVersion({Major}.{Minor})";

        public static bool operator ==(ProtocolVersion left, ProtocolVersion right) => left.Equals(right);

        public static bool operator !=(ProtocolVersion left, ProtocolVersion right) => !left.Equals(right);
    }

    public sealed class BuildIdentity
    {
        public const int MaximumFieldLength = 128;

        public BuildIdentity(string applicationVersion, string buildCommit, string nativeVersion)
        {
            ApplicationVersion = Validate(applicationVersion, nameof(applicationVersion));
            BuildCommit = Validate(buildCommit, nameof(buildCommit));
            NativeVersion = Validate(nativeVersion, nameof(nativeVersion));
        }

        public string ApplicationVersion { get; }

        public string BuildCommit { get; }

        public string NativeVersion { get; }

        public override string ToString() => "BuildIdentity(redacted)";

        private static string Validate(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > MaximumFieldLength)
                throw new ArgumentException($"A non-empty value of at most {MaximumFieldLength} characters is required.", parameterName);
            return value;
        }
    }

    public sealed class ClientHello
    {
        private readonly ReadOnlyCollection<AssetHash> m_SchemaHashes;

        public ClientHello(ProtocolVersion protocol, IEnumerable<AssetHash> supportedSchemaHashes, BuildIdentity build, ProtocolCapabilities capabilities, DeviceClass deviceClass, ContractId clientNonce)
        {
            if (!protocol.IsValid)
                throw new ArgumentException("A valid protocol version is required.", nameof(protocol));
            if (supportedSchemaHashes == null)
                throw new ArgumentNullException(nameof(supportedSchemaHashes));
            if (build == null)
                throw new ArgumentNullException(nameof(build));
            if (deviceClass <= DeviceClass.Unknown || deviceClass > DeviceClass.Quest)
                throw new ArgumentOutOfRangeException(nameof(deviceClass));
            if (!clientNonce.IsValid)
                throw new ArgumentException("A valid client nonce is required.", nameof(clientNonce));

            List<AssetHash> hashes = new(supportedSchemaHashes);
            if (hashes.Count == 0 || hashes.Count > 16 || hashes.Exists(hash => !hash.IsValid))
                throw new ArgumentException("Between one and sixteen valid schema hashes are required.", nameof(supportedSchemaHashes));
            hashes.Sort();
            for (int index = 1; index < hashes.Count; index++)
            {
                if (hashes[index - 1] == hashes[index])
                    throw new ArgumentException("Schema hashes must be unique.", nameof(supportedSchemaHashes));
            }

            Protocol = protocol;
            m_SchemaHashes = hashes.AsReadOnly();
            Build = build;
            Capabilities = capabilities;
            DeviceClass = deviceClass;
            ClientNonce = clientNonce;
        }

        public ProtocolVersion Protocol { get; }

        public IReadOnlyList<AssetHash> SupportedSchemaHashes => m_SchemaHashes;

        public BuildIdentity Build { get; }

        public ProtocolCapabilities Capabilities { get; }

        public DeviceClass DeviceClass { get; }

        public ContractId ClientNonce { get; }

        public override string ToString() => $"ClientHello(protocol={Protocol}, schemaCount={m_SchemaHashes.Count}, capabilities={Capabilities}, device={DeviceClass})";
    }

    public sealed class ServerHello
    {
        internal ServerHello(ProtocolVersion protocol, Optional<AssetHash> selectedSchemaHash, BuildIdentity build, ProtocolCapabilities capabilities, SessionEpoch session, ContractId serverNonce, CompatibilityDecision decision)
        {
            Protocol = protocol;
            SelectedSchemaHash = selectedSchemaHash;
            Build = build ?? throw new ArgumentNullException(nameof(build));
            Capabilities = capabilities;
            Session = session;
            ServerNonce = serverNonce;
            Decision = decision;
        }

        public ProtocolVersion Protocol { get; }

        public Optional<AssetHash> SelectedSchemaHash { get; }

        public BuildIdentity Build { get; }

        public ProtocolCapabilities Capabilities { get; }

        public SessionEpoch Session { get; }

        public ContractId ServerNonce { get; }

        public CompatibilityDecision Decision { get; }

        public bool Accepted => Decision == CompatibilityDecision.Accepted;

        public override string ToString() => $"ServerHello(protocol={Protocol}, decision={Decision}, capabilities={Capabilities})";
    }

    public sealed class HandshakePolicy
    {
        private readonly ReadOnlyCollection<AssetHash> m_SchemaHashes;
        private readonly Func<ContractId> m_NonceFactory;

        public HandshakePolicy(ProtocolVersion protocol, IEnumerable<AssetHash> schemaHashes, BuildIdentity build, ProtocolCapabilities capabilities, ProtocolCapabilities requiredCapabilities, Func<ContractId> nonceFactory)
        {
            if (!protocol.IsValid)
                throw new ArgumentException("A valid protocol version is required.", nameof(protocol));
            if (schemaHashes == null)
                throw new ArgumentNullException(nameof(schemaHashes));
            if (build == null)
                throw new ArgumentNullException(nameof(build));
            if ((capabilities & requiredCapabilities) != requiredCapabilities)
                throw new ArgumentException("Required capabilities must be offered by the host.", nameof(requiredCapabilities));

            List<AssetHash> hashes = new(schemaHashes);
            if (hashes.Count == 0 || hashes.Count > 16 || hashes.Exists(hash => !hash.IsValid))
                throw new ArgumentException("Between one and sixteen valid schema hashes are required.", nameof(schemaHashes));

            Protocol = protocol;
            m_SchemaHashes = hashes.AsReadOnly();
            Build = build;
            Capabilities = capabilities;
            RequiredCapabilities = requiredCapabilities;
            m_NonceFactory = nonceFactory ?? throw new ArgumentNullException(nameof(nonceFactory));
        }

        public ProtocolVersion Protocol { get; }

        public IReadOnlyList<AssetHash> SchemaHashes => m_SchemaHashes;

        public BuildIdentity Build { get; }

        public ProtocolCapabilities Capabilities { get; }

        public ProtocolCapabilities RequiredCapabilities { get; }

        internal ContractId CreateNonce()
        {
            ContractId nonce = m_NonceFactory();
            if (!nonce.IsValid)
                throw new InvalidOperationException("The nonce factory returned an invalid identifier.");
            return nonce;
        }
    }

    public static class HandshakeNegotiator
    {
        public static ServerHello Negotiate(ClientHello client, HandshakePolicy host, SessionEpoch session)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            if (host == null)
                throw new ArgumentNullException(nameof(host));
            if (!session.IsValid)
                throw new ArgumentException("A valid session is required.", nameof(session));

            ProtocolVersion selected = new(host.Protocol.Major, (ushort)Math.Min(host.Protocol.Minor, client.Protocol.Minor));
            if (client.Protocol.Major != host.Protocol.Major)
                return Reject(CompatibilityDecision.ProtocolIncompatible, selected, host, session);

            Optional<AssetHash> schema = SelectSchema(client.SupportedSchemaHashes, host.SchemaHashes);
            if (!schema.HasValue)
                return Reject(CompatibilityDecision.SchemaIncompatible, selected, host, session);

            ProtocolCapabilities effective = client.Capabilities & host.Capabilities;
            if ((effective & host.RequiredCapabilities) != host.RequiredCapabilities)
                return Reject(CompatibilityDecision.CapabilitiesIncompatible, selected, host, session);

            return new ServerHello(selected, schema, host.Build, effective, session, host.CreateNonce(), CompatibilityDecision.Accepted);
        }

        private static ServerHello Reject(CompatibilityDecision decision, ProtocolVersion selected, HandshakePolicy host, SessionEpoch session)
        {
            return new ServerHello(selected, Optional<AssetHash>.None, host.Build, ProtocolCapabilities.None, session, host.CreateNonce(), decision);
        }

        private static Optional<AssetHash> SelectSchema(IReadOnlyList<AssetHash> client, IReadOnlyList<AssetHash> host)
        {
            for (int hostIndex = 0; hostIndex < host.Count; hostIndex++)
            {
                for (int clientIndex = 0; clientIndex < client.Count; clientIndex++)
                {
                    if (host[hostIndex] == client[clientIndex])
                        return Optional<AssetHash>.Some(host[hostIndex]);
                }
            }

            return Optional<AssetHash>.None;
        }
    }
}
