using System;
using System.Collections.Generic;
using System.Linq;
using HBP.Core.Data;
using HBP.Core.Preferences;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    public sealed class QuestTagTable : MonoBehaviour
    {
        [SerializeField] private Text heading, empty;
        [SerializeField] private GameObject columnHeaders;
        [SerializeField] private Transform content;
        [SerializeField] private QuestTagRow rowTemplate;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private Button settings, selectAll, deselectAll;
        [SerializeField] private GameObject choicePrefab;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private ScrollRect settingsScroll;
        private readonly List<QuestTagRow> rows = new();
        private readonly List<QuestLabelChoice> choices = new();
        private readonly HashSet<string> hidden = new(StringComparer.Ordinal);
        private IReadOnlyList<BaseTagValue> values = Array.Empty<BaseTagValue>();
        private BaseTag[] available = Array.Empty<BaseTag>();
        private bool siteScope;
        internal event Action<QuestTagTable> SettingsOpening;
        internal bool OwnsSettingsScroll(ScrollRect candidate) => settingsScroll == candidate;

        private void Awake()
        {
            settings.onClick.AddListener(() =>
            {
                if (settingsPanel.activeSelf) CloseSettings();
                else OpenSettings();
            });
            selectAll.onClick.AddListener(() => SetAll(true));
            deselectAll.onClick.AddListener(() => SetAll(false));
            CloseSettings();
            Render();
        }

        public void Bind(string title, IReadOnlyList<BaseTagValue> tags, bool forSite = false)
        {
            gameObject.SetActive(tags != null);
            if (tags == null)
            {
                values = Array.Empty<BaseTagValue>();
                CloseSettings();
                return;
            }

            heading.text = title;
            bool changed = !ReferenceEquals(values, tags);
            values = tags;
            siteScope = forSite;
            Render();
            if (changed) ScrollToTop(scroll);
        }

        private void OpenSettings()
        {
            SettingsOpening?.Invoke(this);
            RefreshAvailable();
            settingsPanel.SetActive(true);
            settingsPanel.transform.SetAsLastSibling();
            RenderChoices();
            ScrollToTop(settingsScroll);
        }

        public void CloseSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        private void RefreshAvailable()
        {
            TagCollection definitions = PersistentDataManager.IsInitialized ? PersistentDataManager.Tags : null;
            IEnumerable<BaseTag> catalog = definitions == null ? Enumerable.Empty<BaseTag>() : definitions.GeneralTags.Concat(siteScope ? definitions.SitesTags : definitions.PatientsTags);
            available = catalog.Concat(values.Select(value => value.Tag)).GroupBy(tag => tag.ID, StringComparer.Ordinal).Select(group => group.First()).ToArray();
        }

        private void SetAll(bool displayed)
        {
            foreach (var tag in available) SetDisplayed(tag.ID, displayed);
            Render();
            RenderChoices();
        }

        private void SetDisplayed(string id, bool displayed)
        {
            if (displayed) hidden.Remove(id);
            else hidden.Add(id);
        }

        private void Render()
        {
            var displayed = values.Where(value => !hidden.Contains(value.Tag.ID)).ToArray();
            columnHeaders.SetActive(displayed.Length != 0);
            bool hasContent = displayed.Length != 0;
            empty.gameObject.SetActive(!hasContent);
            scroll.gameObject.SetActive(hasContent);
            while (rows.Count < displayed.Length) rows.Add(Instantiate(rowTemplate, content, false));
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].gameObject.SetActive(i < displayed.Length);
                if (i < displayed.Length) rows[i].Bind(displayed[i].Tag.Name, displayed[i].DisplayableValue, i % 2 == 0);
            }
        }

        private void RenderChoices()
        {
            selectAll.interactable = deselectAll.interactable = available.Length != 0;
            while (choices.Count < available.Length)
                choices.Add(Instantiate(choicePrefab, settingsScroll.content, false).GetComponent<QuestLabelChoice>());
            for (int i = 0; i < choices.Count; i++)
            {
                bool visible = i < available.Length;
                choices[i].gameObject.SetActive(visible);
                if (!visible) continue;
                var tag = available[i];
                choices[i].Bind(tag.Name, !hidden.Contains(tag.ID), (_, value) =>
                {
                    SetDisplayed(tag.ID, value);
                    Render();
                });
            }
        }

        private static void ScrollToTop(ScrollRect scroll)
        {
            scroll.StopMovement();
            if (!scroll.isActiveAndEnabled) return;
            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            scroll.verticalNormalizedPosition = 1;
        }
    }
}
