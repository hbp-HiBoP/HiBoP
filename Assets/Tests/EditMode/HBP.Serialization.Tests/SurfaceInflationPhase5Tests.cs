using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HBP.Core.DLL;
using HBP.Core.DLL.HbpCore;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Serialization
{
    public class SurfaceInflationPhase5Tests
    {
        [Test]
        [Category("NativeDll")]
        public async Task MeshManager_SeparatesDisplayedAndReferenceSurfacesForEveryMeshPart()
        {
            RequireInflationLibrary();
            TestLeftRightMesh3D mesh = new(CreateOctahedron(new Vector3(-2f, 0f, 0f)), CreateOctahedron(new Vector3(2f, 0f, 0f)));
            GameObject sceneObject = new("Surface inflation phase 5 scene");
            Base3DScene scene = sceneObject.AddComponent<Base3DScene>();
            MeshManager manager = sceneObject.AddComponent<MeshManager>();
            SetPrivateField(manager, "m_Scene", scene);
            SetPrivateProperty(scene, nameof(Base3DScene.BrainMaterials), new BrainMaterials());
            manager.Meshes.Add(mesh);

            try
            {
                Mesh3DInflatedRepresentation inflated = await mesh.GenerateInflatedRepresentationAsync(FastSettings());
                scene.SceneInformation.ProjectionGridNeedsUpdate = false;
                scene.SceneInformation.SurfaceProjectionNeedsUpdate = false;
                manager.SelectRepresentation(SurfaceRepresentation.Inflated);

                Assert.That(scene.SceneInformation.ProjectionGridNeedsUpdate, Is.False);
                Assert.That(scene.SceneInformation.SurfaceProjectionNeedsUpdate, Is.False);
                Assert.That(scene.SceneInformation.GeneratorNeedsUpdate, Is.False);
                Assert.That(scene.SceneInformation.FunctionalSurfaceNeedsUpdate, Is.True);
                Assert.That(manager.CanClipBrainSurface, Is.False);

                AssertPart(manager, MeshPart.Left, mesh.Left, inflated.Left, mesh.SimplifiedLeft, inflated.SimplifiedLeft);
                AssertPart(manager, MeshPart.Right, mesh.Right, inflated.Right, mesh.SimplifiedRight, inflated.SimplifiedRight);
                AssertPart(manager, MeshPart.Both, mesh.Both, inflated.Both, mesh.SimplifiedBoth, inflated.SimplifiedBoth);

                manager.SelectRepresentation(SurfaceRepresentation.Anatomical);
                manager.UpdateMeshesInformation();

                Assert.That(manager.BrainSurface, Is.SameAs(mesh.Both));
                Assert.That(manager.ReferenceSurface, Is.SameAs(mesh.Both));
                Assert.That(manager.SimplifiedBrainSurface, Is.SameAs(mesh.SimplifiedBoth));
                Assert.That(manager.CanClipBrainSurface, Is.True);
            }
            finally
            {
                mesh.Clean();
                UnityEngine.Object.DestroyImmediate(sceneObject);
            }
        }

        [Test]
        [Category("NativeDll")]
        public void BrainMaterials_CanKeepCurrentCutsWithoutClippingTheBrain()
        {
            RequireInflationLibrary();
            BrainMaterials materials = new();
            using Cut cut = new(Vector3.zero, Vector3.right);
            List<Cut> cuts = new() { cut };

            materials.SetCuts(cuts, 1f, Quaternion.identity, clipBrain: false);
            Assert.That(materials.BrainMaterial.GetInt("_CutCount"), Is.Zero);
            materials.IsTransparent = true;
            Assert.That(materials.BrainMaterial.GetInt("_CutCount"), Is.Zero);

            materials.SetCuts(cuts, 1f, Quaternion.identity, clipBrain: true);
            Assert.That(materials.BrainMaterial.GetInt("_CutCount"), Is.EqualTo(1));
            materials.IsTransparent = false;
            Assert.That(materials.BrainMaterial.GetInt("_CutCount"), Is.EqualTo(1));
        }

        private static void AssertPart(MeshManager manager, MeshPart part, Surface expectedReference, Surface expectedDisplayed, Surface expectedSimplifiedReference, Surface expectedSimplifiedDisplayed)
        {
            manager.SelectMeshPart(part);
            manager.UpdateMeshesInformation();

            Assert.That(manager.ReferenceSurface, Is.SameAs(expectedReference));
            Assert.That(manager.BrainSurface, Is.SameAs(expectedDisplayed));
            Assert.That(manager.SimplifiedMeshToUse, Is.SameAs(expectedSimplifiedReference));
            Assert.That(manager.SimplifiedBrainSurface, Is.SameAs(expectedSimplifiedDisplayed));
            Assert.That(manager.MeshCenter, Is.EqualTo(expectedDisplayed.Center));
            Assert.That(manager.BrainSurface.NumberOfVertices, Is.EqualTo(manager.ReferenceSurface.NumberOfVertices));
            Assert.That(manager.BrainSurface.NumberOfTriangles, Is.EqualTo(manager.ReferenceSurface.NumberOfTriangles));
        }

        private static Mesh3DInflationSettings FastSettings()
        {
            SurfaceInflationOptions options = SurfaceInflationOptions.Inflated;
            options.IterationCount = 4;
            options.ConvergenceTolerance = 1e-8;
            return Mesh3DInflationSettings.Custom(options);
        }

        private static Surface CreateOctahedron(Vector3 offset)
        {
            Surface surface = new();
            surface.SetBuffers(new[]
            {
                offset + new Vector3(0.0f, 1.45f, 0.0f),
                offset + new Vector3(0.0f, -0.85f, 0.0f),
                offset + new Vector3(1.20f, 0.0f, 0.0f),
                offset + new Vector3(0.0f, 0.0f, 0.90f),
                offset + new Vector3(-0.80f, 0.0f, 0.0f),
                offset + new Vector3(0.0f, 0.0f, -1.10f)
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

        private static void SetPrivateProperty(object target, string propertyName, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null, $"Missing property {propertyName}.");
            property.SetValue(target, value);
        }

        private sealed class TestLeftRightMesh3D : LeftRightMesh3D
        {
            public TestLeftRightMesh3D(Surface left, Surface right)
            {
                Name = "Synthetic phase 5 left/right mesh";
                m_Left = left;
                m_Right = right;
                m_Both = (Surface)left.Clone();
                m_Both.Append(right);
                m_SimplifiedLeft = (Surface)left.Clone();
                m_SimplifiedRight = (Surface)right.Clone();
                m_SimplifiedBoth = (Surface)m_Both.Clone();
            }
        }
    }
}
