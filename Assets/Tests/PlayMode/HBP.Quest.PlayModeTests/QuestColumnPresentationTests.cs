#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Quest;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HBP.Tests.Quest
{
    public sealed class QuestColumnPresentationTests
    {
        [Test]
        public void SingleFollowsSelectedColumnAndKeepsPoseWithoutRepeatingSelection()
        {
            using var f = new Fixture();
            f.Select(1);
            f.View.CompletePlacement(f.Presentation);
            f.Columns[1].transform.SetPositionAndRotation(new Vector3(1, 2, 3), Quaternion.Euler(15, 25, 35));
            f.Columns[1].transform.localScale = Vector3.one * 1.7f;
            f.Tick();
            Assert.That(f.View.IsColumnPresented(f.Columns[1]), Is.True);
            Assert.That(f.View.CanInteractWithColumn(f.Columns[0]), Is.False);
            Assert.That(f.Columns[0].Column.BrainMesh.GetComponent<Renderer>().forceRenderingOff, Is.True);
            Assert.That(f.Columns.All(column => column.gameObject.activeSelf), Is.True);
            int selections = 0;
            f.Columns[0].Column.OnSelect.AddListener(() => selections++);
            f.Select(0); // Same path used by a direct canonical selection mutation.
            f.Tick();
            f.Tick();
            Assert.That(f.Columns[0].transform.position, Is.EqualTo(new Vector3(1, 2, 3)));
            Assert.That(f.Columns[0].transform.localScale.x, Is.EqualTo(1.7f));
            f.View.SelectContext(f.Scene, f.Columns[0].Column);
            Assert.That(selections, Is.EqualTo(1));
        }

        [Test]
        public void SplitMergeUsesFrozenTargetAndRegularSubsequentSplit()
        {
            using var f = new Fixture();
            f.Select(0);
            f.View.CompletePlacement(f.Presentation);
            Assert.That(f.View.SetColumnDisplayMode(f.Scene, QuestColumnDisplayMode.Split), Is.True);
            Assert.That(f.Columns.All(column => !f.View.CanInteractWithColumn(column)), Is.True);
            Assert.That(f.View.SetColumnDisplayMode(f.Scene, QuestColumnDisplayMode.Single), Is.False);
            f.Advance(1);
            Assert.That(f.Columns.All(column => f.View.CanInteractWithColumn(column)), Is.True);
            f.Columns[1].transform.SetPositionAndRotation(new Vector3(2, 3, 4), Quaternion.Euler(30, 40, 50));
            f.Columns[1].transform.localScale = Vector3.one * 2;
            f.Select(1);
            Assert.That(f.View.SetColumnDisplayMode(f.Scene, QuestColumnDisplayMode.Single), Is.True);
            f.Advance(.4f);
            f.Select(0);
            f.Advance(.6f);
            f.Tick();
            Assert.That(f.Columns[0].transform.position, Is.EqualTo(new Vector3(2, 3, 4)));
            Assert.That(f.Columns[0].transform.localScale.x, Is.EqualTo(2));
            Assert.That(f.View.IsColumnPresented(f.Columns[1]), Is.False);
            Assert.That(f.View.SetColumnDisplayMode(f.Scene, QuestColumnDisplayMode.Split), Is.True);
            f.Advance(1);
            Assert.That((f.Columns[0].transform.position + f.Columns[1].transform.position) * .5f, Is.EqualTo(new Vector3(2, 3, 4)));
        }

        [Test]
        public void HideFinishesTransitionAndReleasesInteractions()
        {
            using var f = new Fixture();
            f.View.CompletePlacement(f.Presentation);
            int changes = 0;
            f.View.OnPresentationChanging.AddListener(_ => changes++);
            f.View.SetColumnDisplayMode(f.Scene, QuestColumnDisplayMode.Split);
            f.Scene.UpdateVisibleState(false, false);
            typeof(QuestAnatomyView).GetMethod("ApplyVisibility", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(f.View, new object[] { f.Presentation, false });
            Assert.That(f.Presentation.IsTransitioning, Is.False);
            Assert.That(f.Columns.All(column => !f.View.IsColumnPresented(column)), Is.True);
            Assert.That(changes, Is.GreaterThanOrEqualTo(2));
            Assert.That(f.Columns.All(column => column.gameObject.activeSelf), Is.True);
        }

        [Test]
        public async Task HiddenDynamicColumnKeepsAdvancingItsTimeline()
        {
            using var f = new Fixture(dynamicFirst: true);
            f.Select(1);
            f.View.CompletePlacement(f.Presentation);
            f.Tick();
            var timeline = ((Column3DIEEG)f.Columns[0].Column).Timeline;
            timeline.Step = 100;
            timeline.IsPlaying = true;
            await UniTask.Delay(TimeSpan.FromMilliseconds(80));
            await UniTask.NextFrame(PlayerLoopTiming.LastPostLateUpdate);
            Assert.That(f.View.IsColumnPresented(f.Columns[0]), Is.False);
            Assert.That(timeline.CurrentIndex, Is.GreaterThan(0), $"active={f.Columns[0].Column.gameObject.activeInHierarchy}, enabled={f.Columns[0].Column.enabled}, playing={timeline.IsPlaying}, length={timeline.Length}, step={timeline.Step}, dt={Time.deltaTime}");
        }

        [Test]
        public void TitlesUseBoundsWorldUpAndHeadPositionUnderRotationAndScale()
        {
            using var f = new Fixture();
            var column = f.Columns[0];
            column.transform.SetPositionAndRotation(new Vector3(1, 2, 3), Quaternion.Euler(40, 60, 20));
            column.transform.localScale = Vector3.one * 2;
            var head = new Vector3(0, 1, 0);
            column.RefreshTitle(head);
            var title = (Component)new SerializedObject(column).FindProperty("label").objectReferenceValue;
            var frame = column.Column.BrainMesh.transform;
            var expected = frame.TransformPoint(f.Mesh.bounds.center) + Vector3.up * (.5f * 100 + 10) * .002f;
            Assert.That(Vector3.Distance(title.transform.position, expected), Is.LessThan(.0001f));
            Assert.That(Vector3.Dot(title.transform.forward, (title.transform.position - head).normalized), Is.GreaterThan(.999f));
        }

        [Test]
        public void ReplacementRetainsModeAndPosesAndPlacesNewColumns()
        {
            using var previous = new Fixture();
            using var next = new Fixture();
            previous.View.CompletePlacement(previous.Presentation);
            previous.Columns[0].transform.position = new Vector3(1, 2, 3);
            next.Restore(previous.Presentation);
            Assert.That(next.Columns.All(column => column.transform.position == new Vector3(1, 2, 3)), Is.True);
            previous.View.SetColumnDisplayMode(previous.Scene, QuestColumnDisplayMode.Split);
            previous.Advance(1);
            previous.Columns[0].transform.position = new Vector3(4, 5, 6);
            typeof(Column3D).GetProperty("ColumnData").SetValue(next.Columns[1].Column, new AnatomicColumn("New", new BaseConfiguration(), new AnatomicConfiguration(), "new-column"));
            next.Restore(previous.Presentation);
            Assert.That(next.Presentation.DisplayMode, Is.EqualTo(QuestColumnDisplayMode.Split));
            Assert.That(next.Columns[0].transform.position, Is.EqualTo(new Vector3(4, 5, 6)));
            Assert.That(next.Columns[1].transform.position, Is.Not.EqualTo(Vector3.zero));
            Assert.That(next.Columns[0].WorldLayoutBounds().Intersects(next.Columns[1].WorldLayoutBounds()), Is.False);
        }

        [Test]
        public async Task SplitFinishesWithTimeScaleZero()
        {
            using var f = new Fixture();
            float previous = Time.timeScale;
            try
            {
                Time.timeScale = 0;
                f.View.CompletePlacement(f.Presentation);
                f.View.SetColumnDisplayMode(f.Scene, QuestColumnDisplayMode.Split);
                float started = Time.unscaledTime;
                do
                {
                    await UniTask.NextFrame();
                    f.Tick();
                } while (Time.unscaledTime - started < 1.1f);

                Assert.That(f.Presentation.IsTransitioning, Is.False);
            }
            finally
            {
                Time.timeScale = previous;
            }
        }

        [Test]
        public void DisplayControlsSelectColumnsAndLockDuringTransitions()
        {
            using var f = new Fixture();
            f.View.CompletePlacement(f.Presentation);
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Display Window.prefab"));
            try
            {
                root.GetComponent<QuestWindowFollower>().enabled = false;
                root.GetComponent<QuestWindow>().Open();
                var panel = root.GetComponent<QuestDisplayPanel>();
                QuestWindowInteractionTests.Set(panel, "view", f.View);

                void Refresh()
                {
                    typeof(QuestDisplayPanel).GetMethod("Refresh", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(panel, null);
                    foreach (var row in root.GetComponentsInChildren<QuestSceneRow>(true)) RefreshRow(row);
                }

                Refresh();
                var fields = new SerializedObject(root.GetComponentInChildren<QuestSceneRow>(true));
                var selector = (QuestDropdown)fields.FindProperty("columnSelector").objectReferenceValue;
                var separate = (Toggle)fields.FindProperty("separateColumns").objectReferenceValue;
                Assert.That(selector.interactable, Is.True);
                selector.value = 1;
                Assert.That(f.Scene.SelectedColumn, Is.SameAs(f.Columns[1].Column));
                separate.isOn = true;
                Refresh();
                Assert.That(selector.gameObject.activeSelf, Is.True);
                Assert.That(selector.interactable, Is.False);
                Assert.That(separate.interactable, Is.False);
                f.Advance(1);
                Refresh();
                Assert.That(separate.interactable, Is.True);
                Assert.That(selector.interactable, Is.True);
                var poses = f.Columns.Select(column => column.transform.position).ToArray();
                selector.value = 0;
                Assert.That(f.Scene.SelectedColumn, Is.SameAs(f.Columns[0].Column));
                Assert.That(f.Columns.All(column => f.View.IsColumnPresented(column)), Is.True);
                Assert.That(f.Columns.Select(column => column.transform.position), Is.EqualTo(poses));
                separate.isOn = false;
                f.Advance(1);
                Refresh();
                Assert.That(selector.value, Is.EqualTo(0));
                foreach (var column in f.Columns) column.Column.IsSelected = false;
                Refresh();
                Assert.That(selector.options[0].text, Is.EqualTo("Select a column"));
                selector.value = 1;
                Assert.That(f.Scene.SelectedColumn, Is.SameAs(f.Columns[0].Column));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EachRowTargetsItsOwnVisualizationAndLocksOnlyItsTransition()
        {
            using var first = new Fixture();
            using var second = new Fixture();
            first.View.CompletePlacement(first.Presentation);
            QuestSceneTestBinding.Attach(first.View, second.Scene).AddRange(second.Columns);
            var secondPresentation = first.View.Find(second.Scene);
            first.View.CompletePlacement(secondPresentation);
            first.View.SelectContext(first.Scene, first.Columns[0].Column);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Scene Row.prefab");
            var firstRow = Object.Instantiate(prefab).GetComponent<QuestSceneRow>();
            var secondRow = Object.Instantiate(prefab).GetComponent<QuestSceneRow>();
            try
            {
                firstRow.Bind(first.Presentation, null, first.View);
                secondRow.Bind(secondPresentation, null, first.View);
                var firstFields = new SerializedObject(firstRow);
                var secondFields = new SerializedObject(secondRow);
                var firstSelector = (QuestDropdown)firstFields.FindProperty("columnSelector").objectReferenceValue;
                var secondSelector = (QuestDropdown)secondFields.FindProperty("columnSelector").objectReferenceValue;
                var secondSplit = (Toggle)secondFields.FindProperty("separateColumns").objectReferenceValue;
                secondSplit.isOn = true;
                RefreshRow(firstRow);
                RefreshRow(secondRow);
                Assert.That(first.View.Scene, Is.SameAs(first.Scene), "Splitting another row does not require selecting it.");
                Assert.That(first.Presentation.DisplayMode, Is.EqualTo(QuestColumnDisplayMode.Single));
                Assert.That(secondPresentation.DisplayMode, Is.EqualTo(QuestColumnDisplayMode.Split));
                Assert.That(firstSelector.interactable, Is.True);
                Assert.That(secondSelector.interactable, Is.False);
                typeof(QuestAnatomyView).GetMethod("AdvanceTransition", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { secondPresentation, 1f });
                RefreshRow(secondRow);
                secondSelector.value = 1;
                Assert.That(second.Scene.SelectedColumn, Is.SameAs(second.Columns[1].Column));
                Assert.That(first.Scene.SelectedColumn, Is.SameAs(first.Columns[0].Column));
                Assert.That(second.Columns.All(column => first.View.IsColumnPresented(column)), Is.True);
                second.Select(0); // Incoming selection is reflected without a control callback.
                RefreshRow(secondRow);
                Assert.That(secondSelector.value, Is.EqualTo(0));
                second.Scene.UpdateVisibleState(false, false);
                RefreshRow(secondRow);
                Assert.That(secondSelector.interactable, Is.False);
                Assert.That(secondSplit.interactable, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(firstRow.gameObject);
                Object.DestroyImmediate(secondRow.gameObject);
            }
        }

        private static void RefreshRow(QuestSceneRow row) => typeof(QuestSceneRow).GetMethod("Refresh", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(row, null);

        private sealed class Fixture : IDisposable
        {
            private readonly GameObject data = new("Inactive scientific fixture"), root = new("Column presentations");
            public readonly Mesh Mesh;
            public readonly QuestAnatomyView View;
            public readonly Base3DScene Scene;
            public readonly QuestAnatomyView.Presentation Presentation;
            public readonly QuestColumnPresentation[] Columns = new QuestColumnPresentation[2];

            public Fixture(bool dynamicFirst = false)
            {
                data.SetActive(false);
                View = data.AddComponent<QuestAnatomyView>();
                Scene = QuestSceneTestBinding.VisibleScene(data);
                var columns = QuestSceneTestBinding.Attach(View, Scene);
                Presentation = View.Find(Scene);
                Mesh = new Mesh { vertices = new[] { new Vector3(-20, -50, 0), new Vector3(80, -50, 0), new Vector3(80, 50, 0), new Vector3(-20, 50, 0) }, triangles = new[] { 0, 1, 2, 0, 2, 3 } };
                Mesh.RecalculateBounds();
                for (int i = 0; i < 2; i++)
                {
                    var wrapper = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/3D/QuestColumn.prefab"), root.transform);
                    var presentation = wrapper.GetComponent<QuestColumnPresentation>();
                    presentation.enabled = false;
                    var source = new GameObject("Column data");
                    source.transform.SetParent(data.transform, false);
                    Column3D column;
                    Column model;
                    if (i == 0 && dynamicFirst)
                    {
                        column = source.AddComponent<Column3DIEEG>();
                        var ev = new HBP.Core.Data.Event("event", new[] { 1 }, MainSecondaryEnum.Main);
                        var sub = new SubBloc("sub", 0, MainSecondaryEnum.Main, new TimeWindow(0, 3000), new TimeWindow(0, 0), new[] { ev }, Array.Empty<Icon>(), Array.Empty<Treatment>());
                        var bloc = new Bloc("bloc", 0, "", "sub_event_CODE", new[] { sub });
                        var stats = new Dictionary<SubBloc, List<SubBlocEventsStatistics>> { [sub] = new() { new SubBlocEventsStatistics { StatisticsByEvent = new() { [ev] = new EventStatistics() } } } };
                        var ieeg = new IEEGColumn { Name = "Dynamic", Bloc = bloc };
                        ieeg.Data.Timeline = ieeg.Data.ProjectionTimeline = new Timeline(bloc, stats, new Dictionary<SubBloc, int> { [sub] = 0 }, new Frequency(100));
                        model = ieeg;
                    }
                    else
                    {
                        column = source.AddComponent<Column3DAnatomy>();
                        model = new AnatomicColumn("Column " + i, new BaseConfiguration(), new AnatomicConfiguration(), "column-" + i);
                    }

                    typeof(Column3D).GetProperty("ColumnData").SetValue(column, model);
                    var brain = new GameObject("Brain", typeof(MeshFilter), typeof(MeshRenderer));
                    brain.transform.SetParent(source.transform, false);
                    brain.GetComponent<MeshFilter>().sharedMesh = Mesh;
                    typeof(Column3D).GetProperty("BrainMesh").SetValue(column, brain);
                    presentation.Bind(Scene, column);
                    Scene.Columns.Add(column);
                    column.OnSelect.AddListener(() =>
                    {
                        foreach (var other in Scene.Columns)
                            if (other != column)
                                other.IsSelected = false;
                    });
                    columns.Add(presentation);
                    Columns[i] = presentation;
                }

                Select(0);
            }

            public void Select(int index)
            {
                foreach (var column in Columns) column.Column.IsSelected = false;
                Columns[index].Column.IsSelected = true;
            }

            public void Tick() => typeof(QuestAnatomyView).GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(View, null);
            public void Advance(float delta) => typeof(QuestAnatomyView).GetMethod("AdvanceTransition", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { Presentation, delta });
            public void Restore(QuestAnatomyView.Presentation previous) => typeof(QuestAnatomyView).GetMethod("RestorePresentation", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { Presentation, previous });

            public void Dispose()
            {
                QuestSceneTestBinding.Clear(View);
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
                Object.DestroyImmediate(Mesh);
            }
        }
    }
}
#endif
