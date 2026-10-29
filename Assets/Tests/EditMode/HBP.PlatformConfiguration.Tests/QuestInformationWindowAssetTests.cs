using System;
using System.Linq;
using System.Reflection;
using HBP.Core.Object3D;
using HBP.Quest;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Tests.PlatformConfiguration
{
    public sealed class QuestInformationWindowAssetTests
    {
        [TestCase("Site", 640, 700)]
        [TestCase("Signals", 1000, 800)]
        public void WindowsAreAuthoredClosedAndRegisteredInTheBootstrap(string key, int width, int height)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Quest/UI/Quest {key} Window.prefab");
            Assert.That(PrefabUtility.GetPrefabAssetType(asset), Is.EqualTo(PrefabAssetType.Variant));
            Assert.That(AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(asset)), Is.EqualTo("Assets/Prefabs/Quest/UI/Quest Window.prefab"));
            var window = asset.GetComponent<QuestWindow>();
            var fields = new SerializedObject(window);
            Assert.That(window.Key, Is.EqualTo(key));
            Assert.That(fields.FindProperty("initiallyOpen").boolValue, Is.False);
            Assert.That(((RectTransform)asset.transform).sizeDelta, Is.EqualTo(new Vector2(width, height)));
            var bootstrap = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestBootstrap.prefab");
            var manager = new SerializedObject(bootstrap.GetComponent<QuestWindowsManager>()).FindProperty("initialWindows");
            Assert.That(Enumerable.Range(0, manager.arraySize).Select(i => manager.GetArrayElementAtIndex(i).objectReferenceValue).OfType<QuestWindow>().Count(w => w.Key == key), Is.EqualTo(1));
            var toolbar = bootstrap.GetComponentInChildren<QuestToolbar>(true);
            Assert.That(new SerializedObject(toolbar).FindProperty(key == "Site" ? "siteButton" : "signalsButton").objectReferenceValue, Is.Not.Null);
        }

        [Test]
        public void AllInformationPanelReferencesAreSerializedOnTheirSceneInstances()
        {
            var bootstrap = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestBootstrap.prefab");
            var components = bootstrap.GetComponentsInChildren<MonoBehaviour>(true).Where(c => c is QuestSitePanel || c is QuestSignalsPanel);
            foreach (var component in components)
            {
                var so = new SerializedObject(component);
                var p = so.GetIterator();
                while (p.NextVisible(true))
                    if (p.propertyType == SerializedPropertyType.ObjectReference && p.name != "m_Script")
                        Assert.That(p.objectReferenceValue, Is.Not.Null, component.GetType().Name + "." + p.propertyPath);
            }

            foreach (var list in bootstrap.GetComponentsInChildren<QuestLabelList>(true))
            {
                var so = new SerializedObject(list);
                Assert.That(so.FindProperty("scroll").objectReferenceValue, Is.Not.Null);
                Assert.That(so.FindProperty("rowPrefab").objectReferenceValue, Is.Not.Null);
            }

            foreach (var scroll in bootstrap.GetComponentsInChildren<ScrollRect>(true).Where(s => s.GetComponentInParent<QuestSitePanel>() != null || s.GetComponentInParent<QuestSignalsPanel>() != null))
            {
                Assert.That(scroll.content, Is.Not.Null, scroll.name);
                Assert.That(scroll.viewport, Is.Not.Null, scroll.name);
                Assert.That(scroll.horizontal, Is.False, scroll.name);
                Assert.That(scroll.vertical, Is.True, scroll.name);
            }

            foreach (string path in new[] { "Assets/Prefabs/Quest/UI/Quest Signals Window.prefab", "Assets/Prefabs/Quest/UI/Quest Signal Matrix Cell.prefab" })
            foreach (var graphic in AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<Graphic>(true))
                Assert.That(graphic.GetComponent<CanvasRenderer>(), Is.Not.Null, path + "/" + graphic.name);
        }

        [Test]
        public void SiteSummaryKeepsWholeLabelsAndFitsItsEllipsis()
        {
            var fit = typeof(QuestLabelSummary).GetMethod("Fit", BindingFlags.Static | BindingFlags.NonPublic);
            var labels = new[] { "Alpha", "Beta", "Gamma" };
            string Fit(float width) => (string)fit.Invoke(null, new object[] { labels, width, new Func<string, float>(value => value.Length) });
            Assert.That(Fit(18), Is.EqualTo("Alpha, Beta, Gamma"));
            Assert.That(Fit(14), Is.EqualTo("Alpha, Beta, …"));
            Assert.That(Fit(7), Is.EqualTo("…"));
            Assert.That((string)fit.Invoke(null, new object[] { Array.Empty<string>(), 100f, new Func<string, float>(value => value.Length) }), Is.EqualTo("No labels"));
        }

        [Test]
        public void ColorPickerKeepsDraftsUntilApplyAndCancelsWithoutMutation()
        {
            var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Color Picker.prefab"));
            try
            {
                var picker = root.GetComponent<QuestColorPicker>();
                typeof(QuestColorPicker).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(picker, null);
                int mutations = 0;
                Color selected = default;
                var fields = new SerializedObject(picker);
                var control = fields.FindProperty("control").objectReferenceValue;
                picker.Open(Color.red, value =>
                {
                    mutations++;
                    selected = value;
                });
                control.GetType().GetProperty("CurrentColor").SetValue(control, Color.cyan);
                Assert.That(mutations, Is.Zero);
                ((Button)fields.FindProperty("apply").objectReferenceValue).onClick.Invoke();
                Assert.That(mutations, Is.EqualTo(1));
                Assert.That(selected, Is.EqualTo(Color.cyan));
                picker.Open(Color.red, _ => mutations++);
                control.GetType().GetProperty("CurrentColor").SetValue(control, Color.green);
                ((Button)fields.FindProperty("cancel").objectReferenceValue).onClick.Invoke();
                Assert.That(mutations, Is.EqualTo(1));
                picker.Open(Color.red, value =>
                {
                    mutations++;
                    selected = value;
                });
                var palette = (Button)fields.FindProperty("palette").GetArrayElementAtIndex(0).objectReferenceValue;
                palette.onClick.Invoke();
                Assert.That(mutations, Is.EqualTo(2));
                Assert.That(selected, Is.EqualTo(palette.GetComponent<Image>().color));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SitePanelsAreLateralAndLabelCatalogueKeepsItsOpenOrder()
        {
            var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Site Window.prefab"));
            try
            {
                var fields = new SerializedObject(root.GetComponent<QuestSitePanel>());
                foreach (string field in new[] { "labelPicker", "colorPicker" })
                {
                    var value = fields.FindProperty(field).objectReferenceValue;
                    var rect = (RectTransform)(value is GameObject go ? go.transform : ((Component)value).transform);
                    Assert.That(rect.parent, Is.EqualTo(root.transform));
                    Assert.That(rect.anchorMin.x, Is.EqualTo(1));
                    Assert.That(rect.pivot.x, Is.Zero);
                }

                foreach (string name in new[] { "identity", "patientTags", "siteTags" })
                {
                    var text = (Text)fields.FindProperty(name).objectReferenceValue;
                    text.GetComponent<HBP.Theme.ThemeElement>().Set();
                    Assert.That(text.supportRichText, Is.True, name);
                }

                var input = (InputField)fields.FindProperty("labelName").objectReferenceValue;
                Assert.That(input, Is.InstanceOf<QuestTextInputField>());
                Assert.That(input.readOnly, Is.False);
                Assert.That(input.shouldActivateOnSelect, Is.True);
                Assert.That(input.textComponent, Is.Not.Null);
                var picker = (QuestColorPicker)fields.FindProperty("colorPicker").objectReferenceValue;
                var pickerContent = picker.transform.Find("Content");
                Assert.That(pickerContent.Find("ColorTitle"), Is.Null);
                Assert.That(pickerContent.GetComponentInChildren<InputField>(true), Is.InstanceOf<QuestTextInputField>());
                Assert.That(((GameObject)fields.FindProperty("labelPicker").objectReferenceValue).GetComponent<Image>().color, Is.EqualTo(pickerContent.GetComponent<Image>().color));
                picker.Open(Color.red, null);
                foreach (var theme in picker.GetComponentsInChildren<HBP.Theme.ThemeElement>(true)) theme.Set();
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)pickerContent);
                var pickerBounds = ((RectTransform)pickerContent).rect;
                foreach (RectTransform child in pickerContent)
                {
                    var corners = new Vector3[4];
                    child.GetLocalCorners(corners);
                    foreach (var corner in corners)
                    {
                        var point = pickerContent.InverseTransformPoint(child.TransformPoint(corner));
                        Assert.That(point.x, Is.InRange(pickerBounds.xMin + 11.9f, pickerBounds.xMax - 11.9f), child.name);
                        Assert.That(point.y, Is.InRange(pickerBounds.yMin + 11.9f, pickerBounds.yMax - 11.9f), child.name);
                    }
                }

                var list = (QuestLabelList)fields.FindProperty("labelList").objectReferenceValue;
                list.Bind(new[] { "Alpha", "Gamma" }, _ => false, (_, __) => { });
                list.Include(new[] { "Alpha", "Beta", "Gamma", "Delta" });
                CollectionAssert.AreEqual(new[] { "Alpha", "Gamma", "Beta", "Delta" }, (string[])typeof(QuestLabelList).GetField("values", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(list));
                Assert.That(root.transform.Find("Background/Content Viewport/Content/SiteActivity").gameObject.activeSelf, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SignalsUsesDesktopGraphAxesAndPreservesReceivedErrorBands()
        {
            var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Signal Graph.prefab"));
            object receivedData = null;
            try
            {
                var view = root.GetComponent<QuestSignalGraphView>();
                var graph = root.GetComponent<HBP.UI.Informations.Graphs.Graph>();
                var bind = typeof(QuestSignalGraphView).GetMethod("Bind");
                var type = bind.GetParameters()[0].ParameterType;
                var state = Activator.CreateInstance(type);
                type.GetField("XMin").SetValue(state, -100f);
                type.GetField("XMax").SetValue(state, 500f);
                type.GetField("YMin").SetValue(state, -10f);
                type.GetField("YMax").SetValue(state, 10f);
                var curveType = type.GetField("Curves").FieldType.GetElementType();
                var curve = Activator.CreateInstance(curveType);
                curveType.GetField("Id").SetValue(curve, "site");
                curveType.GetField("Name").SetValue(curve, "A1");
                curveType.GetField("Enabled").SetValue(curve, true);
                curveType.GetField("HasShape").SetValue(curve, true);
                curveType.GetField("Resource").SetValue(curve, "samples");
                var curves = Array.CreateInstance(curveType, 1);
                curves.SetValue(curve, 0);
                type.GetField("Curves").SetValue(state, curves);
                var resources = new System.Collections.Generic.Dictionary<string, float[]> { ["samples"] = new[] { -100f, 1f, 4f, 200f, 2f, 6f, 500f, 3f, 8f } };
                bind.Invoke(view, new[] { state, resources });
                receivedData = graph.Curves[0].GetType().GetProperty("Data").GetValue(graph.Curves[0]);
                CollectionAssert.AreEqual(new[] { 4f, 6f, 8f }, (float[])receivedData.GetType().GetProperty("Shapes").GetValue(receivedData));
                bind.Invoke(view, new[] { state, resources });
                Assert.That(graph.Curves[0].GetType().GetProperty("Data").GetValue(graph.Curves[0]), Is.SameAs(receivedData));
                var axes = root.GetComponentsInChildren<HBP.UI.Informations.Graphs.Axis>();
                Assert.That(axes, Has.Length.EqualTo(2));
                Assert.That(axes.SelectMany(axis => axis.GetComponentsInChildren<HBP.UI.Informations.Graphs.MajorTickMark>()).Count(tick => tick.Label.Length > 0), Is.GreaterThan(4));
                Assert.That(root.GetComponentsInChildren<Toggle>().All(toggle => !toggle.enabled), Is.True);
                curveType.GetField("Color").SetValue(curve, new[] { .2f, .9f, .4f, 1f });
                curveType.GetField("Thickness").SetValue(curve, 7f);
                bind.Invoke(view, new[] { state, resources });
                var styledData = graph.Curves[0].GetType().GetProperty("Data").GetValue(graph.Curves[0]);
                Assert.That(styledData, Is.Not.SameAs(receivedData));
                var dataField = typeof(HBP.UI.Informations.Graphs.Curve).GetField("m_Data", BindingFlags.Instance | BindingFlags.NonPublic);
                var nativeCurve = root.GetComponentsInChildren<HBP.UI.Informations.Graphs.Curve>(true).Single(value => ReferenceEquals(dataField.GetValue(value), styledData));
                var renderer = (Graphic)typeof(HBP.UI.Informations.Graphs.Curve).GetField("m_CurveRenderer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(nativeCurve);
                Assert.That(renderer.color, Is.EqualTo(new Color(.2f, .9f, .4f, 1f)));
                Assert.That((float)renderer.GetType().GetProperty("LineThickness").GetValue(renderer), Is.EqualTo(7f));
                var shape = (Graphic)typeof(HBP.UI.Informations.Graphs.ShapedCurve).GetField("m_ShapeRenderer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(nativeCurve);
                Assert.That(shape.color, Is.EqualTo(new Color(.2f, .9f, .4f, .5f)));
                view.Clear();
                Assert.That((UnityEngine.Object)styledData == null, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            Assert.That((UnityEngine.Object)receivedData == null, Is.True, "Closing the graph must release its decoded curve copies.");
        }

        [TestCase(0), TestCase(1), TestCase(2), TestCase(3)]
        public void SignalsMatrixKeepsBlocksSitesAndAlignedTimeWindows(int format)
        {
            var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Signal Matrix Grid.prefab"));
            try
            {
                var grid = root.GetComponent<QuestSignalMatrixGrid>();
                ((RectTransform)root.transform).sizeDelta = new Vector2(1000, 500);
                var bind = typeof(QuestSignalMatrixGrid).GetMethod("Bind");
                var type = bind.GetParameters()[0].ParameterType;
                var state = Activator.CreateInstance(type);
                type.GetField("Format").SetValue(state, format);
                type.GetField("TrialHeight").SetValue(state, 30f);
                type.GetField("TrialRatio").SetValue(state, .1f);
                type.GetField("BlocRatio").SetValue(state, .25f);
                type.GetField("ProtocolRatio").SetValue(state, .7f);
                type.GetField("Colormap").SetValue(state, new byte[] { 0, 0, 255, 255, 255, 255, 255, 255, 255, 0, 0, 255 });
                var rowType = type.GetField("Matrices").FieldType.GetElementType();
                var cellType = rowType.GetField("Cells").FieldType.GetElementType();
                var rows = Array.CreateInstance(rowType, 4);
                for (int block = 0; block < 2; block++)
                for (int channel = 0; channel < 2; channel++)
                {
                    var row = Activator.CreateInstance(rowType);
                    foreach (var pair in new[] { ("Group", "data"), ("GroupTitle", "Recording"), ("BlocId", "block" + block), ("BlocTitle", "Repeated block title"), ("ChannelId", "site" + channel), ("ChannelTitle", "A" + channel) }) rowType.GetField(pair.Item1).SetValue(row, pair.Item2);
                    rowType.GetField("Min").SetValue(row, -10f);
                    rowType.GetField("Max").SetValue(row, 10f);
                    rowType.GetField("Selected").SetValue(row, new[] { true, false });
                    var cells = Array.CreateInstance(cellType, 2);
                    for (int index = 0; index < 2; index++)
                    {
                        var cell = Activator.CreateInstance(cellType);
                        cellType.GetField("WindowStart").SetValue(cell, index == 0 ? -100f : -200f);
                        cellType.GetField("WindowEnd").SetValue(cell, index == 0 ? 500f : 300f);
                        if (index == 0)
                        {
                            cellType.GetField("Resource").SetValue(cell, "samples");
                            cellType.GetField("Rows").SetValue(cell, 2);
                            cellType.GetField("Columns").SetValue(cell, 4);
                            cellType.GetField("Start").SetValue(cell, -100f);
                            cellType.GetField("End").SetValue(cell, 500f);
                        }

                        cells.SetValue(cell, index);
                    }

                    rowType.GetField("Cells").SetValue(row, cells);
                    rows.SetValue(row, block * 2 + channel);
                }

                type.GetField("Matrices").SetValue(state, rows);
                bind.Invoke(grid, new[] { state, new System.Collections.Generic.Dictionary<string, float[]> { ["samples"] = new[] { 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f } } });
                for (int pass = 0; pass < 4; pass++)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)root.transform);
                    foreach (var group in root.GetComponentsInChildren<QuestSignalMatrixGroup>()) typeof(QuestSignalMatrixGroup).GetMethod("OnRectTransformDimensionsChange", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(group, null);
                    foreach (var channel in root.GetComponentsInChildren<QuestSignalMatrixRow>()) typeof(QuestSignalMatrixRow).GetMethod("OnRectTransformDimensionsChange", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(channel, null);
                }

                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)root.transform);
                var blocs = root.GetComponentsInChildren<QuestSignalMatrixBloc>();
                Assert.That(blocs, Has.Length.EqualTo(2), "Identical names must not merge distinct protocol blocks.");
                foreach (var bloc in blocs)
                {
                    var channels = bloc.GetComponentsInChildren<QuestSignalMatrixRow>();
                    Assert.That(channels, Has.Length.EqualTo(2));
                    Assert.That(channels[0].transform.parent, Is.SameAs(channels[1].transform.parent));
                    Assert.That(channels[0].transform.position.x, Is.LessThan(channels[1].transform.position.x));
                    Assert.That(channels[0].transform.position.y, Is.EqualTo(channels[1].transform.position.y).Within(.1f));
                    foreach (var channel in channels)
                    {
                        float width = ((RectTransform)channel.transform).rect.width;
                        float expectedHeight = format switch { 0 => 60f, 1 => .1f * width * 2, 2 => .25f * width, _ => 10f };
                        Assert.That(channel.GetComponent<LayoutElement>().preferredHeight, Is.EqualTo(expectedHeight).Within(.1f));
                        var cells = channel.GetComponentsInChildren<QuestSignalMatrixCell>();
                        Assert.That(cells, Has.Length.EqualTo(2), "Missing sub-blocks must retain their aligned filler cell.");
                        Assert.That(((RectTransform)cells[0].transform).rect.width / ((RectTransform)cells[1].transform).rect.width, Is.EqualTo(600f / 500).Within(.01f));
                    }
                }

                var textures = root.GetComponentsInChildren<RawImage>().Select(image => image.texture).Where(texture => texture != null).ToArray();
                Assert.That(textures.Length, Is.GreaterThanOrEqualTo(5));
                grid.Clear();
                Assert.That(textures.All(texture => texture == null), Is.True, "Closing the matrix grid must release tiles and its colormap.");
                rowType.GetField("Selected").SetValue(rows.GetValue(0), new bool[16000]);
                try
                {
                    bind.Invoke(grid, new[] { state, new System.Collections.Generic.Dictionary<string, float[]> { ["samples"] = new float[8] } });
                    Assert.Fail("Oversized overlay should fail before rendering.");
                }
                catch (TargetInvocationException exception)
                {
                    Assert.That(exception.InnerException, Is.TypeOf<System.IO.InvalidDataException>());
                }

                grid.Clear();
                Assert.That(root.GetComponentsInChildren<QuestSignalMatrixGroup>(true), Is.Empty, "A rendering failure must not orphan a partly constructed group.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GraphBandUsesTheSameFullThicknessAsDesktop()
        {
            var root = new GameObject("Graph band check", typeof(RectTransform), typeof(CanvasRenderer), typeof(QuestSignalGraphGraphic));
            try
            {
                ((RectTransform)root.transform).sizeDelta = new Vector2(100, 100);
                var graphic = root.GetComponent<QuestSignalGraphGraphic>();
                var bind = typeof(QuestSignalGraphGraphic).GetMethod("Bind");
                var graphType = bind.GetParameters()[0].ParameterType;
                var graph = Activator.CreateInstance(graphType);
                graphType.GetField("XMax").SetValue(graph, 1f);
                graphType.GetField("YMin").SetValue(graph, -10f);
                graphType.GetField("YMax").SetValue(graph, 10f);
                var curveType = graphType.GetField("Curves").FieldType.GetElementType();
                var curve = Activator.CreateInstance(curveType);
                curveType.GetField("Enabled").SetValue(curve, true);
                curveType.GetField("HasShape").SetValue(curve, true);
                curveType.GetField("Resource").SetValue(curve, "samples");
                var curves = Array.CreateInstance(curveType, 1);
                curves.SetValue(curve, 0);
                graphType.GetField("Curves").SetValue(graph, curves);
                bind.Invoke(graphic, new object[] { graph, new System.Collections.Generic.Dictionary<string, float[]> { ["samples"] = new[] { 0f, 0f, 4f, 1f, 0f, 4f } } });
                using var vertices = new VertexHelper();
                typeof(QuestSignalGraphGraphic).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(VertexHelper) }, null).Invoke(graphic, new object[] { vertices });
                var vertex = new UIVertex();
                vertices.PopulateUIVertex(ref vertex, 40);
                Assert.That(vertex.position.y, Is.EqualTo(-10f).Within(.001f));
                vertices.PopulateUIVertex(ref vertex, 41);
                Assert.That(vertex.position.y, Is.EqualTo(10f).Within(.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void LabelCatalogueCountsSharedLabelsAndIgnoresUnrelatedStateChanges()
        {
            var index = (QuestSceneLabelIndex)Activator.CreateInstance(typeof(QuestSceneLabelIndex), true);
            var update = typeof(QuestSceneLabelIndex).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            var a = new SiteState();
            var b = new SiteState();
            a.AddLabel("Language");
            b.AddLabel("Language");
            b.AddLabel("Motor");
            try
            {
                update.Invoke(index, new object[] { a });
                update.Invoke(index, new object[] { b });
                CollectionAssert.AreEqual(new[] { "Language", "Motor" }, index.Snapshot());
                int revision = index.Revision;
                a.IsHighlighted = true;
                update.Invoke(index, new object[] { a });
                Assert.That(index.Revision, Is.EqualTo(revision));
                a.RemoveLabel("Language");
                update.Invoke(index, new object[] { a });
                CollectionAssert.AreEqual(new[] { "Language", "Motor" }, index.Snapshot());
                b.RemoveLabel("Language");
                update.Invoke(index, new object[] { b });
                CollectionAssert.AreEqual(new[] { "Motor" }, index.Snapshot());
            }
            finally
            {
                index.Dispose();
            }
        }
    }
}
