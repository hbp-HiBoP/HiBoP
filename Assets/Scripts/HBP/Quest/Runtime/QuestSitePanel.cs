using System;
using System.Linq;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using HBP.Sync;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    public sealed class QuestSitePanel : MonoBehaviour
    {
        [SerializeField] private QuestAnatomyView view;
        [SerializeField] private QuestAnatomySession session;
        [SerializeField] private Button openPatient;
        [SerializeField] private QuestWindow window;
        [SerializeField] private Text identity, error;
        [SerializeField] private QuestTagTable patientTags, siteTags;
        [SerializeField] private Button highlighted, blacklisted, color, chooseLabels, newLabel, closeLabels;
        [SerializeField] private HBP.Theme.ThemeElement highlightedIcon, blacklistedIcon;
        [SerializeField] private HBP.Theme.State activeState;
        [SerializeField] private Image colorSample;
        [SerializeField] private QuestColorPicker colorPicker;
        [SerializeField] private GameObject labelPicker;
        [SerializeField] private QuestLabelList labelList;
        [SerializeField] private QuestLabelSummary labelSummary;
        [SerializeField] private InputField labelName;
        [SerializeField] private QuestSitePresetPanel preset;
        private Base3DScene scene;
        private Column3D column;
        private Site site;
        private QuestSceneLabelIndex index;
        private bool dirty, lastReady;
        private int catalogRevision = -1;

        private void Awake()
        {
            openPatient.onClick.AddListener(() =>
            {
                if (CanOpenPatient) session.RequestOpenPatient();
            });
            highlighted.onClick.AddListener(() => Change(() => site.State.IsHighlighted = !site.State.IsHighlighted));
            blacklisted.onClick.AddListener(() => Change(() => site.State.IsBlackListed = !site.State.IsBlackListed));
            color.onClick.AddListener(OpenColor);
            chooseLabels.onClick.AddListener(OpenLabels);
            closeLabels.onClick.AddListener(CloseLabels);
            labelName.onValueChanged.AddListener(_ => RefreshAddButton());
            if (labelName is QuestTextInputField questInput) questInput.KeyboardError += SetError;
            newLabel.onClick.AddListener(AddLabel);
            window.OnChanged.AddListener(OnWindowChanged);
            patientTags.SettingsOpening += OnTagSettingsOpening;
            siteTags.SettingsOpening += OnTagSettingsOpening;
            Module3DMain.OnSelectColumn.AddListener(OnSelectedColumn);
            if (view != null)
            {
                view.OnContextSelected.AddListener(OnContextSelected);
            }

            CancelDrafts();
            dirty = true;
        }

        internal bool Ready => site != null && view != null && view.Scene == scene && scene.SelectedColumn == column && column.SelectedSite == site && QuestAnatomySession.CanInteractWith(scene) && scene.IsVisible;
        internal Base3DScene CurrentScene => scene;
        internal Column3D CurrentColumn => column;
        internal Site CurrentSite => site;
        internal int LabelCatalogueRevision => index?.Revision ?? -1;
        internal string[] SceneLabels => index?.Snapshot() ?? Array.Empty<string>();
        private bool CanOpenPatient => Ready && view.Find(scene) is { IsPlaced: true, IsTransitioning: false } && session != null && session.CanOpenPatient;

        private bool Change(Action action)
        {
            if (!Ready) return false;
            try
            {
                action();
                SetError("");
                dirty = true;
                return true;
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
            {
                SetError(exception.Message);
                dirty = true;
                return false;
            }
        }

        private void SetLabel(string label, bool add)
        {
            Change(() =>
            {
                var labels = site.State.Labels.ToList();
                if (add && !labels.Contains(label)) labels.Add(label);
                if (!add) labels.Remove(label);
                _ = new SetSiteLabels(new ColumnId(column.ColumnData.ID), new SiteId(site.Information.FullID), labels);
                if (add) site.State.AddLabel(label);
                else site.State.RemoveLabel(label);
            });
            labelList.Refresh();
        }

        private void AddLabel()
        {
            string value = labelName.text.Trim();
            if (value.Length == 0) return;
            bool added = Change(() =>
            {
                var labels = site.State.Labels.ToList();
                if (!labels.Contains(value)) labels.Add(value);
                _ = new SetSiteLabels(new ColumnId(column.ColumnData.ID), new SiteId(site.Information.FullID), labels);
                site.State.AddLabel(value);
            });
            if (added)
            {
                labelName.SetTextWithoutNotify("");
                labelList.Include(new[] { value });
                labelList.Refresh();
                RefreshAddButton();
            }
        }

        private void RefreshAddButton() => newLabel.interactable = Ready && !string.IsNullOrWhiteSpace(labelName.text);

        private void OpenColor()
        {
            if (!Ready) return;
            preset.Close();
            CloseTagSettings();
            CloseLabels();
            var target = site;
            var targetColumn = column;
            colorPicker.Open(site.State.Color, value =>
            {
                if (site == target && column == targetColumn) Change(() => target.State.Color = value);
            });
        }

        private void OpenLabels()
        {
            if (!Ready) return;
            preset.Close();
            if (labelPicker.activeSelf)
            {
                CloseLabels();
                return;
            }

            colorPicker.Cancel();
            CloseTagSettings();
            labelPicker.SetActive(true);
            labelPicker.transform.SetAsLastSibling();
            labelList.Bind(index.Snapshot(), value => site != null && site.State.Labels.Contains(value), SetLabel);
            catalogRevision = index.Revision;
            RefreshAddButton();
        }

        private void CloseLabels()
        {
            if (labelName != null)
            {
                if (labelName.touchScreenKeyboard != null) labelName.touchScreenKeyboard.active = false;
                labelName.DeactivateInputField();
                labelName.SetTextWithoutNotify("");
            }

            if (labelPicker != null) labelPicker.SetActive(false);
        }

        private void OnWindowChanged(QuestWindow value)
        {
            if (!value.IsOpen) CancelDrafts();
        }

        private void CancelDrafts(bool closePreset = true)
        {
            if (closePreset && preset != null) preset.Close();
            if (colorPicker != null) colorPicker.Cancel();
            CloseTagSettings();
            CloseLabels();
        }

        private void CloseTagSettings()
        {
            if (patientTags != null) patientTags.CloseSettings();
            if (siteTags != null) siteTags.CloseSettings();
        }

        private void OnTagSettingsOpening(QuestTagTable owner)
        {
            preset.Close();
            if (owner != patientTags) patientTags.CloseSettings();
            if (owner != siteTags) siteTags.CloseSettings();
            CloseLabels();
            colorPicker.Cancel();
        }

        internal bool IsTagSettingsScroll(ScrollRect scroll) => patientTags.OwnsSettingsScroll(scroll) || siteTags.OwnsSettingsScroll(scroll);

        private void OnStateChanged() => dirty = true;

        private void Update()
        {
            RefreshSelection();
            if (!window.IsOpen) return;
            openPatient.interactable = CanOpenPatient;
            if (lastReady != Ready)
            {
                lastReady = Ready;
                dirty = true;
            }

            if (dirty) Refresh();
            if (labelPicker.activeSelf && index != null && catalogRevision != index.Revision)
            {
                catalogRevision = index.Revision;
                labelList.Include(index.Snapshot());
            }
        }

        internal void PreparePresetPanel() => CancelDrafts(false);

        private void OnSelectedSite(Site value) => RefreshSelection();

        private void OnSelectedColumn(Column3D value)
        {
            if (value != column) preset.SelectionChanged();
            RefreshSelection();
        }

        private void OnContextSelected(Base3DScene selectedScene, Column3D selectedColumn)
        {
            if (selectedScene != scene || selectedColumn != column) preset.SelectionChanged(selectedScene != scene);
            RefreshSelection();
        }

        internal void RefreshSelection()
        {
            var nextScene = view != null ? view.Scene : null;
            var nextColumn = nextScene != null ? nextScene.SelectedColumn : null;
            var nextSite = nextColumn != null ? nextColumn.SelectedSite : null;
            if (nextScene != scene || nextColumn != column || nextSite != site)
            {
                CancelDrafts(false);
                preset.SelectionChanged(nextScene != scene);
                if (column != null) column.OnSelectSite.RemoveListener(OnSelectedSite);
                if (site != null) site.State.OnChangeState.RemoveListener(OnStateChanged);
                if (nextScene != scene)
                {
                    index?.Dispose();
                    index = nextScene != null ? new QuestSceneLabelIndex(nextScene) : null;
                }

                scene = nextScene;
                column = nextColumn;
                site = nextSite;
                if (column != null) column.OnSelectSite.AddListener(OnSelectedSite);
                if (site != null) site.State.OnChangeState.AddListener(OnStateChanged);
                SetError("");
                dirty = true;
            }
        }

        private void SetError(string value)
        {
            if (error == null) return;
            error.text = value;
            error.transform.parent.gameObject.SetActive(value.Length > 0);
        }

        private void Refresh()
        {
            dirty = false;
            identity.supportRichText = true;
            identity.text = site != null ? "<b>" + Escape(site.Information.Name) + " (" + Escape(site.Information.Patient.Name) + ")</b>" : "Select a site to inspect it.";
            foreach (var control in new Selectable[] { highlighted, blacklisted, color, chooseLabels, labelName }) control.interactable = Ready;
            RefreshAddButton();
            highlightedIcon.Set(site != null && site.State.IsHighlighted ? activeState : highlightedIcon.Element.DefaultState);
            blacklistedIcon.Set(site != null && site.State.IsBlackListed ? activeState : blacklistedIcon.Element.DefaultState);
            colorSample.color = site != null ? site.State.Color : SiteState.DefaultColor;
            patientTags.Bind("Patient tags", site != null ? site.Information.Patient.Tags : null);
            siteTags.Bind("Site tags", site != null ? site.Information.SiteData.Tags : null, forSite: true);
            labelSummary.SetLabels(site != null ? site.State.Labels.ToArray() : Array.Empty<string>());
            if (labelPicker.activeSelf) labelList.Refresh();
        }

        private static string Escape(string value) => (value ?? "").Replace("<", "‹").Replace(">", "›");

        private void OnDestroy()
        {
            Module3DMain.OnSelectColumn.RemoveListener(OnSelectedColumn);
            if (view != null)
            {
                view.OnContextSelected.RemoveListener(OnContextSelected);
            }

            if (column != null) column.OnSelectSite.RemoveListener(OnSelectedSite);
            if (site != null) site.State.OnChangeState.RemoveListener(OnStateChanged);
            index?.Dispose();
            CancelDrafts();
            if (labelName is QuestTextInputField questInput) questInput.KeyboardError -= SetError;
            if (window != null) window.OnChanged.RemoveListener(OnWindowChanged);
            if (patientTags != null) patientTags.SettingsOpening -= OnTagSettingsOpening;
            if (siteTags != null) siteTags.SettingsOpening -= OnTagSettingsOpening;
        }
    }
}
