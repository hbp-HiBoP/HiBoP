#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using HBP.Quest;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using Object = UnityEngine.Object;
using Site = HBP.Core.Object3D.Site;

namespace HBP.Tests.Quest
{
    public sealed class QuestSitePresetApplicationTests
    {
        [Test]
        public void ApplyAndRevertUseOneBatchAndRestoreOriginalValuesAndLabelOrder()
        {
            using var f = new Fixture();
            f.A.State.ApplyState(false, true, Color.blue, new[] { "First", "Remove", "Existing" });
            var preset = new QuestSitePreset { Highlight = QuestPresetBoolean.On, Blacklist = QuestPresetBoolean.On, SetColor = true, Color = Color.red };
            preset.SetLabelAction("Existing", QuestPresetLabelAction.Add);
            preset.SetLabelAction("New", QuestPresetLabelAction.Add);
            preset.SetLabelAction("Remove", QuestPresetLabelAction.Remove);
            using var action = new QuestSitePresetApplication();
            action.Bind(f.Scene, f.Column, f.A);
            int batches = 0, notifications = 0;
            f.A.State.OnChangeState.AddListener(() => notifications++);
            f.Scene.SiteConfigurationBatchRouter = (changes, apply) =>
            {
                Assert.That(changes.Count, Is.EqualTo(1));
                batches++;
                apply();
                return true;
            };
            Assert.That(action.ApplyOrRevert(preset), Is.True);
            Assert.That(action.CanRevert, Is.True);
            Assert.That(f.A.State.IsBlackListed, Is.True);
            Assert.That(f.A.State.Color, Is.EqualTo(Color.red));
            CollectionAssert.AreEqual(new[] { "First", "Existing", "New" }, f.A.State.Labels);
            Assert.That(action.ApplyOrRevert(preset), Is.True);
            Assert.That(action.CanRevert, Is.False);
            Assert.That(f.A.State.IsHighlighted, Is.True);
            Assert.That(f.A.State.IsBlackListed, Is.False);
            Assert.That(f.A.State.Color, Is.EqualTo(Color.blue));
            CollectionAssert.AreEqual(new[] { "First", "Remove", "Existing" }, f.A.State.Labels);
            Assert.That(batches, Is.EqualTo(2));
            Assert.That(notifications, Is.EqualTo(2));
        }

        [Test]
        public void UnrelatedEditsSurviveRevertWhileOverlappingEditsInvalidateItImmediately()
        {
            using var f = new Fixture();
            var preset = new QuestSitePreset { Highlight = QuestPresetBoolean.On };
            preset.SetLabelAction("New", QuestPresetLabelAction.Add);
            using var action = new QuestSitePresetApplication();
            action.Bind(f.Scene, f.Column, f.A);
            action.ApplyOrRevert(preset);
            f.A.State.Color = Color.green;
            f.A.State.AddLabel("Manual");
            Assert.That(action.CanRevert, Is.True);
            action.ApplyOrRevert(preset);
            Assert.That(f.A.State.Color, Is.EqualTo(Color.green));
            CollectionAssert.AreEqual(new[] { "Manual" }, f.A.State.Labels);
            action.ApplyOrRevert(preset);
            f.A.State.IsHighlighted = false;
            f.A.State.IsHighlighted = true;
            Assert.That(action.CanRevert, Is.False, "An intervening edit cannot regain ownership by restoring the value.");
            action.ApplyOrRevert(new QuestSitePreset { SetColor = true, Color = Color.blue });
            f.A.State.Color = Color.red;
            Assert.That(action.CanRevert, Is.False, "Authoritative color corrections also invalidate the local undo.");
        }

        [Test]
        public void LeavingTargetLosesUndoAndNoOpDoesNotCreateOne()
        {
            using var f = new Fixture();
            using var action = new QuestSitePresetApplication();
            var preset = new QuestSitePreset { Highlight = QuestPresetBoolean.On };
            action.Bind(f.Scene, f.Column, f.A);
            action.ApplyOrRevert(preset);
            action.Bind(f.Scene, f.Column, f.B);
            action.Bind(f.Scene, f.Column, f.A);
            Assert.That(action.CanRevert, Is.False);
            Assert.That(action.ApplyOrRevert(preset), Is.False);
            Assert.That(action.CanRevert, Is.False);
        }

