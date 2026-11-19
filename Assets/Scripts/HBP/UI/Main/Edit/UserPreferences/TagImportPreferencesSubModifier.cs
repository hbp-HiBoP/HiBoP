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
            Bind(m_TrueValues, values => Object.TrueValues = values);
            Bind(m_FalseValues, values => Object.FalseValues = values);
            Bind(m_IgnoredValues, values => Object.IgnoredValues = values);
        }

        protected override void SetFields(TagImportPreferences objectToDisplay)
        {
            base.SetFields(objectToDisplay);
            m_TrueValues.SetTextWithoutNotify(Format(objectToDisplay.TrueValues));
            m_FalseValues.SetTextWithoutNotify(Format(objectToDisplay.FalseValues));
            m_IgnoredValues.SetTextWithoutNotify(Format(objectToDisplay.IgnoredValues));
        }

        private static void Bind(InputField input, Action<List<string>> setValues)
        {
            input.onValueChanged.AddListener(value => setValues(Parse(value)));
            input.onEndEdit.AddListener(value =>
            {
                List<string> values = Parse(value);
                setValues(values);
                input.SetTextWithoutNotify(Format(values));
            });
        }

        private static List<string> Parse(string value)
        {
            return Normalize((value ?? string.Empty).Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)).ToList();
        }

        private static IEnumerable<string> Normalize(IEnumerable<string> values)
        {
            return (values ?? Enumerable.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static string Format(IEnumerable<string> values)
        {
            return string.Join(",", Normalize(values));
        }
    }
}
