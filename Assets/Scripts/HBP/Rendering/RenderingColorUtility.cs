using System;
using UnityEngine;

namespace HBP.Rendering
{
    public static class RenderingColorUtility
    {
        public static Color SrgbToLinear(Color color)
        {
            return new Color(SrgbToLinear(color.r), SrgbToLinear(color.g), SrgbToLinear(color.b), color.a);
        }

        public static Color LinearToSrgb(Color color)
        {
            return new Color(LinearToSrgb(color.r), LinearToSrgb(color.g), LinearToSrgb(color.b), color.a);
        }

        public static float NormalizeRange(float value, float minimum, float maximum)
        {
            if (maximum < minimum)
                throw new ArgumentException("Maximum must be greater than or equal to minimum.");

            return maximum == minimum ? 0.5f : Mathf.Clamp01((value - minimum) / (maximum - minimum));
        }

        public static float NormalizeDiverging(float value, float minimum, float middle, float maximum)
        {
            if (middle < minimum || maximum < middle)
                throw new ArgumentException("Expected minimum <= middle <= maximum.");

            if (value <= middle)
            {
                return middle == minimum ? 0.5f : 0.5f * Mathf.Clamp01((value - minimum) / (middle - minimum));
            }

            return maximum == middle ? 0.5f : 0.5f + 0.5f * Mathf.Clamp01((value - middle) / (maximum - middle));
        }

        public static int PaletteIndex(float normalizedValue, int colorCount)
        {
            if (colorCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(colorCount));

            return Mathf.RoundToInt(Mathf.Clamp01(normalizedValue) * (colorCount - 1));
        }

        public static Color ComposeScientificColor(Color anatomyLinear, Color scientificSrgb, float alpha)
        {
            Color scientificLinear = SrgbToLinear(scientificSrgb);
            return Color.Lerp(anatomyLinear, scientificLinear, Mathf.Clamp01(alpha));
        }

        public static float ComposeAlpha(float normalizedSourceAlpha, float userAlpha)
        {
            return Mathf.Clamp01(normalizedSourceAlpha * userAlpha);
        }

        private static float SrgbToLinear(float channel)
        {
            return channel <= 0.04045f ? channel / 12.92f : Mathf.Pow((channel + 0.055f) / 1.055f, 2.4f);
        }

        private static float LinearToSrgb(float channel)
        {
            return channel <= 0.0031308f ? 12.92f * channel : 1.055f * Mathf.Pow(channel, 1.0f / 2.4f) - 0.055f;
        }
    }
}
