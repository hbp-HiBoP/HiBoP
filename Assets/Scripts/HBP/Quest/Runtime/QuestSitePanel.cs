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
            CancelDrafts();
            dirty = true;
        }

        private bool Ready => site != null && view != null && view.Scene == scene && scene.SelectedColumn == column && column.SelectedSite == site && QuestAnatomySession.CanInteractWith(scene) && scene.IsVisible;
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
            if (labelPicker.activeSelf)
            {
                CloseLabels();
                return;
            }

            colorPicker.Cancel();
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

        private void CancelDrafts()
        {
            if (colorPicker != null) colorPicker.Cancel();
            CloseLabels();
        }

        private void OnStateChanged() => dirty = true;

        private void Update()
        {
            var nextScene = view != null ? view.Scene : null;
            var nextColumn = nextScene != null ? nextScene.SelectedColumn : null;
            var nextSite = nextColumn != null ? nextColumn.SelectedSite : null;
            if (nextScene != scene || nextColumn != column || nextSite != site)
            {
                CancelDrafts();
                if (site != null) site.State.OnChangeState.RemoveListener(OnStateChanged);
                if (nextScene != scene)
                {
                    index?.Dispose();
                    index = nextScene != null ? new QuestSceneLabelIndex(nextScene) : null;
                }

                scene = nextScene;
                column = nextColumn;
                site = nextSite;
                if (site != null) site.State.OnChangeState.AddListener(OnStateChanged);
                SetError("");
                dirty = true;
            }

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
            patientTags.Bind(site != null ? site.Information.Patient.Name : "", site != null ? site.Information.Patient.Tags : null);
            siteTags.Bind(site != null ? site.Information.Name : "", site != null ? site.Information.SiteData.Tags : null);
            labelSummary.SetLabels(site != null ? site.State.Labels.ToArray() : Array.Empty<string>());
            if (labelPicker.activeSelf) labelList.Refresh();
        }

        private static string Escape(string value) => (value ?? "").Replace("<", "‹").Replace(">", "›");

        private void OnDestroy()
        {
            if (site != null) site.State.OnChangeState.RemoveListener(OnStateChanged);
            index?.Dispose();
            CancelDrafts();
            if (labelName is QuestTextInputField questInput) questInput.KeyboardError -= SetError;
            if (window != null) window.OnChanged.RemoveListener(OnWindowChanged);
        }
    }
}
