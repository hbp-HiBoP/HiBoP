using System;
using System.Linq;
using HBP.Core.DLL;
using HBP.Core.Enums;
using HBP.Core.Tools;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Serialization
{
    public class ManagedCutTextureCompositionTests
    {
        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        [Category("HbpCoreOnly")]
        public void HbpCoreCutGenerator_ProducesVolumeAndFmriOverlayForEveryOrientationAndFlip()
        {
            NativeParityAssert.RequireHbpCore();

            foreach (CutOrientation orientation in new[] { CutOrientation.Axial, CutOrientation.Coronal, CutOrientation.Sagittal })
            {
                foreach (bool flip in new[] { false, true })
                {
                    CutResult result = RenderCut(orientation, flip, blurFactor: 0);
                    Assert.That(result.Width, Is.GreaterThan(0), $"{orientation} flip={flip}");
                    Assert.That(result.Height, Is.GreaterThan(0), $"{orientation} flip={flip}");
                    Assert.That(result.Base, Has.Length.EqualTo(result.Width * result.Height));
                    Assert.That(result.Overlay, Has.Length.EqualTo(result.Base.Length));
                    Assert.That(result.Base, Has.Some.Matches<Color32>(pixel => pixel.r != 0 || pixel.g != 0 || pixel.b != 0));
                    Assert.That(result.Overlay, Is.Not.EqualTo(result.Base), $"{orientation} flip={flip} fMRI overlay");
                    Assert.That(result.Overlay, Has.All.Matches<Color32>(pixel => pixel.a == 255));
                }
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        [Category("HbpCoreOnly")]
        public void HbpCoreCutGenerator_BlurZeroAndOneAreIdentityAndBlurThreeMatchesBinomialKernel()
        {
            NativeParityAssert.RequireHbpCore();
            CutResult noBlur = RenderCut(CutOrientation.Axial, flip: false, blurFactor: 0);
            CutResult identityBlur = RenderCut(CutOrientation.Axial, flip: false, blurFactor: 1);
            CutResult radiusOneBlur = RenderCut(CutOrientation.Axial, flip: false, blurFactor: 3);

            Assert.That(identityBlur.Base, Is.EqualTo(noBlur.Base));
            Assert.That(radiusOneBlur.Base, Is.Not.EqualTo(noBlur.Base));
            AssertBinomialBlur3x3(noBlur.Base, radiusOneBlur.Base, noBlur.Width, noBlur.Height);
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        [Category("HbpCoreOnly")]
        public void ManagedCutComposition_DrawsSiteMarkerOverOverlayWithoutChangingOtherPixels()
        {
            NativeParityAssert.RequireHbpCore();
            CutResult result = RenderCut(CutOrientation.Coronal, flip: true, blurFactor: 0);
            Color32[] composed = (Color32[])result.Overlay.Clone();
            int centerX = Mathf.RoundToInt(0.5f * (result.Width - 1));
            int centerY = Mathf.RoundToInt(0.5f * (result.Height - 1));

            UnityTextureFactory.DrawSiteMarkers(composed, result.Width, result.Height, new[] { new Vector2(0.5f, 0.5f) }, radius: 1);

            for (int y = 0; y < result.Height; ++y)
            {
                for (int x = 0; x < result.Width; ++x)
                {
                    int index = y * result.Width + x;
                    bool markerPixel = (x - centerX) * (x - centerX) + (y - centerY) * (y - centerY) <= 1;
                    Assert.That(
                        composed[index],
                        Is.EqualTo(markerPixel ? new Color32(255, 0, 0, 255) : result.Overlay[index]),
                        $"pixel ({x},{y})");
                }
            }
        }

        private static CutResult RenderCut(CutOrientation orientation, bool flip, int blurFactor)
        {
            using Volume volume = new();
            Assert.That(volume.LoadNIFTIFile(NativeParityAssert.NativePath("Nifti", "fmri_3d.nii")), Is.True);
            using HBP.Core.Object3D.Cut cut = new(volume.Center, volume.GetOrientationVector(orientation, flip))
            {
                Orientation = orientation,
                Flip = flip,
                Position = 0.5f,
                NumberOfCuts = 8
            };
            using CutGeometryGenerator geometry = new();
            geometry.Initialize(volume, cut, 8);
            using CutGenerator generator = new();
            generator.Initialize(null, geometry, blurFactor);
            generator.FillTextureWithVolume(UnityTextureFactory.Generate1DColorPixels(ColorType.Grayscale), 0.0f, 1.0f);
            Color32[] basePixels = generator.CopyBasePixels();
            generator.FillTextureWithFMRI(volume, 0.25f, 1.0f, 0.25f, 1.0f, 0.5f);
            Vector2Int size = geometry.TextureSize;
            return new CutResult(size.x, size.y, basePixels, generator.CopyOverlayPixels());
        }

        private static void AssertBinomialBlur3x3(Color32[] source, Color32[] actual, int width, int height)
        {
            int[] weights = { 1, 2, 1 };
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    int red = 0;
                    int green = 0;
                    int blue = 0;
                    int alpha = 0;
                    for (int kernelY = -1; kernelY <= 1; ++kernelY)
                    {
                        int sampleY = Reflect101(y + kernelY, height);
                        for (int kernelX = -1; kernelX <= 1; ++kernelX)
                        {
                            int sampleX = Reflect101(x + kernelX, width);
                            Color32 pixel = source[sampleY * width + sampleX];
                            int weight = weights[kernelY + 1] * weights[kernelX + 1];
                            red += pixel.r * weight;
                            green += pixel.g * weight;
                            blue += pixel.b * weight;
                            alpha += pixel.a * weight;
                        }
                    }

                    Color32 blurred = actual[y * width + x];
                    Assert.That(blurred.r, Is.EqualTo((float)red / 16).Within(1.0f), $"red ({x},{y})");
                    Assert.That(blurred.g, Is.EqualTo((float)green / 16).Within(1.0f), $"green ({x},{y})");
                    Assert.That(blurred.b, Is.EqualTo((float)blue / 16).Within(1.0f), $"blue ({x},{y})");
                    Assert.That(blurred.a, Is.EqualTo((float)alpha / 16).Within(1.0f), $"alpha ({x},{y})");
                }
            }
        }

        private static int Reflect101(int index, int length)
        {
            if (length <= 1) return 0;
            while (index < 0 || index >= length)
            {
                index = index < 0 ? -index : 2 * length - index - 2;
            }
            return index;
        }

        private readonly struct CutResult
        {
            public CutResult(int width, int height, Color32[] basePixels, Color32[] overlay)
            {
                Width = width;
                Height = height;
                Base = basePixels;
                Overlay = overlay;
            }

            public int Width { get; }
            public int Height { get; }
            public Color32[] Base { get; }
            public Color32[] Overlay { get; }
        }
    }
}
