using System;
using System.IO;
using System.Linq;
using HBP.Core.DLL;
using HBP.Core.Enums;
using HBP.Transfer.Anatomy;
using HBP.Transfer.Projection;
using NUnit.Framework;
using UnityEngine;
using Plane = HBP.Core.DLL.Plane;

namespace HBP.Tests.Quest
{
    public class NativeProjectionInputTests
    {
        private string root;

        [SetUp]
        public void SetUp() => root = Path.Combine(Application.temporaryCachePath, "quest017-tests-" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(root)) Directory.Delete(root);
        }

        internal static AnatomySnapshot Snapshot()
        {
            var sites = new[]
            {
                new AnatomySite("a", "A1", "A", 0, 0, 7, new float[] { 13, 2, 3 }, new float[] { 1, 1, 1, 1 }, 2, true, AnatomySiteFlags.Filtered, false),
                new AnatomySite("b", "B1", "B", 1, 0, 9, new float[] { -4, 5, 6 }, new float[] { 1, 1, 1, 1 }, 2, false, AnatomySiteFlags.Filtered | AnatomySiteFlags.Masked, true)
            };
            return AnatomySnapshot.Create("../../transfer", "../../session", "v", "c", 1, new AnatomyCoordinateSpace(AnatomyContacts.FrameId, AnatomyHandedness.Left, AnatomyLengthUnit.Millimeter, 1, new float[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 }), AnatomyWinding.Clockwise, true, new float[] { 1, 1, 1, 1 }, new float[] { -2, 1, 4, 5, 2, 1, 1, 6, 9 }, new float[] { .6f, 0, .8f, .6f, 0, .8f, .6f, 0, .8f }, new uint[] { 0, 1, 2 }, new float[] { 0, 0, 1, 0, 0, 1 }, new AnatomyContacts("MNI", false, new[] { "patient-é" }, sites), new AnatomyProjection(File.ReadAllBytes(Path.Combine(Application.dataPath, "Data/IRM/MNI.nii")), 80, 1, 15, 2, .8f));
        }

        [Test]
        public void NativeRoundTripPreservesSurfaceWindingNormalsSitesAndMniVolume()
        {
            var source = Snapshot();
            var snapshot = AnatomySnapshotCodec.Decode(AnatomySnapshotCodec.Encode(source));
            using var expectedVolume = new Volume();
            Assert.That(expectedVolume.LoadNIFTIFile(Path.Combine(Application.dataPath, "Data/IRM/MNI.nii")), Is.True);
            var input = NativeProjectionInputs.Create(snapshot, root);
            var mesh = new Mesh();
            try
            {
                input.Surface.UpdateMeshFromDLL(mesh);
                Assert.That(mesh.vertices.SelectMany(p => new[] { p.x, p.y, p.z }), Is.EqualTo(snapshot.Positions.ToArray()));
                Assert.That(mesh.normals.SelectMany(p => new[] { p.x, p.y, p.z }), Is.EqualTo(snapshot.Normals.ToArray()));
                Assert.That(mesh.triangles, Is.EqualTo(new[] { 0, 1, 2 }));
                Assert.That(mesh.uv.SelectMany(p => new[] { p.x, p.y }), Is.EqualTo(snapshot.Uvs.ToArray()));
                Assert.That(input.Sites.NumberOfSites, Is.EqualTo(2));
                Assert.That(input.Sites.GetNativePositions(), Is.EqualTo(new[] { new Vector3(-13, 2, 3), new Vector3(4, 5, 6) }));
                Assert.That(input.Sites.GetMask(), Is.EqualTo(new[] { 0, 1 }));
                using var plane = new Plane(new Vector3(13, 2, 3), Vector3.right);
                input.Sites.GetSitesOnPlane(plane, .01f, out int[] onPlane);
                Assert.That(onPlane, Is.EqualTo(new[] { 1, 0 }), "Native plane wrapper reflects X, proving the site uses the same anatomical frame.");
                Assert.That(input.Volume.Dimensions, Is.EqualTo(expectedVolume.Dimensions));
                Assert.That(input.Volume.Spacing, Is.EqualTo(expectedVolume.Spacing));
                Assert.That(input.Volume.Center, Is.EqualTo(expectedVolume.Center));
                foreach (CutOrientation orientation in Enum.GetValues(typeof(CutOrientation)))
                    Assert.That(input.Volume.GetOrientationVector(orientation, false), Is.EqualTo(expectedVolume.GetOrientationVector(orientation, false)));
                foreach (var point in new[] { Vector3.zero, new Vector3(13, -17, 29), new Vector3(-9, 7, 3) })
                    Assert.That(input.Volume.GetValueFromPosition(point), Is.EqualTo(expectedVolume.GetValueFromPosition(point)));
                Assert.That(File.ReadAllBytes(input.VolumePath), Is.EqualTo(source.Projection.VolumeBytes.ToArray()));
                Assert.That(Path.GetDirectoryName(input.VolumePath), Does.StartWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mesh);
                input.Dispose();
            }

            Assert.That(input.CleanupError, Is.Null);
            Assert.That(input.Volume.getHandle().Handle, Is.EqualTo(IntPtr.Zero));
            Assert.That(input.Surface.getHandle().Handle, Is.EqualTo(IntPtr.Zero));
            Assert.That(input.Sites.getHandle().Handle, Is.EqualTo(IntPtr.Zero));
            Assert.That(Directory.GetFileSystemEntries(root), Is.Empty);
            input.Dispose();
        }

        [Test]
        public void LockedVolumeReturnsFalseAndCanBeReloaded()
        {
            Directory.CreateDirectory(root);
            string path = Path.Combine(root, "locked.nii");
            File.Copy(Path.Combine(Application.dataPath, "Data/IRM/MNI.nii"), path);
            try
            {
                using var volume = new Volume();
                using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    Assert.That(volume.LoadNIFTIFile(path), Is.False);
                    Assert.That(volume.IsLoaded, Is.False);
                    Assert.That(volume.SourceFileSha256, Is.Null);
                }

                Assert.That(volume.LoadNIFTIFile(path), Is.True);
                Assert.That(volume.SourceFileSha256, Is.Not.Null);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void FailedCandidateDoesNotReleasePreviousInputsOrFile()
        {
            var snapshot = Snapshot();
            using var previous = NativeProjectionInputs.Create(snapshot, root);
            string blocker = Path.Combine(root, "blocked");
            File.WriteAllText(blocker, "not a directory");
            try
            {
                Assert.Throws<IOException>(() => NativeProjectionInputs.Create(snapshot, blocker));
                Assert.Throws<InvalidDataException>(() => NativeProjectionInputs.Create(null, root));
                Assert.That(previous.Volume.IsLoaded, Is.True);
                Assert.That(previous.Surface.NumberOfVertices, Is.EqualTo(3));
                Assert.That(previous.Sites.NumberOfSites, Is.EqualTo(2));
                Assert.That(File.Exists(previous.VolumePath), Is.True);
                Assert.That(Directory.GetDirectories(root).Length, Is.EqualTo(1));
            }
            finally
            {
                File.Delete(blocker);
            }
        }
    }
}