        [Test]
        public void InvalidLabelPayloadCannotPartiallyApplyOtherPresetAttributesOffline()
        {
            using var f = new Fixture();
            using var action = new QuestSitePresetApplication();
            action.Bind(f.Scene, f.Column, f.A);
            var preset = new QuestSitePreset { Highlight = QuestPresetBoolean.On, Blacklist = QuestPresetBoolean.On };
            preset.SetLabelAction(new string('X', 500), QuestPresetLabelAction.Add);
            Assert.Throws<InvalidOperationException>(() => action.ApplyOrRevert(preset));
            Assert.That(f.A.State.IsHighlighted || f.A.State.IsBlackListed, Is.False);
            Assert.That(f.A.State.Labels, Is.Empty);
            Assert.That(action.CanRevert, Is.False);
        }

        [Test]
        public void EditingPresetOrTransientSelectionChangeInvalidatesUndoAndPersistsInstructions()
        {
            using var f = new Fixture();
            f.Editor.Open();
            f.Toggle("highlightOn").isOn = true;
            f.Editor.ApplyOrRevert();
            Assert.That(f.Text("applyText").text, Is.EqualTo("Revert"));
            var name = f.Field<InputField>("labelName");
            name.text = "Future";
            f.Button("newLabel").onClick.Invoke();
            Assert.That(f.Text("applyText").text, Is.EqualTo("Apply"));
            Assert.That(f.A.State.Labels, Does.Not.Contain("Future"));
            var saved = JsonUtility.FromJson<QuestSitePreset>(File.ReadAllText(QuestSitePresetPanel.DefaultPath));
            Assert.That(saved.GetLabelAction("Future"), Is.EqualTo(QuestPresetLabelAction.Add));
            f.Editor.ApplyOrRevert();
            Assert.That(f.A.State.Labels, Does.Contain("Future"));
            Assert.That(f.Text("applyText").text, Is.EqualTo("Revert"));
            f.View.OnPresentationChanging.Invoke(f.Scene);
            f.Tick();
            Assert.That(f.Text("applyText").text, Is.EqualTo("Revert"), "Changing local presentation alone preserves undo.");
            f.View.OnContextSelected.Invoke(f.Scene, null);
            f.View.OnContextSelected.Invoke(f.Scene, f.Column);
            f.Tick();
            Assert.That(f.Text("applyText").text, Is.EqualTo("Apply"), "Even an immediate context round trip loses undo.");
            f.Toggle("blacklistOn").isOn = true;
            f.Editor.ApplyOrRevert();
            Assert.That(f.Text("applyText").text, Is.EqualTo("Revert"));
            f.Select(f.B);
            f.Select(f.A);
            f.Tick();
            Assert.That(f.Text("applyText").text, Is.EqualTo("Apply"));
            Assert.That(f.A.State.IsHighlighted, Is.True);
        }

        [Test]
        public void PresetAndColorPanelsStayOpenSideBySideAndColorDraftDoesNotMutateSite()
        {
            using var f = new Fixture();
            f.Editor.Open();
            var panel = f.Field<GameObject>("panel");
            f.Button("chooseColor").onClick.Invoke();
            var picker = f.Field<QuestColorPicker>("colorPicker");
            Assert.That(panel.activeSelf && picker.gameObject.activeSelf, Is.True);
            var rect = (RectTransform)picker.transform;
            Assert.That(rect.parent, Is.EqualTo(panel.transform));
            Assert.That(rect.anchorMin.x, Is.EqualTo(1));
            Assert.That(rect.pivot.x, Is.Zero);
            Assert.That(rect.anchoredPosition.x, Is.GreaterThan(0));
            Assert.That(rect.localScale, Is.EqualTo(Vector3.one));
            var fields = new SerializedObject(picker);
            var palette = (Button)fields.FindProperty("palette").GetArrayElementAtIndex(0).objectReferenceValue;
            palette.onClick.Invoke();
            Assert.That(panel.activeSelf, Is.True);
            Assert.That(picker.gameObject.activeSelf, Is.False);
            Assert.That(f.Toggle("colorEnabled").isOn, Is.True);
            Assert.That(f.A.State.Color, Is.EqualTo(SiteState.DefaultColor));
            f.Editor.ApplyOrRevert();
            Assert.That(f.A.State.Color, Is.EqualTo(palette.GetComponent<Image>().color));
            f.Editor.Close();
            f.Editor.ApplyOrRevert();
            Assert.That(f.A.State.Color, Is.EqualTo(SiteState.DefaultColor), "Closing the editor preserves the current undo.");
        }

