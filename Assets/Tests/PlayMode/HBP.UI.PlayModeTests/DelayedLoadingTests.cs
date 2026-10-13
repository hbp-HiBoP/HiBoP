using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.UI.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using System.Reflection;
using HBP.Core.Tools;

namespace HBP.Tests.PlayMode.UI
{
    public sealed class DelayedLoadingTests
    {
        [Test]
        public void ApplicationStartupRegistersTheSingletonUsedByPairing()
        {
            if (ApplicationManager.IsInitialized) Assert.Ignore("Requires an isolated application manager.");
            var root = new GameObject("Application startup registration test");
            try
            {
                root.AddComponent<ApplicationManager>();
                Assert.That(ApplicationManager.IsInitialized, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [TestCase(false), TestCase(true)]
        public async Task PairingPresentationCanPauseForInputAndDrainsCancellation(bool cancel)
        {
            if (LoadingManager.IsInitialized) Assert.Ignore("Requires an isolated loading manager.");
            var root = new GameObject("Pairing loading presentation test");
            root.SetActive(false);
            var presenter = root.AddComponent<PairingTestPresenter>();
            var manager = root.AddComponent<LoadingManager>();
            typeof(LoadingManager).GetField("m_LoadingCircle", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, presenter);
            root.SetActive(true);
            var release = new UniTaskCompletionSource();
            LoadingManager.PresentationHandle handle = null;
            var task = LoadingManager.PresentAsync(async (_, token) =>
            {
                await release.Task;
                token.ThrowIfCancellationRequested();
            }, CancellationToken.None, value => handle = value).AsTask();
            try
            {
                Assert.That(presenter.Visible, Is.True);
                await handle.PauseAsync();
                Assert.That(presenter.Visible, Is.False);
                handle.Resume();
                Assert.That(presenter.Visible, Is.True);
                if (cancel) presenter.OnCancel.Invoke();
                Assert.That(task.IsCompleted, Is.False);
                Assert.That(presenter.Visible, Is.True, "Cancellation must not hide work which still owns captured resources.");
                release.TrySetResult();
                Exception failure = null;
                try
                {
                    await task;
                }
                catch (Exception error)
                {
                    failure = error;
                }

                Assert.That(failure is OperationCanceledException, Is.EqualTo(cancel));
                Assert.That(presenter.Visible, Is.False);
            }
            finally
            {
                release.TrySetResult();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        [Timeout(30000)]
        public async Task DelayedOperationIsStartedOnceAndDrainedAfterTheDelay(bool cancel)
        {
            using var cancellation = new CancellationTokenSource();
            var release = new UniTaskCompletionSource();
            int starts = 0;
            Task loading = LoadingManager.LoadDelayedAsync(async (_, token) =>
            {
                starts++;
                await release.Task;
                token.ThrowIfCancellationRequested();
            }, cancellation.Token, delayMilliseconds: 0).AsTask();
            bool completedBeforeRelease;
            try
            {
                await UniTask.NextFrame();
                if (cancel) cancellation.Cancel();
                completedBeforeRelease = loading.IsCompleted;
            }
            finally
            {
                release.TrySetResult();
            }

            Exception failure = null;
            try
            {
                await loading;
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            Assert.That(completedBeforeRelease, Is.False, "The loading operation must retain its inputs until the worker finishes.");
            if (cancel) Assert.That(failure, Is.InstanceOf<OperationCanceledException>());
            else Assert.That(failure, Is.Null);
            Assert.That(starts, Is.EqualTo(1));
        }

        [Test, Timeout(30000)]
        public async Task OperationFinishingBeforeTheDelayIsStillAwaitedSuccessfully()
        {
            int starts = 0;
            await LoadingManager.LoadDelayedAsync(async (_, _) =>
            {
                starts++;
                await UniTask.Yield();
            }, CancellationToken.None, delayMilliseconds: 2000);
            Assert.That(starts, Is.EqualTo(1));
        }
    }

    public sealed class PairingTestPresenter : MonoBehaviour, ILoadingPresenter
    {
        public UnityEvent OnCancel { get; } = new();
        public bool Visible { get; private set; }

        public void Initialize()
        {
        }

        public void Open(bool showInformations = true, bool cancelable = false) => Visible = true;

        public void SetOptions(bool showInformations, bool cancelable)
        {
        }

        public void Close() => Visible = false;

        public void ChangePercentage(float progress, float durationInSeconds, LoadingText message)
        {
        }
    }
}
