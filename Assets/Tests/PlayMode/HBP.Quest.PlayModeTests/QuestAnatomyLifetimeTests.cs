#if UNITY_EDITOR
using System;
using System.Collections;
using System.Threading.Tasks;
using HBP.Quest;
using HBP.Transfer.Anatomy;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace HBP.Tests.Quest
{
    public class QuestAnatomyLifetimeTests
    {
        private static AnatomySnapshot Snapshot(string id, bool visible = true, float opacity = 1)
        {
            return AnatomySnapshot.Create(id, "session", "visualization", "column", 1, new AnatomyCoordinateSpace(AnatomyMeshUploader.FrameId, AnatomyHandedness.Left, AnatomyLengthUnit.Millimeter, 1, new float[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 }), AnatomyWinding.Clockwise, visible, new[] { 0.2f, 0.4f, 0.7f, opacity }, new float[] { 0, 0, 0, 100, 0, 0, 0, 100, 0 }, new float[] { 0, 0, -1, 0, 0, -1, 0, 0, -1 }, new uint[] { 0, 1, 2 }, Array.Empty<float>());
        }

        [UnityTest]
        public IEnumerator ReplacementClearAndDestroy_ReleaseMeshesWithoutFrameRebuilds()
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestAnatomy.prefab"));
            var view = root.GetComponent<QuestAnatomyView>();
            var renderer = root.GetComponentInChildren<MeshRenderer>();
            Material material = renderer.sharedMaterial;
            try
            {
                root.transform.position = new Vector3(1, 2, 3);
                root.transform.rotation = Quaternion.Euler(10, 20, 30);
                root.transform.localScale = Vector3.one * 2;
                for (int cycle = 0; cycle < 3; cycle++)
                {
                    view.ApplySnapshot(Snapshot("first"));
                    Mesh first = view.SharedMesh;
                    Assert.That(first.isReadable, Is.False);
                    var block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(block);
                    Assert.That(block.GetVector("_BaseColor"), Is.EqualTo(new Vector4(0.2f, 0.4f, 0.7f, 1)));
                    int count = view.UploadCount;
                    yield return null;
                    yield return null;
                    Assert.That(view.SharedMesh, Is.SameAs(first));
                    Assert.That(view.UploadCount, Is.EqualTo(count));
                    Assert.Throws<ArgumentException>(() => view.ApplySnapshot(Snapshot("bad", opacity: 0.5f)));
                    Assert.That(view.SharedMesh, Is.SameAs(first));
                    view.ApplySnapshot(Snapshot("second", false));
                    Mesh second = view.SharedMesh;
                    Assert.That(renderer.enabled, Is.False);
                    Assert.That(root.transform.position, Is.EqualTo(new Vector3(1, 2, 3)));
                    Assert.That(root.transform.localScale, Is.EqualTo(Vector3.one * 2));
                    Assert.That(view.BufferBytes, Is.EqualTo(78));
                    yield return null;
                    Assert.That(first == null, Is.True, "Replaced mesh must be destroyed after the frame.");
                    view.Clear();
                    view.Clear();
                    Assert.That(view.BufferBytes, Is.Zero);
                    Assert.That(view.TransferId, Is.Null);
                    Assert.That(renderer.GetComponent<MeshFilter>().sharedMesh, Is.Null);
                    yield return null;
                    Assert.That(second == null, Is.True);
                }

                view.ApplySnapshot(Snapshot("last"));
                Mesh last = view.SharedMesh;
                Object.Destroy(root);
                yield return null;
                yield return null;
                Assert.That(last == null, Is.True);
                Assert.That(material != null, Is.True, "Serialized material is shared, not owned by the view.");
            }
            finally
            {
                if (root != null) Object.DestroyImmediate(root);
            }
        }

        [Test]
        public async Task WorkerDelivery_IsRejectedWithoutChangingTheView()
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestAnatomy.prefab"));
            var view = root.GetComponent<QuestAnatomyView>();
            var snapshot = Snapshot("worker");
            try
            {
                Exception caught = null;
                await Task.Run(() =>
                {
                    try
                    {
                        view.ApplySnapshot(snapshot);
                    }
                    catch (Exception exception)
                    {
                        caught = exception;
                    }
                });
                Assert.That(caught, Is.TypeOf<InvalidOperationException>());
                Assert.That(view.SharedMesh, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
#endif
