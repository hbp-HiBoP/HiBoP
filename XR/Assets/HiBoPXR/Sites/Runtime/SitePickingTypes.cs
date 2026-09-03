using System;
using CRNL.HiBoP.Contracts;
using UnityEngine;

namespace CRNL.HiBoP.XR.Sites
{
    public readonly struct SiteSelectionContext : IEquatable<SiteSelectionContext>
    {
        public SiteSelectionContext(SessionEpoch session, ContractId columnId, StateRevision stateRevision, ScopeRevision scopeRevision) : this(session, columnId, new ScopeKey(ScopeType.Column, columnId), stateRevision, scopeRevision)
        {
        }

        public SiteSelectionContext(SessionEpoch session, ContractId columnId, ScopeKey columnScope, StateRevision stateRevision, ScopeRevision scopeRevision)
        {
            if (!session.IsValid)
                throw new ArgumentException("A valid session is required.", nameof(session));
            if (!columnId.IsValid)
                throw new ArgumentException("A valid column ID is required.", nameof(columnId));
            if (!columnScope.IsValid || columnScope.Type != ScopeType.Column)
                throw new ArgumentException("A valid column scope is required.", nameof(columnScope));
            Session = session;
            ColumnId = columnId;
            ColumnScope = columnScope;
            StateRevision = stateRevision;
            ScopeRevision = scopeRevision;
        }

        public SessionEpoch Session { get; }

        public ContractId ColumnId { get; }

        public ScopeKey ColumnScope { get; }

        public StateRevision StateRevision { get; }

        public ScopeRevision ScopeRevision { get; }

        public bool IsValid => Session.IsValid && ColumnId.IsValid && ColumnScope.IsValid && ColumnScope.Type == ScopeType.Column;

        public bool Equals(SiteSelectionContext other) => Session == other.Session && ColumnId == other.ColumnId && ColumnScope == other.ColumnScope && StateRevision == other.StateRevision && ScopeRevision == other.ScopeRevision;

        public override bool Equals(object obj) => obj is SiteSelectionContext other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Session.GetHashCode();
                hash = (hash * 397) ^ ColumnId.GetHashCode();
                hash = (hash * 397) ^ ColumnScope.GetHashCode();
                hash = (hash * 397) ^ StateRevision.GetHashCode();
                return (hash * 397) ^ ScopeRevision.GetHashCode();
            }
        }

        public static bool operator ==(SiteSelectionContext left, SiteSelectionContext right) => left.Equals(right);

        public static bool operator !=(SiteSelectionContext left, SiteSelectionContext right) => !left.Equals(right);
    }

    public readonly struct SiteDirtyRange
    {
        public SiteDirtyRange(int start, int count)
        {
            if (start < 0)
                throw new ArgumentOutOfRangeException(nameof(start));
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            Start = start;
            Count = count;
        }

        public int Start { get; }

        public int Count { get; }
    }

    public readonly struct SitePickResult
    {
        internal SitePickResult(int index, ContractId siteId, float rayDistanceMillimeters, float surfaceDistanceMillimeters, Vector3 localPointMillimeters)
        {
            Index = index;
            SiteId = siteId;
            RayDistanceMillimeters = rayDistanceMillimeters;
            SurfaceDistanceMillimeters = surfaceDistanceMillimeters;
            LocalPointMillimeters = localPointMillimeters;
        }

        public bool Hit => Index >= 0;

        public int Index { get; }

        public ContractId SiteId { get; }

        public float RayDistanceMillimeters { get; }

        public float SurfaceDistanceMillimeters { get; }

        public Vector3 LocalPointMillimeters { get; }

        public static SitePickResult None => new(-1, default, float.PositiveInfinity, float.PositiveInfinity, default);
    }

    public enum SiteMeasurementRole : byte
    {
        Unknown = 0,
        Activity = 1,
        Amplitude = 2,
        Latency = 3,
    }

    public enum SiteMeasurementUnit : byte
    {
        Unknown = 0,
        Volt = 1,
        Millivolt = 2,
        Microvolt = 3,
        Second = 4,
        Millisecond = 5,
    }

    public readonly struct SiteSelectionMeasurement
    {
        public SiteSelectionMeasurement(SiteMeasurementRole role, float value, SiteMeasurementUnit unit)
        {
            if (role == SiteMeasurementRole.Unknown)
                throw new ArgumentOutOfRangeException(nameof(role));
            if (unit == SiteMeasurementUnit.Unknown)
                throw new ArgumentOutOfRangeException(nameof(unit));
            if (!IsFinite(value))
                throw new ArgumentOutOfRangeException(nameof(value));
            bool voltage = unit == SiteMeasurementUnit.Volt || unit == SiteMeasurementUnit.Millivolt || unit == SiteMeasurementUnit.Microvolt;
            bool time = unit == SiteMeasurementUnit.Second || unit == SiteMeasurementUnit.Millisecond;
            if ((role == SiteMeasurementRole.Latency && !time) || (role != SiteMeasurementRole.Latency && !voltage))
                throw new ArgumentException("Measurement unit is incompatible with its allowlisted role.", nameof(unit));
            Role = role;
            Value = value;
            Unit = unit;
        }

        public SiteMeasurementRole Role { get; }

        public float Value { get; }

        public SiteMeasurementUnit Unit { get; }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    /// <summary>Transient allowlisted selection data. Do not serialize, cache or log this type.</summary>
    public sealed class SiteSelectionMetadata : IDisposable
    {
        private SiteSelectionMeasurement[] m_Measurements;

        public SiteSelectionMetadata(SessionEpoch session, ContractId siteId, ContractId columnId, StateRevision sourceStateRevision, string siteLabel, SiteSelectionMeasurement[] measurements, bool selected, bool highlighted, bool blacklisted)
        {
            if (!session.IsValid)
                throw new ArgumentException("A valid session is required.", nameof(session));
            if (!siteId.IsValid)
                throw new ArgumentException("A valid site ID is required.", nameof(siteId));
            if (!columnId.IsValid)
                throw new ArgumentException("A valid column ID is required.", nameof(columnId));
            ValidateLabel(siteLabel);
            if (measurements == null)
                throw new ArgumentNullException(nameof(measurements));
            if (measurements.Length > 2)
                throw new ArgumentException("At most two allowlisted measurements may be displayed.", nameof(measurements));

            Session = session;
            SiteId = siteId;
            ColumnId = columnId;
            SourceStateRevision = sourceStateRevision;
            SiteLabel = siteLabel;
            m_Measurements = (SiteSelectionMeasurement[])measurements.Clone();
            Selected = selected;
            Highlighted = highlighted;
            Blacklisted = blacklisted;
        }

        public SessionEpoch Session { get; private set; }

        public ContractId SiteId { get; private set; }

        public ContractId ColumnId { get; private set; }

        public StateRevision SourceStateRevision { get; private set; }

        public string SiteLabel { get; private set; }

        public SiteSelectionMeasurement[] Measurements => m_Measurements == null ? Array.Empty<SiteSelectionMeasurement>() : (SiteSelectionMeasurement[])m_Measurements.Clone();

        public bool Selected { get; private set; }

        public bool Highlighted { get; private set; }

        public bool Blacklisted { get; private set; }

        public bool IsCleared => !SiteId.IsValid;

        public void Dispose()
        {
            Session = default;
            SiteId = default;
            ColumnId = default;
            SourceStateRevision = default;
            SiteLabel = null;
            if (m_Measurements != null)
                Array.Clear(m_Measurements, 0, m_Measurements.Length);
            m_Measurements = null;
            Selected = false;
            Highlighted = false;
            Blacklisted = false;
        }

        private static void ValidateLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label) || label.Length > 64)
                throw new ArgumentException("The transient site label must contain 1 to 64 characters.", nameof(label));
            for (int index = 0; index < label.Length; index++)
            {
                if (char.IsControl(label[index]))
                    throw new ArgumentException("The transient site label cannot contain control characters.", nameof(label));
            }
        }
    }
}
