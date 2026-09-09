using HBP.Core.Enums;
using HBP.Core.Object3D;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Transfer.Anatomy.Desktop
{
    public class SiteAppearanceTests
    {
        // Independent pre-extraction rule from a564f8c9811a, including the two sequential clamps.
        internal static (float Scale, SiteType Type, bool Visible) Legacy(float value, float min, float middle, float max, bool masked, bool outOfRoi, bool filtered, bool blacklisted, bool showAll, bool hideBlacklisted, bool upToDate)
        {
            if (value < min) value = min;
            if (value > max) value = max;
            value -= middle;
            bool positive;
            if (value < 0)
            {
                positive = false;
                value = 0.5f + 2 * (value / (min - middle));
            }
            else if (value > 0)
            {
                positive = true;
                value = 0.5f + 2 * (value / (max - middle));
            }
            else
            {
                positive = false;
                value = 0.5f;
            }

            if (masked || (outOfRoi && !showAll) || !filtered) return (value, SiteType.Normal, false);
            if (blacklisted) return (1, SiteType.BlackListed, !hideBlacklisted);
            return upToDate ? (value, positive ? SiteType.Positive : SiteType.Negative, true) : (1, SiteType.Normal, true);
        }

        [TestCase(-10f, 0f, 10f)]
        [TestCase(-8f, -1f, 3f)]
        [TestCase(2f, 2f, 2f)]
        [TestCase(0f, 0f, 10f)]
        [TestCase(-10f, 0f, 0f)]
        public void MatchesLegacyAtBoundsAndForEveryVisibilityCombination(float min, float middle, float max)
        {
            foreach (float value in new[] { -100f, min, (min + middle) / 2, middle, (middle + max) / 2, max, 100f, -float.Epsilon, float.Epsilon })
                for (int flags = 0; flags < 128; flags++)
                {
                    bool masked = (flags & 1) != 0, roi = (flags & 2) != 0, filtered = (flags & 4) != 0, blacklisted = (flags & 8) != 0, showAll = (flags & 16) != 0, hide = (flags & 32) != 0, current = (flags & 64) != 0;
                    var activity = SiteAppearance.FromActivity(value, min, middle, max);
                    var actual = SiteAppearance.Resolve(activity.Scale, activity.Type == SiteType.Positive, masked, roi, filtered, blacklisted, showAll, hide, current);
                    var expected = Legacy(value, min, middle, max, masked, roi, filtered, blacklisted, showAll, hide, current);
                    Assert.That(actual.Visible, Is.EqualTo(expected.Visible));
                    if (!actual.Visible) continue; // Hidden Desktop transforms/materials intentionally retain earlier state.
                    Assert.That(actual.Scale, Is.EqualTo(expected.Scale));
                    Assert.That(actual.Type, Is.EqualTo(expected.Type));
                    Assert.That(float.IsNaN(actual.Scale) || float.IsInfinity(actual.Scale), Is.False);
                }
        }

        [Test]
        public void PaletteRemainsTheAuthoredDesktopPalette()
        {
            var palette = Resources.Load<SharedMaterials>("Objects/Shared Materials").Site;
            Assert.That(palette.GetSharedMaterial(false, SiteType.Positive, Color.green), Is.SameAs(palette.Positive.Normal));
            Assert.That(palette.GetSharedMaterial(false, SiteType.Negative, Color.green), Is.SameAs(palette.Negative.Normal));
            Assert.That(palette.GetSharedMaterial(false, SiteType.BlackListed, Color.green), Is.SameAs(palette.Blacklisted.Normal));
        }
    }
}
