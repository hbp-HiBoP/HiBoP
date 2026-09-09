using HBP.Core.Enums;

namespace HBP.Core.Object3D
{
    /// <summary>Scientific sphere scale, color category and visibility. No material, selection or placement.
    /// Consumers map Type through the existing palette and apply their local gain separately.</summary>
    public readonly struct SiteAppearance
    {
        public float Scale { get; }
        public SiteType Type { get; }
        public bool Visible { get; }

        private SiteAppearance(float scale, SiteType type, bool visible)
        {
            Scale = scale;
            Type = type;
            Visible = visible;
        }

        /// <summary>Value is already sampled (TemporalSample.Evaluate for Desktop sites), using the
        /// full prepared series' normalization range. The middle belongs to the negative category.</summary>
        public static SiteAppearance FromActivity(float value, float minimum, float middle, float maximum)
        {
            if (value < minimum) value = minimum;
            if (value > maximum) value = maximum;
            value -= middle;
            if (value < 0) return new SiteAppearance(0.5f + 2 * (value / (minimum - middle)), SiteType.Negative, true);
            if (value > 0) return new SiteAppearance(0.5f + 2 * (value / (maximum - middle)), SiteType.Positive, true);
            return new SiteAppearance(0.5f, SiteType.Negative, true);
        }

        /// <summary>Rendering exclusions differ from projection masks: a blacklisted contact can
        /// remain visible, at unit scale, although it contributes no projected activity.</summary>
        public static SiteAppearance Resolve(float activityScale, bool positive, bool masked, bool outOfRoi, bool filtered, bool blacklisted, bool showAllSites, bool hideBlacklistedSites, bool upToDate)
        {
            bool visible = !masked && (!outOfRoi || showAllSites) && filtered && (!blacklisted || !hideBlacklistedSites);
            if (blacklisted) return new SiteAppearance(1, SiteType.BlackListed, visible);
            if (!upToDate) return new SiteAppearance(1, SiteType.Normal, visible);
            return new SiteAppearance(activityScale, positive ? SiteType.Positive : SiteType.Negative, visible);
        }
    }
}
