using HBP.Core.Preferences;
using HBP.UI.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class TagImportPreferencesSubModifier : SubModifier<TagImportPreferences>
    {
        [SerializeField] InputField m_TrueValues;
        [SerializeField] InputField m_FalseValues;
        [SerializeField] InputField m_IgnoredValues;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                m_TrueValues.interactable = value;
                m_FalseValues.interactable = value;
                m_IgnoredValues.interactable = value;
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            m_TrueValues.onValueChanged.AddListener(value => Object.TrueValues = Parse(value));
            m_FalseValues.onValueChanged.AddListener(value => Object.FalseValues = Parse(value));
            m_IgnoredValues.onValueChanged.AddListener(value => Object.IgnoredValues = Parse(value));
        }

        protected override void SetFields(TagImportPreferences objectToDisplay)
        {
            base.SetFields(objectToDisplay);
            m_TrueValues.text = Format(objectToDisplay.TrueValues);
            m_FalseValues.text = Format(objectToDisplay.FalseValues);
            m_IgnoredValues.text = Format(objectToDisplay.IgnoredValues);
        }

        private static List<string> Parse(string value)
        {
            return (value ?? string.Empty).Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(token => token.Trim()).Where(token => token.Length > 0).ToList();
        }

        private static string Format(IEnumerable<string> values)
        {
            return string.Join("\n", values ?? Enumerable.Empty<string>());
        }
    }
}
