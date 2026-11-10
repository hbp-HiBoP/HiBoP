using System.Linq;
using HBP.Quest;
using HBP.Theme;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using Object = UnityEngine.Object;

namespace HBP.Tests.PlatformConfiguration
{
    public class QuestUIAssetTests
    {
        [Test]
        public void DropdownsAcrossQuestPrefabsUseTheSharedSlimDesktopScrollbarStyle()
        {
            int dropdowns = 0;
            var track = AssetDatabase.LoadAssetAtPath<Element>("Assets/Resources/Themes/Quest/Elements/QuestDropdownScrollbar.asset");
            var handle = AssetDatabase.LoadAssetAtPath<Element>("Assets/Resources/Themes/Quest/Elements/QuestDropdownScrollbarHandle.asset");
            foreach (var pair in new[] { (track, "Toolbar Dropdown Scrollbar"), (handle, "Toolbar Dropdown Scrollbar Handle") })
            {
                var desktop = AssetDatabase.LoadAssetAtPath<Element>("Assets/Resources/Themes/Main/Elements/Visualization/Toolbar/" + pair.Item2 + ".asset");
                Assert.That(pair.Item1.SettingsByState[0].Settings, Is.EqualTo(desktop.SettingsByState[0].Settings));
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Quest" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var dropdown in root.GetComponentsInChildren<QuestDropdown>(true))
                {
                    dropdowns++;
                    var bar = dropdown.template.GetComponent<UnityEngine.UI.ScrollRect>().verticalScrollbar;
                    Assert.That(bar, Is.Not.Null, path + " / " + dropdown.name);
                    Assert.That(((RectTransform)bar.transform).sizeDelta.x, Is.EqualTo(12), path + " / " + dropdown.name);
                    Assert.That(bar.GetComponent<ThemeElement>().Element, Is.SameAs(track), path);
                    Assert.That(bar.handleRect.GetComponent<ThemeElement>().Element, Is.SameAs(handle), path);
                }
            }

            Assert.That(dropdowns, Is.GreaterThan(10), "Check instantiated window controls as well as the common prefab.");
        }

        [Test]
        public void SiteProbe_IsAuthoredAndWiredToUniversalPointerWithSharedThemeColor()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestBootstrap.prefab");
            var pointer = root.GetComponentInChildren<QuestPointerInput>(true);
            var probe = root.GetComponentInChildren<QuestSiteProbe>(true);
            Assert.That(probe, Is.Not.Null);
            Assert.That(new SerializedObject(pointer).FindProperty("siteProbe").objectReferenceValue, Is.SameAs(probe));
            var fields = new SerializedObject(probe);
            foreach (string field in new[] { "anatomy", "policy", "right", "marker", "markerRenderer" })
                Assert.That(fields.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            var marker = (Transform)fields.FindProperty("marker").objectReferenceValue;
            Assert.That(marker.gameObject.activeSelf, Is.False);
            Assert.That(marker.GetComponent<Collider>(), Is.Null, "The probe must not add a physical response.");
            Assert.That(marker.GetComponent<MeshFilter>().sharedMesh, Is.Not.Null);
            var policy = (QuestInteractionPolicy)fields.FindProperty("policy").objectReferenceValue;
            Assert.That(policy.FeedbackColor, Is.Not.Null);
            Assert.That(policy.SiteProbeRadius, Is.EqualTo(.0005f));
        }

