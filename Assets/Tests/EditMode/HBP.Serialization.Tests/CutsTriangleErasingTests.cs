using System.Linq;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using UnityEngine;
using ObjectCut = HBP.Core.Object3D.Cut;

namespace HBP.Tests.Serialization
{
    public class CutsTriangleErasingTests
    {
        [Test]
        public void Cut_ConfigurationRoundTripAndRuntimeDefaultsAreStable()
        {
            using TempDirectoryScope temp = new();
            Cut source = new(new Vector3(1, 2, 3), CutOrientation.Custom, true, 0.75f);

            string path = temp.GetPath("cuts-triangle-erasing-cut.json");
            Assert.That(ClassLoaderSaver.SaveToJSon(source, path, true), Is.True);
            Cut loaded = ClassLoaderSaver.LoadFromJson<Cut>(path);

            Assert.That(loaded.Normal.ToVector3(), Is.EqualTo(new Vector3(1, 2, 3)));
            Assert.That(loaded.Orientation, Is.EqualTo(CutOrientation.Custom));
            Assert.That(loaded.Flip, Is.True);
            Assert.That(loaded.Position, Is.EqualTo(0.75f).Within(0.0001f));

            ObjectCut runtimeCut = new(new Vector3(4, 5, 6), new Vector3(0, 1, 0));

            Assert.That(runtimeCut.Point, Is.EqualTo(new Vector3(4, 5, 6)));
            Assert.That(runtimeCut.Normal, Is.EqualTo(new Vector3(0, 1, 0)));
            Assert.That(runtimeCut.Orientation, Is.EqualTo(CutOrientation.Axial));
            Assert.That(runtimeCut.Flip, Is.False);
            Assert.That(runtimeCut.NumberOfCuts, Is.EqualTo(500));
            Assert.That(runtimeCut.Position, Is.EqualTo(0.5f));
            Assert.That(runtimeCut.ConvertToArray(), Is.EqualTo(new[] { -4f, 5f, 6f, 0f, 1f, 0f }),
                "The serialized native plane layout must reflect Unity X through the centralized reference-system conversion.");
        }

        [Test]
        public void CutTexturesUtility_GuiTexturesUseDisplayedCutTextureWithFunctionalOverlay()
        {
            CutTexturesUtility utility = new();
            Texture2D baseTexture = new(2, 2, TextureFormat.RGBA32, false);
            baseTexture.SetPixels32(new[]
            {
                new Color32(10, 0, 0, 255), new Color32(20, 0, 0, 255),
                new Color32(30, 0, 0, 255), new Color32(40, 0, 0, 255)
            });
            baseTexture.Apply();
            Texture2D functionalTexture = new(2, 2, TextureFormat.RGBA32, false);
            functionalTexture.SetPixels32(Enumerable.Repeat(new Color32(200, 0, 0, 255), 4).ToArray());
            functionalTexture.Apply();
            Texture2D guiTexture = new(1, 1, TextureFormat.RGBA32, false);
            utility.BaseBrainCutTextures.Add(baseTexture);
            utility.BrainCutTextures.Add(functionalTexture);
            utility.GUIBrainCutTextures.Add(guiTexture);
            ObjectCut cut = new()
            {
                ID = 0,
                Orientation = CutOrientation.Sagittal,
                Flip = false
            };

            try
            {
                utility.CreateGUIMRITextures(new() { cut });

                Color32[] guiPixels = guiTexture.GetPixels32();
                Assert.That(guiPixels.All(pixel => pixel.r == 200), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(baseTexture);
                Object.DestroyImmediate(functionalTexture);
                Object.DestroyImmediate(guiTexture);
            }
        }

        [Test]
        public void CutTexturesUtility_ResizeGuiTexturesPadsUnityTextureWithoutNativeGuiMirror()
        {
            CutTexturesUtility utility = new();
            Texture2D guiTexture = new(2, 3, TextureFormat.RGBA32, false);
            guiTexture.SetPixels32(new[]
            {
                new Color32(10, 0, 0, 255), new Color32(20, 0, 0, 255),
                new Color32(30, 0, 0, 255), new Color32(40, 0, 0, 255),
                new Color32(50, 0, 0, 255), new Color32(60, 0, 0, 255)
            });
            utility.GUIBrainCutTextures.Add(guiTexture);
            ObjectCut cut = new()
            {
                ID = 0,
                Orientation = CutOrientation.Axial
            };

            try
            {
                utility.ResizeGUIMRITextures(new() { cut });

                Assert.That(guiTexture.width, Is.EqualTo(3));
                Assert.That(guiTexture.height, Is.EqualTo(3));
                Assert.That(guiTexture.GetPixels32().Count(pixel => pixel.r == 0 && pixel.g == 0 && pixel.b == 0), Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(guiTexture);
            }
        }

        [Test]
        public void CutTexturesUtility_GuiHelpersIgnoreZeroSizedSyntheticCutTextures()
        {
            CutTexturesUtility utility = new();
            Texture2D baseTexture = new(1, 1);
            Texture2D brainTexture = new(2, 2);
            Texture2D guiTexture = new(2, 2);
            utility.BaseBrainCutTextures.Add(baseTexture);
            utility.BrainCutTextures.Add(brainTexture);
            utility.GUIBrainCutTextures.Add(guiTexture);
            utility.CutGenerators.Add(new HBP.Core.DLL.CutGenerator());
            ObjectCut cut = new()
            {
                ID = 0,
                Orientation = CutOrientation.Axial,
                Flip = true
            };

            try
            {
                Assert.DoesNotThrow(() => utility.CreateGUIMRITextures(new() { cut }));
                Assert.DoesNotThrow(() => utility.ResizeGUIMRITextures(new() { cut }));

                Assert.That(utility.GUIBrainCutTextures.Single().width, Is.EqualTo(2));
                Assert.That(utility.GUIBrainCutTextures.Single().height, Is.EqualTo(2));
            }
            finally
            {
                utility.Clean();
                Object.DestroyImmediate(baseTexture);
                Object.DestroyImmediate(brainTexture);
                Object.DestroyImmediate(guiTexture);
            }
        }
    }
}
