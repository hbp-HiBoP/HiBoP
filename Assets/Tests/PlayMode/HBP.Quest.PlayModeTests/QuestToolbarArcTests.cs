#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Quest;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HBP.Tests.Quest
{
    public class QuestToolbarArcTests
    {
        [Test]
        public async Task ProductionToolbar_UsesEqualMarginsDespiteALegacyHeightOverride()
        {
            using var scope = new ToolbarScope(true);
            await UniTask.NextFrame();
            var rect = scope.Toolbar.GetComponent<RectTransform>();
            float width = rect.rect.width;
            Vector3 placement = scope.Toolbar.GetComponent<QuestWindow>().Placement;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 150);
            scope.Layout.RebuildLayout();
            Canvas.ForceUpdateCanvases();
            var button = (RectTransform)scope.Buttons[0].transform;
            float sideMargin = (rect.rect.width - scope.Buttons.Length * button.rect.width - (scope.Buttons.Length - 1) * 4) * .5f;
            Assert.That(sideMargin, Is.EqualTo(4).Within(.01));
            var surface = scope.Content.GetComponent<QuestToolbarBackground>();
            var face = button.GetComponent<QuestToolbarBackground>();
            float x = rect.rect.xMin + sideMargin + button.rect.width * .5f;
            float scale = rect.lossyScale.x;
            float topMargin = Vector3.Distance(surface.transform.TransformPoint(surface.SurfacePoint(x, rect.rect.yMax)), button.TransformPoint(face.SurfacePoint(0, button.rect.yMax))) / scale;
            float bottomMargin = Vector3.Distance(surface.transform.TransformPoint(surface.SurfacePoint(x, rect.rect.yMin)), button.TransformPoint(face.SurfacePoint(0, button.rect.yMin))) / scale;
            Assert.That(topMargin, Is.EqualTo(sideMargin).Within(.01));
            Assert.That(bottomMargin, Is.EqualTo(sideMargin).Within(.01));
            Assert.That(rect.rect.width, Is.EqualTo(width));
            Assert.That(scope.Toolbar.GetComponent<QuestWindow>().Placement, Is.EqualTo(placement));
            QuestWindowInteractionTests.Capture(scope.LookAtToolbar(), "Quest Toolbar Equal Margins Production.png");
        }

        [Test]
        public async Task Arc_GrowsAndShrinksWithActiveButtonsWithoutResizingIconsOrChangingTheRail()
        {
            using var scope = new ToolbarScope();
            await UniTask.NextFrame();
            scope.Layout.RebuildLayout();
            Vector3 initialPosition = scope.Toolbar.transform.position;
            float initialWidth = scope.Toolbar.GetComponent<RectTransform>().rect.width;
            var first = scope.Buttons[0];
            Vector2 size = ((RectTransform)first.transform).rect.size;
            Assert.That(initialWidth, Is.EqualTo(244).Within(.01));
            Assert.That(scope.Toolbar.GetComponent<RectTransform>().rect.height, Is.EqualTo(size.y + 8).Within(.01), "Only a thin margin should surround the button row.");
            Assert.That(size, Is.EqualTo(new Vector2(76, 72)));
            Assert.That(scope.Toolbar.GetComponent<QuestWindow>().Placement, Is.EqualTo(new Vector3(0, -.8f, .65f)));
            Assert.That(scope.Buttons[0].transform.position.y, Is.EqualTo(scope.Buttons.Last().transform.position.y).Within(.0001), "The curvature follows the horizontal rail, even on the inclined panel.");
            var surface = scope.Content.GetComponent<QuestToolbarBackground>();
            Assert.That(scope.Buttons[0].transform.position.z, Is.LessThan(surface.transform.TransformPoint(surface.SurfacePoint(0, 0)).z));
            QuestWindowInteractionTests.Capture(scope.LookAtToolbar(), "Quest Toolbar Curved 3.png");
            for (int i = 0; i < 8; i++)
            {
                var extra = Object.Instantiate(first.gameObject, scope.Content, false);
                extra.name = "Extra control " + i;
            }

            scope.Layout.RebuildLayout();
            Canvas.ForceUpdateCanvases();
            float expanded = scope.Toolbar.GetComponent<RectTransform>().rect.width;
            Assert.That(expanded, Is.GreaterThan(initialWidth * 2));
            foreach (var button in scope.Buttons)
            {
                Assert.That(((RectTransform)button.transform).rect.size, Is.EqualTo(size));
                Assert.That(button.transform.right.y, Is.EqualTo(0).Within(.0001));
            }

            Assert.That(Vector3.Angle(scope.Buttons[0].transform.right, scope.Buttons.Last().transform.right), Is.LessThanOrEqualTo(60), "Adding controls must not create an excessively wrapped panel.");
            Assert.That(scope.Toolbar.transform.position, Is.EqualTo(initialPosition));
            QuestWindowInteractionTests.Capture(scope.LookAtToolbar(), "Quest Toolbar Curved 11.png");
            first.gameObject.SetActive(false);
            scope.Layout.RebuildLayout();
            Assert.That(scope.Toolbar.GetComponent<RectTransform>().rect.width, Is.EqualTo(expanded - 80).Within(.01));
            foreach (var button in scope.Buttons) button.gameObject.SetActive(false);
            scope.Layout.RebuildLayout();
            Assert.That(scope.Toolbar.GetComponent<RectTransform>().rect.width, Is.GreaterThan(0));
            first.gameObject.SetActive(true);
            scope.Layout.RebuildLayout();
            Assert.That(first.transform.localPosition.x, Is.EqualTo(0).Within(.001));
        }

        [Test]
        public async Task Arc_TrackedRaysSelectEveryButtonAndTheCurvedGapsAndRespectCanvasGroups()
        {
            using var scope = new ToolbarScope();
            await UniTask.NextFrame();
            scope.Layout.RebuildLayout();
            Canvas.ForceUpdateCanvases();
            var pick = typeof(QuestPointerInput).GetMethod("IsUiHitAbove", BindingFlags.Static | BindingFlags.NonPublic);

            RaycastResult Select(Vector3 point)
            {
                var data = new QuestPointerEventData(scope.Events)
                {
                    trackedDevicePosition = scope.Camera.transform.position,
                    trackedDeviceOrientation = Quaternion.LookRotation(point - scope.Camera.transform.position)
                };
                var hits = new List<RaycastResult>();
                scope.Raycaster.Raycast(data, hits);
                RaycastResult selected = default;
                foreach (var hit in hits)
                    if (hit.gameObject.GetComponent<Graphic>().raycastTarget && (bool)pick.Invoke(null, new object[] { hit, selected }))
                        selected = hit;
                return selected;
            }

            int clicks = 0;
            foreach (var button in scope.Buttons)
            {
                button.interactable = true;
                button.onClick.AddListener(() => clicks++);
                var rect = (RectTransform)button.transform;
                Vector3 center = rect.TransformPoint(rect.rect.center);
                var hit = Select(center);
                Assert.That(hit.isValid, Is.True, button.name);
                Assert.That(hit.gameObject.GetComponentInParent<Button>(), Is.SameAs(button), button.name);
                Assert.That(Vector3.Distance(hit.worldPosition, center), Is.LessThan(.002));
                ExecuteEvents.ExecuteHierarchy(hit.gameObject, new PointerEventData(scope.Events), ExecuteEvents.pointerClickHandler);
                var face = button.GetComponent<QuestToolbarBackground>();
                Assert.That(button.targetGraphic, Is.SameAs(face));
                foreach (float x in new[] { -33f, 33f })
                {
                    Vector3 edge = rect.TransformPoint(face.SurfacePoint(x, 30));
                    var edgeHit = Select(edge);
                    Assert.That(edgeHit.gameObject, Is.SameAs(button.gameObject), "The curved button corners must remain clickable.");
                    Assert.That(Vector3.Distance(edgeHit.worldPosition, edge), Is.LessThan(.001));
                }
            }

            Assert.That(clicks, Is.EqualTo(3));
            var background = scope.Content.GetComponent<QuestToolbarBackground>();
            float gap = (.5f - (scope.Buttons.Length - 1) * .5f) * 80;
            foreach (float x in new[] { gap, scope.Toolbar.GetComponent<RectTransform>().rect.width * .5f - 2 })
            {
                Vector3 point = background.transform.TransformPoint(background.SurfacePoint(x, 0));
                var hit = Select(point);
                Assert.That(hit.gameObject, Is.EqualTo(background.gameObject), "Gaps and the outer border must keep the toolbar hovered.");
                Assert.That(Vector3.Distance(hit.worldPosition, point), Is.LessThan(.002));
            }

            Vector3 outside = background.transform.TransformPoint(background.SurfacePoint(scope.Toolbar.GetComponent<RectTransform>().rect.width, 0));
            Assert.That(Select(outside).isValid, Is.False, "The original flat rectangle must not create phantom hits.");
            scope.Toolbar.AddComponent<CanvasGroup>().blocksRaycasts = false;
            Assert.That(Select(scope.Buttons[0].transform.position).isValid, Is.False);
        }

        [Test]
        public async Task Arc_ThemeRefreshPreservesTheSlicedBackgroundAndCurvedMesh()
        {
            using var scope = new ToolbarScope();
            await UniTask.NextFrame();
            scope.Layout.RebuildLayout();
            var background = scope.Content.GetComponent<QuestToolbarBackground>();
            scope.Content.GetComponent<HBP.Theme.ThemeElement>().Set();
            Canvas.ForceUpdateCanvases();
            Assert.That(background.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(background.sprite, Is.Not.Null);
            var mesh = background.canvasRenderer.GetMesh();
            Assert.That(mesh, Is.Not.Null);
            Assert.That(mesh.vertexCount, Is.GreaterThan(40));
            Assert.That(mesh.bounds.size.z, Is.GreaterThan(2), "The themed background must actually be curved in 3D.");
            foreach (var button in scope.Buttons)
            {
                var face = button.GetComponent<QuestToolbarBackground>();
                button.GetComponent<HBP.Theme.ThemeElement>().Set();
                Canvas.ForceUpdateCanvases();
                Assert.That(face.sprite, Is.Not.Null);
                Assert.That(face.type, Is.EqualTo(Image.Type.Sliced));
                Assert.That(face.canvasRenderer.GetMesh().bounds.size.z, Is.GreaterThan(.1f), "Each button must have its own curved surface.");
                float x = (Array.IndexOf(scope.Buttons, button) - (scope.Buttons.Length - 1) * .5f) * 80;
                Vector3 buttonEdge = face.transform.TransformPoint(face.SurfacePoint(33, 30));
                Vector3 sharedEdge = background.transform.TransformPoint(background.SurfacePoint(x + 33, 30));
                Assert.That(Vector3.Distance(buttonEdge, sharedEdge), Is.LessThan(.0001), "The button curvature must continue the background arc.");
            }
        }

        [Test]
        public async Task CompactToolbar_TooltipWaitsForHoverSupportsBothHandsAndDoesNotInterceptRays()
        {
            using var scope = new ToolbarScope();
            await UniTask.NextFrame();
            var button = scope.Buttons[0];
            var tooltip = button.transform.Find("Tooltip").gameObject;
            var left = new PointerEventData(scope.Events) { pointerId = 0 };
            var right = new PointerEventData(scope.Events) { pointerId = 1 };
            Assert.That(tooltip.activeSelf, Is.False);
            ExecuteEvents.Execute(button.gameObject, left, ExecuteEvents.pointerEnterHandler);
            await UniTask.NextFrame();
            Assert.That(tooltip.activeSelf, Is.False, "A passing pointer should not immediately flash a tooltip.");
            await UniTask.Delay(TimeSpan.FromSeconds(.25), ignoreTimeScale: true);
            Assert.That(tooltip.activeSelf, Is.True);
            Assert.That(tooltip.GetComponentInChildren<Text>().text, Is.EqualTo("Connection"));
            Assert.That(tooltip.transform.localPosition.y, Is.GreaterThan(((RectTransform)button.transform).rect.yMax));
            Assert.That(tooltip.GetComponentsInChildren<Graphic>().All(graphic => !graphic.raycastTarget), Is.True);
            var tooltipCanvas = tooltip.GetComponent<Canvas>();
            Assert.That(tooltipCanvas.overrideSorting, Is.True);
            Assert.That(tooltipCanvas.sortingOrder, Is.GreaterThan(scope.Toolbar.GetComponent<Canvas>().sortingOrder), "The tooltip must render above the toolbar.");
            Canvas.ForceUpdateCanvases();
            var hits = new List<RaycastResult>();
            scope.Raycaster.Raycast(new QuestPointerEventData(scope.Events)
            {
                trackedDevicePosition = scope.Camera.transform.position,
                trackedDeviceOrientation = Quaternion.LookRotation(tooltip.transform.position - scope.Camera.transform.position)
            }, hits);
            Assert.That(hits.Any(hit => hit.gameObject.GetComponent<Graphic>().raycastTarget), Is.False, "The tooltip must never take ownership of the ray.");
            QuestWindowInteractionTests.Capture(scope.LookAtToolbar(), "Quest Toolbar Compact Tooltip.png");
            ExecuteEvents.Execute(button.gameObject, right, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(button.gameObject, left, ExecuteEvents.pointerExitHandler);
            Assert.That(tooltip.activeSelf, Is.True, "The other hand is still hovering.");
            Assert.That(button.targetGraphic.canvasRenderer.GetColor().a, Is.GreaterThan(0));
            ExecuteEvents.Execute(button.gameObject, right, ExecuteEvents.pointerExitHandler);
            Assert.That(tooltip.activeSelf, Is.False);
            ExecuteEvents.Execute(button.gameObject, left, ExecuteEvents.pointerEnterHandler);
            button.gameObject.SetActive(false);
            Assert.That(tooltip.activeSelf, Is.False);
            button.gameObject.SetActive(true);
            await UniTask.Delay(TimeSpan.FromSeconds(.25), ignoreTimeScale: true);
            Assert.That(tooltip.activeSelf, Is.False, "Reopening must not retain a stale hover.");
        }

        [Test]
        public async Task CompactToolbar_StatusFitsItsTextBelowTheBarAndAllTooltipsClearTheUpperEdge()
        {
            using var scope = new ToolbarScope();
            await UniTask.NextFrame();
            scope.Layout.RebuildLayout();
            Canvas.ForceUpdateCanvases();
            var bar = (RectTransform)scope.Content;
            var status = (RectTransform)scope.Toolbar.transform.Find("Status");
            Assert.That(status.parent, Is.SameAs(scope.Toolbar.transform), "The status must be separate from the button background.");
            var corners = new Vector3[4];
            status.GetWorldCorners(corners);
            Assert.That(corners.Max(corner => bar.InverseTransformPoint(corner).y), Is.LessThan(bar.rect.yMin - 9));
            Assert.That(status.GetComponentsInChildren<Graphic>().All(graphic => !graphic.raycastTarget), Is.True);
            var label = status.GetComponentInChildren<Text>();
            var toolbarFields = new SerializedObject(scope.Toolbar.GetComponent<QuestToolbar>());
            Assert.That(toolbarFields.FindProperty("connectionStatus").objectReferenceValue, Is.SameAs(label));
            float shortWidth = status.rect.width;
            label.text = "Receiving...";
            LayoutRebuilder.ForceRebuildLayoutImmediate(status);
            Assert.That(status.rect.width, Is.GreaterThan(shortWidth));
            Assert.That(status.rect.width, Is.EqualTo(label.preferredWidth + 24).Within(.1));
            label.text = "Pairing";
            LayoutRebuilder.ForceRebuildLayoutImmediate(status);
            var camera = scope.LookAtToolbar();
            float barTop = scope.Content.GetComponent<QuestToolbarBackground>().canvasRenderer.GetMesh().vertices.Max(vertex => camera.WorldToScreenPoint(bar.TransformPoint(vertex)).y);
            foreach (var button in scope.Buttons)
            {
                var tip = (RectTransform)button.transform.Find("Tooltip");
                tip.GetWorldCorners(corners);
                Assert.That(corners.Min(corner => camera.WorldToScreenPoint(corner).y), Is.GreaterThan(barTop), "Each tooltip must sit above the visible toolbar.");
                QuestWindowInteractionTests.Set(button.GetComponent<QuestToolbarButton>(), "tooltipDelay", 0f);
                ExecuteEvents.Execute(button.gameObject, new PointerEventData(scope.Events) { pointerId = 0 }, ExecuteEvents.pointerEnterHandler);
                await UniTask.NextFrame();
                QuestWindowInteractionTests.Capture(camera, "Quest Toolbar Tight " + button.name + ".png");
                ExecuteEvents.Execute(button.gameObject, new PointerEventData(scope.Events) { pointerId = 0 }, ExecuteEvents.pointerExitHandler);
            }
        }

        [Test]
        public async Task CompactToolbar_IconsDimWhenDisabledAndInvisibleButtonsHighlightOnHover()
        {
            using var scope = new ToolbarScope();
            await UniTask.NextFrame();
            var button = scope.Buttons[0];
            var icon = button.transform.Find("Icon").GetComponent<Image>();
            Color enabled = icon.color;
            Assert.That(scope.Buttons.First(b => b.name == "Cuts").transform.Find("Icon").GetComponent<Image>().color.grayscale, Is.LessThan(enabled.grayscale * .5f), "The initially unavailable control must already have a dim icon.");
            Assert.That(button.colors.normalColor.a, Is.EqualTo(0));
            Assert.That(button.colors.highlightedColor.a, Is.GreaterThan(0));
            Assert.That(button.GetComponent<HBP.Theme.SelectableStateSetter>(), Is.Not.Null);
            button.interactable = false;
            await UniTask.NextFrame();
            await UniTask.NextFrame();
            Assert.That(icon.color.grayscale, Is.LessThan(enabled.grayscale * .5f));
            var data = new PointerEventData(scope.Events) { pointerId = 0 };
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerEnterHandler);
            await UniTask.Delay(TimeSpan.FromSeconds(.25), ignoreTimeScale: true);
            Assert.That(button.transform.Find("Tooltip").gameObject.activeSelf, Is.True, "Unavailable controls still explain their icon.");
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerExitHandler);
            button.interactable = true;
            await UniTask.NextFrame();
            await UniTask.NextFrame();
            Assert.That(icon.color, Is.EqualTo(enabled));
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerEnterHandler);
            Assert.That(button.targetGraphic.canvasRenderer.GetColor().a, Is.GreaterThan(0));
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerExitHandler);
            Assert.That(button.targetGraphic.canvasRenderer.GetColor().a, Is.EqualTo(0), "A click must not leave keyboard selection looking like a stale hover.");
        }

        [Test]
        public async Task CompactToolbar_ActivatedThemeTracksWindowOpenCloseAndToolbarToggle()
        {
            using var scope = new ToolbarScope();
            var windowObject = new GameObject("Display window", typeof(RectTransform), typeof(Canvas), typeof(QuestWindow));
            windowObject.transform.SetParent(scope.Root.transform, false);
            var window = windowObject.GetComponent<QuestWindow>();
            QuestWindowInteractionTests.Set(window, "key", "Display");
            scope.Root.GetComponent<QuestWindowsManager>().Register(window);
            var button = scope.Buttons.First(b => b.name == "Display");
            var theme = button.GetComponent<HBP.Theme.ThemeElement>();
            await UniTask.NextFrame();
            Assert.That(theme.ActualState.name, Is.EqualTo("Activated"));
            Assert.That(button.colors.normalColor.a, Is.EqualTo(1));
            button.interactable = false;
            await UniTask.NextFrame();
            Assert.That(button.colors.disabledColor.a, Is.EqualTo(1), "An unavailable action still indicates that its window is open.");
            button.interactable = true;
            QuestWindowInteractionTests.Capture(scope.LookAtToolbar(), "Quest Toolbar Compact Activated.png");
            button.onClick.Invoke();
            Assert.That(window.IsOpen, Is.False);
            await UniTask.NextFrame();
            Assert.That(theme.ActualState, Is.SameAs(theme.Element.DefaultState));
            button.onClick.Invoke();
            Assert.That(window.IsOpen, Is.True);
            await UniTask.NextFrame();
            Assert.That(theme.ActualState.name, Is.EqualTo("Activated"));
            window.Close();
            await UniTask.NextFrame();
            Assert.That(theme.ActualState, Is.SameAs(theme.Element.DefaultState), "Closing from the window must update the toolbar.");
        }

        private sealed class ToolbarScope : IDisposable
        {
            public GameObject Root { get; }
            public GameObject Toolbar { get; }
            public Camera Camera { get; }
            public EventSystem Events { get; }
            public Transform Content => Toolbar.transform.Find("Background");
            public QuestToolbarArcLayout Layout => Toolbar.GetComponent<QuestToolbarArcLayout>();
            public QuestToolbarRaycaster Raycaster => Toolbar.GetComponent<QuestToolbarRaycaster>();
            public Button[] Buttons => Content.GetComponentsInChildren<Button>().Where(button => button.gameObject.activeSelf).ToArray();

            public ToolbarScope(bool useProductionInstance = false)
            {
                Root = new GameObject("Curved toolbar fixture");
                Root.SetActive(false);
                var head = new GameObject("Head");
                head.transform.SetParent(Root.transform, false);
                head.transform.position = Vector3.up * 1.6f;
                Camera = head.AddComponent<Camera>();
                Camera.nearClipPlane = .01f;
                Camera.fieldOfView = 65;
                Events = Root.AddComponent<EventSystem>();
                var manager = Root.AddComponent<QuestWindowsManager>();
                var source = useProductionInstance ? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestBootstrap.prefab").GetComponentInChildren<QuestToolbar>(true).gameObject : AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Toolbar.prefab");
                Toolbar = Object.Instantiate(source, Root.transform);
                QuestWindowInteractionTests.Set(Toolbar.GetComponent<QuestToolbar>(), "windows", manager);
                QuestWindowInteractionTests.Set(Toolbar.GetComponent<QuestToolbar>(), "connection", null);
                QuestWindowInteractionTests.Set(Toolbar.GetComponent<QuestToolbar>(), "cuts", null);
                QuestWindowInteractionTests.Set(manager, "head", head.transform);
                QuestWindowInteractionTests.Set(manager, "uiCamera", Camera);
                QuestWindowInteractionTests.Set(manager, "initialWindows", new[] { Toolbar.GetComponent<QuestWindow>() });
                Root.SetActive(true);
                Toolbar.GetComponent<QuestWindowFollower>().enabled = false;
            }

            public Camera LookAtToolbar()
            {
                Camera.transform.rotation = Quaternion.LookRotation(Toolbar.transform.position - Camera.transform.position, Vector3.up);
                Canvas.ForceUpdateCanvases();
                return Camera;
            }

            public void Dispose() => Object.DestroyImmediate(Root);
        }
    }
}
#endif
