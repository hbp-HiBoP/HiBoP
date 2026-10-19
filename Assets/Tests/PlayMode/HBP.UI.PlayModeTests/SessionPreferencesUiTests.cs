using System.Reflection;
using HBP.Core.Enums;
using HBP.Core.Preferences;
using HBP.UI.Main;
using HBP.UI.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using DataManager = HBP.Core.Data.DataManager;

namespace HBP.Tests.PlayMode.UI
{
    public sealed class SessionPreferencesUiTests
    {
        [Test]
        public void TagParsingRefreshKeepsValuesSeparatedWithoutChangingTheDraft()
        {
            var prefab = Resources.Load<GameObject>("Prefabs/UI/Windows/User preferences window");
            var modifier = Object.Instantiate(prefab.GetComponentInChildren<TagImportPreferencesSubModifier>(true));
            modifier.gameObject.SetActive(false);
            try
            {
                var preferences = new TagImportPreferences();
                preferences.TrueValues.Add(" TRUE ");
                var originalValues = preferences.TrueValues.ToArray();
                var input = modifier.transform.Find("True values/InputField").GetComponent<InputField>();
                int changeCount = 0;
                input.onValueChanged.AddListener(_ => changeCount++);

                modifier.Object = preferences;
                modifier.Refresh();
                modifier.Refresh();

                Assert.That(input.text, Is.EqualTo("true,yes"));
                Assert.That(preferences.TrueValues, Is.EqualTo(originalValues));
                Assert.That(changeCount, Is.Zero);
                Assert.That(modifier.transform.Find("Section Title/Label").GetComponent<Text>().text, Is.EqualTo("Tags parsing"));
                Assert.That(modifier.transform.childCount, Is.EqualTo(4));
                foreach (string label in new[] { "True values", "False values", "Ignored values" })
                {
                    var row = modifier.transform.Find(label);
                    Assert.That(row.Find("Prefix/Label").GetComponent<Text>().text, Is.EqualTo(label));
                    Assert.That(row.Find("InputField").GetComponent<Tooltip>().Text, Does.Contain("\nMatching ignores case and surrounding whitespace."));
                }
            }
            finally
            {
                Object.DestroyImmediate(modifier.gameObject);
            }
        }

        [TestCase("True values", " yes ,YES, true, ,yes, ", "yes,true")]
        [TestCase("False values", " no ,NO,false, ,no, ", "no,false")]
        [TestCase("Ignored values", " n/a ,N/A, -, ,-, ", "n/a,-")]
        public void TagParsingNormalizesOnEndEditAndKeepsTheCurrentDraft(string label, string entered, string expected)
        {
            var prefab = Resources.Load<GameObject>("Prefabs/UI/Windows/User preferences window");
            var modifier = Object.Instantiate(prefab.GetComponentInChildren<TagImportPreferencesSubModifier>(true));
            modifier.gameObject.SetActive(false);
            try
            {
                var firstDraft = new TagImportPreferences();
                modifier.Object = firstDraft;
                var currentDraft = new TagImportPreferences();
                modifier.Object = currentDraft;
                var input = modifier.transform.Find(label + "/InputField").GetComponent<InputField>();
                input.text = entered;
                Assert.That(input.text, Is.EqualTo(entered), "Typing must not rewrite the field before editing ends.");

                input.onEndEdit.Invoke(input.text);
                Assert.That(input.text, Is.EqualTo(expected));
                var property = typeof(TagImportPreferences).GetProperty(label.Split(' ')[0] + "Values");
                Assert.That(property.GetValue(currentDraft), Is.EqualTo(expected.Split(',')));
                Assert.That(property.GetValue(firstDraft), Is.EqualTo(property.GetValue(new TagImportPreferences())));
                modifier.Refresh();
                Assert.That(input.text, Is.EqualTo(expected));
                Assert.That(currentDraft.CreatePolicy(), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(modifier.gameObject);
            }
        }

        [Test]
        public void AuthoredPreferencesControlsKeepNormalizationInDraftWithoutQuestAtlasControls()
        {
            var prefab = Resources.Load<GameObject>("Prefabs/UI/Windows/User preferences window");
            Assert.That(prefab, Is.Not.Null);
            var originalNormalization = DataManager.DefaultNormalization;
            var originalAveraging = DataManager.DefaultAveraging;
            var selectionRoot = SelectionManager.IsInitialized ? null : new GameObject("Preferences UI selection manager");
            if (selectionRoot != null) selectionRoot.AddComponent<SelectionManager>();
            var root = Object.Instantiate(prefab);
            root.SetActive(false);
            try
            {
                var atlas = root.GetComponentInChildren<AtlasesPreferencesSubModifier>(true);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                Assert.That(atlas, Is.Not.Null);
                foreach (var child in atlas.GetComponentsInChildren<Transform>(true))
                    Assert.That(child.name, Is.Not.EqualTo("Quest Atlas Retry").And.Not.EqualTo("Session Atlas Status"));
                var eeg = root.GetComponentInChildren<EEGPreferencesSubModifier>(true);
                eeg.Object = new EEGPreferences();
                var dropdown = (Dropdown)typeof(EEGPreferencesSubModifier).GetField("m_EEGNormalizationDropdown", flags).GetValue(eeg);
                dropdown.value = (int)NormalizationType.Protocol;
                Assert.That(eeg.RequestedNormalization, Is.EqualTo(NormalizationType.Protocol));
                Assert.That(DataManager.DefaultNormalization, Is.EqualTo(NormalizationType.None));
            }
            finally
            {
                Object.DestroyImmediate(root);
                if (selectionRoot != null) Object.DestroyImmediate(selectionRoot);
                DataManager.DefaultNormalization = originalNormalization;
                DataManager.DefaultAveraging = originalAveraging;
            }
        }
    }
}
