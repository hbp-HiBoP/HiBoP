#if UNITY_EDITOR
using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Tools;
using HBP.Quest;
using HBP.UI.Tools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HBP.Tests.Quest
{
    public class QuestLoadingTests
    {
        [Test]
        public async Task QuestLoading_ProgressCancellationAndSharedManagerRemainFunctional()
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/Quest Loading Manager.prefab"));
            var cameraObject = new GameObject("Loading test camera", typeof(Camera));
            try
            {
                var presenter = root.GetComponentInChildren<QuestLoadingCircle>(true);
                Assert.That(presenter.gameObject.activeSelf, Is.False, "The manager initializes and closes its serialized presenter.");
                var follower = root.GetComponentInChildren<QuestLoadingFollower>(true);
                var camera = cameraObject.GetComponent<Camera>();
                camera.nearClipPlane = .01f;
                follower.SetCamera(camera);
                presenter.Open(true, true);
                var cancel = presenter.GetComponentInChildren<Button>();
                var fields = new SerializedObject(presenter);
                var brain = (Image)fields.FindProperty("brain").objectReferenceValue;
                var box = (RectTransform)fields.FindProperty("informationBox").objectReferenceValue;
                var ring = (Image)fields.FindProperty("progressRing").objectReferenceValue;
                var information = (Text)fields.FindProperty("information").objectReferenceValue;
                Assert.That(brain.sprite, Is.EqualTo(Resources.Load<Sprite>("BrainAnim/0")));
                Assert.That(ring.fillAmount, Is.Zero);
                Assert.That(box.rect.height, Is.GreaterThanOrEqualTo(100));
                presenter.ChangePercentage(.6f, 1, new LoadingText("Preparing ", "anatomy", " and synchronized visualization…"));
                presenter.Render(.5f);
                Assert.That(ring.fillAmount, Is.EqualTo(.3f).Within(.001));
                Assert.That(brain.sprite, Is.EqualTo(Resources.Load<Sprite>("BrainAnim/30")));
                Assert.That(information.text, Does.Contain("anatomy"));
                Assert.That(information.fontSize, Is.GreaterThanOrEqualTo(28));
                presenter.Render(.5f);
                follower.Step(0, true);
                root.GetComponentInChildren<Canvas>().worldCamera = camera;
                Canvas.ForceUpdateCanvases();
                await UniTask.NextFrame();
                QuestWindowInteractionTests.Capture(camera, "Quest Loading.png");
                presenter.Close();

                bool cancelled = false;

                async UniTask<int> Work(Action<float, float, LoadingText> update, System.Threading.CancellationToken token)
                {
                    update(.4f, 0, new LoadingText(message: "Cancellable task"));
                    await UniTask.NextFrame(token);
                    cancel.onClick.Invoke();
                    cancel.onClick.Invoke();
                    await UniTask.NextFrame(token);
                    return 1;
                }

                try
                {
                    await LoadingManager.LoadAsync<int>(Work);
                }
                catch (OperationCanceledException)
                {
                    cancelled = true;
                }

                Assert.That(cancelled, Is.True, "The Quest cancel button cancels the original task pipeline.");
                Assert.That(presenter.gameObject.activeSelf, Is.False);
                int result = await LoadingManager.LoadAsync<int>(update => UniTask.FromResult(7), false);
                Assert.That(result, Is.EqualTo(7));
                presenter.Open(false);
                presenter.ChangePercentage(.8f, 0, new LoadingText(message: "Hidden detail"));
                presenter.Render(0);
                Assert.That(box.gameObject.activeSelf, Is.False);
                Assert.That(cancel.gameObject.activeInHierarchy, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void QuestLoadingFollow_UsesDeadZoneDelayAndFreezesWithoutTracking()
        {
            var root = new GameObject("Loading follower", typeof(QuestLoadingFollower));
            var content = new GameObject("Visible content");
            var head = new GameObject("Head", typeof(Camera));
            try
            {
                var follower = root.GetComponent<QuestLoadingFollower>();
                QuestWindowInteractionTests.Set(follower, "content", content);
                follower.SetCamera(head.GetComponent<Camera>());
                follower.Step(0, true);
                var initial = root.transform.position;
                Assert.That(initial.z, Is.EqualTo(1.15f).Within(.001));
                head.transform.rotation = Quaternion.Euler(0, 15, 0);
                follower.Step(1, true);
                Assert.That(root.transform.position, Is.EqualTo(initial));
                head.transform.rotation = Quaternion.Euler(0, 60, 0);
                follower.Step(.2f, true);
                Assert.That(root.transform.position, Is.EqualTo(initial));
                follower.Step(.25f, true);
                Assert.That(root.transform.position.x, Is.GreaterThan(0));
                Assert.That(root.transform.position.x, Is.LessThan(1.15f * Mathf.Sin(60 * Mathf.Deg2Rad)));
                var returning = root.transform.position;
                head.transform.rotation = Quaternion.Euler(0, -60, 0);
                follower.Step(1, false);
                Assert.That(root.transform.position, Is.EqualTo(returning));
                content.SetActive(false);
                follower.Step(0, true);
                content.SetActive(true);
                follower.Step(0, true);
                Assert.That(Vector3.Angle(root.transform.position - head.transform.position, head.transform.forward), Is.LessThan(.001));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(content);
                Object.DestroyImmediate(head);
            }
        }
    }
}
#endif
