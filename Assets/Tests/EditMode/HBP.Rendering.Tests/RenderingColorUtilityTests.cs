using HBP.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Rendering
{
    public class RenderingColorUtilityTests
    {
        private const float Tolerance = 0.00001f;

        [Test]
        public void SrgbToLinear_UsesTheStandardTransferFunctionExactlyOnce()
        {
            Color linear = RenderingColorUtility.SrgbToLinear(new Color(0.0f, 0.04045f, 0.5f, 0.25f));

            Assert.That(linear.r, Is.EqualTo(0.0f).Within(Tolerance));
            Assert.That(linear.g, Is.EqualTo(0.0031308f).Within(Tolerance));
            Assert.That(linear.b, Is.EqualTo(0.21404114f).Within(Tolerance));
            Assert.That(linear.a, Is.EqualTo(0.25f));
        }

        [Test]
        public void SrgbLinearRoundTrip_PreservesRepresentativePaletteColor()
        {
            Color paletteColor = new Color(235.0f / 255.0f, 181.0f / 255.0f, 120.0f / 255.0f, 0.7f);

            Color roundTrip = RenderingColorUtility.LinearToSrgb(RenderingColorUtility.SrgbToLinear(paletteColor));

            Assert.That(roundTrip.r, Is.EqualTo(paletteColor.r).Within(Tolerance));
            Assert.That(roundTrip.g, Is.EqualTo(paletteColor.g).Within(Tolerance));
            Assert.That(roundTrip.b, Is.EqualTo(paletteColor.b).Within(Tolerance));
            Assert.That(roundTrip.a, Is.EqualTo(paletteColor.a));
        }

        [TestCase(-2.0f, 0.0f)]
        [TestCase(-1.0f, 0.0f)]
        [TestCase(0.0f, 0.5f)]
        [TestCase(1.0f, 1.0f)]
        [TestCase(2.0f, 1.0f)]
        public void NormalizeRange_ClampsDeterministically(float value, float expected)
        {
            Assert.That(RenderingColorUtility.NormalizeRange(value, -1.0f, 1.0f), Is.EqualTo(expected));
        }

        [Test]
        public void NormalizeRange_DegenerateRangeMapsToCenter()
        {
            Assert.That(RenderingColorUtility.NormalizeRange(12.0f, 4.0f, 4.0f), Is.EqualTo(0.5f));
        }

        [TestCase(-4.0f, 0.0f)]
        [TestCase(-2.0f, 0.0f)]
        [TestCase(-1.0f, 0.25f)]
        [TestCase(0.0f, 0.5f)]
        [TestCase(2.0f, 0.75f)]
        [TestCase(4.0f, 1.0f)]
        [TestCase(8.0f, 1.0f)]
        public void NormalizeDiverging_MapsBothSidesAndClamps(float value, float expected)
        {
            Assert.That(RenderingColorUtility.NormalizeDiverging(value, -2.0f, 0.0f, 4.0f), Is.EqualTo(expected));
        }

        [TestCase(-1.0f, 0)]
        [TestCase(0.0f, 0)]
        [TestCase(0.5f, 2)]
        [TestCase(1.0f, 4)]
        [TestCase(2.0f, 4)]
        public void PaletteIndex_UsesClampedNearestNeighborMapping(float value, int expected)
        {
            Assert.That(RenderingColorUtility.PaletteIndex(value, 5), Is.EqualTo(expected));
        }

        [Test]
        public void ScientificColor_IsConvertedThenComposedAfterAnatomy()
        {
            Color anatomyLinear = new Color(0.1f, 0.2f, 0.3f, 1.0f);
            Color scientificSrgb = new Color(0.5f, 0.25f, 1.0f, 1.0f);
            Color scientificLinear = RenderingColorUtility.SrgbToLinear(scientificSrgb);

            Color result = RenderingColorUtility.ComposeScientificColor(anatomyLinear, scientificSrgb, 1.0f);

            Assert.That(result.r, Is.EqualTo(scientificLinear.r).Within(Tolerance));
            Assert.That(result.g, Is.EqualTo(scientificLinear.g).Within(Tolerance));
            Assert.That(result.b, Is.EqualTo(scientificLinear.b).Within(Tolerance));
        }

        [TestCase(0.4f, 0.5f, 0.2f)]
        [TestCase(2.0f, 1.0f, 1.0f)]
        [TestCase(-1.0f, 0.5f, 0.0f)]
        public void ComposeAlpha_MultipliesAndSaturates(float source, float user, float expected)
        {
            Assert.That(RenderingColorUtility.ComposeAlpha(source, user), Is.EqualTo(expected));
        }
    }
}
