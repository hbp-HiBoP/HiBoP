using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using HBP.Core.Enums;
using HBP.Core.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace HBP.Tests.Serialization
{
    public class UnityTextureFactoryTests
    {
        [OneTimeTearDown]
        public void CollectDestroyedUnityObjectWrappersBeforeSceneRestore()
        {
            // DestroyImmediate releases the native textures, but their managed wrappers remain
            // eligible for collection. Collect them before the Unity Test Framework restores a
            // loaded project scene: Unity 6000.5.2f1 can otherwise crash in its liveness scan
            // after this class runs behind the native migration/parity suites.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

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
        public void Generate1DColorTexture_BrainColorMatchesLegacyOpenCVTexture()
        {
            Color32[] unityPixels = UnityTextureFactory.Generate1DColorPixels(ColorType.BrainColor);
            Color32[] legacyPixels = GenerateLegacy1DTexturePixelsOrIgnore(ColorType.BrainColor);

            Assert.That(legacyPixels.Length, Is.EqualTo(unityPixels.Length));
            AssertPixelsWithinTolerance(legacyPixels, unityPixels, 1);
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
        public void Generate2DColorTexture_MatchesLegacyOpenCVTexture()
        {
            Color32[] unityPixels = UnityTextureFactory.Generate2DColorPixels(ColorType.Default, ColorType.MatLab);
            Color32[] legacyPixels = GenerateLegacy2DTexturePixelsOrIgnore(ColorType.Default, ColorType.MatLab);

            AssertPixelsWithinTolerance(legacyPixels, unityPixels, 1);
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
            return GenerateLegacyTexturePixelsOrIgnore(() => generate_1D_color_Texture((int)colorType));
        }

        private static Color32[] GenerateLegacy2DTexturePixelsOrIgnore(ColorType horizontalColorType, ColorType verticalColorType)
        {
            return GenerateLegacyTexturePixelsOrIgnore(() => generate_2D_color_Texture((int)horizontalColorType, (int)verticalColorType));
        }

        private static Color32[] GenerateLegacyTexturePixelsOrIgnore(Func<IntPtr> createTexture)
        {
            IntPtr texture = IntPtr.Zero;
            try
            {
                texture = createTexture();
                if (texture == IntPtr.Zero)
                {
                    Assert.Ignore("hbp_export returned a null legacy texture.");
                }

                int width = get_width_Texture(texture);
                int height = get_height_Texture(texture);
                Color32[] pixels = new Color32[width * height];
                GCHandle handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
                try
                {
                    update_Texture(texture, handle.AddrOfPinnedObject(), 255);
                }
                finally
                {
                    handle.Free();
                }

                return pixels;
            }
            catch (Exception exception) when (IsMissingLegacyTextureDependency(exception))
            {
                Assert.Ignore($"hbp_export texture comparison unavailable: {exception.Message}");
                return Array.Empty<Color32>();
            }
            finally
            {
                if (texture != IntPtr.Zero)
                {
                    delete_Texture(texture);
                }
            }
        }

        private static bool IsMissingLegacyTextureDependency(Exception exception)
        {
            return exception is DllNotFoundException
                or EntryPointNotFoundException
                or BadImageFormatException
                || exception.InnerException != null && IsMissingLegacyTextureDependency(exception.InnerException);
        }

        [DllImport("hbp_export", EntryPoint = "generate_1D_color_Texture", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr generate_1D_color_Texture(int idColor);

        [DllImport("hbp_export", EntryPoint = "generate_2D_color_Texture", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr generate_2D_color_Texture(int idColor1, int idColor2);

        [DllImport("hbp_export", EntryPoint = "delete_Texture", CallingConvention = CallingConvention.Cdecl)]
        private static extern void delete_Texture(IntPtr texture);

        [DllImport("hbp_export", EntryPoint = "get_width_Texture", CallingConvention = CallingConvention.Cdecl)]
        private static extern int get_width_Texture(IntPtr texture);

        [DllImport("hbp_export", EntryPoint = "get_height_Texture", CallingConvention = CallingConvention.Cdecl)]
        private static extern int get_height_Texture(IntPtr texture);

        [DllImport("hbp_export", EntryPoint = "update_Texture", CallingConvention = CallingConvention.Cdecl)]
        private static extern void update_Texture(IntPtr texture, IntPtr colors, int alpha);
    }
}
