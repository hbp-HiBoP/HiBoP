using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.DLL;
using HBP.Core.Enums;
using HBP.Data.Module3D;
using HBP.Transfer.Anatomy;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HBP.Tests.Transfer.Anatomy.Desktop
{
    public class DensityProjectionTests
    {
        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(8, false)]
        [TestCase(8, true)]
        public void CommonPathMatchesPreviousDesktop(int siteCount, bool allMasked)
        {
            using var volume = LoadVolume();
            using var surface = new Surface();
            surface.SetBuffers(new[] { new Vector3(10, 0, 0), new Vector3(11, 1, 0), new Vector3(9, 0, 1), new Vector3(1000, 0, 0) }, new[] { 0, 1, 2, 1, 2, 3 });
            using var sites = new RawSiteList();
            for (int i = 0; i < siteCount; ++i) sites.AddSite("S" + i, new Vector3(-10 + i, i, 0), 0, i);
            var masks = Enumerable.Range(0, siteCount).Select(i => allMasked || i % 3 == 2).ToArray();
            foreach (VolumeInterpolation interpolation in Enum.GetValues(typeof(VolumeInterpolation)))
            foreach (SiteInfluenceByDistanceType rule in Enum.GetValues(typeof(SiteInfluenceByDistanceType)))
                Compare(volume, surface, sites, masks, 32, interpolation, rule);
        }

        [Test]
        public void PreparedMniFixtureMatchesPreviousDesktopAtFullGrid()
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-questAnatomyFixture");
            if (index < 0) Assert.Ignore("Supply the QUEST-017 Desktop HBNA v3 capture with -questAnatomyFixture.");
            var snapshot = AnatomySnapshotCodec.Decode(File.ReadAllBytes(args[index + 1]));
            Assert.That(snapshot.Contacts.Sites.Count, Is.EqualTo(8));
            var vertices = Enumerable.Range(0, snapshot.Positions.Count / 3).Select(i => new Vector3(snapshot.Positions[i * 3], snapshot.Positions[i * 3 + 1], snapshot.Positions[i * 3 + 2])).ToArray();
            using var volume = LoadVolume();
            using var surface = new Surface();
            surface.SetBuffers(vertices, snapshot.Indices.ToArray().Select(i => (int)i).ToArray());
            using var sites = new RawSiteList();
            foreach (var site in snapshot.Contacts.Sites) sites.AddSite(site.Name, new Vector3(-site.Position[0], site.Position[1], site.Position[2]), site.PatientIndex, site.SourceIndex);
            Compare(volume, surface, sites, snapshot.Contacts.Sites.Select(site => site.EffectiveMasked).ToArray(), 80, VolumeInterpolation.Trilinear, SiteInfluenceByDistanceType.Quadratic);
        }

        // Reference is the orchestration in 28e858495, retained ONLY in tests.
        private static void Compare(Volume volume, Surface surface, RawSiteList sites, bool[] masks, int dimension, VolumeInterpolation interpolation, SiteInfluenceByDistanceType rule)
        {
            using var oldGrid = new ActivityProjectionGrid();
            oldGrid.Initialize(volume, dimension, interpolation);
            using var oldDensity = new DensityGenerator();
            oldDensity.Initialize(oldGrid);
            using var oldProjection = new SurfaceGenerator();
            oldProjection.Initialize(oldDensity, surface);
            oldProjection.ComputeMainUV(0, 1);
            oldProjection.ComputeNullUV();
            using var grid = ActivityProjectionGrid.Create(volume, dimension, interpolation);
            using var density = new DensityGenerator();
            using var projection = new SurfaceGenerator();
            density.Initialize(grid);
            projection.Initialize(density, surface, 0, 1);
            Assert.That(grid.Dimensions, Is.EqualTo(oldGrid.Dimensions));
            Vector2[] initial = null;
            foreach (float distance in new[] { 15f, 25f, 15f })
            {
                for (int i = 0; i < masks.Length; ++i) sites.UpdateMask(i, masks[i]);
                oldDensity.ComputeActivity(sites, distance, rule);
                sites.UpdateMasks(masks);
                density.ComputeActivity(sites, distance, rule);
                Assert.That(density.MaxDensity, Is.EqualTo(oldDensity.MaxDensity));
                foreach (float alpha in new[] { 0f, .8f, 1f })
                {
                    oldProjection.ComputeActivityUV(0, alpha);
                    projection.ComputeActivityUV(0, alpha);
                    Assert.That(projection.ActivityUV, Is.EqualTo(oldProjection.ActivityUV), "Activity UV must be bit-exact.");
                    Assert.That(projection.AlphaUV, Is.EqualTo(oldProjection.AlphaUV), "Alpha UV must be bit-exact.");
                    Assert.That(projection.ProjectionCoverage.validVertexCount, Is.EqualTo(oldProjection.ProjectionCoverage.validVertexCount));
                    Assert.That(projection.ProjectionCoverage.classification, Is.EqualTo(oldProjection.ProjectionCoverage.classification));
                    Assert.That(projection.ActivityUV.All(uv => float.IsFinite(uv.x) && float.IsFinite(uv.y)), Is.True);
                }

                if (distance == 15 && initial != null) Assert.That(projection.ActivityUV, Is.EqualTo(initial));
                if (initial == null) initial = (Vector2[])projection.ActivityUV.Clone();
            }

            if (masks.All(mask => mask)) Assert.That(density.MaxDensity, Is.Zero);
            else Assert.That(density.MaxDensity, Is.GreaterThan(0));
            // Surface and grid invalidation must not retain a previous binding.
            using var changedGrid = ActivityProjectionGrid.Create(volume, dimension + 1, interpolation);
            density.Initialize(changedGrid);
            projection.Initialize(density, surface, 0, 1);
            Assert.That(projection.ProjectionCoverage.classification, Is.EqualTo(SurfaceProjectionClassification.Unavailable));
            sites.UpdateMasks(masks);
            density.ComputeActivity(sites, 15, rule);
            projection.ComputeActivityUV(0, .8f);
            density.Initialize(grid);
            projection.Initialize(density, surface, 0, 1);
            sites.UpdateMasks(masks);
            density.ComputeActivity(sites, 15, rule);
            projection.ComputeActivityUV(0, 1);
            Assert.That(projection.ActivityUV, Is.EqualTo(initial));
        }

        [Test]
        public void DesktopAdapterFreezesEffectiveMasksAndCallsCommonPath()
        {
            using var volume = LoadVolume();
            using var grid = ActivityProjectionGrid.Create(volume, 32, VolumeInterpolation.Trilinear);
            using var surface = new Surface();
            surface.SetBuffers(new[] { new Vector3(10, 0, 0), new Vector3(11, 1, 0), new Vector3(9, 0, 1) }, new[] { 0, 1, 2 });
            var root = new GameObject("Density adapter test");
            root.SetActive(false);
            var column = root.AddComponent<Column3DAnatomy>();
            using var density = new DensityGenerator();
            using var projection = new SurfaceGenerator();
            try
            {
                SetProperty(column, "ActivityGenerator", density);
                column.SurfaceGenerator = projection;
                var site = root.AddComponent<HBP.Core.Object3D.Site>();
                site.State = new HBP.Core.Object3D.SiteState { IsFiltered = true };
                SetProperty(column, "Sites", new List<HBP.Core.Object3D.Site> { site });
                column.RawElectrodes.AddSite("S1", new Vector3(-10, 0, 0), 0, 0);
                column.ActivityGenerator.Initialize(grid);
                column.SurfaceGenerator.Initialize(column.ActivityGenerator, surface, 0, 1);
                Action compute = column.PrepareActivityComputation(false, SiteInfluenceByDistanceType.Quadratic, false).Compute;
                site.State.IsMasked = true;
                column.AnatomyParameters.InfluenceDistance = 25;
                compute();
                Assert.That(column.RawElectrodes.GetMask(), Is.EqualTo(new[] { 0 }), "Captured mask remains independent of subsequent UI edits.");
                Assert.That(density.MaxDensity, Is.GreaterThan(0));
                column.ComputeSurfaceBrainUVWithActivity();
                Assert.That(projection.ActivityUV.Length, Is.EqualTo(3));
                column.PrepareActivityComputation(false, SiteInfluenceByDistanceType.Quadratic, false).Compute();
                Assert.That(density.MaxDensity, Is.Zero);
            }
            finally
            {
                column.RawElectrodes.Dispose();
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void InvalidMaskCountDoesNotMutateNativeSites()
        {
            using var sites = new RawSiteList();
            sites.AddSite("S1", Vector3.zero, 0, 0);
            sites.UpdateMask(0, true);
            using var density = new DensityGenerator();
            Assert.Throws<ArgumentException>(() => sites.UpdateMasks(Array.Empty<bool>()));
            Assert.That(sites.GetMask(), Is.EqualTo(new[] { 1 }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task DestructionDefersHandlesUntilActualWorkerCompletion(bool fail)
        {
            using var volume = LoadVolume();
            var grid = ActivityProjectionGrid.Create(volume, 32, VolumeInterpolation.Trilinear);
            var root = new GameObject("Density lifetime test");
            root.SetActive(false);
            var scene = root.AddComponent<Base3DScene>();
            var column = root.AddComponent<Column3DAnatomy>();
            var density = new DensityGenerator();
            SetProperty(column, "ActivityGenerator", density);
            column.RawElectrodes.AddSite("S1", new Vector3(-10, 0, 0), 0, 0);
            density.Initialize(grid);
            var sites = column.RawElectrodes;
            var allowCompute = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var finished = new TaskCompletionSource<bool>();
            using var observerCancellation = new CancellationTokenSource();
            var work = Task.Run(async () =>
            {
                try
                {
                    await allowCompute.Task;
                    sites.UpdateMasks(fail ? Array.Empty<bool>() : new[] { false });
                    density.ComputeActivity(sites, 15, SiteInfluenceByDistanceType.Quadratic);
                }
                finally
                {
                    finished.TrySetResult(true);
                }
            });
            var completion = finished.Task.AsUniTask().ToAsyncLazy().Task;
            typeof(Column3D).GetProperty("GeneratorWork", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(column, completion);
            typeof(Base3DScene).GetField("m_GeneratorWork", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(scene, completion);
            typeof(Base3DScene).GetField("m_ActivityProjectionGrid", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(scene, grid);
            var releaseColumn = (UniTask)typeof(Column3D).GetMethod("ReleaseResources", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(column, null);
            var releaseScene = (UniTask)typeof(Base3DScene).GetMethod("BeginClose", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(scene, null);
            try
            {
                var observer = finished.Task.AsUniTask().AttachExternalCancellation(observerCancellation.Token);
                observerCancellation.Cancel();
                Exception observerError = null;
                try
                {
                    await observer;
                }
                catch (OperationCanceledException exception)
                {
                    observerError = exception;
                }

                Assert.That(observerError, Is.Not.Null, "Only the observer was cancelled, not the native worker.");
                Assert.That(releaseColumn.Status.IsCompleted() || releaseScene.Status.IsCompleted(), Is.False);
                Assert.That(sites.getHandle().Handle, Is.Not.EqualTo(IntPtr.Zero));
                Assert.That(density.getHandle().Handle, Is.Not.EqualTo(IntPtr.Zero));
                Assert.That(grid.getHandle().Handle, Is.Not.EqualTo(IntPtr.Zero));
            }
            finally
            {
                allowCompute.TrySetResult(true);
                Exception workerError = null;
                try
                {
                    await work;
                }
                catch (Exception exception)
                {
                    workerError = exception;
                }

                await releaseColumn;
                await releaseScene;
                Object.DestroyImmediate(root);
                Assert.That(workerError, fail ? Is.TypeOf<ArgumentException>() : Is.Null);
            }

            Assert.That(sites.getHandle().Handle, Is.EqualTo(IntPtr.Zero));
            Assert.That(density.getHandle().Handle, Is.EqualTo(IntPtr.Zero));
            Assert.That(grid.getHandle().Handle, Is.EqualTo(IntPtr.Zero));
        }

        [TestCase(typeof(Column3DIEEG))]
        [TestCase(typeof(Column3DCCEP))]
        [TestCase(typeof(Column3DStatic))]
        public async Task SignalPreparationFreezesValuesDimensionsMasksAndCalibration(Type columnType)
        {
            using var volume = LoadVolume();
            using var grid = ActivityProjectionGrid.Create(volume, 24, VolumeInterpolation.Trilinear);
            using var surface = new Surface();
            surface.SetBuffers(new[] { new Vector3(10, 0, 0), new Vector3(11, 1, 0), new Vector3(9, 0, 1) }, new[] { 0, 1, 2 });
            var root = new GameObject("Signal preparation test");
            root.SetActive(false);
            var column = (Column3D)root.AddComponent(columnType);
            using var generator = new IEEGGenerator();
            using var reference = new IEEGGenerator();
            using var projection = new SurfaceGenerator();
            using var referenceProjection = new SurfaceGenerator();
            try
            {
                SetProperty(column, "ActivityGenerator", generator);
                var site = root.AddComponent<HBP.Core.Object3D.Site>();
                site.State = new HBP.Core.Object3D.SiteState { IsFiltered = true };
                SetProperty(column, "Sites", new List<HBP.Core.Object3D.Site> { site });
                column.RawElectrodes.AddSite("S1", new Vector3(-10, 0, 0), 0, 0);
                var timeline = (HBP.Core.Data.Timeline)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(HBP.Core.Data.Timeline));
                typeof(HBP.Core.Data.Timeline).GetProperty("Length").SetValue(timeline, 1);
                if (column is Column3DIEEG) SetProperty(column, "ColumnData", new HBP.Core.Data.IEEGColumn { Data = new HBP.Core.Data.Processed.IEEGData { ProjectionTimeline = timeline } });
                if (column is Column3DCCEP) SetProperty(column, "ColumnData", new HBP.Core.Data.CCEPColumn { Data = new HBP.Core.Data.Processed.CCEPData { ProjectionTimeline = timeline } });
                var valuesProperty = columnType.GetProperty("ActivityValues");
                valuesProperty.SetValue(column, new[] { 1f });
                if (column is Column3DStatic) columnType.GetProperty("Labels").SetValue(column, new[] { "value" });
                var parameters = column is Column3DDynamic dynamicColumn ? dynamicColumn.DynamicParameters : ((Column3DStatic)column).StaticParameters;
                parameters.SetSpanValues(-2, 0, 2);
                column.RawElectrodes.UpdateMask(0, false);
                generator.Initialize(grid);
                reference.Initialize(grid);
                reference.ComputeActivity(column.RawElectrodes, parameters.InfluenceDistance, new[] { 1f }, 1, 1, SiteInfluenceByDistanceType.Quadratic);
                reference.AdjustValues(parameters.Middle, parameters.SpanMin, parameters.SpanMax);
                projection.Initialize(generator, surface, 0, 1);
                referenceProjection.Initialize(reference, surface, 0, 1);
                var prepared = column.PrepareActivityComputation(false, SiteInfluenceByDistanceType.Quadratic, false);
                valuesProperty.SetValue(column, Array.Empty<float>());
                typeof(HBP.Core.Data.Timeline).GetProperty("Length").SetValue(timeline, 0);
                if (column is Column3DStatic) columnType.GetProperty("Labels").SetValue(column, Array.Empty<string>());
                parameters.InfluenceDistance = 1;
                parameters.SetSpanValues(-100, 0, 100);
                site.State.IsMasked = true;
                await Task.Run(prepared.Compute);
                projection.ComputeActivityUV(0, .8f);
                referenceProjection.ComputeActivityUV(0, .8f);
                Assert.That(column.RawElectrodes.GetMask(), Is.EqualTo(new[] { 0 }));
                Assert.That(projection.ActivityUV, Is.EqualTo(referenceProjection.ActivityUV));
                Assert.That(projection.AlphaUV, Is.EqualTo(referenceProjection.AlphaUV));
            }
            finally
            {
                column.RawElectrodes.Dispose();
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task VolumePreparationFreezesVolumeListAndCalibration(bool meg)
        {
            using var volume = LoadVolume();
            using var grid = ActivityProjectionGrid.Create(volume, 24, VolumeInterpolation.Trilinear);
            using var surface = new Surface();
            surface.SetBuffers(new[] { Vector3.zero, Vector3.one, Vector3.up }, new[] { 0, 1, 2 });
            var root = new GameObject("Volume preparation test");
            root.SetActive(false);
            var column = meg ? (Column3D)root.AddComponent<Column3DMEG>() : root.AddComponent<Column3DFMRI>();
            using ActivityGenerator generator = meg ? new MEGGenerator() : new FMRIGenerator();
            using ActivityGenerator reference = meg ? new MEGGenerator() : new FMRIGenerator();
            using var projection = new SurfaceGenerator();
            using var referenceProjection = new SurfaceGenerator();
            var fmri = new HBP.Core.Object3D.FMRI();
            fmri.Volumes[0].Dispose();
            fmri.Volumes.Clear();
            fmri.Volumes.Add(volume);
            try
            {
                SetProperty(column, "ActivityGenerator", generator);
                SetProperty(column, "Sites", new List<HBP.Core.Object3D.Site>());
                if (meg)
                {
                    var data = new HBP.Core.Data.MEGColumn();
                    data.Data.MEGItems.Add(new HBP.Core.Data.Processed.MEGItem { FMRI = fmri });
                    SetProperty(column, "ColumnData", data);
                }
                else
                {
                    var data = new HBP.Core.Data.FMRIColumn();
                    data.Data.FMRIs.Add(Tuple.Create(fmri, (HBP.Core.Data.Patient)null));
                    SetProperty(column, "ColumnData", data);
                }

                generator.Initialize(grid);
                reference.Initialize(grid);
                var pairs = new[] { (volume, fmri.MaskVolume) };
                object parameters = meg ? ((Column3DMEG)column).MEGParameters : ((Column3DFMRI)column).FMRIParameters;
                T Read<T>(string name) => (T)parameters.GetType().GetProperty(name).GetValue(parameters);
                if (reference is MEGGenerator m)
                {
                    m.ComputeActivity(pairs);
                    m.AdjustValues(Read<float>("FMRINegativeCalMinFactor"), Read<float>("FMRINegativeCalMaxFactor"), Read<float>("FMRIPositiveCalMinFactor"), Read<float>("FMRIPositiveCalMaxFactor"));
                    m.HideExtremeValues(Read<bool>("HideLowerValues"), Read<bool>("HideMiddleValues"), Read<bool>("HideHigherValues"));
                }
                else if (reference is FMRIGenerator f)
                {
                    f.ComputeActivity(pairs);
                    f.AdjustValues(Read<float>("FMRINegativeCalMinFactor"), Read<float>("FMRINegativeCalMaxFactor"), Read<float>("FMRIPositiveCalMinFactor"), Read<float>("FMRIPositiveCalMaxFactor"));
                    f.HideExtremeValues(Read<bool>("HideLowerValues"), Read<bool>("HideMiddleValues"), Read<bool>("HideHigherValues"));
                }

                projection.Initialize(generator, surface, 0, 1);
                referenceProjection.Initialize(reference, surface, 0, 1);
                var prepared = column.PrepareActivityComputation(false, SiteInfluenceByDistanceType.Quadratic, false);
                fmri.Volumes.Clear();
                parameters.GetType().GetProperty("HideHigherValues").SetValue(parameters, !Read<bool>("HideHigherValues"));
                parameters.GetType().GetProperty("FMRIPositiveCalMinFactor").SetValue(parameters, .5f);
                if (meg) ((Column3DMEG)column).ColumnMEGData.Data.MEGItems.Clear();
                else ((Column3DFMRI)column).ColumnFMRIData.Data.FMRIs.Clear();
                await Task.Run(prepared.Compute);
                projection.ComputeActivityUV(0, .8f);
                referenceProjection.ComputeActivityUV(0, .8f);
                Assert.That(projection.ActivityUV, Is.EqualTo(referenceProjection.ActivityUV));
                Assert.That(projection.AlphaUV, Is.EqualTo(referenceProjection.AlphaUV));
            }
            finally
            {
                fmri.MaskVolume.Dispose();
                column.RawElectrodes.Dispose();
                Object.DestroyImmediate(root);
            }
        }

        private static Volume LoadVolume()
        {
            var volume = new Volume();
            if (volume.LoadNIFTIFile(Path.Combine(Application.dataPath, "Data/IRM/MNI.nii"))) return volume;
            volume.Dispose();
            throw new InvalidOperationException("MNI fixture could not be loaded.");
        }

        private static void SetProperty(Column3D column, string name, object value) => typeof(Column3D).GetProperty(name).SetValue(column, value);
    }
}
