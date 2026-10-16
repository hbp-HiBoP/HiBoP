using System.Collections.Generic;
using HBP.Core.Enums;
using HBP.UI.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    /// <summary>Quest presentation of the shared dialog lifecycle, buttons and asynchronous result.</summary>
    public sealed class QuestDialogBox : DialogBox
    {
        [SerializeField] private Text title;
        [SerializeField] private Text message;
        [SerializeField] private LayoutElement messageViewport;
        [SerializeField] private HBP.Theme.ThemeElement icon;
        [SerializeField] private HBP.Theme.State warningState;
        [SerializeField] private HBP.Theme.State errorState;
        private static readonly List<QuestDialogBox> dialogs = new();
        public static QuestDialogBox Current => dialogs.Count == 0 ? null : dialogs[dialogs.Count - 1];

        protected override void SetIcon(DialogBoxType type) => icon.Set(type == DialogBoxType.Warning ? warningState : type == DialogBoxType.Error ? errorState : icon.Element.DefaultState);

        protected override void SetRect()
        {
            dialogs.Add(this);
            var parentCanvas = transform.parent.GetComponentInParent<Canvas>();
            var camera = parentCanvas.worldCamera;
            // RectMask2D must use the moving dialog's coordinates, not the manager's fixed canvas.
            transform.SetParent(parentCanvas.rootCanvas.transform.parent, true);
            GetComponent<Canvas>().worldCamera = camera;
            var follower = GetComponent<QuestLoadingFollower>();
            follower.SetCamera(camera);
            var tracker = camera.GetComponent<QuestDevicePoseTracker>();
            follower.Step(0, tracker == null || tracker.IsTracked);
        }

        protected override void SetMessages(string titleText, string messageText)
        {
            title.text = titleText;
            message.text = messageText;
            Canvas.ForceUpdateCanvases();
            messageViewport.preferredHeight = Mathf.Clamp(message.preferredHeight, 80, 400);
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
        }

        protected override void ButtonsCreated()
        {
            GetComponent<QuestLoadingOverlay>().RefreshGraphics();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
            Canvas.ForceUpdateCanvases();
        }

        protected override void OnDisable()
        {
            dialogs.Remove(this);
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            dialogs.Remove(this);
            base.OnDestroy();
        }
    }
}
