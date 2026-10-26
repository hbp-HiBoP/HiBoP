using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using HBP.Sync.Scene;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    public sealed class QuestSceneRow : MonoBehaviour
    {
        [SerializeField] private Text title, details;
        [SerializeField] private Toggle visibility;
        [SerializeField] private Button close;
        [SerializeField] private Button select;
        [SerializeField] private QuestDropdown columnSelector;
        [SerializeField] private Toggle separateColumns;
        [SerializeField] private HBP.Theme.State selectedState;
        private QuestAnatomyView.Presentation presentation;
        private QuestAnatomySession session;
        private QuestAnatomyView view;
        private bool closing;
        private string error;
        private readonly List<string> columnNames = new();
        private bool selectorHasNoSelection;

        private void Awake()
        {
            visibility.onValueChanged.AddListener(value =>
            {
                if (QuestAnatomySession.CanInteractWith(presentation?.Scene) && !closing) presentation.Scene.UpdateVisibleState(value, false);
            });
            close.onClick.AddListener(() => CloseAsync().Forget());
            if (select != null)
                select.onClick.AddListener(() =>
                {
                    if (CanSelect) view.SelectContext(presentation.Scene, presentation.Scene.SelectedColumn);
                });
            if (columnSelector != null)
                columnSelector.onValueChanged.AddListener(index =>
                {
                    if (selectorHasNoSelection) index--;
                    if (CanEditColumns && index >= 0 && index < presentation.Scene.Columns.Count)
                        view.SelectContext(presentation.Scene, presentation.Scene.Columns[index]);
                });
            if (separateColumns != null)
                separateColumns.onValueChanged.AddListener(value =>
                {
                    if (CanEditColumns) view.SetColumnDisplayMode(presentation.Scene, value ? QuestColumnDisplayMode.Split : QuestColumnDisplayMode.Single);
                });
        }

        public void Bind(QuestAnatomyView.Presentation value, QuestAnatomySession owner, QuestAnatomyView context = null)
        {
            presentation = value;
            session = owner;
            view = context;
            Refresh();
        }

        private async UniTaskVoid CloseAsync()
        {
            if (closing || !QuestAnatomySession.CanInteractWith(presentation?.Scene)) return;
            closing = true;
            error = null;
            try
            {
                await session.RequestCloseAsync(presentation.Scene);
            }
            catch (Exception exception)
            {
                error = exception.Message;
            }
            finally
            {
                closing = false;
            }
        }

        private void Update() => Refresh();
        private bool CanSelect => view != null && presentation != null && presentation.Scene.IsVisible && !presentation.IsTransitioning && !closing && QuestAnatomySession.CanInteractWith(presentation.Scene);
        private bool CanEditColumns => CanSelect && presentation.IsPlaced;

        private void Refresh()
        {
            var scene = presentation?.Scene;
            if (scene == null) return;
            title.text = scene.Visualization.Name;
            // Selection uses the same themed text states as the other Quest controls.
            var theme = title.GetComponent<HBP.Theme.ThemeElement>();
            if (theme != null)
            {
                if (scene.IsSelected) theme.Set(selectedState);
                else theme.Set();
            }

            var record = SceneReconciliationRecord.Get(scene);
            string state = record?.Status switch
            {
                ReconciliationStatus.Orphan => "Local to Quest",
                ReconciliationStatus.Synchronized => session.IsSceneConnected(scene) ? "Shared with Desktop" : "Desktop disconnected",
                ReconciliationStatus.Reconciling => "Reconnecting",
                ReconciliationStatus.OutOfSync => "Reconnection failed",
                _ => "Local / disconnected"
            };
            details.text = error ?? $"{scene.Columns.Count} columns · {state}";
            visibility.SetIsOnWithoutNotify(scene.IsVisible);
            visibility.interactable = close.interactable = !closing && QuestAnatomySession.CanInteractWith(scene);
            if (select != null) select.interactable = CanSelect;
            RefreshColumns();
        }

        private void RefreshColumns()
        {
            if (columnSelector == null || separateColumns == null) return;
            var scene = presentation.Scene;
            separateColumns.SetIsOnWithoutNotify(presentation.DisplayMode == QuestColumnDisplayMode.Split);
            separateColumns.interactable = CanEditColumns && scene.Columns.Count > 1;
            columnSelector.interactable = CanEditColumns && scene.Columns.Count > 0;
            selectorHasNoSelection = scene.SelectedColumn == null;
            var names = scene.Columns.Select(column => column.Name).ToList();
            if (selectorHasNoSelection) names.Insert(0, "Select a column");
            if (!columnNames.SequenceEqual(names))
            {
                columnNames.Clear();
                columnNames.AddRange(names);
                columnSelector.ClearOptions();
                columnSelector.AddOptions(names);
            }

            columnSelector.SetValueWithoutNotify(selectorHasNoSelection ? 0 : Mathf.Max(0, scene.Columns.IndexOf(scene.SelectedColumn)));
            columnSelector.RefreshShownValue();
        }
    }
}
