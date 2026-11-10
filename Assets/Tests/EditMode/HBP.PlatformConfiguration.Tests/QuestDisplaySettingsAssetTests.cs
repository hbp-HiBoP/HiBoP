using System.Linq;
using HBP.Quest;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Tests.PlatformConfiguration
{
    public sealed class QuestDisplaySettingsAssetTests
    {
        [Test]
        public void SettingsAreAnAuthoredManagedWindowWithFourOrderedSections()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Display Settings Window.prefab");
            var window = root.GetComponent<QuestWindow>();
            Assert.That(window.Key, Is.EqualTo("DisplaySettings"));
            Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(root).name, Is.EqualTo("Quest Window"));
            var fields = new SerializedObject(window);
            Assert.That(fields.FindProperty("initiallyOpen").boolValue, Is.False);
            Assert.That(fields.FindProperty("initiallyPinned").boolValue, Is.True);
            var panel = root.GetComponent<QuestDisplaySettingsPanel>();
            var iterator = new SerializedObject(panel).GetIterator();
            while (iterator.NextVisible(true))
                if (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.name != "view")
                    Assert.That(iterator.objectReferenceValue, Is.Not.Null, iterator.propertyPath);
            var rows = root.transform.Find("Background/Content Viewport/Content/Visualizations/Viewport/Rows");
            var sections = rows.Cast<Transform>().Where(t => t.GetComponentInChildren<Text>(true)?.GetComponent<HBP.Theme.ThemeElement>()?.Element.name == "QuestWindowSectionTitle").Select(t => t.GetComponentInChildren<Text>(true).text).ToArray();
            Assert.That(sections, Is.EqualTo(new[] { "Scene", "Activity", "Site", "Atlas" }));
            Assert.That(rows.Find("Colors Row/Colormap"), Is.Not.Null);
            Assert.That(rows.Find("Activity Row").childCount, Is.EqualTo(2));
            Assert.That(rows.Find("Activity Row/Remove Projection").localScale, Is.EqualTo(Vector3.one));
            Assert.That(rows.Find("Size Row/Size Icon").localScale, Is.EqualTo(Vector3.one));
            var size = (Slider)new SerializedObject(panel).FindProperty("siteSize").objectReferenceValue;
            Assert.That(size.minValue, Is.EqualTo(.1f));
            Assert.That(size.maxValue, Is.EqualTo(2));
        }

        [Test]
        public void BootstrapAndToolbarWireSettingsAndPreserveDisplayIdentity()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestBootstrap.prefab");
            var panel = root.GetComponentInChildren<QuestDisplaySettingsPanel>(true);
            Assert.That(panel, Is.Not.Null);
            Assert.That(new SerializedObject(panel).FindProperty("view").objectReferenceValue, Is.SameAs(root.GetComponentInChildren<QuestAnatomyView>(true)));
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Display Settings Window.prefab");
            Assert.That(((RectTransform)panel.transform).sizeDelta, Is.EqualTo(((RectTransform)source.transform).sizeDelta), "The bootstrap inherits the authored settings dimensions.");
            var windows = new SerializedObject(root.GetComponent<QuestWindowsManager>()).FindProperty("initialWindows");
            Assert.That(Enumerable.Range(0, windows.arraySize).Select(i => windows.GetArrayElementAtIndex(i).objectReferenceValue), Does.Contain(panel.GetComponent<QuestWindow>()));
            var toolbar = new SerializedObject(root.GetComponentInChildren<QuestToolbar>(true));
            var button = (Button)toolbar.FindProperty("displaySettingsButton").objectReferenceValue;
            Assert.That(button, Is.Not.Null);
            Assert.That(button.transform.localScale, Is.EqualTo(Vector3.one), "The production button must not cover the Quest interface.");
            var toolbarSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Toolbar.prefab");
            foreach (var control in toolbarSource.transform.Find("Background").GetComponentsInChildren<Button>(true))
                Assert.That(control.transform.localScale, Is.EqualTo(Vector3.one), control.name);
            Assert.That(new SerializedObject(button.GetComponent<QuestToolbarButton>()).FindProperty("windowKey").stringValue, Is.EqualTo("DisplaySettings"));
            Assert.That(button.GetComponentInChildren<Text>(true).text, Is.EqualTo("Display Settings"));
            var display = root.GetComponentInChildren<QuestDisplayPanel>(true);
            Assert.That(display.GetComponent<QuestWindow>().Key, Is.EqualTo("Display"));
            Assert.That(display.transform.Find("Background/Header/Title").GetComponent<Text>().text, Is.EqualTo("Visualizations"));
            Assert.That(display.transform.Find("Background/Content Viewport/Content/Display Actions"), Is.Null);
            Assert.That(new SerializedObject(display).FindProperty("projection"), Is.Null);
        }

        [TestCase("colormap", "HBP.UI.Toolbar.Colormap", "Functional")]
        [TestCase("brainColor", "HBP.UI.Toolbar.BrainColor", "Brain")]
        [TestCase("mriColor", "HBP.UI.Toolbar.CutColor", "MRI")]
        public void PaletteDropdownsUseDesktopTexturesAndAFixedBlackTitle(string field, string desktopType, string title)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Display Settings Window.prefab");
            var panel = new SerializedObject(root.GetComponent<QuestDisplaySettingsPanel>());
            var dropdown = (Dropdown)panel.FindProperty(field).objectReferenceValue;
            var desktop = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/3D/UI/3D Menu.prefab").GetComponentsInChildren<MonoBehaviour>(true).Single(c => c.GetType().FullName == desktopType);
            var source = (Dropdown)new SerializedObject(desktop).FindProperty("m_Dropdown").objectReferenceValue;
            Assert.That(dropdown.options.Select(option => option.image), Is.EqualTo(source.options.Select(option => option.image)));
            Assert.That(dropdown.options.All(option => option.image != null), Is.True);
            Assert.That(dropdown.captionImage, Is.Not.Null);
            Assert.That(dropdown.itemImage, Is.Not.Null);
            Assert.That(dropdown.captionText, Is.Null, "The title must remain fixed when the selected palette changes.");
            Assert.That(dropdown.itemText, Is.Null);
            Assert.That(dropdown.template.GetComponentInChildren<Toggle>(true).graphic, Is.Null, "Palette textures must remain unobscured by the selection checkmark.");
            Assert.That(dropdown.template.Find("Viewport/Content/Item/Item Checkmark").gameObject.activeSelf, Is.False);
            var label = dropdown.transform.Find("Label").GetComponent<Text>();
            Assert.That(label.text, Is.EqualTo(title));
            Assert.That(dropdown.transform.Find("Arrow").gameObject.activeSelf, Is.False);
            Assert.That(label.GetComponent<HBP.Theme.ThemeElement>().Element.SettingsByState.SelectMany(state => state.Settings).OfType<HBP.Theme.Text>().All(setting => setting.FontStyle == FontStyle.Bold), Is.True);
            var desktopLabelTheme = source.GetComponentInChildren<Text>(true).GetComponent<HBP.Theme.ThemeElement>().Element;
            var desktopBlack = desktopLabelTheme.SettingsByState.SelectMany(state => state.Settings).OfType<HBP.Theme.Color>().First().Value;
            Assert.That(label.GetComponent<HBP.Theme.ThemeElement>().Element.SettingsByState.SelectMany(state => state.Settings).OfType<HBP.Theme.Color>().First().Value, Is.EqualTo(desktopBlack));
        }
    }
}
