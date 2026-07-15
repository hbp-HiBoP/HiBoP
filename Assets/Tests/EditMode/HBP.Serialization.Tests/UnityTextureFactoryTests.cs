using System;
using System.IO;
using System.Linq;
using HBP.Core.Enums;
using HBP.Core.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace HBP.Tests.Serialization
{
    public class UnityTextureFactoryTests
    {
        private static readonly ColorType[] AllColorTypes = (ColorType[])Enum.GetValues(typeof(ColorType));

        [Test]
        [Category("NativeMigration")]
        public void Generate1DColorTexture_UsesUnityPixelsWithoutNativeTextureHandle()
        {
            Texture2D texture = UnityTextureFactory.Generate1DColorTexture(ColorType.Grayscale);

            try
            {
                Assert.That(texture.width, Is.EqualTo(UnityTextureFactory.ColormapSize));
                Assert.That(texture.height, Is.EqualTo(1));

                Color32[] pixels = texture.GetPixels32();
                Assert.That(pixels[0], Is.EqualTo(new Color32(0, 0, 0, 255)));
                Assert.That(pixels[^1].r, Is.GreaterThan(250));
                Assert.That(pixels[^1].g, Is.GreaterThan(250));
                Assert.That(pixels[^1].b, Is.GreaterThan(250));
                Assert.That(pixels[^1].a, Is.EqualTo(255));
            }
            finally
            {
                UnityObject.DestroyImmediate(texture);
            }
        }

        [Test]
        [Category("NativeMigration")]
        public void Generate1DColorTexture_KeepsHistoricalBrainColor()
        {
            Texture2D texture = UnityTextureFactory.Generate1DColorTexture(ColorType.BrainColor);

            try
            {
                Color32[] pixels = texture.GetPixels32();
                AssertHistoricalBrainColor(pixels);
            }
            finally
            {
                UnityObject.DestroyImmediate(texture);
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        [Category("NativeParity")]
        [LegacyParityOnly]
        public void Generate1DColorTexture_BrainColorMatchesLegacyOpenCVTexture()
        {
            Color32[] unityPixels = UnityTextureFactory.Generate1DColorPixels(ColorType.BrainColor);
            Color32[] legacyPixels = GenerateLegacy1DTexturePixelsOrIgnore(ColorType.BrainColor);

            Assert.That(legacyPixels.Length, Is.EqualTo(unityPixels.Length));
            AssertPixelsWithinTolerance(legacyPixels, unityPixels, 1);
        }

        [TestCaseSource(nameof(AllColorTypes))]
        [Category("NativeMigration")]
        public void Generate1DColorPixels_EveryColormapHasExpectedOpaqueEndpoints(ColorType colorType)
        {
            Color32[] pixels = UnityTextureFactory.Generate1DColorPixels(colorType);
            (Color32 start, Color32 end) = ExpectedColormapEndpoints(colorType);

            Assert.That(pixels, Has.Length.EqualTo(UnityTextureFactory.ColormapSize));
            Assert.That(pixels[0], Is.EqualTo(start));
            Assert.That(pixels[^1].r, Is.EqualTo(end.r).Within(5), $"{colorType} final red");
            Assert.That(pixels[^1].g, Is.EqualTo(end.g).Within(5), $"{colorType} final green");
            Assert.That(pixels[^1].b, Is.EqualTo(end.b).Within(5), $"{colorType} final blue");
            Assert.That(pixels, Has.All.Matches<Color32>(pixel => pixel.a == 255));
        }

        [Test]
        [Category("NativeMigration")]
        public void Generate2DColorTexture_CombinesHorizontalAndVerticalColormapsInUnity()
        {
            Texture2D texture = UnityTextureFactory.Generate2DColorTexture(ColorType.RedYellow, ColorType.BlueGreen);

            try
            {
                Assert.That(texture.width, Is.EqualTo(UnityTextureFactory.ColormapSize));
                Assert.That(texture.height, Is.EqualTo(UnityTextureFactory.ColormapSize));

                Color32[] pixels = texture.GetPixels32();
                Color32 bottomLeft = pixels[0];
                Color32 topLeft = pixels[(texture.height - 1) * texture.width];

                Assert.That(bottomLeft.b, Is.GreaterThan(250));
                Assert.That(bottomLeft.r, Is.LessThan(5));
                Assert.That(topLeft.r, Is.GreaterThan(250));
                Assert.That(topLeft.g, Is.EqualTo(0));
                Assert.That(topLeft.b, Is.EqualTo(0));
            }
            finally
            {
                UnityObject.DestroyImmediate(texture);
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        [Category("NativeParity")]
        [LegacyParityOnly]
        public void Generate2DColorTexture_MatchesLegacyOpenCVTexture()
        {
            Color32[] unityPixels = UnityTextureFactory.Generate2DColorPixels(ColorType.Default, ColorType.MatLab);
            Color32[] legacyPixels = GenerateLegacy2DTexturePixelsOrIgnore(ColorType.Default, ColorType.MatLab);

            AssertPixelsWithinTolerance(legacyPixels, unityPixels, 1);
        }

        [Test]
        [Category("NativeMigration")]
        public void Generate2DColorPixels_AcceptsEveryColormapOnBothAxesWithOpaqueOutput()
        {
            foreach (ColorType colorType in AllColorTypes)
            {
                Color32[] horizontal = UnityTextureFactory.Generate2DColorPixels(colorType, ColorType.Grayscale, out int horizontalWidth, out int horizontalHeight);
                Assert.That(horizontal, Has.Length.EqualTo(horizontalWidth * horizontalHeight), $"horizontal {colorType}");
                Assert.That(horizontalHeight, Is.EqualTo(UnityTextureFactory.ColormapSize), $"horizontal {colorType}");
                Assert.That(horizontal, Has.All.Matches<Color32>(pixel => pixel.a == 255), $"horizontal {colorType}");

                Color32[] vertical = UnityTextureFactory.Generate2DColorPixels(ColorType.Grayscale, colorType, out int verticalWidth, out int verticalHeight);
                Assert.That(vertical, Has.Length.EqualTo(verticalWidth * verticalHeight), $"vertical {colorType}");
                Assert.That(verticalHeight, Is.EqualTo(UnityTextureFactory.ColormapSize), $"vertical {colorType}");
                Assert.That(vertical, Has.All.Matches<Color32>(pixel => pixel.a == 255), $"vertical {colorType}");
            }
        }

        [Test]
        [Category("NativeMigration")]
        public void GenerateColorPixels_ExposesUnityOwnedBuffersForTemporaryNativeBridges()
        {
            Color32[] oneDimensional = UnityTextureFactory.Generate1DColorPixels(ColorType.BrainColor);
            Color32[] twoDimensional = UnityTextureFactory.Generate2DColorPixels(ColorType.RedYellow, ColorType.BlueGreen);

            Assert.That(oneDimensional.Length, Is.EqualTo(UnityTextureFactory.ColormapSize));
            Assert.That(twoDimensional.Length, Is.EqualTo(UnityTextureFactory.ColormapSize * UnityTextureFactory.ColormapSize));
            AssertHistoricalBrainColor(oneDimensional);
            Assert.That(twoDimensional[0].b, Is.GreaterThan(250));
            Assert.That(twoDimensional[^1].r, Is.GreaterThan(250));
        }

        [Test]
        [Category("NativeMigration")]
        public void GenerateDistributionHistogram_RendersExpectedSizeAndDataLineInUnity()
        {
            float[] values = { -1.0f, -0.5f, 0.0f, 0.25f, 0.5f, 1.0f, 1.0f, 1.0f };
            Texture2D texture = UnityTextureFactory.GenerateDistributionHistogram(values, 64, 128, -1.0f, 1.0f);

            try
            {
                Assert.That(texture.width, Is.EqualTo(128));
                Assert.That(texture.height, Is.EqualTo(64));

                Color32[] pixels = texture.GetPixels32();
                Assert.That(pixels.Count(pixel => pixel.r == 255 && pixel.g == 0 && pixel.b == 0), Is.GreaterThan(0));
            }
            finally
            {
                UnityObject.DestroyImmediate(texture);
            }
        }

        [Test]
        [Category("NativeMigration")]
        public void GenerateDistributionHistogram_FromBinsRendersWithoutNativeTextureHandle()
        {
            int[] bins = { 0, 2, 8, 4, 1 };
            Texture2D texture = UnityTextureFactory.GenerateDistributionHistogram(bins, 32, 64, false);

            try
            {
                Assert.That(texture.width, Is.EqualTo(64));
                Assert.That(texture.height, Is.EqualTo(32));

                Color32[] pixels = texture.GetPixels32();
                Assert.That(pixels.Count(pixel => pixel.r == 255 && pixel.g == 0 && pixel.b == 0), Is.GreaterThan(bins.Length));
            }
            finally
            {
                UnityObject.DestroyImmediate(texture);
            }
        }

        [Test]
        [Category("NativeMigration")]
        public void GenerateDistributionHistogramPixels_BinsUseSquareRootScalingAndGreyAreaOption()
        {
            int[] bins = { 0, 4, 0 };

            Color32[] lineOnly = UnityTextureFactory.GenerateDistributionHistogramPixels(bins, 5, 5, false);
            Assert.That(lineOnly[0], Is.EqualTo(new Color32(255, 0, 0, 255)), "left zero endpoint");
            Assert.That(lineOnly[4 * 5 + 2], Is.EqualTo(new Color32(255, 0, 0, 255)), "maximum endpoint");
            Assert.That(lineOnly[4], Is.EqualTo(new Color32(255, 0, 0, 255)), "right zero endpoint");
            Assert.That(lineOnly, Has.Some.EqualTo(new Color32(40, 40, 40, 255)));

            Color32[] grey = UnityTextureFactory.GenerateDistributionHistogramPixels(new int[3], 3, 4, true);
            Assert.That(grey, Has.All.EqualTo(new Color32(90, 90, 90, 255)));
        }

        [Test]
        [Category("NativeMigration")]
        public void GenerateDistributionHistogramPixels_FloatAutoRangeMatchesExplicitRangeAndHandlesEmptyData()
        {
            float[] data = { -2, -1, 0, 1, 2, 2 };

            Color32[] automatic = UnityTextureFactory.GenerateDistributionHistogramPixels(data, 16, 32, withGreyArea: false);
            Color32[] explicitRange = UnityTextureFactory.GenerateDistributionHistogramPixels(data, 16, 32, -2, 2, false);
            Color32[] empty = UnityTextureFactory.GenerateDistributionHistogramPixels(Array.Empty<float>(), 2, 3, withGreyArea: true);

            Assert.That(automatic, Is.EqualTo(explicitRange));
            Assert.That(empty, Has.All.EqualTo(new Color32(40, 40, 40, 255)));
        }

        [Test]
        [Category("NativeMigration")]
        public void UpdateDistributionHistogram_ReinitializesTextureAndRejectsInvalidInputs()
        {
            Texture2D texture = new(1, 1, TextureFormat.RGBA32, false);
            try
            {
                UnityTextureFactory.UpdateDistributionHistogram(texture, new[] { 0, 1, 0 }, 7, 9, false);
                Assert.That(texture.width, Is.EqualTo(9));
                Assert.That(texture.height, Is.EqualTo(7));
                Assert.That(texture.GetPixels32(), Has.Some.EqualTo(new Color32(255, 0, 0, 255)));

                Assert.That(() => UnityTextureFactory.GenerateDistributionHistogramPixels((float[])null, 1, 1), Throws.ArgumentNullException);
                Assert.That(() => UnityTextureFactory.GenerateDistributionHistogramPixels(new[] { 1.0f }, 0, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(() => UnityTextureFactory.GenerateDistributionHistogramPixels(new[] { 1 }, 1, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
            }
            finally
            {
                UnityObject.DestroyImmediate(texture);
            }
        }

        [Test]
        [Category("NativeMigration")]
        public void Texture2DEncodeToPNG_ReplacesNativeTexturePngExport()
        {
            Texture2D texture = new(2, 2, TextureFormat.RGBA32, false);
            texture.SetPixels32(new[]
            {
                new Color32(255, 0, 0, 255),
                new Color32(0, 255, 0, 255),
                new Color32(0, 0, 255, 255),
                new Color32(255, 255, 255, 255)
            });
            texture.Apply();

            try
            {
                byte[] png = texture.EncodeToPNG();

                Assert.That(png, Is.Not.Null);
                Assert.That(png.Length, Is.GreaterThan(8));
                Assert.That(png.Take(8).ToArray(), Is.EqualTo(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }));
            }
            finally
            {
                UnityObject.DestroyImmediate(texture);
            }
        }

        [Test]
        [Category("NativeMigration")]
        public void Texture2DPng_RoundTripPreservesDimensionsBottomLeftOrientationRgbaAndColorType()
        {
            Texture2D source = new(2, 2, TextureFormat.RGBA32, false, linear: true);
            Texture2D decoded = new(1, 1, TextureFormat.RGBA32, false, linear: true);
            Color32[] expected =
            {
                new(255, 0, 0, 17),
                new(0, 255, 0, 64),
                new(0, 0, 255, 128),
                new(240, 180, 120, 255)
            };
            source.SetPixels32(expected);
            source.Apply(false, false);

            try
            {
                byte[] png = source.EncodeToPNG();
                Assert.That(ReadBigEndianInt32(png, 16), Is.EqualTo(2), "IHDR width");
                Assert.That(ReadBigEndianInt32(png, 20), Is.EqualTo(2), "IHDR height");
                Assert.That(png[24], Is.EqualTo(8), "IHDR bit depth");
                Assert.That(png[25], Is.EqualTo(6), "IHDR RGBA color type");
                Assert.That(ImageConversion.LoadImage(decoded, png, markNonReadable: false), Is.True);
                Assert.That(decoded.width, Is.EqualTo(2));
                Assert.That(decoded.height, Is.EqualTo(2));
                Assert.That(decoded.GetPixels32(), Is.EqualTo(expected));
                Assert.That((Color32)decoded.GetPixel(0, 0), Is.EqualTo(expected[0]), "Unity bottom-left pixel");
                Assert.That((Color32)decoded.GetPixel(1, 1), Is.EqualTo(expected[3]), "Unity top-right pixel");
            }
            finally
            {
                UnityObject.DestroyImmediate(source);
                UnityObject.DestroyImmediate(decoded);
            }
        }

        [Test]
        [Category("NativeMigration")]
        public void Texture2DPng_Rgb24UsesRgbColorTypeAndDecodesOpaque()
        {
            Texture2D source = new(1, 1, TextureFormat.RGB24, false, linear: false);
            Texture2D decoded = new(1, 1, TextureFormat.RGBA32, false, linear: false);
            source.SetPixel(0, 0, new Color32(12, 34, 56, 255));
            source.Apply(false, false);

            try
            {
                byte[] png = source.EncodeToPNG();
                Assert.That(png[25], Is.EqualTo(2), "IHDR RGB color type");
                Assert.That(ImageConversion.LoadImage(decoded, png), Is.True);
                Assert.That((Color32)decoded.GetPixel(0, 0), Is.EqualTo(new Color32(12, 34, 56, 255)));
            }
            finally
            {
                UnityObject.DestroyImmediate(source);
                UnityObject.DestroyImmediate(decoded);
            }
        }

        [Test]
        [Category("NativeMigration")]
        public void RuntimeTextureBridge_IsRemovedFromCoreDllWrappers()
        {
            Assert.That(Type.GetType("HBP.Core.DLL.Texture, Assembly-CSharp"), Is.Null);
            string wrapperPath = Path.Combine(Application.dataPath, "Scripts", "HBP", "Core", "DLL", "Texture.cs");
            Assert.That(File.Exists(wrapperPath), Is.False);
        }

        [Test]
        [Category("NativeMigration")]
        public void DrawCenteredText_UsesUnityPixelsAndTopBasedCoordinates()
        {
            Texture2D texture = new(64, 32, TextureFormat.RGBA32, false);
            Color32[] black = Enumerable.Repeat(new Color32(0, 0, 0, 255), texture.width * texture.height).ToArray();
            texture.SetPixels32(black);

            try
            {
                UnityTextureFactory.DrawCenteredText(texture, "A1", 32, 8);

                Color32[] pixels = texture.GetPixels32();
                int textPixelCount = pixels.Count(pixel => pixel.r == 220 && pixel.g == 220 && pixel.b == 220);
                int topHalfTextPixelCount = pixels
                    .Select((pixel, index) => new { pixel, y = index / texture.width })
                    .Count(item => item.y >= texture.height / 2 && item.pixel.r == 220 && item.pixel.g == 220 && item.pixel.b == 220);

                Assert.That(textPixelCount, Is.GreaterThan(0));
                Assert.That(topHalfTextPixelCount, Is.EqualTo(textPixelCount));
            }
            finally
            {
                UnityObject.DestroyImmediate(texture);
            }
        }

        [Test]
        [Category("NativeMigration")]
        public void DrawCenteredText_RendersVideoLabelsWithLowercaseDegreeAndDecimalTime()
        {
            Texture2D lowercase = CreateBlackTexture(160, 40);
            Texture2D uppercase = CreateBlackTexture(160, 40);
            Texture2D time = CreateBlackTexture(160, 40);

            try
            {
                UnityTextureFactory.DrawCenteredText(lowercase, "Column n\u00B01", 80, 10);
                UnityTextureFactory.DrawCenteredText(uppercase, "COLUMN N\u00B01", 80, 10);
                UnityTextureFactory.DrawCenteredText(time, "276097.38ms", 80, 30);

                Color32[] lowercasePixels = lowercase.GetPixels32();
                Color32[] uppercasePixels = uppercase.GetPixels32();
                Color32[] timePixels = time.GetPixels32();

                Assert.That(lowercasePixels.Count(IsVideoTextPixel), Is.GreaterThan(0));
                Assert.That(timePixels.Count(IsVideoTextPixel), Is.GreaterThan(0));
                Assert.That(lowercasePixels, Is.Not.EqualTo(uppercasePixels));
            }
            finally
            {
                UnityObject.DestroyImmediate(lowercase);
                UnityObject.DestroyImmediate(uppercase);
                UnityObject.DestroyImmediate(time);
            }
        }

        [Test]
        [Category("NativeMigration")]
        public void DrawSiteMarkers_ColorsProjectedSitesRedOnUnityPixels()
        {
            Color32[] pixels = Enumerable.Repeat(new Color32(0, 0, 0, 255), 16).ToArray();

            UnityTextureFactory.DrawSiteMarkers(pixels, 4, 4, new[] { new Vector2(1.0f / 3.0f, 2.0f / 3.0f) });

            Color32 marker = pixels[2 * 4 + 1];
            Assert.That(marker, Is.EqualTo(new Color32(255, 0, 0, 255)));
            Assert.That(pixels.Count(pixel => pixel.r == 255 && pixel.g == 0 && pixel.b == 0), Is.EqualTo(1));
        }

        [Test]
        [Category("NativeMigration")]
        public void DrawSiteMarkers_ClampsFinitePositionsClipsRadiusAndIgnoresNonFinitePositions()
        {
            Color32[] pixels = Enumerable.Repeat(new Color32(0, 0, 0, 255), 25).ToArray();

            UnityTextureFactory.DrawSiteMarkers(
                pixels,
                5,
                5,
                new[]
                {
                    new Vector2(-2, -3),
                    new Vector2(2, 3),
                    new Vector2(float.NaN, 0.5f),
                    new Vector2(float.PositiveInfinity, 0.5f)
                },
                radius: 1);

            Color32 red = new(255, 0, 0, 255);
            Assert.That(pixels[0], Is.EqualTo(red));
            Assert.That(pixels[1], Is.EqualTo(red));
            Assert.That(pixels[5], Is.EqualTo(red));
            Assert.That(pixels[24], Is.EqualTo(red));
            Assert.That(pixels[23], Is.EqualTo(red));
            Assert.That(pixels[19], Is.EqualTo(red));
            Assert.That(pixels.Count(pixel => pixel.Equals(red)), Is.EqualTo(6));
        }

        [Test]
        [Category("NativeMigration")]
        public void ResizeToSquare_CentersPaddingAndCropsSymmetricallyWithOpaqueBlackBackground()
        {
            Texture2D padded = new(2, 1, TextureFormat.RGBA32, false);
            Texture2D cropped = new(4, 2, TextureFormat.RGBA32, false);
            padded.SetPixels32(new[] { Pixel(1), Pixel(2) });
            cropped.SetPixels32(new[] { Pixel(1), Pixel(2), Pixel(3), Pixel(4), Pixel(5), Pixel(6), Pixel(7), Pixel(8) });
            padded.Apply(false, false);
            cropped.Apply(false, false);

            try
            {
                UnityTextureFactory.ResizeToSquare(padded, 4);
                Assert.That(padded.width, Is.EqualTo(4));
                Assert.That(padded.height, Is.EqualTo(4));
                Color32[] paddedPixels = padded.GetPixels32();
                Assert.That(paddedPixels[1 * 4 + 1], Is.EqualTo(Pixel(1)));
                Assert.That(paddedPixels[1 * 4 + 2], Is.EqualTo(Pixel(2)));
                Assert.That(paddedPixels.Where((_, index) => index != 5 && index != 6), Has.All.EqualTo(new Color32(0, 0, 0, 255)));

                UnityTextureFactory.ResizeToSquare(cropped, 2);
                Assert.That(cropped.GetPixels32().Select(pixel => pixel.r), Is.EqualTo(new byte[] { 2, 3, 6, 7 }));
            }
            finally
            {
                UnityObject.DestroyImmediate(padded);
                UnityObject.DestroyImmediate(cropped);
            }
        }

        [Test]
        [Category("NativeMigration")]
        public void RotateCutPixels_CoversEveryOrientationAndFlipCombination()
        {
            (CutOrientation Orientation, bool Flip, int Width, int Height, byte[] Expected)[] cases =
            {
                (CutOrientation.Sagittal, false, 2, 3, new byte[] { 1, 2, 3, 4, 5, 6 }),
                (CutOrientation.Sagittal, true, 2, 3, new byte[] { 6, 5, 4, 3, 2, 1 }),
                (CutOrientation.Axial, false, 3, 2, new byte[] { 2, 4, 6, 1, 3, 5 }),
                (CutOrientation.Axial, true, 3, 2, new byte[] { 6, 4, 2, 5, 3, 1 }),
                (CutOrientation.Coronal, false, 3, 2, new byte[] { 2, 4, 6, 1, 3, 5 }),
                (CutOrientation.Coronal, true, 3, 2, new byte[] { 5, 3, 1, 6, 4, 2 }),
                (CutOrientation.Custom, false, 2, 3, new byte[] { 1, 2, 3, 4, 5, 6 }),
                (CutOrientation.Custom, true, 2, 3, new byte[] { 1, 2, 3, 4, 5, 6 })
            };

            foreach ((CutOrientation orientation, bool flip, int expectedWidth, int expectedHeight, byte[] expected) in cases)
            {
                Color32[] rotated = UnityTextureFactory.RotateCutPixels(TestPixels2x3(), 2, 3, orientation, flip, out int width, out int height);
                byte[] actual = Enumerable.Range(0, height)
                    .SelectMany(row => Enumerable.Range(0, width).Select(column => PixelId(rotated, width, height, row, column)))
                    .ToArray();
                Assert.That(width, Is.EqualTo(expectedWidth), $"{orientation} flip={flip}");
                Assert.That(height, Is.EqualTo(expectedHeight), $"{orientation} flip={flip}");
                Assert.That(actual, Is.EqualTo(expected), $"{orientation} flip={flip}");
            }
        }

        private static Texture2D CreateBlackTexture(int width, int height)
        {
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(Enumerable.Repeat(new Color32(0, 0, 0, 255), width * height).ToArray());
            texture.Apply(false, false);
            return texture;
        }

        private static bool IsVideoTextPixel(Color32 pixel)
        {
            return pixel.r == 220 && pixel.g == 220 && pixel.b == 220;
        }

        [Test]
        [Category("NativeMigration")]
        public void RotateCutPixels_SagittalFlipMatchesLegacyDoubleFlip()
        {
            Color32[] source = TestPixels2x3();

            Color32[] rotated = UnityTextureFactory.RotateCutPixels(source, 2, 3, CutOrientation.Sagittal, true, out int width, out int height);

            Assert.That(width, Is.EqualTo(2));
            Assert.That(height, Is.EqualTo(3));
            Assert.That(PixelId(rotated, width, height, 0, 0), Is.EqualTo(6));
            Assert.That(PixelId(rotated, width, height, 0, 1), Is.EqualTo(5));
            Assert.That(PixelId(rotated, width, height, 2, 0), Is.EqualTo(2));
            Assert.That(PixelId(rotated, width, height, 2, 1), Is.EqualTo(1));
        }

        [Test]
        [Category("NativeMigration")]
        public void RotateCutPixels_AxialUsesLegacyCounterClockwiseRotation()
        {
            Color32[] source = TestPixels2x3();

            Color32[] rotated = UnityTextureFactory.RotateCutPixels(source, 2, 3, CutOrientation.Axial, false, out int width, out int height);

            Assert.That(width, Is.EqualTo(3));
            Assert.That(height, Is.EqualTo(2));
            Assert.That(PixelId(rotated, width, height, 0, 0), Is.EqualTo(2));
            Assert.That(PixelId(rotated, width, height, 0, 2), Is.EqualTo(6));
            Assert.That(PixelId(rotated, width, height, 1, 0), Is.EqualTo(1));
            Assert.That(PixelId(rotated, width, height, 1, 2), Is.EqualTo(5));
        }

        [Test]
        [Category("NativeMigration")]
        public void RotateCutPixels_CoronalFlipMatchesLegacyReverseCounterClockwiseRotation()
        {
            Color32[] source = TestPixels2x3();

            Color32[] rotated = UnityTextureFactory.RotateCutPixels(source, 2, 3, CutOrientation.Coronal, true, out int width, out int height);

            Assert.That(width, Is.EqualTo(3));
            Assert.That(height, Is.EqualTo(2));
            Assert.That(PixelId(rotated, width, height, 0, 0), Is.EqualTo(5));
            Assert.That(PixelId(rotated, width, height, 0, 2), Is.EqualTo(1));
            Assert.That(PixelId(rotated, width, height, 1, 0), Is.EqualTo(6));
            Assert.That(PixelId(rotated, width, height, 1, 2), Is.EqualTo(2));
        }

        private static Color32[] TestPixels2x3()
        {
            return new[]
            {
                Pixel(5), Pixel(6),
                Pixel(3), Pixel(4),
                Pixel(1), Pixel(2)
            };
        }

        private static Color32 Pixel(byte id)
        {
            return new Color32(id, 0, 0, 255);
        }

        private static (Color32 Start, Color32 End) ExpectedColormapEndpoints(ColorType colorType)
        {
            return colorType switch
            {
                ColorType.Hot => (new Color32(0, 0, 0, 255), new Color32(255, 255, 255, 255)),
                ColorType.Winter => (new Color32(0, 0, 255, 255), new Color32(0, 255, 128, 255)),
                ColorType.Warm => (new Color32(255, 165, 0, 255), new Color32(255, 255, 0, 255)),
                ColorType.Surface => (new Color32(0, 0, 0, 255), new Color32(255, 255, 255, 255)),
                ColorType.Cool => (new Color32(0, 127, 255, 255), new Color32(0, 255, 255, 255)),
                ColorType.RedYellow => (new Color32(255, 0, 0, 255), new Color32(255, 255, 0, 255)),
                ColorType.BlueGreen => (new Color32(0, 0, 255, 255), new Color32(0, 255, 0, 255)),
                ColorType.ACTC => (new Color32(0, 0, 0, 255), new Color32(255, 0, 0, 255)),
                ColorType.Bone => (new Color32(0, 0, 0, 255), new Color32(255, 255, 255, 255)),
                ColorType.GEColor => (new Color32(0, 0, 0, 255), new Color32(255, 255, 255, 255)),
                ColorType.Gold => (new Color32(0, 0, 0, 255), new Color32(255, 255, 255, 255)),
                ColorType.XRain => (new Color32(0, 0, 0, 255), new Color32(255, 0, 0, 255)),
                ColorType.MatLab => (new Color32(0, 0, 255, 255), new Color32(255, 0, 0, 255)),
                ColorType.Default => (new Color32(0, 0, 0, 255), new Color32(255, 255, 255, 255)),
                ColorType.BrainColor => (new Color32(235, 181, 120, 255), new Color32(235, 181, 120, 255)),
                ColorType.White => (new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255)),
                ColorType.SoftGrayscale => (new Color32(150, 150, 150, 255), new Color32(100, 100, 100, 255)),
                _ => (new Color32(0, 0, 0, 255), new Color32(255, 255, 255, 255))
            };
        }

        private static bool IsHistoricalBrainColor(Color32 pixel)
        {
            return pixel.r == 235 && pixel.g == 181 && pixel.b == 120 && pixel.a == 255;
        }

        private static void AssertHistoricalBrainColor(Color32[] pixels)
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                if (IsHistoricalBrainColor(pixels[i]))
                {
                    continue;
                }

                Color32 pixel = pixels[i];
                Assert.Fail($"Pixel {i} was RGBA({pixel.r},{pixel.g},{pixel.b},{pixel.a}).");
            }
        }

        private static byte PixelId(Color32[] pixels, int width, int height, int rowFromTop, int column)
        {
            return pixels[(height - 1 - rowFromTop) * width + column].r;
        }

        private static int ReadBigEndianInt32(byte[] bytes, int offset)
        {
            return bytes[offset] << 24 | bytes[offset + 1] << 16 | bytes[offset + 2] << 8 | bytes[offset + 3];
        }

        private static void AssertPixelsWithinTolerance(Color32[] actual, Color32[] expected, byte tolerance)
        {
            Assert.That(actual.Length, Is.EqualTo(expected.Length));
            for (int i = 0; i < actual.Length; i++)
            {
                Color32 actualPixel = actual[i];
                Color32 expectedPixel = expected[i];
                Assert.That(Mathf.Abs(actualPixel.r - expectedPixel.r), Is.LessThanOrEqualTo(tolerance), $"Pixel {i} red channel");
                Assert.That(Mathf.Abs(actualPixel.g - expectedPixel.g), Is.LessThanOrEqualTo(tolerance), $"Pixel {i} green channel");
                Assert.That(Mathf.Abs(actualPixel.b - expectedPixel.b), Is.LessThanOrEqualTo(tolerance), $"Pixel {i} blue channel");
                Assert.That(Mathf.Abs(actualPixel.a - expectedPixel.a), Is.LessThanOrEqualTo(tolerance), $"Pixel {i} alpha channel");
            }
        }

        private static Color32[] GenerateLegacy1DTexturePixelsOrIgnore(ColorType colorType)
        {
            return GenerateLegacyTexturePixelsOrIgnore(() => LegacyTextureBridge.Generate1D((int)colorType));
        }

        private static Color32[] GenerateLegacy2DTexturePixelsOrIgnore(ColorType horizontalColorType, ColorType verticalColorType)
        {
            return GenerateLegacyTexturePixelsOrIgnore(() => LegacyTextureBridge.Generate2D((int)horizontalColorType, (int)verticalColorType));
        }

        private static Color32[] GenerateLegacyTexturePixelsOrIgnore(Func<LegacyTextureBridge> createTexture)
        {
            LegacyTextureBridge texture = null;
            try
            {
                texture = createTexture();
                if (texture.Handle == IntPtr.Zero)
                {
                    Assert.Ignore("hbp_export returned a null legacy texture.");
                }
                return texture.GetPixels(out _, out _);
            }
            catch (Exception exception) when (IsMissingLegacyTextureDependency(exception))
            {
                Assert.Ignore($"hbp_export texture comparison unavailable: {exception.Message}");
                return Array.Empty<Color32>();
            }
            finally
            {
                texture?.Dispose();
            }
        }

        private static bool IsMissingLegacyTextureDependency(Exception exception)
        {
            return exception is DllNotFoundException
                or EntryPointNotFoundException
                or BadImageFormatException
                || exception.InnerException != null && IsMissingLegacyTextureDependency(exception.InnerException);
        }
    }
}