        [Test]
        public void SiteSelectionRing_HasIndependentQuestThemeAndDesktopSpriteAndAnimation()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/3D/QuestColumn.prefab");
            var ring = root.GetComponent<QuestSiteSelectionRing>();
            Assert.That(new SerializedObject(root.GetComponent<QuestColumnPresentation>()).FindProperty("selectionRing").objectReferenceValue, Is.SameAs(ring));
            var fields = new SerializedObject(ring);
            var board = (RectTransform)fields.FindProperty("billboard").objectReferenceValue;
            var image = (UnityEngine.UI.Image)fields.FindProperty("image").objectReferenceValue;
            Assert.That(board.gameObject.activeSelf, Is.False);
            Assert.That(board.GetComponent<Canvas>().renderMode, Is.EqualTo(RenderMode.WorldSpace));
            Assert.That(board.GetComponent<UnityEngine.UI.GraphicRaycaster>(), Is.Null);
            Assert.That(image.raycastTarget, Is.False);
            Assert.That(image.sprite, Is.SameAs(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Selection.png")));
            Assert.That(image.GetComponent<Animator>().runtimeAnimatorController, Is.SameAs(AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Sprites/Selection Animation Controller.controller")));
            var element = image.GetComponent<ThemeElement>().Element;
            Assert.That(AssetDatabase.GetAssetPath(element), Is.EqualTo("Assets/Resources/Themes/Quest/Elements/QuestSiteSelectionRing.asset"));
            var settings = element.SettingsByState.Single().Settings;
            Assert.That(settings.OfType<HBP.Theme.Image>().Single().SourceImage, Is.SameAs(image.sprite));
            Assert.That(settings.OfType<HBP.Theme.Image>().Single().Material.shader.name, Is.EqualTo("HBP/Quest/Site Selection"));
            Assert.That(settings.OfType<HBP.Theme.Color>().Single(), Is.SameAs(AssetDatabase.LoadAssetAtPath<HBP.Theme.Color>("Assets/Resources/Themes/Main/Settings/Colors/Default Color.asset")));
        }

        [Test]
        public void WindowPrefabs_ShareTheBaseCornerResizeHandles()
        {
            const string demoPath = "Assets/Prefabs/Quest/UI/Quest Resizable Window Demo.prefab";
            var demo = AssetDatabase.LoadAssetAtPath<GameObject>(demoPath);
            var handles = demo.GetComponentsInChildren<QuestWindowResizer>(true);
            Assert.That(PrefabUtility.GetPrefabAssetType(demo), Is.EqualTo(PrefabAssetType.Variant));
            Assert.That(handles.Select(h => h.Edge), Is.EquivalentTo(new[] { QuestWindowResizer.WindowEdge.BottomLeft, QuestWindowResizer.WindowEdge.BottomRight }));
            foreach (var resizer in handles)
            {
                var fields = new SerializedObject(resizer);
                Assert.That(fields.FindProperty("window").objectReferenceValue, Is.EqualTo(demo.GetComponent<QuestWindow>()));
                Assert.That(fields.FindProperty("minimumSize").vector2Value, Is.EqualTo(new Vector2(500, 350)));
                Assert.That(fields.FindProperty("maximumSize").vector2Value, Is.EqualTo(new Vector2(6000, 5000)));
                Assert.That(resizer.GetComponent<UnityEngine.UI.Image>().raycastTarget, Is.True);
                Assert.That(AssetDatabase.GetAssetPath(resizer.GetComponent<ThemeElement>().Element), Is.EqualTo("Assets/Resources/Themes/Quest/Elements/QuestWindowResizeSurface.asset"));
            }

            foreach (string path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Quest" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var window = prefab.GetComponent<QuestWindow>();
                if (window == null || window.Key == "Toolbar") continue;
                Assert.That(prefab.GetComponentsInChildren<QuestWindowResizer>(true).Select(h => h.Edge), Is.EquivalentTo(new[] { QuestWindowResizer.WindowEdge.BottomLeft, QuestWindowResizer.WindowEdge.BottomRight }), path);
            }
        }

        [Test]
        public void LoadingPrefabs_KeepDesktopPresenterAndWireIndependentQuestPresentation()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestBootstrap.prefab");
            var presenter = root.GetComponentInChildren<QuestLoadingCircle>(true);
            var fields = new SerializedObject(presenter);
            foreach (string field in new[] { "progressRing", "brain", "informationBox", "prefix", "information", "suffix", "loadingEffect", "cancelContainer", "cancel" })
                Assert.That(fields.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            var manager = root.GetComponentInChildren<HBP.UI.Tools.LoadingManager>(true);
            Assert.That(new SerializedObject(manager).FindProperty("m_LoadingCircle").objectReferenceValue, Is.EqualTo(presenter));
            var follower = root.GetComponentInChildren<QuestLoadingFollower>(true);
            Assert.That(new SerializedObject(follower).FindProperty("headCamera").objectReferenceValue, Is.Not.Null);
            Assert.That(new SerializedObject(follower).FindProperty("content").objectReferenceValue, Is.EqualTo(presenter.gameObject));
            Assert.That(follower.GetComponent<TrackedDeviceRaycaster>(), Is.Not.Null);
            var overlay = follower.GetComponent<QuestLoadingOverlay>();
            Assert.That(overlay, Is.Not.Null);
            var overlayMaterial = new SerializedObject(overlay).FindProperty("material").objectReferenceValue as Material;
            Assert.That(overlayMaterial, Is.Not.Null);
            Assert.That(overlayMaterial.shader.name, Is.EqualTo("HBP/Quest/Loading Overlay"));
            Assert.That(follower.GetComponent<Canvas>().sortingOrder, Is.GreaterThanOrEqualTo(30000));
            Assert.That(root.GetComponentInChildren<HBP.UI.Tools.LoadingCircle>(true), Is.Null);
            foreach (var theme in presenter.GetComponentsInChildren<ThemeElement>(true))
                Assert.That(AssetDatabase.GetAssetPath(theme.Element), Does.StartWith("Assets/Resources/Themes/Quest/Elements/"));
            foreach (var path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                if (path.StartsWith("Assets/Prefabs/Quest/")) continue;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var desktopManager in prefab.GetComponentsInChildren<HBP.UI.Tools.LoadingManager>(true))
                    Assert.That(new SerializedObject(desktopManager).FindProperty("m_LoadingCircle").objectReferenceValue, Is.TypeOf<HBP.UI.Tools.LoadingCircle>(), path);
            }

            var column = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/3D/QuestColumn.prefab");
            Assert.That(new SerializedObject(column.GetComponent<QuestAnatomyManipulator>()).FindProperty("rayTarget").objectReferenceValue, Is.TypeOf<MeshCollider>());
        }

        [Test]
        public void QuestThemeCompositionsAndTextSettings_AreIndependentOfDesktop()
        {
            var paths = AssetDatabase.FindAssets("t:Element", new[] { "Assets/Resources/Themes/Quest" }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
            Assert.That(paths.Length, Is.GreaterThanOrEqualTo(20));
            foreach (var path in paths)
            {
                var element = AssetDatabase.LoadAssetAtPath<Element>(path);
                Assert.That(AssetDatabase.GetAssetPath(element.DefaultState), Does.StartWith("Assets/Resources/Themes/Quest/States/"), path);
                foreach (var state in element.SettingsByState)
                {
                    Assert.That(AssetDatabase.GetAssetPath(state.State), Does.StartWith("Assets/Resources/Themes/Quest/States/"), path);
                    Assert.That(state.Settings.All(s => s != null), Is.True, path);
                    foreach (var setting in state.Settings.Where(s => s is HBP.Theme.Text || s is HBP.Theme.LayoutElement))
                        Assert.That(AssetDatabase.GetAssetPath(setting), Does.StartWith("Assets/Resources/Themes/Quest/Settings/"), path);
                }
            }
        }

        [Test]
        public void QuestColors_AreSharedSettingsAndQuestTypographyDoesNotChangeDesktop()
        {
            var element = AssetDatabase.LoadAssetAtPath<Element>("Assets/Resources/Themes/Quest/Elements/QuestTitle.asset");
            var settings = element.SettingsByState.Single(s => s.State == element.DefaultState).Settings;
            var color = settings.OfType<HBP.Theme.Color>().Single();
            var text = settings.OfType<HBP.Theme.Text>().Single();
            Assert.That(AssetDatabase.GetAssetPath(color), Does.StartWith("Assets/Resources/Themes/Main/Settings/Colors/"));
            var desktop = AssetDatabase.LoadAssetAtPath<HBP.Theme.Text>("Assets/Resources/Themes/Main/Settings/Texts/Large Normal.asset");
            int desktopSize = desktop.FontSize, originalSize = text.FontSize;
            var originalColor = color.Value;
            var preview = new GameObject("Quest theme test", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            try
            {
                text.FontSize = 39;
                color.Value = UnityEngine.Color.magenta;
                element.Set(preview);
                Assert.That(preview.GetComponent<UnityEngine.UI.Text>().fontSize, Is.EqualTo(39));
                Assert.That(preview.GetComponent<UnityEngine.UI.Text>().color, Is.EqualTo(color.Value));
                Assert.That(desktop.FontSize, Is.EqualTo(desktopSize));
            }
            finally
            {
                text.FontSize = originalSize;
                color.Value = originalColor;
                Object.DestroyImmediate(preview);
            }
        }

        [TestCase("Connection")]
        [TestCase("Cuts")]
        [TestCase("Display")]
        [TestCase("Recenter")]
        public void ToolbarIcons_AreImageSettingsUsingHiBoPSprites(string role)
        {
            var element = AssetDatabase.LoadAssetAtPath<Element>("Assets/Resources/Themes/Quest/Elements/Quest" + role + "Icon.asset");
            var icon = element.SettingsByState.SelectMany(s => s.Settings).OfType<HBP.Theme.Image>().Single();
            Assert.That(icon.SourceImage, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(icon), Does.StartWith("Assets/Resources/Themes/Quest/Settings/Images/"));
            Assert.That(AssetDatabase.GetAssetPath(icon.SourceImage), Does.Not.StartWith("Assets/Resources/Themes/Quest"), "Reuses the HiBoP pictogram library.");
        }

        [Test]
        public void Bootstrap_WiresUniversalPointersAndIndependentManagedWindows()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestBootstrap.prefab");
            var manager = root.GetComponent<QuestWindowsManager>();
            var input = root.GetComponentInChildren<QuestAnatomyInput>(true);
            Assert.That(new SerializedObject(input).FindProperty("windows").objectReferenceValue, Is.SameAs(manager));
            var fields = new SerializedObject(manager);
            Assert.That(fields.FindProperty("head").objectReferenceValue, Is.Not.Null);
            Assert.That(fields.FindProperty("uiCamera").objectReferenceValue, Is.Not.Null);
            var initial = fields.FindProperty("initialWindows");
            Assert.That(initial.arraySize, Is.EqualTo(8));
            for (int i = 0; i < initial.arraySize; i++) Assert.That(initial.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null);
            var pointer = root.GetComponentInChildren<QuestPointerInput>(true);
            Assert.That(pointer, Is.Not.Null);
            Assert.That(pointer.GetComponent<InputSystemUIInputModule>(), Is.Null);
            foreach (var window in root.GetComponentsInChildren<QuestWindow>(true))
            {
                Assert.That(window.transform.parent, Is.EqualTo(root.transform), window.Key);
                var windowFields = new SerializedObject(window);
                Assert.That(windowFields.FindProperty("canvas").objectReferenceValue, Is.Not.Null, window.Key);
                Assert.That(windowFields.FindProperty("content").objectReferenceValue, Is.Not.Null, window.Key);
                Assert.That(windowFields.FindProperty("raycaster").objectReferenceValue, Is.InstanceOf<TrackedDeviceRaycaster>(), window.Key);
                Assert.That(window.GetComponent<QuestWindowFollower>(), Is.Not.Null);
                foreach (var theme in window.GetComponentsInChildren<ThemeElement>(true))
                    Assert.That(AssetDatabase.GetAssetPath(theme.Element), Does.StartWith("Assets/Resources/Themes/Quest/Elements/"));
                Assert.That(window.GetComponentsInChildren<HBP.UI.Tools.Window>(true), Is.Empty, "Quest windows may reuse shared controls and scientific graphs, but must not embed Desktop windows.");
            }

            var toolbar = root.GetComponentInChildren<QuestToolbar>(true);
            var toolbarSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Toolbar.prefab");
            Assert.That(((RectTransform)toolbar.transform).rect.height, Is.EqualTo(((RectTransform)toolbarSource.transform).rect.height), "The production instance must inherit the toolbar height.");
            Assert.That(((UnityEngine.UI.Button)new SerializedObject(toolbar).FindProperty("cutsButton").objectReferenceValue).interactable, Is.False);
            Assert.That(((UnityEngine.UI.Button)new SerializedObject(toolbar).FindProperty("cutsButton").objectReferenceValue).GetComponentInChildren<UnityEngine.UI.Text>(true).text, Is.EqualTo("Cuts"));
            var cuts = root.GetComponentInChildren<QuestCutsPanel>(true);
            Assert.That(cuts.GetComponent<QuestWindow>().Key, Is.EqualTo("Cuts"));
            Assert.That(new SerializedObject(cuts.GetComponent<QuestWindow>()).FindProperty("initiallyOpen").boolValue, Is.False);
            Assert.That(new SerializedObject(toolbar).FindProperty("cuts").objectReferenceValue, Is.SameAs(cuts));
            foreach (string field in new[] { "view", "window", "rowPrefab", "rows", "keypad", "add", "suspend", "suspendLabel" })
                Assert.That(new SerializedObject(cuts).FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            var handles = root.GetComponent<QuestCutHandles>();
            Assert.That(new SerializedObject(pointer).FindProperty("cuts").objectReferenceValue, Is.SameAs(handles));
            foreach (string field in new[] { "view", "panel", "pointer", "policy", "gizmoPrefab" })
                Assert.That(new SerializedObject(handles).FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            Assert.That(new SerializedObject(toolbar.GetComponent<QuestWindowFollower>()).FindProperty("horizontalOnly").boolValue, Is.True);
            Assert.That(new SerializedObject(toolbar.GetComponent<QuestWindowFollower>()).FindProperty("tiltTowardHead").boolValue, Is.True);
        }

        [Test]
        public void RoiWindow_UsesManagedWindowControlsAndSharedScientificProxies()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestBootstrap.prefab");
            var panel = root.GetComponentInChildren<QuestROIPanel>(true);
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.GetComponent<QuestWindow>().Key, Is.EqualTo("ROI"));
            Assert.That(new SerializedObject(panel.GetComponent<QuestWindow>()).FindProperty("initiallyOpen").boolValue, Is.False);
            var fields = new SerializedObject(panel);
            foreach (string name in new[] { "view", "window", "selector", "roiName", "addROI", "removeROI", "addSphere", "suspend", "suspendLabel", "rowPrefab", "rows", "status" })
                Assert.That(fields.FindProperty(name).objectReferenceValue, Is.Not.Null, name);
            var add = (UnityEngine.UI.Button)fields.FindProperty("addROI").objectReferenceValue;
            var suspend = (UnityEngine.UI.Button)fields.FindProperty("suspend").objectReferenceValue;
            var nameField = (QuestTextInputField)fields.FindProperty("roiName").objectReferenceValue;
            var remove = (UnityEngine.UI.Button)fields.FindProperty("removeROI").objectReferenceValue;
            Assert.That(suspend.transform.parent, Is.SameAs(add.transform.parent));
            Assert.That(remove.transform.parent, Is.SameAs(nameField.transform.parent));
            Assert.That(remove.transform.GetSiblingIndex(), Is.EqualTo(nameField.transform.GetSiblingIndex() + 1));
            Assert.That(((UnityEngine.UI.Button)fields.FindProperty("addSphere").objectReferenceValue).transform.parent, Is.SameAs(fields.FindProperty("rows").objectReferenceValue));
            var row = (QuestROISphereRow)fields.FindProperty("rowPrefab").objectReferenceValue;
            Assert.That(row.GetComponentsInChildren<UnityEngine.UI.Toggle>(true), Is.Empty);
            Assert.That(row.GetComponentsInChildren<UnityEngine.UI.Button>(true), Has.Length.EqualTo(1));
            Assert.That(new SerializedObject(row).FindProperty("definition").objectReferenceValue, Is.InstanceOf<UnityEngine.UI.Text>());
            var handles = root.GetComponent<QuestROIHandles>();
            var hs = new SerializedObject(handles);
            foreach (string name in new[] { "view", "panel", "pointer", "cuts", "policy", "feedback" })
                Assert.That(hs.FindProperty(name).objectReferenceValue, Is.Not.Null, name);
            Assert.That(new SerializedObject(root.GetComponentInChildren<QuestPointerInput>(true)).FindProperty("rois").objectReferenceValue, Is.SameAs(handles));
            var toolbar = new SerializedObject(root.GetComponentInChildren<QuestToolbar>(true));
            Assert.That(toolbar.FindProperty("rois").objectReferenceValue, Is.SameAs(panel));
            Assert.That(toolbar.FindProperty("roiButton").objectReferenceValue, Is.Not.Null);
            Assert.That(UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest ROI Window.prefab")).name, Is.EqualTo("Quest Window"));
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/3D/QuestROIProxy.prefab").GetComponent<QuestROISphere>(), Is.Not.Null);
            var icon = AssetDatabase.LoadAssetAtPath<Element>("Assets/Resources/Themes/Quest/Elements/QuestToolbarROIIcon.asset");
            Assert.That(icon.SettingsByState.SelectMany(state => state.Settings).OfType<HBP.Theme.Image>().Distinct().Single().SourceImage, Is.Not.Null);
        }

        [Test]
        public void QuestInputFields_CenterTheirTextVertically()
        {
            foreach (string path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Quest/UI" }).Select(AssetDatabase.GUIDToAssetPath))
            foreach (var input in AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<UnityEngine.UI.InputField>(true))
            {
                Assert.That((int)input.textComponent.alignment / 3, Is.EqualTo(1), path + "/" + input.name);
                if (input.placeholder is UnityEngine.UI.Text text) Assert.That((int)text.alignment / 3, Is.EqualTo(1), path + " placeholder");
            }
        }

        [Test]
        public void CutPrefabs_AuthorAllControlsAndLocalGizmoWithIndependentQuestSettings()
        {
            var row = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Cut Row.prefab").GetComponent<QuestCutRow>();
            foreach (string field in new[] { "title", "orientation", "position", "flip", "minus", "plus", "remove", "custom" })
                Assert.That(new SerializedObject(row).FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            Assert.That(new SerializedObject(row).FindProperty("normalFields").arraySize, Is.EqualTo(3));
            var gizmo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/3D/Quest Cut Gizmo.prefab").GetComponent<QuestCutGizmo>();
            foreach (string field in new[] { "outline", "rail" })
                Assert.That(new SerializedObject(gizmo).FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            Assert.That(gizmo.GetComponentInChildren<Collider>(), Is.Null, "Bounded plane contact is geometric and must not block scientific raycasts.");
            Assert.That(gizmo.transform.Find("Handle"), Is.Null);
            Assert.That(gizmo.transform.Find("Stem"), Is.Null);
            var settings = gizmo.GetComponent<ThemeElement>().Element.SettingsByState.Single().Settings.OfType<QuestCutGizmoSettings>().Single();
            Assert.That(settings.Normal && settings.Hovered && settings.Occupied, Is.True);
            Assert.That(AssetDatabase.GetAssetPath(settings), Does.StartWith("Assets/Resources/Themes/Quest/Settings/"));
        }

        [Test]
        public void BaseDropdown_HasItsSerializedTemplateAndTextReferences()
        {
            var dropdown = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Dropdown.prefab").GetComponent<QuestDropdown>();
            Assert.That(dropdown.template, Is.Not.Null);
            Assert.That(dropdown.captionText, Is.Not.Null);
            Assert.That(dropdown.itemText, Is.Not.Null);
            Assert.That(dropdown.options.Count, Is.EqualTo(3));
        }

        [Test]
        public void Bootstrap_AllWindowsParticipateInPersistenceWithUniqueStableKeys()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestBootstrap.prefab");
            var initial = new SerializedObject(root.GetComponent<QuestWindowsManager>()).FindProperty("initialWindows");
            var managed = Enumerable.Range(0, initial.arraySize).Select(i => initial.GetArrayElementAtIndex(i).objectReferenceValue).Cast<QuestWindow>().ToArray();
            Assert.That(managed, Is.Not.Empty);
            Assert.That(managed.All(window => window != null && !string.IsNullOrWhiteSpace(window.Key)), Is.True, "Every window needs a stable persistence key.");
            Assert.That(managed.Select(window => window.Key).Distinct().Count(), Is.EqualTo(managed.Length), "Window placement keys must be unique.");
            Assert.That(root.GetComponentsInChildren<QuestWindow>(true).All(window => managed.Contains(window)), Is.True, "Every authored window participates in common placement and persistence.");
        }
    }
}
