#if UNITY_EDITOR
using System;
using System.IO;
using HBP.Quest;
using HBP.Transfer.Anatomy;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HBP.Tests.QuestAnatomy
{
    public class AnatomyManipulationTests
    {
        private GameObject root;
        private QuestAnatomyView view;
        private QuestAnatomyManipulator manipulation;
        private Pose left, right;

        [SetUp]
        public void SetUp()
        {
            root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestAnatomy.prefab"));
            view = root.GetComponent<QuestAnatomyView>();
            manipulation = root.GetComponent<QuestAnatomyManipulator>();
            view.ApplySnapshot(Snapshot());
            left = new Pose(new Vector3(-0.1f, 0, 0), Quaternion.identity);
            right = new Pose(new Vector3(0.1f, 0, 0), Quaternion.identity);
            Step(false, false);
        }

        [TearDown]
        public void TearDown()
        {
            view.Clear();
            Object.DestroyImmediate(root);
        }

        private static AnatomySnapshot Snapshot() => AnatomySnapshot.Create("grab", "session", "visualization", "column", 1, new AnatomyCoordinateSpace(AnatomyMeshUploader.FrameId, AnatomyHandedness.Left, AnatomyLengthUnit.Millimeter, 1, new float[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 }), AnatomyWinding.Clockwise, true, new float[] { 1, 1, 1, 1 }, new float[] { -50, -30, 0, 100, 0, 0, 0, 80, 0 }, new float[] { 0, 0, -1, 0, 0, -1, 0, 0, -1 }, new uint[] { 0, 1, 2 }, Array.Empty<float>());

        private void Step(bool l, bool r, bool lt = true, bool rt = true) => manipulation.Step(left, lt, l, right, rt, r);
        private static void Near(Vector3 actual, Vector3 expected) => Assert.That(Vector3.Distance(actual, expected), Is.LessThan(0.00001f));

        [TestCase(true)]
        [TestCase(false)]
        public void SingleGrab_PreservesOffsetAndRotatesWithoutSnapOrReleaseDrift(bool useLeft)
        {
            Pose hand = useLeft ? left : right;
            Step(useLeft, !useLeft);
            Near(root.transform.position, Vector3.zero);
            Vector3 displacement = new Vector3(0.2f, 0.3f, -0.1f);
            Quaternion rotation = Quaternion.Euler(30, 60, 15);
            if (useLeft) left = new Pose(hand.position + displacement, rotation);
            else right = new Pose(hand.position + displacement, rotation);
            Step(useLeft, !useLeft);
            Near(root.transform.position, hand.position + displacement + rotation * -hand.position);
            Assert.That(Quaternion.Angle(root.transform.rotation, rotation), Is.LessThan(0.01f));
            var final = new Pose(root.transform.position, root.transform.rotation);
            Step(false, false);
            for (int i = 0; i < 100; i++)
            {
                left.position += Vector3.one;
                right.position -= Vector3.one;
                Step(false, false);
            }

            Assert.That(root.transform.position, Is.EqualTo(final.position));
            Assert.That(root.transform.rotation, Is.EqualTo(final.rotation));
            Assert.That(manipulation.IsGrabbed, Is.False);
        }

        [Test]
        public void TwoHands_TransformAroundMidpointAndClampUniformPositiveScale()
        {
            Step(true, true);
            left.position = new Vector3(0, -0.2f, 1);
            right.position = new Vector3(0, 0.2f, 1);
            Step(true, true);
            Near(root.transform.position, Vector3.forward);
            Near(root.transform.localScale, Vector3.one * 2);
            Near(root.transform.right, Vector3.up);
            left.position = Vector3.left * 100;
            right.position = Vector3.right * 100;
            Step(true, true);
            Near(root.transform.localScale, Vector3.one * 4);
            // Re-grab with a large baseline so a valid close pair reaches the lower bound.
            Step(true, false);
            Step(true, true);
            left.position = Vector3.left * 0.05f;
            right.position = Vector3.right * 0.05f;
            Step(true, true);
            Near(root.transform.localScale, Vector3.one * 0.25f);
        }

        [Test]
        public void HandTransitions_RebaseWithoutJump_ThenSurvivingHandContinues()
        {
            Step(true, false);
            left.position += Vector3.up;
            Step(true, false);
            Vector3 before = root.transform.position;
            right.position = Vector3.one * 3; // Second controller need not touch the brain again.
            Step(true, true);
            Near(root.transform.position, before);
            Step(false, true);
            Near(root.transform.position, before);
            right.position += Vector3.forward;
            Step(false, true);
            Near(root.transform.position, before + Vector3.forward);
        }

        [Test]
        public void TrackingLoss_FreezesAndRequiresReleaseBeforeRegrab()
        {
            Step(true, false);
            left.position += Vector3.up;
            Step(true, false);
            Vector3 before = root.transform.position;
            left.position += Vector3.one * 10;
            Step(true, false, false);
            Step(true, false);
            Near(root.transform.position, before);
            Assert.That(manipulation.IsGrabbed, Is.False);
            left.position = before;
            Step(false, false);
            Step(true, false);
            Assert.That(manipulation.IsGrabbed, Is.True);
        }

        [Test]
        public void NearGrab_RequiresFreshPressAndUsesRotatedScaledBounds()
        {
            left.position = Vector3.one * 10;
            Step(true, false);
            left.position = Vector3.zero;
            Step(true, false);
            Assert.That(manipulation.IsGrabbed, Is.False, "Moving into range with grip already held must not capture.");
            Step(false, false);
            root.transform.SetPositionAndRotation(new Vector3(2, 1, 3), Quaternion.Euler(20, 60, 90));
            root.transform.localScale = Vector3.one * 3;
            left.position = root.transform.TransformPoint(view.SharedMesh.bounds.center * 0.001f);
            Step(true, false);
            Assert.That(manipulation.IsGrabbed, Is.True);
        }

        [Test]
        public void CoincidentHands_FreezeAndRebaseWhenSeparated()
        {
            Step(true, true);
            left.position = right.position = Vector3.one;
            Step(true, true);
            Near(root.transform.position, Vector3.zero);
            Near(root.transform.localScale, Vector3.one);
            left.position = Vector3.left * 0.1f;
            right.position = Vector3.right * 0.1f;
            Step(true, true);
            Near(root.transform.position, Vector3.zero);
            left.position += Vector3.up;
            right.position += Vector3.up;
            Step(true, true);
            Near(root.transform.position, Vector3.up);
        }

        [Test]
        public void CancelAndClear_ReleaseWithoutMovingOrReacquiringHeldGrips()
        {
            Step(true, false);
            manipulation.CancelGrab();
            left.position += Vector3.up;
            Step(true, false);
            Near(root.transform.position, Vector3.zero);
            Assert.That(manipulation.IsGrabbed, Is.False);
            view.Clear();
            Step(false, false);
            Step(true, true);
            Assert.That(manipulation.IsGrabbed, Is.False);
        }

        [Test]
        public void Recenter_KeepsMeshScaleAndChildren_AndCancelsGrab()
        {
            Step(true, true);
            Mesh mesh = view.SharedMesh;
            var frame = root.GetComponentInChildren<MeshFilter>().transform;
            root.transform.localScale = Vector3.one * 2;
            manipulation.Recenter(new Pose(new Vector3(1, 1.7f, 2), Quaternion.Euler(0, 90, 0)));
            Near(root.transform.TransformPoint(mesh.bounds.center * 0.001f), new Vector3(1.65f, 1.58f, 2));
            Near(root.transform.localScale, Vector3.one * 2);
            Assert.That(frame.parent, Is.EqualTo(root.transform));
            Assert.That(frame.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(frame.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(frame.localScale, Is.EqualTo(Vector3.one * 0.001f));
            Assert.That(view.SharedMesh, Is.SameAs(mesh));
            Assert.That(manipulation.IsGrabbed, Is.False);
            var position = root.transform.position;
            Step(true, true);
            Near(root.transform.position, position);
        }

        [Test]
        public void ActualMniFixture_AllGesturesPreserveEncodedBuffersModelDistancesAndMesh()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-questAnatomyFixture");
            if (index < 0) Assert.Ignore("Pass -questAnatomyFixture for the real Desktop MNI invariant check.");
            byte[] bytes = File.ReadAllBytes(args[index + 1]);
            AnatomySnapshot snapshot = AnatomySnapshotCodec.Decode(bytes);
            view.ApplySnapshot(snapshot);
            Mesh mesh = view.SharedMesh;
            Bounds bounds = mesh.bounds;
            var a = new Vector3(snapshot.Positions[0], snapshot.Positions[1], snapshot.Positions[2]);
            var b = new Vector3(snapshot.Positions[3], snapshot.Positions[4], snapshot.Positions[5]);
            float distance = Vector3.Distance(a, b);
            left.position = bounds.center * 0.001f;
            right.position = left.position + Vector3.right * 0.2f;
            Step(true, false);
            left.position += Vector3.up;
            left.rotation = Quaternion.Euler(20, 30, 40);
            Step(true, false);
            Step(true, true);
            right.position += Vector3.right * 0.5f;
            Step(true, true);
            Step(false, false);
            manipulation.Recenter(new Pose(Vector3.up * 1.7f, Quaternion.identity));
            Assert.That(AnatomySnapshotCodec.Encode(snapshot), Is.EqualTo(bytes), "Every source component remains bit-exact.");
            Assert.That(view.SharedMesh, Is.SameAs(mesh));
            Assert.That(mesh.bounds, Is.EqualTo(bounds));
            Assert.That(view.UploadCount, Is.EqualTo(2), "Gestures must not upload/rebuild meshes.");
            Assert.That(Vector3.Distance(new Vector3(snapshot.Positions[0], snapshot.Positions[1], snapshot.Positions[2]), new Vector3(snapshot.Positions[3], snapshot.Positions[4], snapshot.Positions[5])), Is.EqualTo(distance));
            TestContext.Out.WriteLine($"MNI vertices={mesh.vertexCount}; unchanged pair distance={distance:R} mm; encoded bytes={bytes.Length}; presentation scale={root.transform.localScale.x}");
        }

        [Test]
        public void Prefabs_SerializeManipulationSettingsAndRigReferences()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestBootstrap.prefab");
            var input = new SerializedObject(prefab.GetComponentInChildren<QuestAnatomyInput>(true));
            foreach (string field in new[] { "view", "manipulator", "head", "left", "right" })
                Assert.That(input.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            var settings = new SerializedObject(manipulation);
            Assert.That(settings.FindProperty("view").objectReferenceValue, Is.EqualTo(view));
            Assert.That(settings.FindProperty("minimumScale").floatValue, Is.EqualTo(0.25f));
            Assert.That(settings.FindProperty("maximumScale").floatValue, Is.EqualTo(4f));
            Assert.That(settings.FindProperty("recenterDistanceMeters").floatValue, Is.EqualTo(0.65f));
        }
    }
}
#endif
