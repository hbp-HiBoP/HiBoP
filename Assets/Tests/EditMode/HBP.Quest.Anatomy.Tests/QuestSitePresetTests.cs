using HBP.Core.Object3D;
using HBP.Quest;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Quest
{
    public sealed class QuestSitePresetTests
    {
        [Test]
        public void KeepPreservesEveryAttributeAndDoesNotAliasLabels()
        {
            var state = new SiteState();
            state.ApplyState(true, true, Color.blue, new[] { "Existing" });
            var preset = new QuestSitePreset();
            var result = preset.Configure(state);
            Assert.That(preset.HasActions, Is.False);
            Assert.That(result.IsBlacklisted && result.IsHighlighted, Is.True);
            Assert.That(result.Color, Is.EqualTo(Color.blue));
            CollectionAssert.AreEqual(new[] { "Existing" }, result.Labels);
            result.Labels[0] = "Changed";
            Assert.That(state.Labels[0], Is.EqualTo("Existing"));
        }

        [TestCase(QuestPresetBoolean.Keep, true)]
        [TestCase(QuestPresetBoolean.On, true)]
        [TestCase(QuestPresetBoolean.Off, false)]
        public void BooleanInstructionsAssignAbsoluteValues(QuestPresetBoolean instruction, bool expected)
        {
            var state = new SiteState();
            state.ApplyState(true, true, Color.blue, new string[0]);
            var result = new QuestSitePreset { Highlight = instruction, Blacklist = instruction }.Configure(state);
            Assert.That(result.IsHighlighted, Is.EqualTo(expected));
            Assert.That(result.IsBlacklisted, Is.EqualTo(expected));
        }

        [Test]
        public void LabelsCanBeAddedRemovedOrKeptWithoutDuplicatesOrSceneDefinitions()
        {
            var state = new SiteState();
            state.ApplyState(false, false, Color.blue, new[] { "Existing", "Remove", "Keep" });
            var preset = new QuestSitePreset();
            preset.SetLabelAction("Existing", QuestPresetLabelAction.Add);
            preset.SetLabelAction("Remove", QuestPresetLabelAction.Remove);
            preset.SetLabelAction(" New ", QuestPresetLabelAction.Add);
            preset.SetLabelAction("Absent", QuestPresetLabelAction.Remove);
            preset.SetLabelAction("Keep", QuestPresetLabelAction.Add);
            preset.SetLabelAction("Keep", QuestPresetLabelAction.Keep);
            CollectionAssert.AreEqual(new[] { "Existing", "Keep", "New" }, preset.Configure(state).Labels);
            CollectionAssert.AreEqual(new[] { "Existing", "Remove", "Keep" }, state.Labels);
            Assert.That(preset.SetLabelAction("New", QuestPresetLabelAction.Add), Is.False);
            Assert.That(preset.GetLabelAction("Keep"), Is.EqualTo(QuestPresetLabelAction.Keep));
        }

        [Test]
        public void LocalJsonRoundTripRetainsColorAndAbsentLabelInstructions()
        {
            var preset = new QuestSitePreset { Highlight = QuestPresetBoolean.Off, SetColor = true, Color = Color.cyan };
            preset.SetLabelAction("New", QuestPresetLabelAction.Add);
            preset.SetLabelAction("Old", QuestPresetLabelAction.Remove);
            var restored = JsonUtility.FromJson<QuestSitePreset>(JsonUtility.ToJson(preset));
            restored.Validate();
            Assert.That(restored.Highlight, Is.EqualTo(QuestPresetBoolean.Off));
            Assert.That(restored.SetColor, Is.True);
            Assert.That(restored.Color, Is.EqualTo(Color.cyan));
            Assert.That(restored.GetLabelAction("New"), Is.EqualTo(QuestPresetLabelAction.Add));
            Assert.That(restored.GetLabelAction("Old"), Is.EqualTo(QuestPresetLabelAction.Remove));
        }
    }
}
