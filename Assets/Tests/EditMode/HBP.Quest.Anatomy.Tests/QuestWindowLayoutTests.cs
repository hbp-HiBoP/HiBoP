using System;
using System.IO;
using System.Text.RegularExpressions;
using HBP.Quest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HBP.Tests.Quest
{
    public class QuestWindowLayoutTests
    {
        private string folder, path;

        [SetUp]
        public void SetUp()
        {
            folder = Path.Combine(Path.GetTempPath(), "HiBoP-window-layout-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            path = Path.Combine(folder, "layout.json");
        }

        [TearDown]
        public void TearDown() => Directory.Delete(folder, true);

        [Test]
        public void Layout_RoundTripsRelativePosesAndKeepsUnknownWindowKeys()
        {
            var store = new QuestWindowLayoutStore(path);
            Assert.That(store.TryGet("new-window", out _), Is.False);
            var pose = new Pose(new Vector3(-.6f, .2f, 1.3f), Quaternion.Euler(10, -25, 0));
            store.Set("new-window", pose);
            store.Save();
            string first = File.ReadAllText(path);
            Assert.That(first, Does.Not.Contain("$type"));
            Assert.That(first, Does.Contain("\n"));
            var reopened = new QuestWindowLayoutStore(path);
            Assert.That(reopened.TryGet("new-window", out var restored), Is.True);
            Assert.That(Vector3.Distance(pose.position, restored.position), Is.LessThan(.00001f));
            Assert.That(Quaternion.Angle(pose.rotation, restored.rotation), Is.LessThan(.01f));
            reopened.Set("another-future-window", Pose.identity);
            reopened.Save(); // Exercise atomic replacement of an existing file too.
            var again = new QuestWindowLayoutStore(path);
            Assert.That(again.TryGet("new-window", out _), Is.True);
            Assert.That(again.TryGet("another-future-window", out _), Is.True);
            Assert.That(Directory.GetFiles(folder).Length, Is.EqualTo(1));
        }

        [TestCase("{broken")]
        [TestCase("{\"Version\":1}")]
        [TestCase("{\"Version\":3,\"Windows\":{}}")]
        public void Layout_InvalidDocumentFallsBackWithoutWritingOnLoad(string json)
        {
            File.WriteAllText(path, json);
            LogAssert.Expect(LogType.Warning, new Regex("Unable to load Quest window layout"));
            var store = new QuestWindowLayoutStore(path);
            Assert.That(store.TryGet("Window", out _), Is.False);
            Assert.That(File.ReadAllText(path), Is.EqualTo(json));
            store.Set("Window", Pose.identity);
            store.Save();
            if (json.Contains("\"Version\":3")) Assert.That(File.ReadAllText(path), Is.EqualTo(json), "Never overwrite a newer format.");
            else Assert.That(new QuestWindowLayoutStore(path).TryGet("Window", out _), Is.True, "A deliberate move can recover from a malformed document.");
        }

        [Test]
        public void Layout_InvalidEntryDoesNotDiscardOtherPlacements()
        {
            File.WriteAllText(path, "{\"Version\":1,\"Windows\":{\"Bad\":{\"Position\":[0,0,1],\"Rotation\":[0,0,0,0]},\"Good\":{\"Position\":[1,2,3],\"Rotation\":[0,0,0,1]}}}");
            LogAssert.Expect(LogType.Warning, "Ignoring invalid Quest window placement 'Bad'.");
            var store = new QuestWindowLayoutStore(path);
            Assert.That(store.TryGet("Bad", out _), Is.False);
            Assert.That(store.TryGet("Good", out var pose), Is.True);
            Assert.That(pose.position, Is.EqualTo(new Vector3(1, 2, 3)));
            Assert.Throws<ArgumentException>(() => store.Set("Bad", new Pose(new Vector3(float.NaN, 0, 0), Quaternion.identity)));
            Assert.Throws<ArgumentException>(() => store.Set("Bad", new Pose(Vector3.zero, default)));
        }

        [Test]
        public void WindowState_RoundTripsAllGenericPropertiesAndPoseUpdatesKeepThem()
        {
            var store = new QuestWindowLayoutStore(path);
            store.SetState("Window", new QuestWindowLayoutStore.WindowState(Pose.identity, new Vector2(1450, 920), 130, false, false, 2));
            store.Save();
            var reopened = new QuestWindowLayoutStore(path);
            Assert.That(reopened.TryGetState("Window", out var state), Is.True);
            Assert.That(state.Size, Is.EqualTo(new Vector2(1450, 920)));
            Assert.That(state.ZoomPercent, Is.EqualTo(130));
            Assert.That(state.Pinned, Is.False);
            Assert.That(state.Open, Is.False);
            Assert.That(state.Order, Is.EqualTo(2));
            reopened.Set("Window", new Pose(Vector3.one, Quaternion.identity));
            reopened.Save();
            Assert.That(new QuestWindowLayoutStore(path).TryGetState("Window", out state), Is.True);
            Assert.That(state.Pose.position, Is.EqualTo(Vector3.one));
            Assert.That(state.Size, Is.EqualTo(new Vector2(1450, 920)));
            Assert.That(state.ZoomPercent, Is.EqualTo(130));
            Assert.That(state.Pinned, Is.False);
            Assert.That(state.Open, Is.False);
            Assert.That(state.Order, Is.EqualTo(2));
        }

        [Test]
        public void VersionOneLayout_KeepsPlacementsAndUsesPrefabDefaultsForNewProperties()
        {
            const string legacy = "{\"Version\":1,\"Windows\":{\"Old\":{\"Position\":[-0.6,0.2,1.3],\"Rotation\":[0,0,0,1]}}}";
            File.WriteAllText(path, legacy);
            var store = new QuestWindowLayoutStore(path);
            Assert.That(store.TryGetState("Old", out var state), Is.True);
            Assert.That(state.Pose.position, Is.EqualTo(new Vector3(-.6f, .2f, 1.3f)));
            Assert.That(state.Size, Is.Null);
            Assert.That(state.ZoomPercent, Is.Null);
            Assert.That(state.Pinned, Is.Null);
            Assert.That(state.Open, Is.Null);
            Assert.That(File.ReadAllText(path), Is.EqualTo(legacy));
            store.Save();
            Assert.That(File.ReadAllText(path), Does.Contain("\"Version\": 2"));
            Assert.That(new QuestWindowLayoutStore(path).TryGet("Old", out var restored), Is.True);
            Assert.That(restored.position, Is.EqualTo(state.Pose.position));
        }

        [Test]
        public void InvalidOptionalSettings_FallBackWithoutDiscardingThePoseOrPinState()
        {
            File.WriteAllText(path, "{\"Version\":2,\"Windows\":{\"Window\":{\"Position\":[1,2,3],\"Rotation\":[0,0,0,1],\"Size\":[-1,800],\"ZoomPercent\":999,\"Pinned\":false,\"Open\":true,\"Order\":-1}}}");
            var store = new QuestWindowLayoutStore(path);
            Assert.That(store.TryGetState("Window", out var state), Is.True);
            Assert.That(state.Pose.position, Is.EqualTo(new Vector3(1, 2, 3)));
            Assert.That(state.Size, Is.Null);
            Assert.That(state.ZoomPercent, Is.Null);
            Assert.That(state.Order, Is.Null);
            Assert.That(state.Pinned, Is.False);
            Assert.That(state.Open, Is.True);
        }

        [Test]
        public void Initialization_WaitsForWearTrackingAndSettlingAndResetsOnInterruption()
        {
            var gate = new QuestWorkspaceInitialization();
            var pose = new Pose(new Vector3(0, 1.6f, 0), Quaternion.Euler(25, 30, 0));
            for (int i = 0; i < 30; i++) Assert.That(gate.Step(1, false, pose), Is.False, "A tracked headset on a desk must never initialize the workspace.");
            Assert.That(gate.Step(.5f, true, pose), Is.False);
            Assert.That(gate.Step(.5f, true, pose), Is.False);
            Assert.That(gate.Step(.5f, false, pose), Is.False, "Removing the headset or losing tracking/focus restarts settling.");
            for (int i = 0; i < 8; i++) Assert.That(gate.Step(.25f, true, pose), Is.False);
            Assert.That(gate.Step(.25f, true, pose), Is.True);
        }

        [Test]
        public void Initialization_BoundsWaitingForMovingUsersAndRejectsVerticalHeading()
        {
            var gate = new QuestWorkspaceInitialization();
            for (int i = 0; i < 20; i++)
                Assert.That(gate.Step(.25f, true, new Pose(Vector3.right * i * .1f, Quaternion.identity)), Is.False);
            Assert.That(gate.Step(.25f, true, new Pose(Vector3.right * 2, Quaternion.identity)), Is.True);
            gate = new QuestWorkspaceInitialization();
            Assert.That(gate.Step(10, true, new Pose(Vector3.zero, Quaternion.Euler(90, 0, 0))), Is.False);
        }

        [Test]
        public void Initialization_FirstSampleAndResumeDoNotCountTimeSpentBeforeWear()
        {
            var gate = new QuestWorkspaceInitialization();
            Assert.That(gate.Step(30, true, Pose.identity), Is.False);
            Assert.That(gate.Step(1, true, Pose.identity), Is.False);
            gate.Reset(); // Lifecycle notifications must reset even if no Update runs while suspended.
            Assert.That(gate.Step(30, true, Pose.identity), Is.False);
            Assert.That(gate.Step(1, true, Pose.identity), Is.False);
            Assert.That(gate.Step(1, true, Pose.identity), Is.True);
        }
    }
}
