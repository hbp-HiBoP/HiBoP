using System.Collections.Generic;
using HBP.Core.Data;
using HBP.Sync;
using HBP.UI.Tools;
using UnityEngine;
using UnityEngine.UI;
using ThemeElement = HBP.Theme.ThemeElement;
using ThemeState = HBP.Theme.State;
using TimelineSegmentView = HBP.UI.Toolbar.SubTimeline;

namespace HBP.Quest
{
    public sealed class QuestTimeline : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasGroup controls;
        [SerializeField] private Toggle play, loop, link;
        [SerializeField] private Image playIcon, pauseIcon;
        [SerializeField] private ThemeElement playTheme, loopTheme, linkTheme;
        [SerializeField] private ThemeState activatedState;
        [SerializeField] private Button minus, plus;
        [SerializeField] private QuestTextInputField step;
        [SerializeField] private QuestTimelineTrack track;
        [SerializeField] private RectTransform segments;
        [SerializeField] private TimelineSegmentView segmentPrefab;
        [SerializeField] private Text eventTooltip;

        private readonly List<TimelineSegmentView> segmentViews = new();
        private QuestAnatomyView view;
        private QuestAnatomyView.Presentation presentation;
        private QuestColumnPresentation column;
        private BasicTimeline timeline;
        private SubTimeline singleSegment;
        private int length, lastIndex = -1;
        private QuestTimelineMarker hoveredMarker;
        private int hoveringPointer;

        public RectTransform Rect => (RectTransform)transform;
        internal QuestColumnPresentation Column => column;
        internal bool CanInteract => isActiveAndEnabled && view != null && view.CanUseTimeline(column) && column.Column.NavigationTimeline == timeline;

        internal void Bind(QuestAnatomyView owner, QuestAnatomyView.Presentation state, QuestColumnPresentation target)
        {
            view = owner;
            presentation = state;
            column = target;
        }

        private void Awake()
        {
            QuestCanvasRenderQueue.Set(canvas, QuestCanvasRenderQueue.Timeline);
            track.Bind(this);
            play.onValueChanged.AddListener(value =>
            {
                if (CanInteract) column.Scene.SetTimelinePlaying(column.Column, value, presentation.GlobalTimeline);
            });
            loop.onValueChanged.AddListener(value =>
            {
                if (CanInteract) column.Scene.SetTimelineLooping(column.Column, value, presentation.GlobalTimeline);
            });
            link.onValueChanged.AddListener(value =>
            {
                if (CanInteract) view.SetTimelineLink(column, value);
            });
            minus.onClick.AddListener(() =>
            {
                if (CanInteract) column.Scene.AdvanceTimeline(column.Column, -1, presentation.GlobalTimeline);
            });
            plus.onClick.AddListener(() =>
            {
                if (CanInteract) column.Scene.AdvanceTimeline(column.Column, 1, presentation.GlobalTimeline);
            });
            if (Application.platform == RuntimePlatform.Android) step.onSubmit.AddListener(ChangeStep);
            else step.onEndEdit.AddListener(ChangeStep);
            step.KeyboardError += ShowTooltip;
            eventTooltip.transform.parent.gameObject.SetActive(false);
        }

        private void ChangeStep(string value)
        {
            if (!CanInteract) return;
            int parsed = int.TryParse(value, out int count) ? count : 1;
            column.Scene.SetTimelineStep(column.Column, Mathf.Clamp(parsed, 1, SetTimelineAnchor.MaximumStep), presentation.GlobalTimeline);
            step.SetTextWithoutNotify(timeline.Step.ToString());
        }

        internal void Seek(int index)
        {
            if (CanInteract && (index != timeline.CurrentIndex || presentation.GlobalTimeline))
                column.Scene.SetTimelineIndex(column.Column, index, presentation.GlobalTimeline);
        }

        internal void Refresh(Camera camera)
        {
            if (canvas.worldCamera != camera) canvas.worldCamera = camera;
            var source = column.Column.NavigationTimeline;
            var segment = source is Timeline ? null : source.CurrentSubtimeline;
            if (timeline != source || length != source.Length || singleSegment != segment)
                Rebuild(source, segment);

            controls.interactable = controls.blocksRaycasts = CanInteract;
            link.gameObject.SetActive(presentation.DisplayMode == QuestColumnDisplayMode.Split);
            RefreshToggle(link, linkTheme, presentation.LinkTimelines);
            RefreshToggle(play, playTheme, timeline.IsPlaying);
            playIcon.enabled = !timeline.IsPlaying;
            pauseIcon.enabled = timeline.IsPlaying;
            RefreshToggle(loop, loopTheme, timeline.IsLooping);
            if (!step.IsEditing && (!int.TryParse(step.text, out int value) || value != timeline.Step))
                step.SetTextWithoutNotify(timeline.Step.ToString());
            if (lastIndex == timeline.CurrentIndex) return;
            lastIndex = timeline.CurrentIndex;
            track.SetValueWithoutNotify(lastIndex);
            foreach (var segmentView in segmentViews) segmentView.UpdateCurrentTime();
        }

        private void RefreshToggle(Toggle toggle, ThemeElement theme, bool isOn)
        {
            toggle.SetIsOnWithoutNotify(isOn);
            var state = isOn ? activatedState : theme.Element.DefaultState;
            if (theme.ActualState != state) theme.Set(state);
        }

        private void Rebuild(BasicTimeline source, SubTimeline segment)
        {
            ClearTooltip();
            foreach (var previous in segmentViews)
            {
                previous.gameObject.SetActive(false);
                Destroy(previous.gameObject);
            }

            segmentViews.Clear();
            timeline = source;
            length = source.Length;
            singleSegment = segment;
            lastIndex = -1;
            track.maxValue = length - 1;
            foreach (var (data, start, end) in TimelineSegmentView.GetSegments(timeline))
            {
                var segmentView = Instantiate(segmentPrefab, segments);
                var rect = (RectTransform)segmentView.transform;
                rect.anchorMin = new Vector2(start, 0);
                rect.anchorMax = new Vector2(end, 1);
                segmentView.Initialize(timeline, data, ConfigureMarker);
                segmentViews.Add(segmentView);
            }
        }

        private void ConfigureMarker(GameObject marker, string eventName, float time, string unit, float presence)
        {
            if (marker.TryGetComponent<Tooltip>(out var desktopTooltip)) desktopTooltip.enabled = false;
            string description = $"{eventName} | {time:0.##} {unit}";
            if (presence < 1f) description += $" | {presence * 100f:0.##}%";
            marker.AddComponent<QuestTimelineMarker>().Bind(this, description);
        }

        internal void ShowEvent(QuestTimelineMarker marker, string description, int pointer)
        {
            hoveredMarker = marker;
            hoveringPointer = pointer;
            ShowTooltip(description);
        }

        internal void HideEvent(QuestTimelineMarker marker, int pointer)
        {
            if (hoveredMarker == marker && hoveringPointer == pointer) ClearTooltip();
        }

        private void ShowTooltip(string message)
        {
            eventTooltip.text = message;
            eventTooltip.transform.parent.gameObject.SetActive(true);
        }

        private void ClearTooltip()
        {
            hoveredMarker = null;
            eventTooltip.transform.parent.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (eventTooltip != null) ClearTooltip();
        }
    }
}
