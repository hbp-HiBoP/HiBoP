using System;
using System.Collections.Generic;
using System.Linq;
using HBP.Core.Data;
using HBP.Core.Object3D;
using UnityEngine;

namespace HBP.Quest
{
    public enum QuestPresetBoolean
    {
        Keep,
        On,
        Off
    }

    public enum QuestPresetLabelAction
    {
        Keep,
        Add,
        Remove
    }

    /// <summary>Local shortcut configuration. Unspecified attributes are preserved.</summary>
    [Serializable]
    public sealed class QuestSitePreset
    {
        public QuestPresetBoolean Highlight, Blacklist;
        public bool SetColor;
        public Color Color = SiteState.DefaultColor;
        public List<LabelInstruction> Labels = new();

        [Serializable]
        public sealed class LabelInstruction
        {
            public string Label;
            public QuestPresetLabelAction Action;
        }

        public bool HasActions => Highlight != QuestPresetBoolean.Keep || Blacklist != QuestPresetBoolean.Keep || SetColor || Labels.Count > 0;

        public QuestPresetLabelAction GetLabelAction(string label) => Labels.FirstOrDefault(value => value.Label == label)?.Action ?? QuestPresetLabelAction.Keep;

        public bool SetLabelAction(string label, QuestPresetLabelAction action)
        {
            label = label?.Trim();
            if (string.IsNullOrEmpty(label) || GetLabelAction(label) == action) return false;
            Labels.RemoveAll(value => value.Label == label);
            if (action != QuestPresetLabelAction.Keep) Labels.Add(new LabelInstruction { Label = label, Action = action });
            return true;
        }

        public SiteConfiguration Configure(SiteState state)
        {
            var labels = state.Labels.ToList();
            foreach (var instruction in Labels)
            {
                if (instruction.Action == QuestPresetLabelAction.Add && !labels.Contains(instruction.Label)) labels.Add(instruction.Label);
                else if (instruction.Action == QuestPresetLabelAction.Remove) labels.RemoveAll(label => label == instruction.Label);
            }

            return new SiteConfiguration(Resolve(Blacklist, state.IsBlackListed), Resolve(Highlight, state.IsHighlighted), SetColor ? Color : state.Color, labels);
        }

        public void Validate()
        {
            if (!Enum.IsDefined(typeof(QuestPresetBoolean), Highlight) || !Enum.IsDefined(typeof(QuestPresetBoolean), Blacklist) || !float.IsFinite(Color.r) || !float.IsFinite(Color.g) || !float.IsFinite(Color.b) || !float.IsFinite(Color.a) || Labels == null)
                throw new ArgumentException("The saved site preset is invalid.");
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var instruction in Labels)
                if (instruction == null || string.IsNullOrWhiteSpace(instruction.Label) || instruction.Label != instruction.Label.Trim() || !names.Add(instruction.Label) || instruction.Action is not (QuestPresetLabelAction.Add or QuestPresetLabelAction.Remove))
                    throw new ArgumentException("The saved site preset contains an invalid label instruction.");
        }

        private static bool Resolve(QuestPresetBoolean value, bool current) => value == QuestPresetBoolean.Keep ? current : value == QuestPresetBoolean.On;
    }
}
