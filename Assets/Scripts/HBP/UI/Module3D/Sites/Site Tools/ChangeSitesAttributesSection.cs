using Cysharp.Threading.Tasks;
using HBP.Core.Object3D;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.UI.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class ChangeSitesAttributesSection : SiteToolSection
    {
        #region Properties

        [SerializeField] private Toggle m_HighlightToggle;
        [SerializeField] private Toggle m_UnhighlightToggle;
        [SerializeField] private Toggle m_BlacklistToggle;
        [SerializeField] private Toggle m_UnblacklistToggle;
        [SerializeField] private Toggle m_ColorToggle;
        [SerializeField] private Button m_ColorPickerButton;
        [SerializeField] private Image m_ColorPickedImage;
        [SerializeField] private Toggle m_AddLabelToggle;
        [SerializeField] private Toggle m_RemoveLabelToggle;
        [SerializeField] private Toggle m_RemoveAllLabelsToggle;
        [SerializeField] private InputField m_AddLabelInputField;
        [SerializeField] private InputField m_RemoveLabelInputField;
        [SerializeField] private Dropdown m_ScopeDropdown;

        protected override List<Site> Sites
        {
            get
            {
                List<Column3D> columns = m_ScopeDropdown.value == 0 ? new() { Scene.SelectedColumn } : Scene.Columns;
                return ApplyFor switch
                {
                    ApplyFor.FilteredSites => columns.SelectMany(c => c.Sites).Where(s => s.State.IsFiltered && !s.State.IsMasked).ToList(),
                    ApplyFor.AllSites => Scene.SelectedColumn.Sites.FindAll(s => !s.State.IsMasked),
                    _ => throw new ArgumentOutOfRangeException(nameof(ApplyFor), ApplyFor, null),
                };
            }
        }

        static bool m_HighlightToggleValue;
        static bool m_UnhighlightToggleValue;
        static bool m_BlacklistToggleValue;
        static bool m_UnblacklistToggleValue;
        static bool m_ColorToggleValue;
        static Color? m_ColorPickedValue;
        static bool m_AddLabelToggleValue;
        static bool m_RemoveLabelToggleValue;
        static bool m_RemoveAllLabelsToggleValue;
        static string m_AddLabelInputFieldValue;
        static string m_RemoveLabelInputFieldValue;
        static int m_ScopeDropdownValue;

        #endregion

        #region Public Methods

        public override void Initialize()
        {
            base.Initialize();

            m_ColorPickerButton.onClick.AddListener(async () => m_ColorPickedImage.color = await ColorPickerManager.OpenColorPickerAsync(m_ColorPickedImage.color));
        }

        public override async UniTask ApplyAsync()
        {
            var changes = new List<SiteConfigurationChange>();
            var targets = new HashSet<Site>(Sites);
            foreach (Column3D column in Scene.Columns)
            foreach (Site site in column.Sites)
            {
                if (!targets.Contains(site)) continue;
                bool highlighted = m_UnhighlightToggle.isOn ? false : m_HighlightToggle.isOn || site.State.IsHighlighted;
                bool blacklisted = m_UnblacklistToggle.isOn ? false : m_BlacklistToggle.isOn || site.State.IsBlackListed;
                Color color = m_ColorToggle.isOn ? m_ColorPickedImage.color : site.State.Color;
                var labels = new List<string>(site.State.Labels);
                if (m_AddLabelToggle.isOn)
                    foreach (string label in ParseLabels(m_AddLabelInputField.text))
                        if (!labels.Contains(label))
                            labels.Add(label);
                if (m_RemoveLabelToggle.isOn)
                    foreach (string label in ParseLabels(m_RemoveLabelInputField.text))
                        labels.Remove(label);
                if (m_RemoveAllLabelsToggle.isOn) labels.Clear();
                if (highlighted == site.State.IsHighlighted && blacklisted == site.State.IsBlackListed && color == site.State.Color && labels.SequenceEqual(site.State.Labels)) continue;
                changes.Add(new SiteConfigurationChange(column, site.Information.FullID, site.State, new Core.Data.SiteConfiguration(blacklisted, highlighted, color, labels)));
            }

            Base3DScene scene = Scene;
            await LoadingManager.LoadAsync(async (update, token) =>
            {
                await UniTask.SwitchToMainThread(token);
                token.ThrowIfCancellationRequested();
                scene.ApplySiteConfigurationBatch(changes);
            });
        }

        public override void StoreSettings()
        {
            m_HighlightToggleValue = m_HighlightToggle.isOn;
            m_UnhighlightToggleValue = m_UnhighlightToggle.isOn;
            m_BlacklistToggleValue = m_BlacklistToggle.isOn;
            m_UnblacklistToggleValue = m_UnblacklistToggle.isOn;
            m_ColorToggleValue = m_ColorToggle.isOn;
            m_ColorPickedValue = m_ColorPickedImage.color;
            m_AddLabelToggleValue = m_AddLabelToggle.isOn;
            m_RemoveLabelToggleValue = m_RemoveLabelToggle.isOn;
            m_RemoveAllLabelsToggleValue = m_RemoveAllLabelsToggle.isOn;
            m_AddLabelInputFieldValue = m_AddLabelInputField.text;
            m_RemoveLabelInputFieldValue = m_RemoveLabelInputField.text;
            m_ScopeDropdownValue = m_ScopeDropdown.value;
        }

        public override void LoadSettings()
        {
            m_HighlightToggle.isOn = m_HighlightToggleValue;
            m_UnhighlightToggle.isOn = m_UnhighlightToggleValue;
            m_BlacklistToggle.isOn = m_BlacklistToggleValue;
            m_UnblacklistToggle.isOn = m_UnblacklistToggleValue;
            m_ColorToggle.isOn = m_ColorToggleValue;
            if (m_ColorPickedValue != null) m_ColorPickedImage.color = m_ColorPickedValue.Value;
            m_AddLabelToggle.isOn = m_AddLabelToggleValue;
            m_RemoveLabelToggle.isOn = m_RemoveLabelToggleValue;
            m_RemoveAllLabelsToggle.isOn = m_RemoveAllLabelsToggleValue;
            m_AddLabelInputField.text = m_AddLabelInputFieldValue;
            m_RemoveLabelInputField.text = m_RemoveLabelInputFieldValue;
            m_ScopeDropdown.value = m_ScopeDropdownValue;
        }

        #endregion

        #region Private Methods

        private static IEnumerable<string> ParseLabels(string text) => text.Contains(',') ? text.Split(',').Select(label => label.Trim()) : new[] { text };

        #endregion
    }
}
