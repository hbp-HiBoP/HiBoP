using System;
using System.IO;
using System.Linq;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;
using CRNL.HiBoP.XR.StaticRendering.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CRNL.HiBoP.XR.StaticRendering.Tests
{
    public class P05StaticRendererTests
    {
        [TearDown]
        public void TearDown()
        {
            SurfaceMeshCache.ClearForTests();
        }

        [Test]
        public void P05ABCD_ProjectAndAssetsMatchResolvedDecisions()
        {
            P05ProjectSetup.Validate();
            Assert.That(QualitySettings.activeColorSpace, Is.EqualTo(ColorSpace.Linear));
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.TypeOf<UniversalRenderPipelineAsset>());
            Assert.That(PlayerSettings.preserveFramebufferAlpha, Is.True);
            Assert.That(((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).allowPostProcessAlphaOutput, Is.True);

            Shader opaque = Shader.Find("HiBoP XR/P05/Surface Opaque");
            Shader transparent = Shader.Find("HiBoP XR/P05/Surface Transparent");
            Assert.That(opaque, Is.Not.Null);
            Assert.That(transparent, Is.Not.Null);
            Assert.That(opaque.isSupported, Is.True);
            Assert.That(transparent.isSupported, Is.True);
            Assert.That(ShaderUtil.ShaderHasError(opaque), Is.False);
            Assert.That(ShaderUtil.ShaderHasError(transparent), Is.False);

            Material opaqueMaterial = AssetDatabase.LoadAssetAtPath<Material>(P05ProjectSetup.OpaqueMaterialPath);
            Material transparentMaterial = AssetDatabase.LoadAssetAtPath<Material>(P05ProjectSetup.TransparentMaterialPath);
            Material transparentDepthMaterial = AssetDatabase.LoadAssetAtPath<Material>(P05ProjectSetup.TransparentDepthMaterialPath);
            Assert.That(opaqueMaterial.renderQueue, Is.EqualTo((int)RenderQueue.Geometry));
            Assert.That(transparentMaterial.renderQueue, Is.EqualTo((int)RenderQueue.Transparent));
            Assert.That(transparentDepthMaterial.renderQueue, Is.EqualTo((int)RenderQueue.Transparent - 1));

            SurfaceAsset anatomical = P05SurfaceAssetBinary.Read(AssetDatabase.LoadAssetAtPath<TextAsset>(P05ProjectSetup.AnatomicalDataPath));
            SurfaceAsset inflated = P05SurfaceAssetBinary.Read(AssetDatabase.LoadAssetAtPath<TextAsset>(P05ProjectSetup.InflatedDataPath));
            Assert.That(anatomical.Representation, Is.EqualTo(SurfaceRepresentation.Anatomical));
            Assert.That(inflated.Representation, Is.EqualTo(SurfaceRepresentation.Inflated));
            Assert.That(anatomical.Hash, Is.Not.EqualTo(inflated.Hash));
            Assert.That(anatomical.Positions.Count, Is.EqualTo(69104));
            Assert.That(anatomical.Indices.Count, Is.EqualTo(138216 * 3));
            Assert.That(inflated.Positions.Count, Is.EqualTo(66299));
            Assert.That(inflated.Indices.Count, Is.EqualTo(132590 * 3));

            using SurfaceMeshLease anatomicalLease = SurfaceMeshCache.Acquire(anatomical);
            using SurfaceMeshLease inflatedLease = SurfaceMeshCache.Acquire(inflated);
            Assert.That(anatomicalLease.Mesh.indexFormat, Is.EqualTo(IndexFormat.UInt32));
            Assert.That(inflatedLease.Mesh.indexFormat, Is.EqualTo(IndexFormat.UInt32));
            Assert.That(anatomicalLease.Mesh.isReadable, Is.False);
            Assert.That(inflatedLease.Mesh.isReadable, Is.False);
        }

        [Test]
        public void GiiDerivedSurfaceBinaryRejectsPayloadCorruption()
        {
            byte[] source = AssetDatabase.LoadAssetAtPath<TextAsset>(P05ProjectSetup.AnatomicalDataPath).bytes;
            byte[] corrupted = (byte[])source.Clone();
            corrupted[^1] ^= 0xff;

            Assert.Throws<InvalidDataException>(() => P05SurfaceAssetBinary.Read(corrupted));
        }

        [Test]
        public void D0SurfaceAsset_UploadPreservesGoldenBuffersAndConvertsMillimetresToMetres()
        {
            SurfaceAsset asset = CreateD0Asset();
            using SurfaceMeshLease lease = SurfaceMeshCache.Acquire(asset);
            Mesh mesh = lease.Mesh;

            Assert.That(mesh.vertexCount, Is.EqualTo(4));
            Assert.That(mesh.indexFormat, Is.EqualTo(IndexFormat.UInt16));
            Assert.That(Vector3.Distance(mesh.bounds.min, new Vector3(-0.001f, -0.001f, 0f)), Is.LessThan(0.0000001f));
            Assert.That(Vector3.Distance(mesh.bounds.max, new Vector3(0.001f, 0.001f, 0f)), Is.LessThan(0.0000001f));
            Assert.That(mesh.isReadable, Is.False, "CPU mesh copies must be released after upload.");
        }

        [Test]
        public void IndexFormatSwitchesAtUInt16VertexLimit()
        {
            Assert.That(SurfaceMeshUploader.SelectIndexFormat(ushort.MaxValue), Is.EqualTo(IndexFormat.UInt16));
            Assert.That(SurfaceMeshUploader.SelectIndexFormat(ushort.MaxValue + 1), Is.EqualTo(IndexFormat.UInt32));
        }

        [Test]
        public void MeshCacheSharesOneUploadAndReleasesAfterLastLease()
        {
            SurfaceAsset asset = CreateD0Asset();
            SurfaceMeshLease first = SurfaceMeshCache.Acquire(asset);
            SurfaceMeshLease second = SurfaceMeshCache.Acquire(asset);

            Assert.That(second.Mesh, Is.SameAs(first.Mesh));
            Assert.That(SurfaceMeshCache.ActiveMeshCount, Is.EqualTo(1));
            first.Dispose();
            Assert.That(SurfaceMeshCache.ActiveMeshCount, Is.EqualTo(1));
            second.Dispose();
            Assert.That(SurfaceMeshCache.ActiveMeshCount, Is.Zero);
        }

        [Test]
        public void RepeatedCreateDisposeCyclesLeaveNoMeshResident()
        {
            SurfaceAsset asset = CreateD0Asset();
            for (int cycle = 0; cycle < 256; cycle++)
            {
                using SurfaceMeshLease lease = SurfaceMeshCache.Acquire(asset);
                Assert.That(lease.Mesh, Is.Not.Null);
            }

            Assert.That(SurfaceMeshCache.ActiveMeshCount, Is.Zero);
        }

        [Test]
        public void UploadRejectsNonCanonicalCoordinatesNormalsAndBounds()
        {
            SurfaceAsset wrongCoordinates = CreateD0Asset(new CoordinateSpace(CoordinateHandedness.Right, CoordinateAxisOrder.Xyz, LengthUnit.Millimeter, 0.001f, Matrix4x4F.Identity, 1));
            Assert.Throws<ArgumentException>(() => SurfaceMeshUploader.CreateMesh(wrongCoordinates));

            SurfaceAsset wrongNormal = CreateD0Asset(CoordinateSpace.DesktopUnityMillimetersV1, new Float3(0f, 0f, 0.5f));
            Assert.Throws<ArgumentException>(() => SurfaceMeshUploader.CreateMesh(wrongNormal));

            SurfaceAsset wrongBounds = CreateD0Asset(CoordinateSpace.DesktopUnityMillimetersV1, new Float3(0f, 0f, 1f), new Bounds3F(new Float3(-2f, -1f, 0f), new Float3(1f, 1f, 0f)));
            Assert.Throws<ArgumentException>(() => SurfaceMeshUploader.CreateMesh(wrongBounds));
        }

        [Test]
        public void RendererRuntimeAssemblyHasNoNetworkCoreDataOrNativeBoundary()
        {
            string runtime = Path.Combine(Application.dataPath, "HiBoPXR", "StaticRendering", "Runtime");
            string combinedSource = string.Join("\n", Directory.GetFiles(runtime, "*.cs").Select(File.ReadAllText));
            string[] forbidden =
            {
                "UnityWebRequest",
                "HttpClient",
                "System.Net",
                "System.Net.Sockets",
                "HBP.Core",
                "HBP.Data",
                "DllImport",
                "LoadLibrary",
            };
            foreach (string token in forbidden)
            {
                Assert.That(combinedSource, Does.Not.Contain(token), token);
            }

            string asmdef = File.ReadAllText(Path.Combine(runtime, "CRNL.HiBoP.XR.StaticRendering.asmdef"));
            Assert.That(asmdef, Does.Contain("CRNL.HiBoP.RenderModel"));
            Assert.That(asmdef, Does.Not.Contain("Protocol"));
            Assert.That(asmdef, Does.Not.Contain("Bootstrap"));
        }

        [Test]
        public void TransparentShadersUseNearestSurfaceDepthPrepass()
        {
            string shaderPath = Path.Combine(Application.dataPath, "HiBoPXR", "StaticRendering", "Shaders", "P05SurfaceTransparent.shader");
            string shader = File.ReadAllText(shaderPath);
            Assert.That(shader, Does.Contain("Blend One OneMinusSrcAlpha, One OneMinusSrcAlpha"));
            Assert.That(shader, Does.Contain("#define P05_PREMULTIPLY_ALPHA 1"));
            Assert.That(shader, Does.Contain("ZWrite Off"));
            Assert.That(shader, Does.Contain("ZTest Equal"));
            Assert.That(shader, Does.Contain("Cull Back"));
            Assert.That(shader, Does.Contain("Fallback Off"));

            string depthShaderPath = Path.Combine(Application.dataPath, "HiBoPXR", "StaticRendering", "Shaders", "P05SurfaceTransparentDepth.shader");
            string depthShader = File.ReadAllText(depthShaderPath);
            Assert.That(depthShader, Does.Contain("ColorMask 0"));
            Assert.That(depthShader, Does.Contain("ZWrite On"));
            Assert.That(depthShader, Does.Contain("ZTest LEqual"));
            Assert.That(depthShader, Does.Contain("\"Queue\" = \"Transparent-1\""));
            Assert.That(depthShader, Does.Contain("Fallback Off"));
        }

        [Test]
        public void PrefabOwnsAllStaticGameObjectsAndSerializedRendererReferences()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(P05ProjectSetup.PrefabPath);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<P05LocalSurfaceBootstrap>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<P05DeviceProfiler>(), Is.Not.Null);
            var bootstrap = new SerializedObject(prefab.GetComponent<P05LocalSurfaceBootstrap>());
            Assert.That(bootstrap.FindProperty("anatomicalSurfaceData").objectReferenceValue, Is.Not.Null);
            Assert.That(bootstrap.FindProperty("inflatedSurfaceData").objectReferenceValue, Is.Not.Null);
            P05StaticSurfaceRenderer[] renderers = prefab.GetComponentsInChildren<P05StaticSurfaceRenderer>(true);
            Assert.That(renderers, Has.Length.EqualTo(2));
            foreach (P05StaticSurfaceRenderer renderer in renderers)
            {
                var serialized = new SerializedObject(renderer);
                Assert.That(serialized.FindProperty("meshFilter").objectReferenceValue, Is.Not.Null);
                Assert.That(serialized.FindProperty("meshRenderer").objectReferenceValue, Is.Not.Null);
                Assert.That(serialized.FindProperty("opaqueMaterial").objectReferenceValue, Is.Not.Null);
                Assert.That(serialized.FindProperty("transparentMaterial").objectReferenceValue, Is.Not.Null);
                Assert.That(serialized.FindProperty("transparentDepthFilter").objectReferenceValue, Is.Not.Null);
                Assert.That(serialized.FindProperty("transparentDepthRenderer").objectReferenceValue, Is.Not.Null);
                Assert.That(serialized.FindProperty("transparentDepthMaterial").objectReferenceValue, Is.Not.Null);
            }

            Quaternion desktopViewRotation = Quaternion.Euler(0f, 100f, 90f);
            Assert.That(Quaternion.Angle(prefab.transform.localRotation, Quaternion.identity), Is.LessThan(0.01f));
            foreach (P05StaticSurfaceRenderer renderer in renderers)
            {
                Assert.That(Quaternion.Angle(renderer.transform.localRotation, Quaternion.Inverse(desktopViewRotation)), Is.LessThan(0.01f));
                Assert.That(renderer.transform.localPosition.z, Is.EqualTo(0f).Within(0.0001f));
            }

            Assert.That(renderers.Select(renderer => renderer.transform.localPosition.x), Is.EquivalentTo(new[] { -0.10f, 0.10f }));
        }

        private static SurfaceAsset CreateD0Asset(CoordinateSpace? coordinateSpace = null, Float3? normal = null, Bounds3F? bounds = null)
        {
            Float3 selectedNormal = normal ?? new Float3(0f, 0f, 1f);
            return new SurfaceAsset(AssetHash.Parse("19149b6a21d4f9df69bd500deacae220caeafb4f480c410de6021c6c7d0e5ea1"), SurfaceRepresentation.Anatomical, coordinateSpace ?? CoordinateSpace.DesktopUnityMillimetersV1, bounds ?? new Bounds3F(new Float3(-1f, -1f, 0f), new Float3(1f, 1f, 0f)), RenderBuffer<Float3>.TakeOwnership(new[] { new Float3(-1f, -1f, 0f), new Float3(1f, -1f, 0f), new Float3(1f, 1f, 0f), new Float3(-1f, 1f, 0f) }), RenderBuffer<Float3>.TakeOwnership(Enumerable.Repeat(selectedNormal, 4).ToArray()), RenderBuffer<uint>.TakeOwnership(new uint[] { 0, 1, 2, 0, 2, 3 }), RenderBuffer<Float2>.TakeOwnership(new[] { new Float2(0f, 0f), new Float2(1f, 0f), new Float2(1f, 1f), new Float2(0f, 1f) }));
        }
    }
}
