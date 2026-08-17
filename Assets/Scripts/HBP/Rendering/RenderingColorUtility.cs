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
            return Color.Lerp(anatomyLinear, scientificLinear, RemapScientificAlpha(alpha));
        }

        public static float RemapScientificAlpha(float alpha)
        {
            float transparency = 1.0f - Mathf.Clamp01(alpha);
            return 1.0f - transparency * transparency;
        }

        public static float ComposeAlpha(float normalizedSourceAlpha, float userAlpha)
        {
            return Mathf.Clamp01(normalizedSourceAlpha * userAlpha);
        }

        public static void ConvertPremultipliedToStraightAlpha(Texture2D texture)
        {
            if (texture == null)
                throw new ArgumentNullException(nameof(texture));

            Color32[] pixels = texture.GetPixels32();
            ConvertPremultipliedToStraightAlpha(pixels);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }

        public static void ConvertPremultipliedToStraightAlpha(Color32[] pixels)
        {
            if (pixels == null)
                throw new ArgumentNullException(nameof(pixels));

            for (int index = 0; index < pixels.Length; ++index)
            {
                Color32 pixel = pixels[index];
                if (pixel.a == 0)
                {
                    pixels[index] = new Color32(0, 0, 0, 0);
                    continue;
                }

                if (pixel.a == byte.MaxValue)
                    continue;

                pixels[index] = new Color32(UnpremultiplySrgb(pixel.r, pixel.a), UnpremultiplySrgb(pixel.g, pixel.a), UnpremultiplySrgb(pixel.b, pixel.a), pixel.a);
            }
        }

        private static byte UnpremultiplySrgb(byte channel, byte alpha)
        {
            float encodedPremultiplied = channel / (float)byte.MaxValue;
            float linearStraight = SrgbToLinear(encodedPremultiplied) / (alpha / (float)byte.MaxValue);
            float encodedStraight = LinearToSrgb(Mathf.Clamp01(linearStraight));
            return (byte)Mathf.RoundToInt(encodedStraight * byte.MaxValue);
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
