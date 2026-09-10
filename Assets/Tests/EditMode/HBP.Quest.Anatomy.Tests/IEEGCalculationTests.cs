using QuestAnatomyView = HBP.Quest.Legacy.QuestAnatomyView;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HBP.Core.DLL;
using HBP.Core.Enums;
using HBP.Quest;
using HBP.Quest.Legacy;
using HBP.Transfer.Anatomy;
using HBP.Transfer.Projection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HBP.Tests.Quest
{
    public class IEEGCalculationTests
    {
        private static AnatomySnapshot Snapshot(float value = -4, float temporalAlpha = 0)
        {
            var s = NativeProjectionInputTests.Snapshot();
            var instant = new IEEGInstant("data", "uV", "bloc", "sub", new string('a', 64), 1, 3, 200, 5, 0, 2, 100, 2, temporalAlpha, -10, 0, 10, new[] { "patient-é_A1", "patient-é_B1" }, new[] { "uV", "uV" }, new byte[] { 2, 0 }, new[] { value, 0 }, new[] { temporalAlpha == 0 ? value : 2, 0 });
            return AnatomySnapshot.Create(Guid.NewGuid().ToString(), s.SessionId, s.VisualizationId, s.ColumnId, s.ContentRevision, s.Coordinates, s.Winding, s.Visible, s.Color.ToArray(), s.Positions.ToArray(), s.Normals.ToArray(), s.Indices.ToArray(), s.Uvs.ToArray(), s.Contacts, s.Projection, instant);
        }

        [TestCase(-4f, 0f)]
        [TestCase(0f, 0f)]
        [TestCase(8f, 0f)]
        [TestCase(-4f, .5f)]
        public async Task ReceivedInstantMatchesDesktopAndRecalculatesExactly(float value, float alpha)
        {
            var s = Snapshot(value, alpha);
            string root = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString());
            var inputs = NativeProjectionInputs.Create(s, root);
            try
            {
                var first = await inputs.ComputeIEEGAsync(s.IEEG, true);
                var second = await inputs.ComputeIEEGAsync(s.IEEG);
                using var grid = ActivityProjectionGrid.Create(inputs.Volume, 80, VolumeInterpolation.Trilinear);
                using var reference = new IEEGGenerator();
                reference.Initialize(grid);
                using var surface = new SurfaceGenerator();
                surface.Initialize(reference, inputs.Surface, 0, 1);
                reference.ComputeActivity(inputs.Sites, 15, s.IEEG.SurfaceValues.ToArray(), 1, 2, SiteInfluenceByDistanceType.Quadratic);
                reference.AdjustValues(0, -10, 10);
                surface.ComputeActivityUV(0, .8f);
                Assert.That(first.ActivityUV, Is.EqualTo(surface.ActivityUV));
                Assert.That(first.AlphaUV, Is.EqualTo(surface.AlphaUV));
                Assert.That(second.ActivityUV, Is.EqualTo(first.ActivityUV));
                Assert.That(second.AlphaUV, Is.EqualTo(first.AlphaUV));
                Assert.That(first.GridPoints, Is.EqualTo(grid.Points));
                Assert.That(first.SiteMasks, Is.EqualTo(new[] { 0, 1 }));
            }
            finally
            {
                inputs.Dispose();
                Directory.Delete(root);
            }
        }

        [Test]
        public async Task RetirementAndNativeFailureReleaseInputs()
        {
            foreach (bool fail in new[] { false, true })
            {
                var s = Snapshot();
                string root = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString());
                var inputs = NativeProjectionInputs.Create(s, root);
                if (fail) inputs.Sites.Dispose();
                var work = inputs.ComputeIEEGAsync(s.IEEG);
                inputs.Dispose();
                Exception failure = await Capture(async () => await work);
                Assert.That(failure != null, Is.EqualTo(fail));
                Assert.That(inputs.Volume.getHandle().Handle, Is.EqualTo(IntPtr.Zero));
                Assert.That(File.Exists(inputs.VolumePath), Is.False);
                Assert.That(inputs.CleanupError, Is.Null);
                Directory.Delete(root);
            }
        }

        [Test]
        public async Task PublicationIsAtomicAndCancellationClearReplacementAndOfflineCalculationAreSafe()
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Tests/Support/QuestPrototype/QuestAnatomy.prefab"));
            var view = instance.GetComponent<QuestAnatomyView>();
            typeof(QuestAnatomyView).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(view, null);
            try
            {
                var s = Snapshot();
                await view.ApplySnapshotAsync(s);
                var mesh = view.SharedMesh;
                var inputs = view.ProjectionInputs;
                using var stop = new CancellationTokenSource();
                var work = view.ApplySnapshotAsync(Snapshot(8), stop.Token);
                Assert.That(view.SharedMesh, Is.SameAs(mesh));
                Assert.That(view.TransferId, Is.EqualTo(s.TransferId));
                stop.Cancel();
                Assert.That(await Capture(() => work), Is.InstanceOf<OperationCanceledException>());
                Assert.That(view.SharedMesh, Is.SameAs(mesh));
                Assert.That(view.ProjectionInputs, Is.SameAs(inputs));
                var invalid = AnatomySnapshot.Create("invalid", s.SessionId, s.VisualizationId, s.ColumnId, 1, s.Coordinates, s.Winding, s.Visible, new[] { 1f, 1f, 1f, .5f }, s.Positions.ToArray(), s.Normals.ToArray(), s.Indices.ToArray(), s.Uvs.ToArray(), s.Contacts, s.Projection, s.IEEG);
                Assert.That(await Capture(() => view.ApplySnapshotAsync(invalid)), Is.TypeOf<ArgumentException>());
                Assert.That(view.SharedMesh, Is.SameAs(mesh), "Failed rendering preparation retains the previous complete result.");
                Assert.That(view.TransferId, Is.EqualTo(s.TransferId));
                view.ToggleSurface();
                view.transform.localScale = Vector3.one * 2;
                var expected = view.IEEGProjection;
                view.RecalculateProjection();
                await view.IEEGCompletion;
                Assert.That(view.IEEGError, Is.Null);
                Assert.That(view.SharedMesh.uv3, Is.EqualTo(expected.ActivityUV));
                Assert.That(view.SharedMesh.uv2, Is.EqualTo(expected.AlphaUV));
                Assert.That(view.SurfaceHidden, Is.True);
                Assert.That(view.transform.localScale, Is.EqualTo(Vector3.one * 2));
                view.RecalculateProjection();
                var oldWork = view.IEEGCompletion;
                await view.ApplySnapshotAsync(Snapshot(8));
                await oldWork;
                Assert.That(view.IEEG.SurfaceValues[0], Is.EqualTo(8));
                Assert.That(inputs.Volume.getHandle().Handle, Is.EqualTo(IntPtr.Zero));
                work = view.ApplySnapshotAsync(s);
                view.Clear();
                Assert.That(await Capture(() => work), Is.InstanceOf<OperationCanceledException>());
                Assert.That(view.SharedMesh, Is.Null);
                Assert.That(view.IEEGComputing, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static async Task<Exception> Capture(Func<Task> action)
        {
            try
            {
                await action();
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }
    }
}