        [TestCase("highlightOn", "highlightOff", "Highlight")]
        [TestCase("blacklistOn", "blacklistOff", "Blacklist")]
        public void BooleanTogglesAreExclusiveAndBothOffMeansKeep(string onName, string offName, string property)
        {
            using var f = new Fixture();
            f.Editor.Open();
            var on = f.Toggle(onName);
            var off = f.Toggle(offName);
            QuestPresetBoolean Saved() => (QuestPresetBoolean)typeof(QuestSitePreset).GetField(property).GetValue(JsonUtility.FromJson<QuestSitePreset>(File.ReadAllText(QuestSitePresetPanel.DefaultPath)));
            Assert.That(on.isOn || off.isOn, Is.False);
            on.isOn = true;
            Assert.That(off.isOn, Is.False);
            Assert.That(Saved(), Is.EqualTo(QuestPresetBoolean.On));
            off.isOn = true;
            Assert.That(on.isOn, Is.False);
            Assert.That(Saved(), Is.EqualTo(QuestPresetBoolean.Off));
            off.isOn = false;
            Assert.That(on.isOn || off.isOn, Is.False);
            Assert.That(Saved(), Is.EqualTo(QuestPresetBoolean.Keep));
        }

        [Test]
        public void LabelColumnsAreExclusiveAndCanReturnToKeepWithoutChangingTheSite()
        {
            using var f = new Fixture();
            f.A.State.AddLabel("Review");
            f.Editor.Open();
            var list = f.Field<QuestSitePresetLabelList>("labelList");
            var row = list.GetComponentInChildren<QuestSitePresetLabelRow>();
            var fields = new SerializedObject(row);
            var add = (Toggle)fields.FindProperty("add").objectReferenceValue;
            var remove = (Toggle)fields.FindProperty("remove").objectReferenceValue;
            QuestPresetLabelAction Saved() => JsonUtility.FromJson<QuestSitePreset>(File.ReadAllText(QuestSitePresetPanel.DefaultPath)).GetLabelAction("Review");
            Assert.That(add.isOn || remove.isOn, Is.False);
            add.isOn = true;
            Assert.That(Saved(), Is.EqualTo(QuestPresetLabelAction.Add));
            remove.isOn = true;
            Assert.That(add.isOn, Is.False);
            Assert.That(Saved(), Is.EqualTo(QuestPresetLabelAction.Remove));
            remove.isOn = false;
            Assert.That(Saved(), Is.EqualTo(QuestPresetLabelAction.Keep));
            CollectionAssert.AreEqual(new[] { "Review" }, f.A.State.Labels);
            f.Toggle("colorEnabled").isOn = true;
            f.Toggle("colorEnabled").isOn = false;
            Assert.That(JsonUtility.FromJson<QuestSitePreset>(File.ReadAllText(QuestSitePresetPanel.DefaultPath)).SetColor, Is.False);
        }

