using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace HBP.Tests.Rendering
{
    public class Phase3SpecializedRenderingTests
    {
        private const string ROIShaderPath = "Assets/Shaders/HBP/ROI/HBPROISphere.shader";
        private const string SiteShaderPath = "Assets/Shaders/HBP/Sites/HBPSite.shader";

        [Test]
        public void ROISphereMesh_IsCompactAndDoesNotRequireBarycentrics()
        {
            Mesh mesh = SharedMeshes.ROISphere;
            int[] triangles = mesh.triangles;
            Vector3[] vertices = mesh.vertices;
            List<Vector3> barycentrics = new();
            List<Vector3> siteBarycentrics = new();
            mesh.GetUVs(3, barycentrics);
            SharedMeshes.Site.GetUVs(3, siteBarycentrics);

            Assert.That(mesh.vertexCount, Is.EqualTo((48 + 1) * 32 + 2));
            Assert.That(triangles, Has.Length.EqualTo(48 * 32 * 2 * 3));
            Assert.That(mesh.vertexCount, Is.LessThan(triangles.Length));
            Assert.That(barycentrics, Is.Empty);
            Assert.That(siteBarycentrics, Is.Empty);
            Assert.That(SharedMeshes.Site.vertexCount, Is.LessThan(SharedMeshes.Site.triangles.Length));

            for (int index = 0; index < triangles.Length; index += 3)
            {
                float doubleArea = Vector3.Cross(vertices[triangles[index + 1]] - vertices[triangles[index]], vertices[triangles[index + 2]] - vertices[triangles[index]]).magnitude;
                Assert.That(doubleArea, Is.GreaterThan(0f), $"triangle {index / 3} must not be degenerate");
            }
        }

        [Test]
        public void SharedMeshes_RecreatesMeshesDestroyedByUnity()
        {
            Mesh roiSphere = SharedMeshes.ROISphere;
            Mesh site = SharedMeshes.Site;

            Object.DestroyImmediate(roiSphere);
            Object.DestroyImmediate(site);

            Mesh rebuiltROISphere = SharedMeshes.ROISphere;
            Mesh rebuiltSite = SharedMeshes.Site;

            Assert.That(rebuiltROISphere, Is.Not.Null);
            Assert.That(rebuiltSite, Is.Not.Null);
            Assert.That(rebuiltROISphere, Is.Not.SameAs(roiSphere));
            Assert.That(rebuiltSite, Is.Not.SameAs(site));
        }

        [Test]
        public void ROISphereShader_UsesAnalyticSilhouetteAndSupportsMetal()
        {
            string source = File.ReadAllText(ROIShaderPath);
            Shader shader = Shader.Find("HBP/ROI/Sphere");

            Assert.That(shader, Is.Not.Null);
            Assert.That(shader.isSupported, Is.True);
            StringAssert.Contains("fwidth(limb)", source);
            StringAssert.Contains("HBP_QuestPeel", source);
            StringAssert.DoesNotContain("barycentric", source);
            StringAssert.DoesNotContain("#pragma geometry", source);
            StringAssert.DoesNotContain("GeometryShader", source);

            PassIdentifier pass = default;
            Assert.That(ShaderUtil.IsGraphicsAPISupported(shader, pass, GraphicsDeviceType.Metal), Is.True);
            Assert.That(ShaderUtil.GetShaderMessages(shader, ShaderCompilerPlatform.Metal), Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ROISphere_RenderSeparatesFillAndRimWithoutAContrastBand(bool blue)
        {
            Shader shader = Shader.Find("HBP/ROI/Sphere");
            GameObject cameraObject = new("ROI test camera", typeof(Camera));
            GameObject sphereObject = new("ROI test sphere", typeof(MeshFilter), typeof(MeshRenderer));
            Material material = new(shader);
            RenderTexture target = RenderTexture.GetTemporary(256, 256, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Texture2D readback = new(256, 256, TextureFormat.RGBA32, false, true);
            RenderTexture previousTarget = RenderTexture.active;

            try
            {
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.orthographic = true;
                camera.orthographicSize = 1.5f;
                camera.transform.position = new Vector3(0, 0, -3);
                camera.targetTexture = target;

                Color tint = blue ? Color.blue : Color.white;
                material.SetColor("_FillColor", tint);
                material.SetColor("_RimColor", tint);
                material.SetFloat("_FillShading", 0f);
                material.SetFloat("_FillHighlight", 0f);
                sphereObject.GetComponent<MeshFilter>().sharedMesh = SharedMeshes.ROISphere;
                sphereObject.GetComponent<MeshRenderer>().sharedMaterial = material;

                const int center = 128 * 256 + 128;
                Color32[] pixels = RenderPixels();
                Assert.That(pixels[0].a, Is.Zero);
                Assert.That(pixels[center].a, Is.InRange(60, 77), "The default fill remains translucent.");
                Assert.That(pixels.Max(pixel => pixel.a), Is.GreaterThan(235), "The rim remains visible.");
                Assert.That(pixels.All(pixel =>
                {
                    int expectedRedAndGreen = blue ? 0 : pixel.a;
                    return Mathf.Abs(pixel.r - expectedRedAndGreen) <= 2 && Mathf.Abs(pixel.g - expectedRedAndGreen) <= 2 && Mathf.Abs(pixel.b - pixel.a) <= 2;
                }), Is.True, "With uniform color, the fill-to-rim transition must contain no dark or light band.");

                material.SetFloat("_FillOpacity", 0f);
                Color32[] rimOnly = RenderPixels();
                Assert.That(rimOnly[center].a, Is.Zero);
                Assert.That(rimOnly.Max(pixel => pixel.a), Is.GreaterThan(235));

                material.SetFloat("_FillOpacity", 0.3f);
                material.SetFloat("_RimThickness", 0f);
                Color32[] fillOnly = RenderPixels();
                Assert.That(fillOnly[center].a, Is.EqualTo(pixels[center].a));
                Assert.That(fillOnly.Max(pixel => pixel.a), Is.LessThanOrEqualTo(77));

                Color32[] RenderPixels()
                {
                    camera.Render();
                    RenderTexture.active = target;
                    readback.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                    readback.Apply();
                    return readback.GetPixels32();
                }
            }
            finally
            {
                RenderTexture.active = previousTarget;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                RenderTexture.ReleaseTemporary(target);
                Object.DestroyImmediate(readback);
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(sphereObject);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void ROIMaterialsAndPrefab_UseWhiteAndSelectionBlueAndPreservePicking()
        {
            Material normal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/ROI/ROI.mat");
            Material selected = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/ROI/ROISelected.mat");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/3D/Objects/ROI Sphere.prefab");

            Assert.That(normal, Is.Not.Null);
            Assert.That(selected, Is.Not.Null);
            Assert.That(normal.shader.name, Is.EqualTo("HBP/ROI/Sphere"));
            Assert.That(selected.shader, Is.SameAs(normal.shader));
            Assert.That(normal.GetFloat("_FillOpacity"), Is.EqualTo(0.3f));
            Assert.That(selected.GetFloat("_FillOpacity"), Is.EqualTo(0.3f));
            Assert.That(normal.GetColor("_FillColor"), Is.EqualTo(Color.white));
            Assert.That(normal.GetColor("_RimColor"), Is.EqualTo(Color.white));
            Assert.That((Color32)selected.GetColor("_FillColor"), Is.EqualTo(new Color32(59, 122, 194, 255)));
            Assert.That(selected.GetColor("_RimColor"), Is.EqualTo(selected.GetColor("_FillColor")));
            Assert.That(prefab.GetComponent<SphereCollider>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<MeshRenderer>().motionVectorGenerationMode, Is.EqualTo(MotionVectorGenerationMode.ForceNoMotion));
        }

        [Test]
        public void SiteShaderAndAssets_StayMinimalAndPreserveEverySerializedState()
        {
            string source = File.ReadAllText(SiteShaderPath);
            Shader shader = Shader.Find("HBP/Site");
            string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Resources/Materials/Sites" });
            SharedMaterials sharedMaterials = AssetDatabase.LoadAssetAtPath<SharedMaterials>("Assets/Resources/Objects/Shared Materials.asset");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/3D/Objects/Site.prefab");

            Assert.That(shader, Is.Not.Null);
            Assert.That(shader.isSupported, Is.True);
            StringAssert.Contains("return _Color", source);
            StringAssert.DoesNotContain("TEXTURE2D", source);
            StringAssert.DoesNotContain("SAMPLE_TEXTURE", source);
            StringAssert.DoesNotContain("TransformObjectToWorldNormal", source);
            StringAssert.DoesNotContain("#pragma geometry", source);
            Assert.That(materialGuids, Has.Length.EqualTo(13));

            foreach (string guid in materialGuids)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.That(material.shader.name, Is.EqualTo("HBP/Site"), material.name);
                Assert.That(material.HasColor("_Color"), Is.True, material.name);
            }

            Assert.That(sharedMaterials.Site.Basic.shader, Is.SameAs(shader));
            AssertStateMaterials(sharedMaterials.Site, SiteType.Positive);
            AssertStateMaterials(sharedMaterials.Site, SiteType.Negative);
            AssertStateMaterials(sharedMaterials.Site, SiteType.Source);
            AssertStateMaterials(sharedMaterials.Site, SiteType.NotASource);
            AssertStateMaterials(sharedMaterials.Site, SiteType.BlackListed);
            Assert.That(prefab.GetComponent<SphereCollider>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<MeshRenderer>().motionVectorGenerationMode, Is.EqualTo(MotionVectorGenerationMode.ForceNoMotion));
            Assert.That(prefab.GetComponent<MeshRenderer>().shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
        }

        [Test]
        public void SiteMaterialCache_RecreatesDestroyedRuntimeMaterial()
        {
            Shader shader = Shader.Find("HBP/Site");
            Material basic = new(shader);
            SiteMaterials materials = new();
            Material cached = null;
            Material replacement = null;

            try
            {
                FieldInfo basicField = typeof(SiteMaterials).GetField("<Basic>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(basicField, Is.Not.Null);
                basicField.SetValue(materials, basic);

                Color color = new(0.53f, 0.15f, 0.15f);
                cached = materials.GetSharedMaterial(false, SiteType.Normal, color);
                Object.DestroyImmediate(cached);
                Assert.That(cached == null, Is.True, "The test setup must leave a destroyed Unity object in the cache.");

                replacement = materials.GetSharedMaterial(false, SiteType.Normal, color);

                Assert.That(replacement == null, Is.False, "A destroyed cached material must be recreated.");
                Assert.That(ReferenceEquals(replacement, cached), Is.False);
                Assert.That(replacement.shader, Is.SameAs(shader));
                Assert.That(Vector4.Distance(replacement.color, new Color(color.r, color.g, color.b, 0.5f)), Is.LessThan(0.0001f));
            }
            finally
            {
                if (replacement != null)
                    Object.DestroyImmediate(replacement);
                Object.DestroyImmediate(basic);
            }
        }

        private static void AssertStateMaterials(SiteMaterials materials, SiteType siteType)
        {
            Material normal = materials.GetSharedMaterial(false, siteType, Color.white);
            Material highlighted = materials.GetSharedMaterial(true, siteType, Color.white);

            Assert.That(normal, Is.Not.Null, $"{siteType} normal");
            Assert.That(highlighted, Is.Not.Null, $"{siteType} highlighted");
            Assert.That(normal, Is.Not.SameAs(highlighted), siteType.ToString());
            Assert.That(normal.shader.name, Is.EqualTo("HBP/Site"), $"{siteType} normal");
            Assert.That(highlighted.shader.name, Is.EqualTo("HBP/Site"), $"{siteType} highlighted");
            Assert.That(highlighted.color.a, Is.GreaterThan(normal.color.a), siteType.ToString());
        }
    }
}
