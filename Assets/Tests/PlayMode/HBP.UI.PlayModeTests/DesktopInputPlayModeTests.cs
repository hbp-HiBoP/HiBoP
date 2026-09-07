#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Data.Module3D;
using HBP.Input;
using HBP.UI.Module3D;
using HBP.UI.Tools.ResizableGrids;
using HBP.Tests.PlayMode.Utilities;
using HBP.UI.Tools;
using HBP.UI.Tools.Lists;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HBP.Tests.PlayMode.UI
{
    public class DesktopInputPlayModeTests
    {
        private PlayModeSceneScope m_Scene;
        private PlayModeWindowHarness m_Window;
        private GameObject m_Manager;
        private InputSystemUIInputModule m_Module;
        private Keyboard m_Keyboard;
        private Mouse m_Mouse;
        private Keyboard m_PreviousKeyboard;
        private Mouse m_PreviousMouse;
        private InputSettings.UpdateMode m_UpdateMode;
        private InputSettings.BackgroundBehavior m_BackgroundBehavior;
        private InputSettings.EditorInputBehaviorInPlayMode m_EditorBehavior;
        private EventSystem m_PreviousEventSystem;
        private ShortcutManager m_Shortcuts;
        private object m_PreviousInputManager;

        [SetUp]
        public async Task SetUp()
        {
            m_PreviousEventSystem = EventSystem.current;
            m_PreviousInputManager = typeof(HBP.Core.Tools.Singleton<HBP.Core.Tools.InputManager>).GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            m_PreviousKeyboard = Keyboard.current;
            m_PreviousMouse = Mouse.current;
            m_UpdateMode = InputSystem.settings.updateMode;
            m_BackgroundBehavior = InputSystem.settings.backgroundBehavior;
            // Batchmode has no focused Game view. Exercise device reset explicitly below.
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            m_EditorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            // UI Submit/Cancel use WasPerformedThisDynamicUpdate in Input System 1.20.
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            m_Keyboard = InputSystem.AddDevice<Keyboard>();
            m_Mouse = InputSystem.AddDevice<Mouse>();
            m_Keyboard.MakeCurrent();
            m_Mouse.MakeCurrent();
            m_Scene = new PlayModeSceneScope("Desktop Input");
            m_Window = new PlayModeWindowHarness(m_Scene.Scene, "Input Canvas");
            Object.DestroyImmediate(m_Window.EventSystem.gameObject);
            m_Manager = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Managers/Input Manager.prefab"));
            var shortcutsObject = new GameObject("Controlled shortcuts");
            shortcutsObject.transform.SetParent(m_Scene.Root.transform);
            shortcutsObject.SetActive(false);
            m_Shortcuts = shortcutsObject.AddComponent<ShortcutManager>();
            m_Module = m_Manager.GetComponent<InputSystemUIInputModule>();
            // Bind only injected devices; don't allow physical input to affect assertions.
            m_Module.actionsAsset = Object.Instantiate(m_Module.actionsAsset);
            m_Module.actionsAsset.devices = new InputDevice[] { m_Keyboard, m_Mouse };
            await UniTask.Yield();
            EventSystem.current = m_Manager.GetComponent<EventSystem>();
            m_Module.ActivateModule();
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Manager != null)
            {
                var actions = m_Module != null ? m_Module.actionsAsset : null;
                Object.DestroyImmediate(m_Manager);
                Object.DestroyImmediate(actions);
            }

            m_Scene?.Dispose();
            if (m_Keyboard != null) InputSystem.RemoveDevice(m_Keyboard);
            if (m_Mouse != null) InputSystem.RemoveDevice(m_Mouse);
            m_PreviousKeyboard?.MakeCurrent();
            m_PreviousMouse?.MakeCurrent();
            InputSystem.settings.updateMode = m_UpdateMode;
            InputSystem.settings.backgroundBehavior = m_BackgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = m_EditorBehavior;
            if (m_PreviousEventSystem != null) EventSystem.current = m_PreviousEventSystem;
            typeof(HBP.Core.Tools.Singleton<HBP.Core.Tools.InputManager>).GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, m_PreviousInputManager);
        }

        [Test]
        public async Task DesktopInput_DeviceRemovalAndReplacement_DoesNotKeepStaleState()
        {
            await UniTask.Yield();
            Keys(Key.LeftCtrl, Key.S);
            InputSystem.QueueStateEvent(m_Mouse, new MouseState { position = new Vector2(120, 80) }.WithButton(MouseButton.Left));
            InputSystem.Update();
            Assert.That(DesktopInput.IsControlPressed, Is.True);
            Assert.That(DesktopInput.IsPressed(Key.S), Is.True);
            Assert.That(DesktopInput.IsLeftMouseButtonPressed, Is.True);

            // Remove only the injected devices; leave physical hardware untouched.
            m_Keyboard.MakeCurrent();
            m_Mouse.MakeCurrent();
            InputSystem.RemoveDevice(m_Keyboard);
            InputSystem.RemoveDevice(m_Mouse);
            Assert.That(Keyboard.current, Is.Null);
            Assert.That(Mouse.current, Is.Null);
            Assert.That(DesktopInput.IsControlPressed, Is.False);
            Assert.That(DesktopInput.IsPressed(Key.S), Is.False);
            Assert.That(DesktopInput.WasPressedThisFrame(Key.S), Is.False);
            Assert.That(DesktopInput.IsAnyKeyPressed, Is.False);
            Assert.That(DesktopInput.WasAnyKeyPressedThisFrame, Is.False);
            Assert.That(DesktopInput.IsLeftMouseButtonPressed, Is.False);
            Assert.That(DesktopInput.IsRightMouseButtonPressed, Is.False);
            Assert.That(DesktopInput.IsMiddleMouseButtonPressed, Is.False);
            Assert.That(DesktopInput.WasLeftMouseButtonPressedThisFrame, Is.False);
            Assert.That(DesktopInput.WasLeftMouseButtonReleasedThisFrame, Is.False);
            Assert.That(DesktopInput.MousePosition, Is.EqualTo(Vector3.zero));
            Assert.That(DesktopInput.MouseDelta, Is.EqualTo(Vector2.zero));
            Assert.That(DesktopInput.ScrollDelta, Is.EqualTo(Vector2.zero));

            m_Keyboard = InputSystem.AddDevice<Keyboard>();
            m_Mouse = InputSystem.AddDevice<Mouse>();
            m_Module.actionsAsset.devices = new InputDevice[] { m_Keyboard, m_Mouse };
            Keys(Key.RightShift);
            InputSystem.QueueStateEvent(m_Mouse, new MouseState { position = new Vector2(240, 160) }.WithButton(MouseButton.Right));
            InputSystem.Update();
            Assert.That(DesktopInput.IsControlPressed, Is.False);
            Assert.That(DesktopInput.IsShiftPressed, Is.True);
            Assert.That(DesktopInput.IsLeftMouseButtonPressed, Is.False);
            Assert.That(DesktopInput.IsRightMouseButtonPressed, Is.True);
            Assert.That(DesktopInput.MousePosition, Is.EqualTo(new Vector3(240, 160, 0)));
        }

        private void MouseState(Vector2 position, MouseButton? button = null, Vector2 scroll = default)
        {
            var state = new MouseState { position = position, scroll = scroll };
            if (button.HasValue) state = state.WithButton(button.Value);
            InputSystem.QueueStateEvent(m_Mouse, state);
            InputSystem.Update();
            m_Module.Process();
        }

        private void Keys(params Key[] keys)
        {
            InputSystem.QueueStateEvent(m_Keyboard, new KeyboardState(keys));
            InputSystem.Update();
        }

        private GameObject Widget(string name, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(m_Window.Root.transform, false);
            ((RectTransform)go.transform).sizeDelta = size;
            return go;
        }

        [Test]
        public async Task InjectedMouse_ClickDragScroll_PreservesPixelAndTickUnits()
        {
            var go = Widget("Pointer receiver", new Vector2(400, 400));
            var receiver = go.AddComponent<DesktopPointerProbe>();
            await UniTask.Yield();
            Canvas.ForceUpdateCanvases();
            Vector2 start = go.transform.position;
            MouseState(start);
            MouseState(start, MouseButton.Left);
            MouseState(start);
            Assert.That(receiver.Clicks, Is.EqualTo(1), "A click must be dispatched once.");
            foreach (var button in new[] { MouseButton.Right, MouseButton.Middle })
            {
                MouseState(start, button);
                MouseState(start + new Vector2(20, 0), button);
                Assert.That(receiver.Delta, Is.EqualTo(new Vector2(20, 0)));
                Assert.That(receiver.Button, Is.EqualTo(button == MouseButton.Right ? PointerEventData.InputButton.Right : PointerEventData.InputButton.Middle));
                MouseState(start + new Vector2(20, 30), button);
                Assert.That(receiver.Delta, Is.EqualTo(new Vector2(0, 30)));
                MouseState(start);
            }

            MouseState(start, scroll: Vector2.up);
            Assert.That(receiver.Scroll, Is.EqualTo(Vector2.up), "One normalized tick must remain one legacy UI tick, not six.");
            MouseState(start, scroll: new Vector2(0, -0.5f));
            Assert.That(receiver.Scroll.y, Is.EqualTo(-0.5f), "Fractional trackpad deltas must survive.");
        }

        [Test]
        public async Task InjectedDrag_CameraMatchesReferenceAxisByAxis_AtTwoUIScales()
        {
            var go = Widget("Camera pointer receiver", new Vector2(600, 600));
            var receiver = go.AddComponent<DesktopPointerProbe>();
            var controlled = new GameObject("Controlled camera", typeof(RectTransform));
            controlled.transform.SetParent(m_Scene.Root.transform);
            controlled.SetActive(false);
            ((RectTransform)controlled.transform).sizeDelta = new Vector2(600, 600);
            var view = controlled.AddComponent<View3D>();
            var camera = controlled.AddComponent<Camera3D>();
            Set(view, "m_Camera3D", camera);
            var ui = controlled.AddComponent<View3DUI>();
            var grid = controlled.AddComponent<ResizableGrid>();
            var scene = controlled.AddComponent<Base3DScene>();
            var roi = controlled.AddComponent<HBP.Data.Module3D.ROIManager>();
            Set(scene, "m_ROIManager", roi);
            Set(ui, "m_Scene", scene);
            Set(ui, "m_View", view);
            Set(ui, "m_RectTransform", (RectTransform)controlled.transform);
            ui.ParentGrid = grid;
            var scaler = m_Window.Root.GetComponent<CanvasScaler>();
            scaler.matchWidthOrHeight = 0;
            var handler = m_Window.Root.AddComponent<CanvasScalerHandler>();
            Set(ui, "m_CanvasScalerHandler", handler);
            receiver.ViewUI = ui;
            await UniTask.Yield();
            Canvas.ForceUpdateCanvases();
            Vector2 start = go.transform.position;
            foreach (float scale in new[] { 1f, 0.5f })
            {
                scaler.referenceResolution = new Vector2(Screen.width * scale, Screen.height * scale);
                Assert.That(handler.Scale, Is.EqualTo(scale));
                foreach (Vector2 delta in new[] { new Vector2(20, 0), new Vector2(0, 20) })
                {
                    camera.transform.SetPositionAndRotation(new Vector3(0, 0, -250), Quaternion.identity);
                    camera.Target = Vector3.zero;
                    MouseState(start);
                    MouseState(start, MouseButton.Middle);
                    MouseState(start + delta, MouseButton.Middle);
                    Vector3 expected = new(-delta.x * scale * 0.2f, -delta.y * scale, -250);
                    Assert.That(Vector3.Distance(camera.transform.position, expected), Is.LessThan(0.0001f));
                    MouseState(start);
                    camera.transform.SetPositionAndRotation(new Vector3(0, 0, -250), Quaternion.identity);
                    camera.Target = Vector3.zero;
                    MouseState(start, MouseButton.Right);
                    MouseState(start + delta, MouseButton.Right);
                    var rotation = Quaternion.AngleAxis(delta.x != 0 ? delta.x * scale : -delta.y * scale, delta.x != 0 ? Vector3.up : Vector3.right);
                    Assert.That(Quaternion.Angle(camera.transform.rotation, rotation), Is.LessThan(0.001f));
                    Assert.That(Vector3.Distance(camera.transform.position, rotation * new Vector3(0, 0, -250)), Is.LessThan(0.001f));
                    MouseState(start);
                }
            }

            camera.transform.SetPositionAndRotation(new Vector3(0, 0, -250), Quaternion.identity);
            camera.Target = Vector3.zero;
            MouseState(start, scroll: Vector2.up);
            Assert.That(camera.transform.position.z, Is.EqualTo(-245f).Within(0.0001f));
            MouseState(start, scroll: new Vector2(0, -0.5f));
            Assert.That(camera.transform.position.z, Is.EqualTo(-247.5f).Within(0.0001f));
            TestContext.Out.WriteLine("Reference f64212b7f: 20px strafe X=-4/Y=-20 at scale 1, X=-2/Y=-10 at scale .5; rotation 20/10 degrees; zoom +1 tick=+5, -.5 tick=-2.5.");
        }

        [Test]
        public async Task InjectedKeyboard_SubmitCancelAndNavigation_AreDispatchedOnce()
        {
            var first = Widget("First button", new Vector2(80, 40)).AddComponent<Button>();
            var second = Widget("Second button", new Vector2(80, 40)).AddComponent<Button>();
            second.transform.localPosition = new Vector3(100, 0);
            first.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = second };
            var probe = first.gameObject.AddComponent<DesktopPointerProbe>();
            int clicks = 0;
            first.onClick.AddListener(() => clicks++);
            await UniTask.Yield();
            EventSystem.current.SetSelectedGameObject(first.gameObject);
            Keys(Key.Enter);
            Assert.That(m_Module.submit.action.WasPressedThisFrame(), Is.True, "Serialized Submit binding receives Enter.");
            m_Module.Process();
            Assert.That(clicks, Is.EqualTo(1));
            Keys();
            m_Module.Process();
            Keys(Key.Escape);
            m_Module.Process();
            Assert.That(probe.Cancels, Is.EqualTo(1));
            Keys();
            m_Module.Process();
            Keys(Key.RightArrow);
            m_Module.Process();
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(second.gameObject));
        }

        [Test]
        public async Task TextFocus_TabAndModifiers_PreserveShortcutPriority()
        {
            InputField Field(string name)
            {
                var go = Widget(name, new Vector2(150, 40));
                var text = new GameObject("Text", typeof(RectTransform), typeof(Text));
                text.transform.SetParent(go.transform, false);
                var label = text.GetComponent<Text>();
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                var field = go.AddComponent<InputField>();
                field.textComponent = label;
                return field;
            }

            var first = Field("First field");
            var second = Field("Second field");
            var switcher = m_Window.Root.AddComponent<FieldsSwitcher>();
            Set(switcher, "m_InputFields", new[] { first, second });
            switcher.enabled = false;
            EventSystem.current.SetSelectedGameObject(first.gameObject);
            first.ActivateInputField();
            await UniTask.Yield();
            Assert.That(first.isFocused, Is.True);
            var shortcuts = m_Shortcuts;
            Keys(Key.LeftCtrl, Key.S);
            Assert.That(Property<bool>(shortcuts, "SaveActionPerformed"), Is.True);
            Assert.That(Property<bool>(shortcuts, "IsWritingInInputField"), Is.True);
            // Calling Update would open the application menu if typing did not win.
            Invoke(shortcuts, "Update");
            Keys();
            Keys(Key.RightCtrl, Key.RightShift, Key.S);
            Assert.That(Property<bool>(shortcuts, "SaveAsActionPerformed"), Is.True);
            Assert.That(Property<bool>(shortcuts, "SaveActionPerformed"), Is.False);
            Invoke(shortcuts, "Update");
            // uGUI edits native text events, independently of keyboard layout/key polling.
            first.ProcessEvent(new Event { type = EventType.KeyDown, character = 'é' });
            Assert.That(first.text, Is.EqualTo("é"));
            Keys(Key.Tab);
            Invoke(switcher, "Update");
            await UniTask.Yield();
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(second.gameObject));
            Keys();
            EventSystem.current.SetSelectedGameObject(null);
            Assert.That(Property<bool>(shortcuts, "IsWritingInInputField"), Is.False);
            Assert.That(Property<bool>(shortcuts, "SaveAsActionPerformed"), Is.False);
        }

        [Test]
        public async Task HeldKeys_RepeatAtExistingListRate_AndDeviceResetReleasesModifiers()
        {
            var browser = m_Window.Root.AddComponent<ListBrowser>();
            browser.enabled = false;
            int selections = 0;
            browser.OnSelectNext.AddListener(() => selections++);
            Keys(Key.DownArrow);
            Invoke(browser, "Update");
            Assert.That(selections, Is.EqualTo(1));
            await UniTask.Yield();
            InputSystem.Update();
            Invoke(browser, "Update");
            Assert.That(selections, Is.EqualTo(1), "Held key must not become another key-down.");
            Set(browser, "m_DownKeyHoldTimer", 0.61f);
            Invoke(browser, "Update");
            Assert.That(selections, Is.EqualTo(2));
            Keys(Key.LeftCtrl, Key.S);
            var shortcuts = m_Shortcuts;
            Assert.That(Property<bool>(shortcuts, "IsControlPressed"), Is.True);
            // This is the device reset used by the configured focus-loss policy.
            InputSystem.ResetDevice(m_Keyboard);
            InputSystem.ResetDevice(m_Mouse);
            InputSystem.Update();
            Assert.That(Property<bool>(shortcuts, "IsControlPressed"), Is.False);
            Assert.That(Property<bool>(shortcuts, "SaveActionPerformed"), Is.False);
            Assert.That(m_Mouse.leftButton.isPressed, Is.False);
            Keys(Key.S);
            Assert.That(Property<bool>(shortcuts, "SaveActionPerformed"), Is.False);
        }

        [Test]
        public async Task InjectedPointer_MovesAndResizesWindow_InBothDirections()
        {
            var go = Widget("Window", new Vector2(200, 150));
            var dragger = go.AddComponent<Dragger>();
            var layout = go.AddComponent<LayoutElement>();
            layout.minWidth = 50;
            layout.minHeight = 50;
            var resizer = go.AddComponent<Resizer>();
            await UniTask.Yield();
            Canvas.ForceUpdateCanvases();
            Vector2 start = go.transform.position;
            MouseState(start, MouseButton.Left);
            dragger.OnBeginDrag();
            MouseState(start + new Vector2(30, 20), MouseButton.Left);
            dragger.OnDrag();
            Assert.That((Vector2)go.transform.position, Is.EqualTo(start + new Vector2(30, 20)));
            var rect = (RectTransform)go.transform;
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            MouseState((Vector2)corners[2] + new Vector2(25, 0), MouseButton.Left);
            resizer.RightDrag();
            Assert.That(rect.sizeDelta.x, Is.EqualTo(225).Within(0.001));
            rect.GetWorldCorners(corners);
            MouseState((Vector2)corners[2] + new Vector2(0, 15), MouseButton.Left);
            resizer.TopDrag();
            Assert.That(rect.sizeDelta.y, Is.EqualTo(165).Within(0.001));
            MouseState(start);
        }

        [Test]
        public async Task InjectedShift_ExtendsTheExistingListSelection()
        {
            var go = new GameObject("Controlled selectable list");
            go.transform.SetParent(m_Scene.Root.transform);
            go.SetActive(false);
            var list = go.AddComponent<DesktopStringList>();
            list.Prepare();
            Keys();
            list.Click("one");
            Assert.That(list.ObjectsSelected, Is.EquivalentTo(new[] { "one" }));
            Keys(Key.LeftShift);
            list.Click("three");
            Assert.That(list.ObjectsSelected, Is.EquivalentTo(new[] { "one", "two", "three" }));
            await UniTask.Yield();
        }

        private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static T Property<T>(object target, string name) => (T)target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        private static void Invoke(object target, string name) => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    }

    public class DesktopPointerProbe : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IBeginDragHandler, IDragHandler, IScrollHandler, ICancelHandler
    {
        public int Clicks, Cancels;
        public Vector2 Delta, Scroll;
        public PointerEventData.InputButton Button;
        public View3DUI ViewUI;
        public void OnPointerClick(PointerEventData data) => Clicks++;

        public void OnPointerDown(PointerEventData data)
        {
        }

        public void OnBeginDrag(PointerEventData data)
        {
        }

        public void OnCancel(BaseEventData data) => Cancels++;

        public void OnDrag(PointerEventData data)
        {
            Delta = data.delta;
            Button = data.button;
            if (ViewUI != null) ViewUI.OnDrag(data);
        }

        public void OnScroll(PointerEventData data)
        {
            Scroll = data.scrollDelta;
            if (ViewUI != null) ViewUI.OnScroll(data);
        }
    }

    public class DesktopStringList : SelectableList<string>
    {
        public void Prepare()
        {
            m_Objects.AddRange(new[] { "one", "two", "three" });
            m_DisplayedObjects.AddRange(m_Objects);
            foreach (string item in m_Objects)
            {
                m_SelectedStateByObject[item] = false;
                m_SelectableStateByObject[item] = true;
            }
        }

        public void Click(string item) => OnChangeSelectionState(item, true);
    }
}
#endif
