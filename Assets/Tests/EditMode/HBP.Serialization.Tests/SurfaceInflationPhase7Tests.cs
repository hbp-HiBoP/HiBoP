using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HBP.Core.DLL;
using HBP.Core.DLL.HbpCore;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HBP.Tests.Serialization
{
    public class SurfaceInflationPhase7Tests
    {
        [Test]
        [Category("NativeDll")]
        public async Task RepresentationTransition_PreservesActivityUvsAndScientificColors()
        {
            RequireInflationLibrary();
            using SceneHarness harness = new();
            await harness.Mesh.GenerateInflatedRepresentationAsync(FastSettings());
            harness.Manager.UpdateMeshesInformation();

            LogAssert.Expect(LogType.Error, "Instantiating mesh due to calling MeshFilter.mesh during edit mode. This will leak meshes. Please use MeshFilter.sharedMesh instead.");
            Mesh unityMesh = harness.Brain.GetComponent<MeshFilter>().mesh;
            harness.Mesh.Both.UpdateMeshFromDLL(unityMesh);
            Vector2[] alphaUvs = Enumerable.Range(0, unityMesh.vertexCount).Select(index => new Vector2(index + 0.25f, index + 0.5f)).ToArray();
            Vector2[] activityUvs = Enumerable.Range(0, unityMesh.vertexCount).Select(index => new Vector2(index + 0.75f, index + 1.0f)).ToArray();
            Color[] colors = Enumerable.Range(0, unityMesh.vertexCount).Select(index => new Color(index / 10.0f, 0.25f, 0.75f, 1.0f)).ToArray();
            Vector2[] columnAlphaUvs = alphaUvs.Select(uv => uv + Vector2.one * 10.0f).ToArray();
            Vector2[] columnActivityUvs = activityUvs.Select(uv => uv + Vector2.one * 20.0f).ToArray();
            Color[] columnColors = colors.Select(color => new Color(color.b, color.r, color.g, color.a)).ToArray();
            unityMesh.uv2 = alphaUvs;
            unityMesh.uv3 = activityUvs;
            unityMesh.colors = colors;
            harness.ColumnMesh.uv2 = columnAlphaUvs;
            harness.ColumnMesh.uv3 = columnActivityUvs;
            harness.ColumnMesh.colors = columnColors;

            harness.Manager.PrepareRepresentationTransition();

            Assert.That(unityMesh.uv2, Is.EqualTo(alphaUvs), "The transition preparation erased projected activity alpha UVs.");
            Assert.That(unityMesh.uv3, Is.EqualTo(activityUvs), "The transition preparation erased projected activity UVs.");
            Assert.That(unityMesh.colors, Is.EqualTo(colors), "The transition preparation replaced atlas/fMRI colors with source-surface colors.");
            Assert.That(harness.ColumnMesh.uv2, Is.EqualTo(columnAlphaUvs), "The transition preparation erased column-specific activity alpha UVs.");
            Assert.That(harness.ColumnMesh.uv3, Is.EqualTo(columnActivityUvs), "The transition preparation erased column-specific activity UVs.");
            Assert.That(harness.ColumnMesh.colors, Is.EqualTo(columnColors), "The transition preparation replaced column-specific scientific colors.");
            Assert.That(unityMesh.GetVertexAttributeDimension(UnityEngine.Rendering.VertexAttribute.TexCoord3), Is.EqualTo(3));
            Assert.That(unityMesh.GetVertexAttributeDimension(UnityEngine.Rendering.VertexAttribute.TexCoord4), Is.EqualTo(3));

            harness.Manager.SelectRepresentation(SurfaceRepresentation.Inflated);
            harness.Manager.UpdateMeshesInformation();
            harness.Manager.UpdateMeshesFromDLL(preserveScientificData: true);

            Assert.That(unityMesh.uv2, Is.EqualTo(alphaUvs), "Publishing the final representation erased projected activity alpha UVs.");
            Assert.That(unityMesh.uv3, Is.EqualTo(activityUvs), "Publishing the final representation erased projected activity UVs.");
            Assert.That(unityMesh.colors, Is.EqualTo(colors), "Publishing the final representation replaced scientific colors.");
            Assert.That(harness.ColumnMesh.uv2, Is.EqualTo(columnAlphaUvs), "Publishing the final representation erased column-specific activity alpha UVs.");
            Assert.That(harness.ColumnMesh.uv3, Is.EqualTo(columnActivityUvs), "Publishing the final representation erased column-specific activity UVs.");
            Assert.That(harness.ColumnMesh.colors, Is.EqualTo(columnColors), "Publishing the final representation replaced column-specific scientific colors.");
        }

        [Test]
        [Category("NativeDll")]
        public async Task BrainCutMeshes_AreHiddenOnlyFromTheMainInflatedView()
        {
            RequireInflationLibrary();
            using SceneHarness harness = new();
            GameObject mainCut = new("Main anatomical cut");
            GameObject columnCut = new("Column anatomical cut");
            GameObject cutViewCopy = new("Cut view copy");
            harness.DisplayedObjects.BrainCutMeshes.Add(mainCut);
            harness.Column.BrainCutMeshes.Add(columnCut);

            InvokeCutVisibilityUpdate(harness.Scene);
            Assert.That(mainCut.activeSelf, Is.True);
            Assert.That(columnCut.activeSelf, Is.True);
            Assert.That(cutViewCopy.activeSelf, Is.True);

            await harness.Mesh.GenerateInflatedRepresentationAsync(FastSettings());
            harness.Manager.SelectRepresentation(SurfaceRepresentation.Inflated);
            InvokeCutVisibilityUpdate(harness.Scene);

            Assert.That(mainCut.activeSelf, Is.False, "An anatomical section mesh must not intersect the intact inflated envelope in the main view.");
            Assert.That(columnCut.activeSelf, Is.False, "Column cut meshes are the copies rendered by the interactive 3D views.");
            Assert.That(cutViewCopy.activeSelf, Is.True, "Independent cut-view objects must remain available in inflated mode.");

            harness.Manager.SelectRepresentation(SurfaceRepresentation.Anatomical);
            InvokeCutVisibilityUpdate(harness.Scene);
            Assert.That(mainCut.activeSelf, Is.True);
            Assert.That(columnCut.activeSelf, Is.True);

            UnityEngine.Object.DestroyImmediate(mainCut);
            UnityEngine.Object.DestroyImmediate(columnCut);
            UnityEngine.Object.DestroyImmediate(cutViewCopy);
        }

        [Test]
        [Category("NativeDll")]
        public async Task RepresentationToggle_RequiresLoadingManagerOnlyForTheFirstInflation()
        {
            RequireInflationLibrary();
            using SceneHarness harness = new();

            Assert.That(RequiresLoadingManager(harness.Scene, SurfaceRepresentation.Inflated), Is.True);
            Assert.That(RequiresLoadingManager(harness.Scene, SurfaceRepresentation.Anatomical), Is.False);

            await harness.Scene.PrepareSurfaceRepresentationAsync(SurfaceRepresentation.Inflated);

            Assert.That(RequiresLoadingManager(harness.Scene, SurfaceRepresentation.Inflated), Is.False);
            Assert.That(RequiresLoadingManager(harness.Scene, SurfaceRepresentation.Anatomical), Is.False);
            Assert.That(harness.Mesh.Representation, Is.EqualTo(SurfaceRepresentation.Anatomical), "Preparing the cache under the loading manager must not publish or animate the representation.");
        }

        private static bool RequiresLoadingManager(Base3DScene scene, SurfaceRepresentation representation)
        {
            Type toggleType = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("HBP.UI.Toolbar.SurfaceRepresentationToggle")).FirstOrDefault(type => type != null);
            Assert.That(toggleType, Is.Not.Null);
            MethodInfo method = toggleType.GetMethod("RequiresLoadingManager", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            return (bool)method.Invoke(null, new object[] { scene, representation });
        }

        private static void InvokeCutVisibilityUpdate(Base3DScene scene)
        {
            MethodInfo method = typeof(Base3DScene).GetMethod("UpdateBrainCutMeshesVisibility", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null);
            method.Invoke(scene, null);
        }

        private static Mesh3DInflationSettings FastSettings()
        {
            SurfaceInflationOptions options = SurfaceInflationOptions.Inflated;
            options.IterationCount = 4;
            options.ConvergenceTolerance = 1e-8;
            return Mesh3DInflationSettings.Custom(options);
        }

        private static Surface CreateOctahedron()
        {
            Surface surface = new();
            surface.SetBuffers(new[]
            {
                new Vector3(0.0f, 1.45f, 0.0f),
                new Vector3(0.0f, -0.85f, 0.0f),
                new Vector3(1.20f, 0.0f, 0.0f),
                new Vector3(0.0f, 0.0f, 0.90f),
                new Vector3(-0.80f, 0.0f, 0.0f),
                new Vector3(0.0f, 0.0f, -1.10f)
            }, new[]
            {
                0, 2, 3,
                0, 3, 4,
                0, 4, 5,
                0, 5, 2,
                1, 3, 2,
                1, 4, 3,
                1, 5, 4,
                1, 2, 5
            });
            surface.ComputeNormals();
            return surface;
        }

        private static void RequireInflationLibrary()
        {
            if (!HbpCoreRuntime.TryGetVersion(out _, out string error))
            {
                Assert.Ignore($"hbp_core is unavailable: {error}");
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName}.");
            field.SetValue(target, value);
        }

        private static void SetAutoProperty(object target, string propertyName, object value)
        {
            Type type = target.GetType();
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField($"<{propertyName}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
                type = type.BaseType;
            }

            Assert.That(field, Is.Not.Null, $"Missing property {propertyName}.");
            field.SetValue(target, value);
        }

        private sealed class SceneHarness : IDisposable
        {
            private readonly GameObject m_Root;

            public Base3DScene Scene { get; }
            public MeshManager Manager { get; }
            public DisplayedObjects DisplayedObjects { get; }
            public TestSingleMesh3D Mesh { get; }
            public Column3DAnatomy Column { get; }
            public GameObject Brain { get; }
            public Mesh ColumnMesh { get; }

            public SceneHarness()
            {
                m_Root = new GameObject("Surface inflation phase 7 harness");
                m_Root.SetActive(false);
                Scene = m_Root.AddComponent<Base3DScene>();
                Manager = m_Root.AddComponent<MeshManager>();
                DisplayedObjects = m_Root.AddComponent<DisplayedObjects>();
                Brain = new GameObject("Brain", typeof(MeshFilter), typeof(MeshRenderer));
                Brain.transform.SetParent(m_Root.transform, false);
                GameObject columnObject = new("Activity column", typeof(Column3DAnatomy));
                columnObject.transform.SetParent(m_Root.transform, false);
                Column = columnObject.GetComponent<Column3DAnatomy>();
                GameObject columnBrain = new("Column brain", typeof(MeshFilter), typeof(MeshRenderer));
                columnBrain.transform.SetParent(columnObject.transform, false);
                ColumnMesh = new Mesh();
                columnBrain.GetComponent<MeshFilter>().sharedMesh = ColumnMesh;
                SetAutoProperty(Column, nameof(Column3D.BrainMesh), columnBrain);
                Scene.Columns.Add(Column);

                SetPrivateField(Manager, "m_Scene", Scene);
                SetPrivateField(Manager, "m_DisplayedObjects", DisplayedObjects);
                SetPrivateField(Scene, "m_MeshManager", Manager);
                SetPrivateField(Scene, "m_DisplayedObjects", DisplayedObjects);
                SetAutoProperty(Scene, nameof(Base3DScene.BrainMaterials), new BrainMaterials());
                SetAutoProperty(DisplayedObjects, nameof(DisplayedObjects.Brain), Brain);

                Mesh = new TestSingleMesh3D(CreateOctahedron());
                Manager.Meshes.Add(Mesh);
                Mesh.Both.UpdateMeshFromDLL(ColumnMesh);
            }

            public void Dispose()
            {
                Mesh.Clean();
                UnityEngine.Object.DestroyImmediate(m_Root);
            }
        }

        private sealed class TestSingleMesh3D : SingleMesh3D
        {
            public TestSingleMesh3D(Surface surface)
            {
                Name = "Synthetic phase 7 mesh";
                m_Both = surface;
                m_SimplifiedBoth = (Surface)surface.Clone();
            }
        }
    }
}
