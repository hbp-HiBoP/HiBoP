using System;

namespace CRNL.HiBoP.Contracts
{
    public enum ScopeType : byte
    {
        Unknown = 0,
        Project = 1,
        Visualization = 2,
        Column = 3,
        BrainInstance = 4,
        Site = 5,
        Cut = 6,
        Roi = 7,
        Timeline = 8,
    }

    public enum ScopeOwner : byte
    {
        Unknown = 0,
        Desktop = 1,
        Quest = 2,
    }

    public readonly struct ScopeKey : IComparable<ScopeKey>, IEquatable<ScopeKey>
    {
        public ScopeKey(ScopeType type, ContractId id)
        {
            if (!ScopeOwnership.IsDefined(type))
                throw new ArgumentOutOfRangeException(nameof(type));
            if (!id.IsValid)
                throw new ArgumentException("A valid scope identifier is required.", nameof(id));

            Type = type;
            Id = id;
        }

        public ScopeType Type { get; }

        public ContractId Id { get; }

        public ScopeOwner Owner => ScopeOwnership.GetOwner(Type);

        public bool IsValid => ScopeOwnership.IsDefined(Type) && Id.IsValid;

        public int CompareTo(ScopeKey other)
        {
            int typeComparison = Type.CompareTo(other.Type);
            return typeComparison != 0 ? typeComparison : Id.CompareTo(other.Id);
        }

        public bool Equals(ScopeKey other)
        {
            return Type == other.Type && Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            return obj is ScopeKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Type * 397) ^ Id.GetHashCode();
            }
        }

        public override string ToString()
        {
            return $"Scope(type={Type}, id={Id})";
        }

        public static bool operator ==(ScopeKey left, ScopeKey right) => left.Equals(right);

        public static bool operator !=(ScopeKey left, ScopeKey right) => !left.Equals(right);
    }

    public static class ScopeOwnership
    {
        public static bool IsDefined(ScopeType type)
        {
            return type >= ScopeType.Project && type <= ScopeType.Timeline;
        }

        public static ScopeOwner GetOwner(ScopeType type)
        {
            if (!IsDefined(type))
                throw new ArgumentOutOfRangeException(nameof(type));
            return type == ScopeType.BrainInstance ? ScopeOwner.Quest : ScopeOwner.Desktop;
        }
    }

    public readonly struct PropertyKey : IComparable<PropertyKey>, IEquatable<PropertyKey>
    {
        public PropertyKey(uint value)
        {
            if (value == 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }

        public uint Value { get; }

        public bool IsValid => Value != 0;

        public int CompareTo(PropertyKey other) => Value.CompareTo(other.Value);

        public bool Equals(PropertyKey other) => Value == other.Value;

        public override bool Equals(object obj) => obj is PropertyKey other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => $"PropertyKey({Value})";

        public static bool operator ==(PropertyKey left, PropertyKey right) => left.Equals(right);

        public static bool operator !=(PropertyKey left, PropertyKey right) => !left.Equals(right);
    }

    /// <summary>
    /// Stable V1 logical property identifiers. Values are never reused.
    /// </summary>
    public static class V1PropertyKeys
    {
        public static readonly PropertyKey ProjectVisualizationMembership = new(1001);

        public static readonly PropertyKey VisualizationColumnMembership = new(2001);
        public static readonly PropertyKey VisualizationSurfaceAsset = new(2002);
        public static readonly PropertyKey VisualizationSurfaceRepresentation = new(2003);
        public static readonly PropertyKey VisualizationHemisphereVisibility = new(2004);
        public static readonly PropertyKey VisualizationShowEdges = new(2005);
        public static readonly PropertyKey VisualizationTransparentBrain = new(2006);
        public static readonly PropertyKey VisualizationBrainOpacity = new(2007);
        public static readonly PropertyKey VisualizationColorMap = new(2008);
        public static readonly PropertyKey VisualizationHideBlacklistedSites = new(2009);
        public static readonly PropertyKey VisualizationShowAllSites = new(2010);
        public static readonly PropertyKey VisualizationAutomaticCut = new(2011);

        public static readonly PropertyKey ColumnKind = new(3001);
        public static readonly PropertyKey ColumnSelected = new(3002);
        public static readonly PropertyKey ColumnActivityOpacity = new(3003);
        public static readonly PropertyKey ColumnMaximumInfluence = new(3004);
        public static readonly PropertyKey ColumnThresholds = new(3005);
        public static readonly PropertyKey ColumnVisibilityBands = new(3006);
        public static readonly PropertyKey ColumnSelectedLabel = new(3007);
        public static readonly PropertyKey ColumnIncludedInTimeline = new(3008);

        public static readonly PropertyKey BrainInstanceBinding = new(4001);
        public static readonly PropertyKey BrainInstancePose = new(4002);
        public static readonly PropertyKey BrainInstanceScale = new(4003);
        public static readonly PropertyKey BrainInstanceVisible = new(4004);

        public static readonly PropertyKey SiteEntity = new(5001);
        public static readonly PropertyKey SiteColumn = new(5002);
        public static readonly PropertyKey SiteSelected = new(5003);
        public static readonly PropertyKey SiteBlacklisted = new(5004);
        public static readonly PropertyKey SiteHighlighted = new(5005);
        public static readonly PropertyKey SiteColor = new(5006);

        public static readonly PropertyKey CutPlane = new(6001);
        public static readonly PropertyKey CutFlip = new(6002);
        public static readonly PropertyKey CutVisible = new(6003);
        public static readonly PropertyKey CutColumnBindings = new(6004);

        public static readonly PropertyKey RoiDefinition = new(7001);
        public static readonly PropertyKey RoiSelectedElement = new(7002);
        public static readonly PropertyKey RoiActive = new(7003);

        public static readonly PropertyKey TimelineLogicalTime = new(8001);
        public static readonly PropertyKey TimelineSample = new(8002);
        public static readonly PropertyKey TimelinePlaybackState = new(8003);
        public static readonly PropertyKey TimelineLooping = new(8004);
        public static readonly PropertyKey TimelineSpeed = new(8005);
        public static readonly PropertyKey TimelineSamplingPolicy = new(8006);
    }
}