        [Test]
        public void BUsesLatestSelectionWhileAHeldAndRequiresANewPressForEachAction()
        {
            var background = InputSystem.settings.backgroundBehavior;
            var editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.RegisterLayout("{\"name\":\"PresetTestController\",\"extend\":\"XRController\",\"controls\":[{\"name\":\"primaryButton\",\"layout\":\"Button\",\"format\":\"FLT\"},{\"name\":\"secondaryButton\",\"layout\":\"Button\",\"format\":\"FLT\"}]}");
            var device = InputSystem.AddDevice("PresetTestController");
            InputSystem.SetDeviceUsage(device, CommonUsages.RightHand);
            try
            {
                using var f = new Fixture();
                f.Editor.Open();
                f.Toggle("highlightOn").isOn = true;
                f.Editor.SendMessage("OnApplicationFocus", true);
                InputSystem.Update();
                InputSystem.QueueDeltaStateEvent(device.GetChildControl<ButtonControl>("primaryButton"), 1f);
                InputSystem.QueueDeltaStateEvent(device.GetChildControl<ButtonControl>("secondaryButton"), 1f);
                InputSystem.Update();
                f.Select(f.B); // The pointer's A selection is processed before LateUpdate.
                f.Tick();
                Assert.That(f.B.State.IsHighlighted, Is.True);
                Assert.That(f.A.State.IsHighlighted, Is.False);
                InputSystem.Update();
                f.Tick();
                Assert.That(f.B.State.IsHighlighted, Is.True, "Holding B must not repeatedly toggle the preset.");
                InputSystem.QueueDeltaStateEvent(device.GetChildControl<ButtonControl>("secondaryButton"), 0f);
                InputSystem.Update();
                f.Tick();
                InputSystem.QueueDeltaStateEvent(device.GetChildControl<ButtonControl>("secondaryButton"), 1f);
                InputSystem.Update();
                f.Tick();
                Assert.That(f.B.State.IsHighlighted, Is.False, "A can remain held while B reverts.");
            }
            finally
            {
                InputSystem.RemoveDevice(device);
                InputSystem.RemoveLayout("PresetTestController");
                InputSystem.settings.backgroundBehavior = background;
                InputSystem.settings.editorInputBehaviorInPlayMode = editor;
            }
        }

        [Test]
        public void SwitchingScenesWithEqualCatalogueRevisionsRebindsLabelsAndPreservesPresetInstructions()
        {
            using var f = new Fixture();
            f.A.State.AddLabel("Old scene");
            f.Editor.Open();
            var name = f.Field<InputField>("labelName");
            name.text = "Future";
            f.Button("newLabel").onClick.Invoke();
            var other = QuestSceneTestBinding.VisibleScene(f.Data);
            Set(other, "m_ImplantationManager", other.gameObject.AddComponent<ImplantationManager>());
            var column = f.Data.AddComponent<Column3DAnatomy>();
            typeof(Column3D).GetProperty("ColumnData").SetValue(column, new AnatomicColumn());
            typeof(Column3D).GetProperty("Sites").SetValue(column, new List<Site> { f.B });
            other.Columns.Add(column);
            column.IsSelected = true;
            typeof(Column3D).GetProperty("SelectedSite").SetValue(column, f.B);
            f.B.State.AddLabel("New scene");
            QuestSceneTestBinding.Attach(f.View, other);
            f.Tick();
            var list = f.Field<QuestSitePresetLabelList>("labelList");
            var values = (string[])typeof(QuestSitePresetLabelList).GetField("values", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(list);
            CollectionAssert.AreEquivalent(new[] { "Future", "New scene" }, values);
            Assert.That(f.Field<GameObject>("panel").activeSelf, Is.True);
        }

        [Test]
        public async Task PresetLayoutFitsAndRendersBothPanelsWithoutOverlap()
        {
            using var f = new Fixture();
            f.A.State.AddLabel("Review");
            f.A.State.AddLabel("Noise");
            f.Root.GetComponent<QuestWindow>().Open();
            typeof(QuestSitePanel).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(f.Root.GetComponent<QuestSitePanel>(), null);
            f.Editor.Open();
            f.Toggle("highlightOn").isOn = true;
            f.Field<InputField>("labelName").text = "New label";
            f.Button("newLabel").onClick.Invoke();
            f.Button("chooseColor").onClick.Invoke();
            var canvas = f.Root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            f.Root.transform.localScale = Vector3.one;
            f.Root.transform.position = Vector3.zero;
            var cameraObject = new GameObject("Preset preview camera", typeof(Camera));
            var target = new RenderTexture(1800, 1000, 24);
            var pixels = new Texture2D(1800, 1000, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                var camera = cameraObject.GetComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 450;
                camera.farClipPlane = 5000;
                camera.transform.position = new Vector3(432, 0, -2000);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.08f, .08f, .08f);
                camera.targetTexture = target;
                canvas.worldCamera = camera;
                await UniTask.NextFrame();
                Canvas.ForceUpdateCanvases();
                var panel = (RectTransform)f.Field<GameObject>("panel").transform;
                LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
                foreach (RectTransform child in panel)
                {
                    if (!child.gameObject.activeSelf || child.GetComponent<LayoutElement>() is { ignoreLayout: true }) continue;
                    var corners = new Vector3[4];
                    child.GetWorldCorners(corners);
                    foreach (var corner in corners)
                    {
                        var point = panel.InverseTransformPoint(corner);
                        Assert.That(point.x, Is.InRange(panel.rect.xMin + 11.9f, panel.rect.xMax - 11.9f), child.name);
                        Assert.That(point.y, Is.InRange(panel.rect.yMin + 11.9f, panel.rect.yMax - 11.9f), child.name);
                    }
                }

                var picker = (RectTransform)f.Field<QuestColorPicker>("colorPicker").transform;
                var pickerCorners = new Vector3[4];
                picker.GetWorldCorners(pickerCorners);
                Assert.That(panel.InverseTransformPoint(pickerCorners[0]).x, Is.GreaterThan(panel.rect.xMax));
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                pixels.Apply();
                Assert.That(pixels.GetPixels32().Any(pixel => pixel.r > 60 || pixel.g > 60 || pixel.b > 60), Is.True, "The preview must contain the rendered UI.");
                string directory = Path.GetFullPath(".artifacts/QuestSitePreset");
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, "preview.png"), pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(pixels);
            }
        }

