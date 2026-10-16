#if UNITY_EDITOR
using System;
using System.Threading.Tasks;
using System.Linq;
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
        [TestCase(true, true), TestCase(true, false), TestCase(false, true), TestCase(false, false)]
        public async Task OverlappingLoads_KeepPresenterUntilLastTransferCompletes(bool quest, bool finishCancelableFirst)
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(quest ? "Assets/Prefabs/Quest/UI/Quest Loading Manager.prefab" : "Assets/Prefabs/LoadingCircle/Loading Manager.prefab"));
            var firstDone = new TaskCompletionSource<int>();
            var secondDone = new TaskCompletionSource<int>();
            try
            {
                var presenter = (MonoBehaviour)root.GetComponentsInChildren<MonoBehaviour>(true).Single(value => value is ILoadingPresenter);
                var cancel = root.GetComponentInChildren<Button>(true);
                Task<int> first = LoadingManager.LoadAsync<int>(async (_, _) => await firstDone.Task).AsTask();
                Task<int> second = LoadingManager.LoadAsync<int>(async _ => await secondDone.Task).AsTask();
                await UniTask.NextFrame();
                Assert.That(presenter.gameObject.activeSelf, Is.True);
                Assert.That(cancel.gameObject.activeInHierarchy, Is.True, "Starting non-cancelable work preserves cancellation for the active transfer.");
                if (finishCancelableFirst) firstDone.SetResult(1);
                else secondDone.SetResult(2);
                if (finishCancelableFirst) Assert.That(await first, Is.EqualTo(1));
                else Assert.That(await second, Is.EqualTo(2));
                await UniTask.NextFrame();
                Assert.That(presenter.gameObject.activeSelf, Is.True, "Finishing one load must not hide another transfer's progress.");
                Assert.That(cancel.gameObject.activeInHierarchy, Is.EqualTo(!finishCancelableFirst));
                if (finishCancelableFirst)
                {
                    secondDone.SetResult(2);
                    Assert.That(await second, Is.EqualTo(2));
                }
                else
                {
                    firstDone.SetResult(1);
                    Assert.That(await first, Is.EqualTo(1));
                }

                await UniTask.NextFrame();
                Assert.That(presenter.gameObject.activeSelf, Is.False);
            }
            finally
            {
                firstDone.TrySetResult(1);
                secondDone.TrySetResult(2);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public async Task QuestLoading_ProgressCancellationAndSharedManagerRemainFunctional()
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Loading Manager.prefab"));
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
        public async Task LoadingOverlay_RendersOverNearerWindowsAndSurvivesThemeRefresh()
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Loading Manager.prefab"));
            var head = new GameObject("Overlay render camera", typeof(Camera));
            var window = new GameObject("Near window", typeof(RectTransform), typeof(Canvas));
            var texture = new RenderTexture(256, 256, 24);
            var pixels = new Texture2D(256, 256, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                var camera = head.GetComponent<Camera>();
                camera.nearClipPlane = .01f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.targetTexture = texture;
                var follower = root.GetComponentInChildren<QuestLoadingFollower>();
                follower.enabled = false;
                follower.SetCamera(camera);
                var presenter = root.GetComponentInChildren<QuestLoadingCircle>(true);
                presenter.Open(true, true);
                follower.Step(0, true);
                var overlay = follower.GetComponent<QuestLoadingOverlay>();
                var overlayMaterial = (Material)new SerializedObject(overlay).FindProperty("material").objectReferenceValue;
                var image = presenter.GetComponentInChildren<Image>();
                foreach (var theme in presenter.GetComponentsInChildren<HBP.Theme.ThemeElement>(true)) theme.Set();
                Canvas.ForceUpdateCanvases();
                foreach (var graphic in presenter.GetComponentsInChildren<Graphic>(true))
                    Assert.That(graphic.material, Is.SameAs(overlayMaterial), "Theme refresh must preserve foreground rendering for texts and images alike.");

                // Isolate one existing loading graphic to make the render assertion unambiguous.
                foreach (var graphic in presenter.GetComponentsInChildren<Graphic>(true)) graphic.enabled = graphic == image;
                image.sprite = null;
                image.type = Image.Type.Simple;
                image.color = Color.green;
                image.rectTransform.position = Vector3.forward * 1.15f;
                image.rectTransform.sizeDelta = Vector2.one * 200;
                var canvas = window.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;
                window.transform.position = Vector3.forward * .5f;
                window.transform.localScale = Vector3.one * .001f;
                var background = new GameObject("Opaque window background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                background.transform.SetParent(window.transform, false);
                background.GetComponent<RectTransform>().sizeDelta = Vector2.one * 1000;
                background.GetComponent<Image>().color = Color.red;
                await UniTask.NextFrame();
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = texture;
                pixels.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                pixels.Apply();
                Color center = pixels.GetPixel(128, 128);
                Assert.That(center.g, Is.GreaterThan(.8f), "The farther loading graphic must be visible above the nearer opaque window.");
                Assert.That(center.r, Is.LessThan(.2f));
                image.enabled = false;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                pixels.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                pixels.Apply();
                Assert.That(pixels.GetPixel(128, 128).r, Is.GreaterThan(.8f), "The test window must actually cover the same pixel.");
            }
            finally
            {
                RenderTexture.active = previous;
                head.GetComponent<Camera>().targetTexture = null;
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(window);
                Object.DestroyImmediate(head);
                Object.DestroyImmediate(pixels);
                Object.DestroyImmediate(texture);
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
