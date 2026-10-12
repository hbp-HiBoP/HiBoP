using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Quest;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Transfer
{
    public sealed class PairingLifetimeTests
    {
        [Test]
        public async Task ClearingDrainsEveryRetiredSceneBeforeReportingAFailure()
        {
            var root = new GameObject("pairing-retired-failure-test");
            root.SetActive(false);
            var retired = new UniTaskCompletionSource();
            var expected = new System.InvalidOperationException("Retired scene cleanup failed");
            Task clear = null;
            try
            {
                var view = root.AddComponent<QuestAnatomyView>();
                var field = typeof(QuestAnatomyView).GetField("releases", BindingFlags.Instance | BindingFlags.NonPublic);
                var releases = (List<UniTask>)field.GetValue(view);
                releases.Add(UniTask.FromException(expected).ToAsyncLazy().Task);
                releases.Add(retired.Task);
                clear = view.ClearAsync().AsTask();
                bool completedBeforeRelease = clear.IsCompleted;
                retired.TrySetResult();
                System.Exception failure = null;
                try
                {
                    await clear;
                }
                catch (System.Exception exception)
                {
                    failure = exception;
                }

                Assert.That(completedBeforeRelease, Is.False, "Globals must outlive every retired scene, including when another release failed.");
                Assert.That(failure, Is.SameAs(expected));
            }
            finally
            {
                retired.TrySetResult();
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public async Task ClearingBeforeGlobalReplacementWaitsForPreparationAndRetiredScenes()
        {
            var root = new GameObject("pairing-lifetime-test");
            root.SetActive(false); // No services initialization or native scene is needed.
            using var cancellation = new CancellationTokenSource();
            var preparing = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var retired = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Task clear = null;
            try
            {
                var view = root.AddComponent<QuestAnatomyView>();
                FieldInfo Field(string name) => typeof(QuestAnatomyView).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
                Field("preparation").SetValue(view, cancellation);
                Field("preparationCompletion").SetValue(view, preparing.Task);
                ((List<UniTask>)Field("releases").GetValue(view)).Add(retired.Task.AsUniTask());
                clear = view.ClearAsync().AsTask();
                Assert.That(cancellation.IsCancellationRequested, Is.True);
                Assert.That(clear.IsCompleted, Is.False);
                preparing.SetResult(true);
                Assert.That(clear.IsCompleted, Is.False);
                retired.SetResult(true);
                await clear;
                Assert.That(((List<UniTask>)Field("releases").GetValue(view)), Is.Empty);
            }
            finally
            {
                preparing.TrySetResult(true);
                retired.TrySetResult(true);
                if (clear != null) await clear;
                Object.DestroyImmediate(root);
            }
        }
    }
}