        private sealed class Fixture : IDisposable
        {
            private readonly GameObject data = new("Preset fixture");
            private readonly string oldPath = QuestSitePresetPanel.DefaultPath;
            private readonly string directory = Path.Combine(Path.GetTempPath(), "hibop-preset-" + Guid.NewGuid().ToString("N"));
            public readonly Base3DScene Scene;
            public readonly Column3D Column;
            public readonly Site A, B;
            public readonly GameObject Root;
            public readonly QuestSitePresetPanel Editor;
            private readonly QuestAnatomyView view;
            private readonly GameObject ui = new("Preset UI fixture");
            public GameObject Data => data;
            public QuestAnatomyView View => view;

            public Fixture()
            {
                Directory.CreateDirectory(directory);
                QuestSitePresetPanel.DefaultPath = Path.Combine(directory, "preset.json");
                data.SetActive(false);
                Scene = QuestSceneTestBinding.VisibleScene(data);
                Set(Scene, "m_ImplantationManager", Scene.gameObject.AddComponent<ImplantationManager>());
                Column = data.AddComponent<Column3DAnatomy>();
                typeof(Column3D).GetProperty("ColumnData").SetValue(Column, new AnatomicColumn());
                typeof(Column3D).GetProperty("Sites").SetValue(Column, new List<Site>());
                Scene.Columns.Add(Column);
                Column.IsSelected = true;
                view = data.AddComponent<QuestAnatomyView>();
                QuestSceneTestBinding.Attach(view, Scene);
                A = AddSite("A");
                B = AddSite("B");
                Select(A);
                ui.SetActive(false);
                Root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Site Window.prefab"), ui.transform, false);
                var owner = Root.GetComponent<QuestSitePanel>();
                Set(owner, "view", view);
                Editor = Root.GetComponent<QuestSitePresetPanel>();
                ui.SetActive(true);
                owner.enabled = false;
                Root.GetComponent<QuestWindowFollower>().enabled = false;
            }

            private Site AddSite(string name)
            {
                var go = new GameObject(name);
                go.transform.SetParent(data.transform, false);
                var site = go.AddComponent<Site>();
                site.State = new SiteState();
                site.Information = new SiteInformation { Name = name, Patient = new Patient { ID = "preset-patient", Name = "Synthetic" }, SiteData = new HBP.Core.Data.Site { Name = name } };
                Column.Sites.Add(site);
                return site;
            }

            public void Select(Site site)
            {
                typeof(Column3D).GetProperty("SelectedSite").SetValue(Column, site);
                Column.OnSelectSite.Invoke(site);
            }

            public T Field<T>(string name) => (T)typeof(QuestSitePresetPanel).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Editor);
            public Button Button(string name) => Field<Button>(name);
            public Toggle Toggle(string name) => Field<Toggle>(name);
            public Text Text(string name) => Field<Text>(name);
            public void Tick() => typeof(QuestSitePresetPanel).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(Editor, null);

            public void Dispose()
            {
                Object.DestroyImmediate(ui);
                QuestSceneTestBinding.Clear(view);
                Object.DestroyImmediate(data);
                QuestSitePresetPanel.DefaultPath = oldPath;
                Directory.Delete(directory, true);
            }
        }

        private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
#endif
