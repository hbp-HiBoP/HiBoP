#if UNITY_EDITOR
using System.Collections.Generic;
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
using Object = UnityEngine.Object;
using Site = HBP.Core.Object3D.Site;

namespace HBP.Tests.Quest
{
    public class QuestSitePanelTests
    {
        [Test]
        public async Task Tables_WrapBothColumnsAtEveryZoomAndReuseRowsWhenSelectionChanges()
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Site Window.prefab"));
            try
            {
                root.GetComponent<QuestSitePanel>().enabled = false;
                root.GetComponent<QuestWindowFollower>().enabled = false;
                root.GetComponent<QuestWindow>().Open();
                var table = root.GetComponentsInChildren<QuestTagTable>(true).First();
                var values = new List<BaseTagValue>
                {
                    new StringTagValue(new StringTag("Short"), "Value"),
                    new StringTagValue(new StringTag(string.Join(" ", Enumerable.Repeat("Long tag name", 8))), string.Join(" ", Enumerable.Repeat("A long description with <literal> markup.", 12))),
                    new StringTagValue(new StringTag(new string('N', 100)), new string('V', 200))
                };
                table.Bind("Patient tags — Test", values);
                foreach (var graphic in table.GetComponentsInChildren<Graphic>(true))
                {
                    var theme = graphic.GetComponent<HBP.Theme.ThemeElement>();
                    Assert.That(theme, Is.Not.Null, graphic.name);
                    Assert.That(theme.Element, Is.Not.Null, graphic.name);
                    Assert.That(graphic.color.r, Is.EqualTo(graphic.color.g).Within(.0001f), graphic.name);
                    Assert.That(graphic.color.g, Is.EqualTo(graphic.color.b).Within(.0001f), graphic.name);
                }

                var themedRows = table.GetComponentsInChildren<QuestTagRow>();
                Assert.That(themedRows[0].GetComponent<HBP.Theme.ThemeElement>().ActualState.name, Is.EqualTo("Alternate"));
                Assert.That(themedRows[1].GetComponent<HBP.Theme.ThemeElement>().ActualState.name, Is.EqualTo("Default"));
                Assert.That(themedRows[0].GetComponent<Image>().color, Is.Not.EqualTo(themedRows[1].GetComponent<Image>().color));
                foreach (int zoom in new[] { 50, 100, 150 })
                {
                    root.GetComponentInChildren<QuestWindowContentZoom>(true).SetZoomPercent(zoom);
                    await UniTask.NextFrame();
                    Canvas.ForceUpdateCanvases();
                    var rows = table.GetComponentsInChildren<QuestTagRow>();
                    Assert.That(rows.Length, Is.EqualTo(3));
                    foreach (var row in rows)
                    {
                        var cells = row.GetComponentsInChildren<Text>();
                        Assert.That(cells[1].rectTransform.rect.width / cells[0].rectTransform.rect.width, Is.EqualTo(1.5f).Within(.02f));
                        foreach (var cell in cells)
                        {
                            Assert.That(cell.font, Is.Not.Null);
                            Assert.That(cell.fontSize, Is.EqualTo(28));
                            Assert.That(cell.supportRichText, Is.False);
                            Assert.That(cell.rectTransform.rect.height + .1f, Is.GreaterThanOrEqualTo(cell.preferredHeight), cell.text);
                        }
                    }

                    Assert.That(((RectTransform)rows[1].transform).rect.height, Is.GreaterThan(((RectTransform)rows[0].transform).rect.height));
                }

                var reused = table.GetComponentsInChildren<QuestTagRow>().First();
                table.Bind("Site tags", new List<BaseTagValue> { new StringTagValue(new StringTag("Other"), "Changed") });
                Assert.That(table.GetComponentsInChildren<QuestTagRow>().Single(), Is.SameAs(reused));
                Assert.That(reused.GetComponentsInChildren<Text>()[1].text, Is.EqualTo("Changed"));
                table.Bind("Site tags", new List<BaseTagValue>());
                Assert.That(table.GetComponentsInChildren<QuestTagRow>(), Is.Empty);
                Assert.That(table.transform.Find("Empty").gameObject.activeSelf, Is.True);
                table.Bind("", null);
                Assert.That(table.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Panel_InspectsInitiallySelectedHiddenSiteAndUpdatesAfterSelectionAndVisibilityChanges()
        {
            var data = new GameObject("Site panel scientific fixture");
            data.SetActive(false);
            var scene = QuestSceneTestBinding.VisibleScene(data);
            var manager = scene.gameObject.AddComponent<ImplantationManager>();
            Set(scene, "m_ImplantationManager", manager);
            var column = data.AddComponent<Column3DAnatomy>();
            typeof(Column3D).GetProperty("ColumnData").SetValue(column, new AnatomicColumn());
            typeof(Column3D).GetProperty("Sites").SetValue(column, new List<Site>());
            scene.Columns.Add(column);
            column.IsSelected = true;
            var view = data.AddComponent<QuestAnatomyView>();
            QuestSceneTestBinding.Attach(view, scene);
            var patient = new Patient { Name = "Patient", Tags = new List<BaseTagValue> { new StringTagValue(new StringTag("Patient tag"), "Patient value") } };
            var a = CreateSite("A1", patient);
            var b = CreateSite("B2", patient);
            column.Sites.Add(a);
            column.Sites.Add(b);
            typeof(Column3D).GetProperty("SelectedSite").SetValue(column, a);
            a.IsActive = false;
            a.State.IsOutOfROI = true;
            scene.ShowAllSites = false;
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Site Window.prefab"));
            try
            {
                var panel = root.GetComponent<QuestSitePanel>();
                panel.enabled = false;
                Set(panel, "view", view);
                root.GetComponent<QuestWindow>().Open();
                Refresh(panel);
                var fields = new SerializedObject(panel);
                var identity = (Text)fields.FindProperty("identity").objectReferenceValue;
                Assert.That(identity.text, Does.Contain("A1"));
                Assert.That(a.gameObject.activeSelf, Is.False, "Inspecting a hidden selection must not reveal it.");
                var table = (QuestTagTable)fields.FindProperty("siteTags").objectReferenceValue;
                Assert.That(table.GetComponentInChildren<QuestTagRow>().GetComponentsInChildren<Text>()[1].text, Is.EqualTo("A1 value"));
                typeof(Column3D).GetProperty("SelectedSite").SetValue(column, b);
                Refresh(panel);
                Assert.That(identity.text, Does.Contain("B2"));
                Assert.That(table.GetComponentInChildren<QuestTagRow>().GetComponentsInChildren<Text>()[1].text, Is.EqualTo("B2 value"));
                b.IsActive = false;
                Refresh(panel);
                Assert.That(identity.text, Does.Contain("B2"));
                typeof(Column3D).GetProperty("SelectedSite").SetValue(column, null);
                Refresh(panel);
                Assert.That(identity.text, Does.Contain("Select a site"));
                Assert.That(table.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
                QuestSceneTestBinding.Clear(view);
                Object.DestroyImmediate(data);
                Object.DestroyImmediate(a.gameObject);
                Object.DestroyImmediate(b.gameObject);
            }
        }

        private static Site CreateSite(string name, Patient patient)
        {
            var site = new GameObject(name, typeof(Site)).GetComponent<Site>();
            site.Information = new SiteInformation
            {
                Name = name, Patient = patient,
                SiteData = new HBP.Core.Data.Site { Name = name, Tags = new List<BaseTagValue> { new StringTagValue(new StringTag("Site tag"), name + " value") } }
            };
            return site;
        }

        private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static void Refresh(QuestSitePanel panel) => typeof(QuestSitePanel).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(panel, null);
    }
}
#endif
