using System;
using System.IO;
using System.Threading.Tasks;
using HBP.Core.DLL;
using HBP.Core.Enums;
using HBP.Quest;
using HBP.Transfer.Projection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HBP.Tests.Quest
{
    public class DensityCalculationTests
    {
        [Test]
        public async Task ReceivedInputsMatchDesktopWrappersAndRecalculateExactly()
        {
            string root = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString("N"));
            var snapshot = NativeProjectionInputTests.Snapshot();
            var inputs = NativeProjectionInputs.Create(snapshot, root);
            try
            {
                var first = await inputs.ComputeDensityAsync(true);
                var second = await inputs.ComputeDensityAsync(true);
                using var grid = ActivityProjectionGrid.Create(inputs.Volume, 80, VolumeInterpolation.Trilinear);
                using var density = new DensityGenerator();
                density.Initialize(grid);
                using var surface = new SurfaceGenerator();
                surface.Initialize(density, inputs.Surface, 0, 1);
                density.ComputeActivity(inputs.Sites, 15, SiteInfluenceByDistanceType.Quadratic);
                surface.ComputeActivityUV(0, .8f);
                Assert.That(first.ActivityUV, Is.EqualTo(surface.ActivityUV));
                Assert.That(first.AlphaUV, Is.EqualTo(surface.AlphaUV));
                Assert.That(first.MaxDensity, Is.EqualTo(density.MaxDensity));
                Assert.That(first.GridPoints, Is.EqualTo(grid.Points));
                Assert.That(first.SiteMasks, Is.EqualTo(new[] { 0, 1 }));
                Assert.That(second.ActivityUV, Is.EqualTo(first.ActivityUV));
                Assert.That(second.AlphaUV, Is.EqualTo(first.AlphaUV));
            }
            finally
            {
                inputs.Dispose();
                Directory.Delete(root);
            }
        }

        [Test]
        public async Task RetirementDuringScheduledCalculationReleasesAfterCompletion()
        {
            string root = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString("N"));
            var inputs = NativeProjectionInputs.Create(NativeProjectionInputTests.Snapshot(), root);
            Task<NativeProjectionInputs.DensityResult> work = inputs.ComputeDensityAsync();
            inputs.Dispose();
            var result = await work;
            Assert.That(result.ActivityUV.Length, Is.EqualTo(3));
            Assert.That(inputs.Volume.getHandle().Handle, Is.EqualTo(IntPtr.Zero));
            Assert.That(inputs.Surface.getHandle().Handle, Is.EqualTo(IntPtr.Zero));
            Assert.That(inputs.Sites.getHandle().Handle, Is.EqualTo(IntPtr.Zero));
            Assert.That(File.Exists(inputs.VolumePath), Is.False);
            Assert.That(inputs.CleanupError, Is.Null);
            Assert.Throws<ObjectDisposedException>(() => inputs.ComputeDensityAsync());
            Directory.Delete(root);
        }

        [Test]
        public async Task FailedNativeWorkStillReleasesRetiredInputs()
        {
            string root = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString("N"));
            var inputs = NativeProjectionInputs.Create(NativeProjectionInputTests.Snapshot(), root);
            inputs.Sites.Dispose(); // Deliberately invalid native input, before any worker exists.
            var work = inputs.ComputeDensityAsync();
            inputs.Dispose();
            Exception failure = null;
            try
            {
                await work;
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            Assert.That(failure, Is.TypeOf<InvalidOperationException>());
            Assert.That(inputs.Volume.getHandle().Handle, Is.EqualTo(IntPtr.Zero));
            Assert.That(inputs.Surface.getHandle().Handle, Is.EqualTo(IntPtr.Zero));
            Assert.That(inputs.CleanupError, Is.Null);
            Directory.Delete(root);
        }

        [Test]
        public async Task ReplacementClearAndOfflineRecalculationPublishOnlyCurrentBuffers()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestAnatomy.prefab");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var view = instance.GetComponent<QuestAnatomyView>();
            typeof(QuestAnatomyView).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(view, null);
            try
            {
                var snapshot = NativeProjectionInputTests.Snapshot();
                view.ApplySnapshot(snapshot);
                var retired = view.ProjectionInputs;
                Task oldWork = view.DensityCompletion;
                view.RecalculateDensity();
                Assert.That(view.DensityCompletion, Is.SameAs(oldWork), "Repeated requests coalesce while publication is pending.");
                view.ApplySnapshot(snapshot);
                Task newWork = view.DensityCompletion;
                await oldWork;
                await newWork;
                Assert.That(view.DensityError, Is.Null);
                Assert.That(view.SharedMesh.uv3, Is.EqualTo(view.Density.ActivityUV));
                Assert.That(view.SharedMesh.uv2, Is.EqualTo(view.Density.AlphaUV));
                Assert.That(retired.Volume.getHandle().Handle, Is.EqualTo(IntPtr.Zero));
                var expected = view.Density.ActivityUV;
                view.ToggleSurface();
                view.transform.localScale = Vector3.one * 2;
                view.RecalculateDensity(); // No session/transport exists in this fixture.
                await view.DensityCompletion;
                Assert.That(view.Density.ActivityUV, Is.EqualTo(expected));
                Assert.That(view.SurfaceHidden, Is.True);
                Assert.That(view.transform.localScale, Is.EqualTo(Vector3.one * 2));
                view.RecalculateDensity();
                Task last = view.DensityCompletion;
                var inputs = view.ProjectionInputs;
                view.Clear();
                await last;
                Assert.That(view.Density, Is.Null);
                Assert.That(view.DensityComputing, Is.False);
                Assert.That(view.DensityError, Is.Null);
                Assert.That(inputs.Volume.getHandle().Handle, Is.EqualTo(IntPtr.Zero));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
