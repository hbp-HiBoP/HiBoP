using System;
using System.Runtime.InteropServices;
using HBP.Core.Data;
using HBP.Core.DLL;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Serialization
{
    public class PreviewSurfaceRuntimeMeshTests
    {
        [Test]
        [Category("NativeDll")]
        public void PreviewInteropTypes_MatchNativeLayout()
        {
            Assert.That(Marshal.SizeOf<PreviewSurfaceOptions>(), Is.EqualTo(40));
            Assert.That(Marshal.OffsetOf<PreviewSurfaceOptions>(nameof(PreviewSurfaceOptions.ThresholdMode)).ToInt32(), Is.EqualTo(4));
            Assert.That(Marshal.OffsetOf<PreviewSurfaceOptions>(nameof(PreviewSurfaceOptions.PadWithBackground)).ToInt32(), Is.EqualTo(36));

            Assert.That(Marshal.SizeOf<PreviewSurfaceReport>(), Is.EqualTo(80));
            Assert.That(Marshal.OffsetOf<PreviewSurfaceReport>(nameof(PreviewSurfaceReport.AppliedThreshold)).ToInt32(), Is.EqualTo(4));
            Assert.That(Marshal.OffsetOf<PreviewSurfaceReport>(nameof(PreviewSurfaceReport.PreprocessingMilliseconds)).ToInt32(), Is.EqualTo(56));
        }

        [Test]
        [Category("NativeDll")]
        public void ExtractPreviewSurface_RequiresLoadedVolume()
        {
            NativeParityAssert.RequireHbpCore();
            using Volume volume = new();

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                volume.ExtractPreviewSurface(PreviewSurfaceOptions.Default, out _));

            StringAssert.Contains("must be loaded", exception.Message);
        }

        [Test]
        [Category("NativeDll")]
        public void ExtractPreviewSurface_ReportsNativeValidationErrorsWithoutInvalidatingVolume()
        {
            NativeParityAssert.RequireHbpCore();
            using Volume volume = LoadSyntheticMRI();
            PreviewSurfaceOptions invalidOptions = FastPreviewOptions();
            invalidOptions.MaximumGridDimension = 31;

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                volume.ExtractPreviewSurface(invalidOptions, out _));

            StringAssert.Contains("InvalidArgument", exception.Message);
            StringAssert.Contains("options are invalid", exception.Message);
            using Surface surface = volume.ExtractPreviewSurface(FastPreviewOptions(), out _);
            Assert.That(surface.IsLoaded, Is.True);
        }

        [Test]
        [Category("NativeDll")]
        public void ExtractPreviewSurface_CreatesLoadedOwnedUnityMesh()
        {
            NativeParityAssert.RequireHbpCore();
            using Volume volume = LoadSyntheticMRI();
            Surface surface = volume.ExtractPreviewSurface(FastPreviewOptions(), out PreviewSurfaceReport report);
            Mesh unityMesh = new();
            try
            {
                Assert.That(surface.IsLoaded, Is.True);
                Assert.That(surface.getHandle().Handle, Is.Not.EqualTo(IntPtr.Zero));
                Assert.That(surface.NumberOfVertices, Is.GreaterThan(0));
                Assert.That(surface.NumberOfTriangles, Is.GreaterThan(0));
                Assert.That(report.StructSize, Is.EqualTo((uint)Marshal.SizeOf<PreviewSurfaceReport>()));
                Assert.That(report.InputX, Is.EqualTo(5));
                Assert.That(report.InputY, Is.EqualTo(5));
                Assert.That(report.InputZ, Is.EqualTo(5));
                Assert.That(report.TriangleCountAfterSimplification, Is.EqualTo(surface.NumberOfTriangles));

                surface.UpdateMeshFromDLL(unityMesh);

                Assert.That(unityMesh.vertexCount, Is.EqualTo(surface.NumberOfVertices));
                Assert.That(unityMesh.triangles.Length, Is.EqualTo(surface.NumberOfTriangles * 3));
            }
            finally
            {
                surface.Dispose();
                surface.Dispose();
                UnityEngine.Object.DestroyImmediate(unityMesh);
            }

            Assert.That(surface.getHandle().Handle, Is.EqualTo(IntPtr.Zero));
        }

        [Test]
        [Category("NativeDll")]
        public void RuntimeSingleMesh3D_IsLoadedNeverUsesGiftiAndCleansOwnedSurfaces()
        {
            NativeParityAssert.RequireHbpCore();
            using Volume volume = LoadSyntheticMRI();
            Surface surface = volume.ExtractPreviewSurface(FastPreviewOptions(), out PreviewSurfaceReport report);
            StubMRI3D sourceMRI = new("synthetic T1");
            RuntimeSingleMesh3D runtimeMesh = new(sourceMRI, surface, report);
            Surface simplifiedSurface = runtimeMesh.SimplifiedBoth;

            Assert.That(runtimeMesh.IsLoaded, Is.True);
            Assert.That(runtimeMesh.Type, Is.EqualTo(MeshType.Patient));
            Assert.That(runtimeMesh.HasBeenLoadedOutside, Is.False);
            Assert.That(runtimeMesh.IsTransient, Is.True);
            Assert.That(runtimeMesh.SupportsMarsAtlas, Is.False);
            Assert.That(runtimeMesh.SupportsMNIResources, Is.False);
            Assert.That(runtimeMesh.SupportsHemispheres, Is.False);
            Assert.That(runtimeMesh.SourceMRI, Is.SameAs(sourceMRI));
            Assert.That(runtimeMesh.Name, Is.EqualTo("MRI preview – synthetic T1"));
            Assert.That(runtimeMesh.Both, Is.SameAs(surface));
            Assert.That(simplifiedSurface, Is.Not.SameAs(surface));
            Assert.DoesNotThrow(runtimeMesh.Load);
            Assert.Throws<NotSupportedException>(() => runtimeMesh.Clone());

            runtimeMesh.Clean();
            runtimeMesh.Clean();

            Assert.That(surface.getHandle().Handle, Is.EqualTo(IntPtr.Zero));
            Assert.That(simplifiedSurface.getHandle().Handle, Is.EqualTo(IntPtr.Zero));
            Assert.That(runtimeMesh.IsLoaded, Is.False);
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => runtimeMesh.Load());
            StringAssert.Contains("cannot be loaded from disk", exception.Message);
        }

        [Test]
        [Category("PreviewSurface.Increment5")]
        public void RuntimePreviewDistanceDiagnosticReportsPercentilesWithoutChangingInfluence()
        {
            Vector3[] vertices = { Vector3.zero };
            Vector3[] sites =
            {
                new(1f, 0f, 0f),
                new(2f, 0f, 0f),
                new(3f, 0f, 0f),
                new(10f, 0f, 0f)
            };

            RuntimePreviewDistanceReport report = RuntimePreviewDistanceDiagnostic.Evaluate(vertices, sites, 2f);

            Assert.That(report.SiteCount, Is.EqualTo(4));
            Assert.That(report.SitesBeyondInfluence, Is.EqualTo(2));
            Assert.That(report.InfluenceDistance, Is.EqualTo(2f));
            Assert.That(report.Percentile50, Is.EqualTo(2f));
            Assert.That(report.Percentile90, Is.EqualTo(10f));
            Assert.That(report.Percentile95, Is.EqualTo(10f));
            Assert.That(report.FractionBeyondInfluence, Is.EqualTo(0.5f));
            Assert.That(report.SuggestedInfluenceDistance, Is.EqualTo(12f));
            Assert.That(report.ShouldWarn, Is.True);

            RuntimePreviewDistanceReport withinRange = RuntimePreviewDistanceDiagnostic.Evaluate(vertices, sites, 10f);
            Assert.That(withinRange.ShouldWarn, Is.False);
            Assert.Throws<ArgumentException>(() => RuntimePreviewDistanceDiagnostic.Evaluate(Array.Empty<Vector3>(), sites, 2f));
            Assert.Throws<ArgumentOutOfRangeException>(() => RuntimePreviewDistanceDiagnostic.Evaluate(vertices, sites, -1f));
        }

        [Test]
        [Category("NativeDll")]
        public void MeshManager_AddRuntimeKeepsRuntimeStateSeparateFromPatientDataAndPreloads()
        {
            NativeParityAssert.RequireHbpCore();
            Patient patient = new(
                "synthetic patient",
                Array.Empty<BaseMesh>(),
                Array.Empty<MRI>(),
                Array.Empty<HBP.Core.Data.Site>(),
                Array.Empty<BaseTagValue>(),
                "synthetic database",
                "synthetic-patient-preview-test");
            int persistentMeshCount = patient.Meshes.Count;
            using Volume volume = LoadSyntheticMRI();
            Surface surface = volume.ExtractPreviewSurface(FastPreviewOptions(), out PreviewSurfaceReport report);
            StubMRI3D sourceMRI = new("synthetic T1");
            RuntimeSingleMesh3D runtimeMesh = new(sourceMRI, surface, report);
            RuntimeSingleMesh3D duplicateSourceRuntimeMesh = new(
                sourceMRI,
                (Surface)surface.Clone(),
                report);
            RuntimeSingleMesh3D secondRuntimeMesh = new(
                new StubMRI3D("synthetic T2"),
                (Surface)surface.Clone(),
                report);
            GameObject managerObject = new("Preview surface MeshManager test");
            MeshManager manager = managerObject.AddComponent<MeshManager>();
            int toolbarNotifications = 0;
            Module3DMain.OnRequestUpdateInToolbar.AddListener(OnToolbarNotification);
            try
            {
                manager.Meshes.Add(new StubMesh3D(MeshType.MNI));
                manager.Meshes.Add(new StubMesh3D(MeshType.MNI));
                manager.Meshes.Add(new StubMesh3D(MeshType.MNI));
                typeof(MeshManager).GetProperty(nameof(MeshManager.MeshPartToDisplay))
                    ?.SetValue(manager, MeshPart.Left);

                manager.AddRuntime(runtimeMesh);
                manager.AddRuntime(secondRuntimeMesh);

                Assert.That(manager.RuntimePreviewMeshes, Is.EqualTo(new[] { runtimeMesh, secondRuntimeMesh }));
                Assert.That(manager.MeshPartToDisplay, Is.EqualTo(MeshPart.Both));
                Assert.That(manager.HasPersistentPatientMesh, Is.False);
                Assert.That(manager.PreloadedMeshes, Is.Empty);
                Assert.That(patient.Meshes.Count, Is.EqualTo(persistentMeshCount));
                Assert.That(toolbarNotifications, Is.EqualTo(2));
                Assert.Throws<InvalidOperationException>(() => manager.AddRuntime(duplicateSourceRuntimeMesh));

                manager.Meshes.Add(new StubMesh3D(MeshType.Patient));
                Assert.That(manager.HasPersistentPatientMesh, Is.True);
            }
            finally
            {
                Module3DMain.OnRequestUpdateInToolbar.RemoveListener(OnToolbarNotification);
                runtimeMesh.Clean();
                duplicateSourceRuntimeMesh.Clean();
                secondRuntimeMesh.Clean();
                UnityEngine.Object.DestroyImmediate(managerObject);
            }

            void OnToolbarNotification()
            {
                toolbarNotifications++;
            }
        }

        [Test]
        public void MRIManager_PatientMRIsIncludesEveryPatientMRIAndExcludesMNI()
        {
            GameObject managerObject = new("Patient MRIManager test");
            MRIManager manager = managerObject.AddComponent<MRIManager>();
            StubMRI3D mni = new("MNI", true);
            StubMRI3D ct = new("CT postimplantation");
            StubMRI3D functional = new("task BOLD functional");
            StubMRI3D structural = new("T1 structural");
            try
            {
                manager.MRIs.AddRange(new MRI3D[] { mni, ct, functional, structural });

                Assert.That(manager.PatientMRIs, Is.EqualTo(new[] { ct, functional, structural }));
            }
            finally
            {
                foreach (MRI3D mri in new MRI3D[] { mni, ct, functional, structural }) mri.Clean();
                UnityEngine.Object.DestroyImmediate(managerObject);
            }
        }

        private static Volume LoadSyntheticMRI()
        {
            Volume volume = new();
            try
            {
                Assert.That(volume.LoadNIFTIFile(NativeParityAssert.NativePath("Nifti", "mri_t1.nii")), Is.True);
                return volume;
            }
            catch
            {
                volume.Dispose();
                throw;
            }
        }

        private static PreviewSurfaceOptions FastPreviewOptions()
        {
            PreviewSurfaceOptions options = PreviewSurfaceOptions.Default;
            options.MaximumGridDimension = 32;
            options.TargetTriangleCount = 1000;
            options.BinaryClosingIterations = 0;
            options.ScalarSmoothingIterations = 0;
            options.FillInternalCavities = 0;
            return options;
        }

        private sealed class StubMesh3D : Mesh3D
        {
            public StubMesh3D(MeshType type)
            {
                Type = type;
            }

            public override void Load()
            {
            }

            public override object Clone()
            {
                return new StubMesh3D(Type);
            }
        }

        private sealed class StubMRI3D : MRI3D
        {
            public StubMRI3D(string name, bool external = false)
            {
                Name = name;
                HasBeenLoadedOutside = external;
            }
        }
    }
}
