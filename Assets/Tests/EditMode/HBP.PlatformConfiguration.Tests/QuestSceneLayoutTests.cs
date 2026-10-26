using System.Collections.Generic;
using HBP.Quest;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Tests.PlatformConfiguration
{
    public sealed class QuestSceneLayoutTests
    {
        private static readonly Rect Area = new(-.5f, -.5f, 1, 1);
        private static readonly Rect Brain = new(-.1f, -.1f, .2f, .2f);

        [Test]
        public void FreeCenter_AndFullViewBothUseCenter()
        {
            Assert.That(QuestSceneLayout.FindOffset(Brain, Area, new Rect[0]), Is.EqualTo(Vector2.zero));
            Assert.That(QuestSceneLayout.FindOffset(Brain, Area, new[] { Area }), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void SearchUsesBothDimensionsAndChoosesNearestFreeGridPoint()
        {
            var obstacles = new[] { new Rect(-.5f, -.1f, 1, .2f) };
            Vector2 offset = QuestSceneLayout.FindOffset(Brain, Area, obstacles);
            Assert.That(offset.x, Is.Zero);
            Assert.That(Mathf.Abs(offset.y), Is.EqualTo(.25f).Within(.0001f));
            var moved = new Rect(Brain.position + offset, Brain.size);
            Assert.That(moved.Overlaps(obstacles[0]), Is.False);
        }

        [Test]
        public void SequentialReservationsDoNotOverlap()
        {
            var occupied = new List<Rect>();
            for (int i = 0; i < 4; i++)
            {
                Vector2 offset = QuestSceneLayout.FindOffset(Brain, Area, occupied);
                var placed = new Rect(Brain.position + offset, Brain.size);
                foreach (var previous in occupied) Assert.That(placed.Overlaps(previous), Is.False);
                occupied.Add(placed);
            }
        }

        [Test]
        public void SearchValidatesReprojectedCandidateRatherThanLinearOffset()
        {
            Vector2 offset = QuestSceneLayout.FindOffset(Brain, Area, new[] { Brain }, value => new Rect(Brain.position + value * 2, Brain.size));
            Assert.That(offset.magnitude, Is.EqualTo(.15f).Within(.0001f));
        }

        [Test]
        public void ProjectionUsesHeadPoseAndIgnoresObjectsBehindIt()
        {
            var pose = new Pose(new Vector3(2, 1, 3), Quaternion.Euler(15, 40, 0));
            var projection = QuestSceneLayout.Projection(pose, null);
            Assert.That(projection.TryProject(new Bounds(pose.position + pose.rotation * Vector3.forward * .65f, Vector3.one * .1f), out var rect), Is.True);
            Assert.That(rect.Contains(Vector2.zero), Is.True);
            Assert.That(projection.TryProject(new Bounds(pose.position - pose.rotation * Vector3.forward, Vector3.one * .1f), out _), Is.False);
        }

        [Test]
        public void DisplayAndTitleReferencesAreAuthoredInPrefabs()
        {
            var display = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Display Window.prefab");
            var fields = new SerializedObject(display.GetComponent<QuestDisplayPanel>());
            Assert.That(fields.FindProperty("columnSelector"), Is.Null);
            Assert.That(display.transform.Find("Background/Content/Column Header"), Is.Null);
            Assert.That(display.transform.Find("Background/Content/Separate Columns"), Is.Null);
            var bootstrap = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestBootstrap.prefab");
            var connected = new SerializedObject(bootstrap.GetComponentInChildren<QuestDisplayPanel>(true));
            foreach (string field in new[] { "session", "view", "rows", "rowPrefab" })
                Assert.That(connected.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            var row = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Scene Row.prefab");
            var rowFields = new SerializedObject(row.GetComponent<QuestSceneRow>());
            Assert.That(rowFields.FindProperty("select").objectReferenceValue, Is.TypeOf<Button>());
            Assert.That(rowFields.FindProperty("columnSelector").objectReferenceValue, Is.TypeOf<QuestDropdown>());
            var split = (Toggle)rowFields.FindProperty("separateColumns").objectReferenceValue;
            var visibility = (Toggle)rowFields.FindProperty("visibility").objectReferenceValue;
            Assert.That(split.graphic, Is.Not.Null);
            Assert.That(split.targetGraphic, Is.Not.Null);
            Assert.That(split.colors, Is.EqualTo(visibility.colors));
            Assert.That(split.transform.Find("Icon").gameObject.activeSelf, Is.True);
            Assert.That(split.transform.Find("Icon").GetComponent<Image>().sprite, Is.SameAs(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/3D Buttons/Scene/StandardViews.png")));
            Assert.That(split.transform.Find("Label").gameObject.activeSelf, Is.False);
            Assert.That(row.transform.Find("Column Controls/Selected Column Prefix").GetComponent<Text>().text, Is.EqualTo("Selected column"));
            var column = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/3D/QuestColumn.prefab");
            var label = (Component)new SerializedObject(column.GetComponent<QuestColumnPresentation>()).FindProperty("label").objectReferenceValue;
            Assert.That(label.GetType().FullName, Is.EqualTo("TMPro.TextMeshPro"));
            Assert.That(new SerializedObject(label).FindProperty("m_fontSize").floatValue, Is.EqualTo(80));
            Assert.That(((RectTransform)label.transform).sizeDelta, Is.EqualTo(new Vector2(400, 100)));
            Assert.That(label.transform.localScale, Is.EqualTo(Vector3.one * .001f));
        }
    }
}
