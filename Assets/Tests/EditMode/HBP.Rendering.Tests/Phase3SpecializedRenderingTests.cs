using System.Collections.Generic;
using System.IO;
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
        private const string ROIShaderPath = "Assets/Shaders/HBP/ROI/HBPROIWireframe.shader";
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
        public void ROIAnalyticCageShader_UsesSparseAnalyticLinesAndSupportsMetal()
        {
            string source = File.ReadAllText(ROIShaderPath);
            Shader shader = Shader.Find("HBP/ROI/AnalyticCage");

            Assert.That(shader, Is.Not.Null);
            Assert.That(shader.isSupported, Is.True);
            StringAssert.Contains("float primaryCircleDistance", source);
            StringAssert.Contains("float diagonalMeridianDistance", source);
            StringAssert.Contains("float latitudeDistance", source);
            StringAssert.Contains("float intermediateCircleDistance", source);
            StringAssert.Contains("float silhouetteDistance", source);
            StringAssert.Contains("fwidth(primaryCircleDistance)", source);
            StringAssert.Contains("fwidth(intermediateCircleDistance)", source);
            StringAssert.Contains("fwidth(silhouetteDistance)", source);
            StringAssert.Contains("_ContrastColor", source);
            StringAssert.Contains("float contrastWidth = coreWidth * 2.25", source);
            StringAssert.Contains("float wireColorWeight = saturate(core * 2.0)", source);
            StringAssert.DoesNotContain("barycentric", source);
            StringAssert.DoesNotContain("#pragma geometry", source);
            StringAssert.DoesNotContain("GeometryShader", source);

            PassIdentifier pass = default;
            Assert.That(ShaderUtil.IsGraphicsAPISupported(shader, pass, GraphicsDeviceType.Metal), Is.True);
            Assert.That(ShaderUtil.GetShaderMessages(shader, ShaderCompilerPlatform.Metal), Is.Empty);
        }

        [Test]
        public void ROIAnalyticCage_RenderKeepsMostOfTheSphereTransparent()
        {
            Shader shader = Shader.Find("HBP/ROI/AnalyticCage");
            GameObject cameraObject = new("ROI cage test camera", typeof(Camera));
            GameObject sphereObject = new("ROI cage test sphere", typeof(MeshFilter), typeof(MeshRenderer));
            Material material = new(shader);
            RenderTexture target = RenderTexture.GetTemporary(256, 256, 24, RenderTextureFormat.ARGB32);
            Texture2D readback = new(256, 256, TextureFormat.RGBA32, false);
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

                material.SetColor("_WireColor", Color.red);
                material.SetColor("_ContrastColor", Color.black);
                sphereObject.GetComponent<MeshFilter>().sharedMesh = SharedMeshes.ROISphere;
                sphereObject.GetComponent<MeshRenderer>().sharedMaterial = material;

                camera.Render();
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                readback.Apply();

                int visiblePixels = 0;
                Color32[] pixels = readback.GetPixels32();
                foreach (Color32 pixel in pixels)
                {
                    if (pixel.a > 12)
                        ++visiblePixels;
                }

                float coverage = visiblePixels / (float)pixels.Length;
                Assert.That(coverage, Is.GreaterThan(0.01f), "the cage must remain visible");
                Assert.That(coverage, Is.LessThan(0.20f), "the cage must not become an opaque sphere");
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
        public void ROIMaterialsAndPrefab_UseTheAnalyticCageAndPreservePicking()
        {
            Material normal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/ROI/ROI.mat");
            Material selected = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/ROI/ROISelected.mat");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/3D/Objects/ROI Sphere.prefab");

            Assert.That(normal, Is.Not.Null);
            Assert.That(selected, Is.Not.Null);
            Assert.That(normal.shader.name, Is.EqualTo("HBP/ROI/AnalyticCage"));
            Assert.That(selected.shader.name, Is.EqualTo("HBP/ROI/AnalyticCage"));
            Assert.That(normal.HasProperty("_BaseColor"), Is.False);
            Assert.That(selected.HasProperty("_BaseColor"), Is.False);
            Assert.That(normal.GetColor("_WireColor"), Is.Not.EqualTo(selected.GetColor("_WireColor")));
            Assert.That(normal.HasColor("_ContrastColor"), Is.True);
            Assert.That(selected.HasColor("_ContrastColor"), Is.True);
            Assert.That(normal.GetColor("_ContrastColor").a, Is.GreaterThan(0f));
            Assert.That(selected.GetColor("_ContrastColor").a, Is.GreaterThan(0f));
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
