using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HBP.Quest;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Transfer
{
    public sealed class PairingLifetimeTests
    {
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
                ((List<Task>)Field("releases").GetValue(view)).Add(retired.Task);
                clear = view.ClearAsync();
                Assert.That(cancellation.IsCancellationRequested, Is.True);
                Assert.That(clear.IsCompleted, Is.False);
                preparing.SetResult(true);
                Assert.That(clear.IsCompleted, Is.False);
                retired.SetResult(true);
                await clear;
                Assert.That(((List<Task>)Field("releases").GetValue(view)), Is.Empty);
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
