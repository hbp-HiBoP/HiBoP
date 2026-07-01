using System.Linq;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using UnityEngine;
using DLLTexture = HBP.Core.DLL.Texture;
using ObjectCut = HBP.Core.Object3D.Cut;

namespace HBP.Tests.Serialization
{
    public class Phase8CutsTriangleErasingTests
    {
        [Test]
        public void Cut_ConfigurationRoundTripAndRuntimeDefaultsAreStable()
        {
            using TempDirectoryScope temp = new();
            Cut source = new(new Vector3(1, 2, 3), CutOrientation.Custom, true, 0.75f);

            string path = temp.GetPath("phase8-cut.json");
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
            Assert.That(runtimeCut.ConvertToArray(), Is.EqualTo(new[] { 4f, 5f, 6f, 0f, 1f, 0f }));
        }

        [Test]
        public void CutTexturesUtility_EmptyDllTexturesUpdateDeterministicBlackGuiTexture()
        {
            CutTexturesUtility utility = new();
            Texture2D guiTexture = new(2, 3);
            utility.DLLGUIBrainCutTextures.Add(new DLLTexture());
            utility.GUIBrainCutTextures.Add(guiTexture);

            try
            {
                utility.UpdateTextures2D();

                Assert.That(guiTexture.width, Is.EqualTo(10));
                Assert.That(guiTexture.height, Is.EqualTo(10));
                Assert.That(guiTexture.GetPixels32(), Is.All.Matches<Color32>(pixel =>
                    pixel.r == 0 && pixel.g == 0 && pixel.b == 0 && pixel.a == 255));
            }
            finally
            {
                utility.Clean();
                Object.DestroyImmediate(guiTexture);
            }
        }

        [Test]
        public void CutTexturesUtility_GuiHelpersIgnoreZeroSizedSyntheticCutTextures()
        {
            CutTexturesUtility utility = new();
            utility.DLLBrainCutTextures.Add(new DLLTexture());
            utility.DLLGUIBrainCutTextures.Add(new DLLTexture());
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

                Assert.That(utility.DLLGUIBrainCutTextures.Single().Width, Is.EqualTo(0));
                Assert.That(utility.DLLGUIBrainCutTextures.Single().Height, Is.EqualTo(0));
            }
            finally
            {
                utility.Clean();
            }
        }
    }
}
