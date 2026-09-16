#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using HBP.Tests.PlayMode.Utilities;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HBP.Tests.SceneTransfer
{
    public sealed class DesktopAnatomyPreparationTests
    {
        private sealed class GatedMesh : SingleMesh3D
        {
            internal readonly ManualResetEventSlim Finish = new();
            internal readonly TaskCompletionSource<bool> Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
            internal int LoadThread;
            internal bool Cleaned;

            internal GatedMesh(SingleMesh data) : base(data, MeshType.Patient, false)
            {
            }

            public override void Load()
            {
                LoadThread = Thread.CurrentThread.ManagedThreadId;
                Started.TrySetResult(true);
                // Only the worker blocks, to deterministically exercise close/cancel.
                if (!Finish.Wait(TimeSpan.FromSeconds(15))) throw new TimeoutException();
                base.Load();
            }

            public override void Clean()
            {
                Cleaned = true;
                base.Clean();
            }
        }

        [TestCase("complete")]
        [TestCase("cancel")]
        [TestCase("close")]
        [Timeout(60000)]
        public async Task ColdPreparationKeepsUnityRunningAndPublishesOnlyFinishedResources(string action)
        {
            using var temp = new PlayModeTempDirectoryScope();
            using var settings = new PlayModePersistentDataScope(temp.Path);
            using var scope = new PlayModeSceneScope("DesktopAnatomyPreparation");
            using var cancel = new CancellationTokenSource();
            string meshFile = Path.GetFullPath("Assets/Tests/Fixtures/Native/Patients/synthetic-patient/t1mri/T1pre_synthetic/default_analysis/segmentation/mesh/synthetic-patient_Lhemi.gii");
            var meshData = new SingleMesh("cold mesh", "", meshFile, "");
            var patient = new Patient { Name = "Cold" };
            patient.Meshes.Add(meshData);
            patient.MRIs.Add(new MRI("cold MRI", Path.GetFullPath("Assets/Tests/Fixtures/Native/Nifti/mri_t1.nii")));
            patient.Meshes.Add((BaseMesh)meshData.Clone());
            patient.MRIs.Add((MRI)patient.MRIs[0].Clone());
            Assert.That(meshData.IsUsable && patient.MRIs[0].IsUsable, Is.True);
            var model = new Visualization("Cold preparation", new[] { patient, new Patient { Name = "Other" } }, Array.Empty<Column>());
            var scene = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/3D/Scenes/Scene 3D Content.prefab"), scope.Root.transform).GetComponent<Base3DScene>();
            scene.gameObject.SetActive(false);
            scene.Initialize(model);
            scene.SceneInformation.Initialized = true;
            var gate = new GatedMesh(meshData);
            scene.MeshManager.Meshes.Add(gate);
            var meshes = scene.MeshManager;
            var mris = scene.MRIManager;

            int mainThread = Thread.CurrentThread.ManagedThreadId, captured = 0;
            Task<int> preparation = null, concurrent = null;
            Task close = null;
            try
            {
                preparation = scene.CapturePreparedAsync(() => ++captured, cancel.Token).AsTask();
                await gate.Started.Task;
                int frame = Time.frameCount;
                await UniTask.NextFrame();
                await UniTask.NextFrame();
                Assert.That(Time.frameCount, Is.GreaterThan(frame));
                Assert.That(gate.LoadThread, Is.Not.EqualTo(mainThread));
                Assert.That(preparation.IsCompleted, Is.False);
                Assert.That(meshes.PreloadedMeshes, Is.Empty, "Incomplete batches must stay private.");
                Assert.That(mris.PreloadedMRIs, Is.Empty);
                if (action == "cancel") cancel.Cancel();
                if (action == "close")
                {
                    close = scene.CleanAsync().AsTask();
                    await UniTask.NextFrame();
                    Assert.That(close.IsCompleted, Is.False);
                    Assert.That(gate.Cleaned, Is.False, "Close must await the native worker.");
                }
                else if (action == "complete")
                    concurrent = scene.CapturePreparedAsync(() => ++captured, cancel.Token).AsTask();

                gate.Finish.Set();
                Exception failure = null;
                try
                {
                    await preparation;
                }
                catch (Exception exception)
                {
                    failure = exception;
                }

                Assert.That(Thread.CurrentThread.ManagedThreadId, Is.EqualTo(mainThread));
                if (action == "complete")
                {
                    Assert.That(failure, Is.Null);
                    await concurrent;
                    Assert.That(captured, Is.EqualTo(2));
                    var preparedMesh = meshes.PreloadedMeshes[patient].Single();
                    var preparedMRI = mris.PreloadedMRIs[patient].Single();
                    Assert.That(preparedMesh.IsLoaded && preparedMRI.IsLoaded, Is.True);
                    await scene.CapturePreparedAsync(() => 0, cancel.Token);
                    Assert.That(meshes.PreloadedMeshes[patient].Single(), Is.SameAs(preparedMesh));
                    Assert.That(mris.PreloadedMRIs[patient].Single(), Is.SameAs(preparedMRI));
                }
                else
                {
                    Assert.That(captured, Is.Zero);
                    Assert.That(failure, action == "close" ? Is.InstanceOf<ObjectDisposedException>() : Is.InstanceOf<OperationCanceledException>());
                    if (close != null)
                    {
                        await close;
                        Assert.That(gate.Cleaned, Is.True);
                        Assert.That(meshes.PreloadedMeshes, Is.Empty);
                        Assert.That(mris.PreloadedMRIs, Is.Empty);
                    }
                }
            }
            finally
            {
                gate.Finish.Set();
                foreach (var work in new Task[] { preparation, concurrent, close })
                    if (work != null)
                        try
                        {
                            await work;
                        }
                        catch
                        {
                            /* The assertion above reports the expected failure. */
                        }

                if (scene != null) await scene.CleanAsync();
                gate.Finish.Dispose();
                await UniTask.NextFrame();
            }
        }
    }
}
#endif
