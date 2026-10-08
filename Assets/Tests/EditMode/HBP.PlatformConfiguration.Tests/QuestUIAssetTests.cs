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
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestColumn.prefab");
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
        public void ResizeDemo_WiresOptionalHandleWithoutChangingExistingWindows()
        {
            const string demoPath = "Assets/Prefabs/Quest/UI/Quest Resizable Window Demo.prefab";
            var demo = AssetDatabase.LoadAssetAtPath<GameObject>(demoPath);
            var handles = demo.GetComponentsInChildren<QuestWindowResizer>(true);
            Assert.That(handles.Select(h => h.Edge), Is.EquivalentTo(System.Enum.GetValues(typeof(QuestWindowResizer.WindowEdge))));
            foreach (var resizer in handles)
            {
                var fields = new SerializedObject(resizer);
                Assert.That(fields.FindProperty("window").objectReferenceValue, Is.EqualTo(demo.GetComponent<QuestWindow>()));
                Assert.That(fields.FindProperty("minimumSize").vector2Value, Is.EqualTo(new Vector2(500, 350)));
                Assert.That(fields.FindProperty("maximumSize").vector2Value, Is.EqualTo(new Vector2(1200, 1000)));
                Assert.That(resizer.GetComponent<UnityEngine.UI.Image>().raycastTarget, Is.True);
                Assert.That(AssetDatabase.GetAssetPath(resizer.GetComponent<ThemeElement>().Element), Is.EqualTo("Assets/Resources/Themes/Quest/Elements/QuestResizeHandle.asset"));
            }

            foreach (string path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Quest" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                if (path == demoPath) continue;
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentInChildren<QuestWindowResizer>(true), Is.Null, path);
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

            var column = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestColumn.prefab");
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
            var fields = new SerializedObject(manager);
            Assert.That(fields.FindProperty("head").objectReferenceValue, Is.Not.Null);
            Assert.That(fields.FindProperty("uiCamera").objectReferenceValue, Is.Not.Null);
            var initial = fields.FindProperty("initialWindows");
            Assert.That(initial.arraySize, Is.EqualTo(3));
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
                Assert.That(windowFields.FindProperty("raycaster").objectReferenceValue, Is.TypeOf<TrackedDeviceRaycaster>(), window.Key);
                Assert.That(window.GetComponent<QuestWindowFollower>(), Is.Not.Null);
                foreach (var theme in window.GetComponentsInChildren<ThemeElement>(true))
                    Assert.That(AssetDatabase.GetAssetPath(theme.Element), Does.StartWith("Assets/Resources/Themes/Quest/Elements/"));
                foreach (var component in window.GetComponentsInChildren<MonoBehaviour>(true))
                    Assert.That(component.GetType().Namespace, Does.Not.StartWith("HBP.UI"), component.GetType().Name);
            }

            var toolbar = root.GetComponentInChildren<QuestToolbar>(true);
            Assert.That(((UnityEngine.UI.Button)new SerializedObject(toolbar).FindProperty("cutsButton").objectReferenceValue).interactable, Is.False);
            Assert.That(new SerializedObject(toolbar.GetComponent<QuestWindowFollower>()).FindProperty("horizontalOnly").boolValue, Is.True);
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
    }
}
