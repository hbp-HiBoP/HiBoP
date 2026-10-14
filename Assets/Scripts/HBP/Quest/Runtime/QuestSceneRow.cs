using System;
using Cysharp.Threading.Tasks;
using HBP.Sync.Scene;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    public sealed class QuestSceneRow : MonoBehaviour
    {
        [SerializeField] private Text title, details;
        [SerializeField] private Button visibility, close;
        [SerializeField] private HBP.Theme.State selectedState;
        private QuestAnatomyView.Presentation presentation;
        private QuestAnatomySession session;
        private bool closing;
        private string error;

        private void Awake()
        {
            visibility.onClick.AddListener(() =>
            {
                if (QuestAnatomySession.CanInteractWith(presentation?.Scene) && !closing) presentation.Scene.UpdateVisibleState(!presentation.Scene.IsVisible, false);
            });
            close.onClick.AddListener(() => CloseAsync().Forget());
        }

        public void Bind(QuestAnatomyView.Presentation value, QuestAnatomySession owner)
        {
            presentation = value;
            session = owner;
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
            visibility.GetComponentInChildren<Text>().text = scene.IsVisible ? "Hide" : "Show";
            visibility.interactable = close.interactable = !closing && QuestAnatomySession.CanInteractWith(scene);
        }
    }
}
