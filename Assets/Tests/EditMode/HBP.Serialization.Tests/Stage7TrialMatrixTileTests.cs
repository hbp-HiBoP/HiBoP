using System;
using System.Linq;
using HBP.Core.Data;
using HBP.Core.DLL;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Serialization
{
    public class Stage7TrialMatrixTileTests
    {
        private static readonly Vector2 Limits = new(0, 1);

        [Test]
        public void WidthBeyondLimit_IsSplitWithoutChangingPixels()
        {
            float[][] values = CreateValues(2, 9);
            TrialMatrixTiles result = TrialMatrixTileBuilder.Build(values, Limits, Grayscale(), false, 1, false, 4);

            Assert.That(result.Tiles.Count, Is.EqualTo(3));
            Assert.That(result.Tiles.All(tile => tile.TextureWidth <= 4 && tile.TextureHeight <= 4), Is.True);
            AssertPixelsEqual(ReferencePixels(values), Reconstruct(result));
        }

        [Test]
        public void HeightBeyondLimit_IsSplitWithoutChangingPixels()
        {
            float[][] values = CreateValues(9, 2);
            TrialMatrixTiles result = TrialMatrixTileBuilder.Build(values, Limits, Grayscale(), false, 1, false, 4);

            Assert.That(result.Tiles.Count, Is.EqualTo(3));
            Assert.That(result.Tiles.All(tile => tile.TextureWidth <= 4 && tile.TextureHeight <= 4), Is.True);
            AssertPixelsEqual(ReferencePixels(values), Reconstruct(result));
        }

        [Test]
        public void BothDimensionsBeyondLimit_CoverEveryPixelExactlyOnce()
        {
            float[][] values = CreateValues(7, 9);
            TrialMatrixTiles result = TrialMatrixTileBuilder.Build(values, Limits, Grayscale(), false, 1, false, 4);

            Assert.That(result.Tiles.Count, Is.EqualTo(6));
            Assert.That(result.Tiles.Sum(tile => tile.CoreWidth * tile.CoreHeight), Is.EqualTo(63));
            AssertPixelsEqual(ReferencePixels(values), Reconstruct(result));
        }

        [Test]
        public void OneDimensionalSmoothing_MatchesMonolithicReference()
        {
            float[][] values = CreateValues(3, 5);
            float[][] reference = values.Select(row => row.LinearSmooth(3)).ToArray();
            TrialMatrixTiles result = TrialMatrixTileBuilder.Build(values, Limits, Grayscale(), true, 3, false, 5);

            Assert.That(result.Width, Is.EqualTo(13));
            Assert.That(result.Height, Is.EqualTo(3));
            AssertPixelsEqual(ReferencePixels(reference), Reconstruct(result), 1);
        }

        [Test]
        public void TwoDimensionalSmoothing_MatchesMonolithicReferenceAcrossSeams()
        {
            float[][] values = CreateValues(4, 5);
            float[][] reference = values.LinearSmooth2D(3);
            TrialMatrixTiles result = TrialMatrixTileBuilder.Build(values, Limits, Grayscale(), true, 3, true, 5);

            Assert.That(result.Width, Is.EqualTo(13));
            Assert.That(result.Height, Is.EqualTo(10));
            Assert.That(result.Tiles.All(tile => tile.TextureWidth <= 5 && tile.TextureHeight <= 5), Is.True);
            Assert.That(result.Tiles.Any(tile => tile.UvRect.x > 0 || tile.UvRect.y > 0), Is.True);
            AssertPixelsEqual(ReferencePixels(reference), Reconstruct(result), 1);
        }

        [Test]
        public void BilinearTiles_ContainNeighbourPixelsInTheirHalos()
        {
            TrialMatrixTiles result = TrialMatrixTileBuilder.Build(CreateValues(4, 5), Limits, Grayscale(), true, 2, true, 5);
            Color32[] reconstructed = Reconstruct(result);
            TrialMatrixTile tile = result.Tiles.First(candidate => candidate.CoreX > 0 && candidate.CoreY > 0);
            int leftHalo = Mathf.RoundToInt(tile.UvRect.x * tile.TextureWidth);
            int bottomHalo = Mathf.RoundToInt(tile.UvRect.y * tile.TextureHeight);

            Assert.That(leftHalo, Is.EqualTo(1));
            Assert.That(bottomHalo, Is.EqualTo(1));
            Assert.That(tile.Pixels[0], Is.EqualTo(reconstructed[(tile.CoreY - 1) * result.Width + tile.CoreX - 1]));
        }

        [Test]
        public void InvalidJaggedInput_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => TrialMatrixTileBuilder.Build(
                new[] { new[] { 0f }, new[] { 0f, 1f } }, Limits, Grayscale(), false, 1, false, 8));
        }

        [Test]
        public void StreamingLimits_MatchConcatenatedReference()
        {
            float[][] values = CreateValues(5, 7);
            Vector2 expected = values.SelectMany(row => row).ToArray().CalculateValueLimit();
            Vector2 actual = StreamingStatistics.CalculateValueLimit(values);

            Assert.That(actual.x, Is.EqualTo(expected.x).Within(1e-5f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(1e-5f));
        }

        private static float[][] CreateValues(int height, int width)
        {
            float[][] result = new float[height][];
            for (int y = 0; y < height; y++)
            {
                result[y] = new float[width];
                for (int x = 0; x < width; x++)
                    result[y][x] = (y * width + x) / (float)Math.Max(1, height * width - 1);
            }
            return result;
        }

        private static Color32[] Grayscale()
        {
            Color32[] colors = new Color32[256];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = new Color32((byte)i, (byte)i, (byte)i, 255);
            return colors;
        }

        private static Color32[] ReferencePixels(float[][] values)
        {
            int height = values.Length;
            int width = values[0].Length;
            Color32[] result = new Color32[width * height];
            Color32[] colors = Grayscale();
            for (int y = 0; y < height; y++)
            {
                float[] row = values[height - 1 - y];
                for (int x = 0; x < width; x++)
                    result[y * width + x] = colors[Mathf.RoundToInt(Mathf.Clamp01(row[x]) * 255)];
            }
            return result;
        }

        private static Color32[] Reconstruct(TrialMatrixTiles result)
        {
            Color32[] pixels = new Color32[result.Width * result.Height];
            foreach (TrialMatrixTile tile in result.Tiles)
            {
                int leftHalo = Mathf.RoundToInt(tile.UvRect.x * tile.TextureWidth);
                int bottomHalo = Mathf.RoundToInt(tile.UvRect.y * tile.TextureHeight);
                for (int y = 0; y < tile.CoreHeight; y++)
                {
                    int source = (y + bottomHalo) * tile.TextureWidth + leftHalo;
                    int destination = (tile.CoreY + y) * result.Width + tile.CoreX;
                    Array.Copy(tile.Pixels, source, pixels, destination, tile.CoreWidth);
                }
            }
            return pixels;
        }

        private static void AssertPixelsEqual(Color32[] expected, Color32[] actual, int tolerance = 0)
        {
            Assert.That(actual.Length, Is.EqualTo(expected.Length));
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.That(Math.Abs(actual[i].r - expected[i].r), Is.LessThanOrEqualTo(tolerance), $"Pixel {i}, red");
                Assert.That(Math.Abs(actual[i].g - expected[i].g), Is.LessThanOrEqualTo(tolerance), $"Pixel {i}, green");
                Assert.That(Math.Abs(actual[i].b - expected[i].b), Is.LessThanOrEqualTo(tolerance), $"Pixel {i}, blue");
                Assert.That(Math.Abs(actual[i].a - expected[i].a), Is.LessThanOrEqualTo(tolerance), $"Pixel {i}, alpha");
            }
        }
    }
}
