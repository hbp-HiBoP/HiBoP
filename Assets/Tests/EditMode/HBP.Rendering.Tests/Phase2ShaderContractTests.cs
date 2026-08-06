using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Rendering
{
    public class Phase2ShaderContractTests
    {
        private const int FixtureLayer = 31;
        private static readonly Color32 ScientificColor = new(51, 153, 230, 255);

        [Test]
        public void ScientificSurface_PreservesPaletteHueAndRespondsToNormalsAcrossActivityAtlasAndFmri()
        {
            Mesh mesh = CreateQuadMesh(Vector3.back, ScientificColor);
            Texture2D scientificTexture = CreateSolidTexture(ScientificColor, false);
            Texture2D alphaTexture = CreateSolidTexture(Color.white, true);
            Texture2D anatomyTexture = CreateSolidTexture(Color.black, false);
            Material brain = CreateBrainMaterial(scientificTexture, alphaTexture, anatomyTexture);
            Material cut = CreateTexturedMaterial("HBP/Cut", scientificTexture);
            Material ui = CreateTexturedMaterial("HBP/UI/Texture", scientificTexture);

            try
            {
                Color32 activityFacing = RenderCenterPixel(brain, mesh);

                float smoothness = brain.GetFloat("_Glossiness");
                brain.SetFloat("_Glossiness", 0f);
                Color32 activityMatte = RenderCenterPixel(brain, mesh);
                brain.SetFloat("_Glossiness", smoothness);

                mesh.normals = CreateNormals(new Vector3(0.75f, 0f, -0.66f).normalized);
                Color32 activityAngled = RenderCenterPixel(brain, mesh);

                mesh.normals = CreateNormals(Vector3.right);
                Color32 activityProfile = RenderCenterPixel(brain, mesh);
                mesh.normals = CreateNormals(Vector3.back);

                brain.SetFloat("_Activity", 0f);
                brain.SetFloat("_Atlas", 1f);
                Color32 atlas = RenderCenterPixel(brain, mesh);

                brain.SetFloat("_Atlas", 0f);
                brain.SetFloat("_FMRI", 1f);
                Color32 fmri = RenderCenterPixel(brain, mesh);

                mesh.colors32 = new[] { (Color32)Color.white, (Color32)Color.white, (Color32)Color.white, (Color32)Color.white };
                Color32 cutColor = RenderCenterPixel(cut, mesh);
                Color32 uiColor = RenderCenterPixel(ui, mesh);

                Assert.That(RelativeLuminance(activityFacing) - RelativeLuminance(activityAngled), Is.GreaterThan(0.05f), "surface normals must create visible scientific relief");
                Assert.That(RelativeLuminance(activityFacing) - RelativeLuminance(activityProfile), Is.GreaterThan(0.15f), "profile normals must recover deep scientific shadows");
                Assert.That(RelativeLuminance(activityFacing) - RelativeLuminance(activityMatte), Is.GreaterThan(0.025f), "smooth scientific surfaces must produce a visible localized highlight");
                AssertHuePreserved(ScientificColor, activityFacing, "activity facing", 0.75f);
                AssertHuePreserved(ScientificColor, activityMatte, "activity matte");
                AssertHuePreserved(ScientificColor, activityAngled, "activity angled");
                AssertHuePreserved(ScientificColor, activityProfile, "activity profile");
                AssertColorsEqual(activityFacing, atlas, "atlas");
                AssertColorsEqual(activityFacing, fmri, "fMRI");
                AssertColorsEqual(ScientificColor, cutColor, "unlit cut");
                AssertColorsEqual(ScientificColor, uiColor, "unlit UI legend");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(ui);
                UnityEngine.Object.DestroyImmediate(cut);
                UnityEngine.Object.DestroyImmediate(brain);
                UnityEngine.Object.DestroyImmediate(anatomyTexture);
                UnityEngine.Object.DestroyImmediate(alphaTexture);
                UnityEngine.Object.DestroyImmediate(scientificTexture);
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void BrainForwardClipping_CoversZeroOneAndTwentyPlanes()
        {
            Mesh mesh = CreateQuadMesh(Vector3.back, ScientificColor);
            Texture2D scientificTexture = CreateSolidTexture(ScientificColor, false);
            Texture2D alphaTexture = CreateSolidTexture(Color.white, true);
            Texture2D anatomyTexture = CreateSolidTexture(Color.black, false);
            Material brain = CreateBrainMaterial(scientificTexture, alphaTexture, anatomyTexture);

            try
            {
                Assert.That(RenderCenterPixel(brain, mesh).a, Is.GreaterThan(0), "zero planes");

                SetCuts(brain, true, 1, CreateCutPoints(20, 2f), CreateCutNormals(20));
                Assert.That(RenderCenterPixel(brain, mesh).a, Is.GreaterThan(0), "one visible plane");

                SetCuts(brain, true, 1, CreateCutPoints(20, -2f), CreateCutNormals(20));
                Assert.That(RenderCenterPixel(brain, mesh).a, Is.EqualTo(0), "one clipping plane");

                Vector4[] twentyPoints = CreateCutPoints(20, 2f);
                twentyPoints[19] = new Vector4(-2f, 0f, 0f, 0f);
                SetCuts(brain, true, 20, twentyPoints, CreateCutNormals(20));
                Assert.That(RenderCenterPixel(brain, mesh).a, Is.EqualTo(0), "strong clipping with twenty planes");

                SetCuts(brain, false, 20, twentyPoints, CreateCutNormals(20));
                Assert.That(RenderCenterPixel(brain, mesh).a, Is.GreaterThan(0), "weak clipping with twenty planes");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(brain);
                UnityEngine.Object.DestroyImmediate(anatomyTexture);
                UnityEngine.Object.DestroyImmediate(alphaTexture);
                UnityEngine.Object.DestroyImmediate(scientificTexture);
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void BrainOpaquePasses_ShareExtrusionAndClippingImplementations()
        {
            Shader shader = Shader.Find("HBP/Brain");

            Assert.That(shader, Is.Not.Null);
            Material material = new(shader);
            try
            {
                Assert.That(material.FindPass("UniversalForward"), Is.GreaterThanOrEqualTo(0));
                Assert.That(material.FindPass("DepthOnly"), Is.GreaterThanOrEqualTo(0));
                Assert.That(material.FindPass("DepthNormals"), Is.GreaterThanOrEqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }

            string common = File.ReadAllText("Assets/Shaders/HBP/Brain/Includes/HBPBrainCommon.hlsl");
            string depth = File.ReadAllText("Assets/Shaders/HBP/Brain/Includes/HBPBrainDepth.hlsl");

            StringAssert.Contains("HBP_ExtrudeBrainVertex(positionOS, normalOS);", common);
            StringAssert.Contains("clip(HBP_ClippingValue(input.positionOS));", common);
            StringAssert.Contains("#include \"HBPBrainCommon.hlsl\"", depth);
            Assert.That(CountOccurrences(depth, "clip(HBP_ClippingValue(input.positionOS));"), Is.EqualTo(2));
        }

        private static Material CreateBrainMaterial(Texture scientific, Texture alpha, Texture anatomy)
        {
            Material material = new(Shader.Find("HBP/Brain"));
            material.SetTexture("_MainTex", anatomy);
            material.SetTexture("_AoTex", alpha);
            material.SetTexture("_ColorTex", scientific);
            material.SetColor("_Color", Color.white);
            material.SetFloat("_Activity", 1f);
            material.SetFloat("_Atlas", 0f);
            material.SetFloat("_FMRI", 0f);
            material.SetInt("_CutCount", 0);
            return material;
        }

        private static Material CreateTexturedMaterial(string shaderName, Texture texture)
        {
            Shader shader = Shader.Find(shaderName);
            Assert.That(shader, Is.Not.Null, shaderName);
            Material material = new(shader);
            material.SetTexture("_MainTex", texture);
            material.SetColor("_Color", Color.white);
            return material;
        }

        private static Mesh CreateQuadMesh(Vector3 normal, Color32 color)
        {
            Mesh mesh = new() { name = "Phase 2 rendering contract quad" };
            mesh.vertices = new[]
            {
                new Vector3(-0.75f, -0.75f, 0f),
                new Vector3(0.75f, -0.75f, 0f),
                new Vector3(0.75f, 0.75f, 0f),
                new Vector3(-0.75f, 0.75f, 0f)
            };
            mesh.normals = CreateNormals(normal);
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            mesh.uv2 = new[] { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };
            mesh.uv3 = new[] { Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f };
            mesh.colors32 = new[] { color, color, color, color };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3[] CreateNormals(Vector3 normal)
        {
            return new[] { normal, normal, normal, normal };
        }

        private static Texture2D CreateSolidTexture(Color color, bool linear)
        {
            Texture2D texture = new(1, 1, TextureFormat.RGBA32, false, linear)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixel(0, 0, color);
            texture.Apply(false, false);
            return texture;
        }

        private static Color32 RenderCenterPixel(Material material, Mesh mesh)
        {
            GameObject cameraObject = new("Phase 2 rendering contract camera");
            GameObject quadObject = new("Phase 2 rendering contract quad");
            RenderTexture target = null;
            Texture2D readback = null;
            RenderTexture previousActive = RenderTexture.active;

            try
            {
                cameraObject.layer = FixtureLayer;
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 1f;
                camera.transform.position = new Vector3(0f, 0f, -2f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.cullingMask = 1 << FixtureLayer;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                camera.enabled = false;

                quadObject.layer = FixtureLayer;
                quadObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                quadObject.AddComponent<MeshRenderer>().sharedMaterial = material;

                target = new RenderTexture(64, 64, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
                {
                    antiAliasing = 1,
                    filterMode = FilterMode.Point
                };
                target.Create();
                camera.targetTexture = target;
                camera.Render();

                RenderTexture.active = target;
                readback = new Texture2D(1, 1, TextureFormat.RGBA32, false, false);
                readback.ReadPixels(new Rect(32, 32, 1, 1), 0, 0, false);
                readback.Apply(false, false);
                return readback.GetPixels32()[0];
            }
            finally
            {
                RenderTexture.active = previousActive;
                if (target != null) target.Release();
                UnityEngine.Object.DestroyImmediate(readback);
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(quadObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void SetCuts(Material material, bool strong, int count, Vector4[] points, Vector4[] normals)
        {
            material.SetInt("_StrongCuts", strong ? 1 : 0);
            material.SetInt("_CutCount", count);
            material.SetVectorArray("_CutPoints", points);
            material.SetVectorArray("_CutNormals", normals);
        }

        private static Vector4[] CreateCutPoints(int count, float x)
        {
            Vector4[] result = new Vector4[count];
            for (int index = 0; index < count; ++index)
                result[index] = new Vector4(x, 0f, 0f, 0f);
            return result;
        }

        private static Vector4[] CreateCutNormals(int count)
        {
            Vector4[] result = new Vector4[count];
            for (int index = 0; index < count; ++index)
                result[index] = new Vector4(1f, 0f, 0f, 0f);
            return result;
        }

        private static int CountOccurrences(string value, string pattern)
        {
            int count = 0;
            int index = 0;
            while ((index = value.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += pattern.Length;
            }

            return count;
        }

        private static void AssertColorsEqual(Color32 expected, Color32 actual, string context)
        {
            Assert.That(Mathf.Abs(expected.r - actual.r), Is.LessThanOrEqualTo(2), context + " red");
            Assert.That(Mathf.Abs(expected.g - actual.g), Is.LessThanOrEqualTo(2), context + " green");
            Assert.That(Mathf.Abs(expected.b - actual.b), Is.LessThanOrEqualTo(2), context + " blue");
            Assert.That(actual.a, Is.GreaterThan(0), context + " alpha");
        }

        private static float RelativeLuminance(Color32 color)
        {
            Color linear = ((Color)color).linear;
            return 0.2126f * linear.r + 0.7152f * linear.g + 0.0722f * linear.b;
        }

        private static void AssertHuePreserved(Color32 expected, Color32 actual, string context, float minimumSaturationRatio = 0.9f)
        {
            Color.RGBToHSV(expected, out float expectedHue, out float expectedSaturation, out _);
            Color.RGBToHSV(actual, out float actualHue, out float actualSaturation, out _);
            float hueDistance = Mathf.Abs(expectedHue - actualHue);
            hueDistance = Mathf.Min(hueDistance, 1f - hueDistance);

            Assert.That(hueDistance, Is.LessThanOrEqualTo(0.015f), context + " hue");
            Assert.That(actualSaturation, Is.GreaterThanOrEqualTo(expectedSaturation * minimumSaturationRatio), context + " saturation");
        }
    }
}
