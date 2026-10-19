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
