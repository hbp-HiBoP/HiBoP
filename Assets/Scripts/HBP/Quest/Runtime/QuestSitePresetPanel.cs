using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HBP.Quest
{
    /// <summary>Headset-local preset editor and B shortcut, active even with its window closed.</summary>
    public sealed class QuestSitePresetPanel : MonoBehaviour
    {
        private static string defaultPath;

        public static string DefaultPath
        {
            get => defaultPath ?? Path.Combine(Application.persistentDataPath, "QuestSitePreset.json");
            set => defaultPath = value;
        }

        [SerializeField] private QuestSitePanel owner;
        [SerializeField] private GameObject panel;
        [SerializeField] private Button configure, close, chooseColor, newLabel, apply;
        [SerializeField] private Toggle highlightOn, highlightOff, blacklistOn, blacklistOff, colorEnabled;
        [SerializeField] private Text applyText, error;
        [SerializeField] private Image colorSample;
        [SerializeField] private QuestColorPicker colorPicker;
        [SerializeField] private QuestSitePresetLabelList labelList;
        [SerializeField] private InputField labelName;
        private QuestSitePreset preset = new();
        private readonly QuestSitePresetApplication application = new();
        private InputAction shortcut;
        private bool loaded, writable = true, focused = true, paused;
        private int catalogueRevision = -1;
        private bool resetCatalogue;

        private void Awake()
        {
            configure.onClick.AddListener(Open);
            close.onClick.AddListener(Close);
            highlightOn.onValueChanged.AddListener(_ => SetBoolean(true));
            highlightOff.onValueChanged.AddListener(_ => SetBoolean(true));
            blacklistOn.onValueChanged.AddListener(_ => SetBoolean(false));
            blacklistOff.onValueChanged.AddListener(_ => SetBoolean(false));
            colorEnabled.onValueChanged.AddListener(value =>
            {
                EnsureLoaded();
                if (preset.SetColor == value) return;
                preset.SetColor = value;
                Changed();
            });
            chooseColor.onClick.AddListener(() =>
            {
                EnsureLoaded();
                colorPicker.Open(preset.Color, color =>
                {
                    if (preset.Color == color && preset.SetColor) return;
                    preset.Color = color;
                    preset.SetColor = true;
                    Changed();
                });
            });
            newLabel.onClick.AddListener(AddLabel);
            labelName.onValueChanged.AddListener(_ => RefreshControls());
            if (labelName is QuestTextInputField input) input.KeyboardError += SetError;
            apply.onClick.AddListener(ApplyOrRevert);
            Close();
        }

        private void SetBoolean(bool highlight)
        {
            EnsureLoaded();
            var on = highlight ? highlightOn : blacklistOn;
            var off = highlight ? highlightOff : blacklistOff;
            var value = on.isOn ? QuestPresetBoolean.On : off.isOn ? QuestPresetBoolean.Off : QuestPresetBoolean.Keep;
            if ((highlight ? preset.Highlight : preset.Blacklist) == value) return;
            if (highlight) preset.Highlight = value;
            else preset.Blacklist = value;
            Changed();
        }

        private void OnEnable()
        {
            shortcut = QuestInteractionInput.CreateSitePresetAction();
            shortcut.Enable();
        }

        // EventSystem.Process completes selection (A) before B, including simultaneous presses.
        private void LateUpdate()
        {
            owner.RefreshSelection();
            application.Bind(owner.CurrentScene, owner.CurrentColumn, owner.CurrentSite);
            if (owner.CurrentScene != null && owner.CurrentScene.IsClosing) application.ForgetScene(owner.CurrentScene);
            if (shortcut.WasPressedThisFrame() && focused && !paused && QuestDialogBox.Current == null && !TouchScreenKeyboard.visible && !(EventSystem.current?.currentSelectedGameObject?.GetComponent<InputField>() is { isFocused: true })) ApplyOrRevert();
            if (panel.activeSelf)
            {
                if (catalogueRevision != owner.LabelCatalogueRevision)
                {
                    catalogueRevision = owner.LabelCatalogueRevision;
                    if (resetCatalogue) BindLabels();
                    else labelList.Include(owner.SceneLabels);
                }

                RefreshControls();
            }
        }

        public void Open()
        {
            if (panel.activeSelf)
            {
                Close();
                return;
            }

            EnsureLoaded();
            owner.RefreshSelection();
            owner.PreparePresetPanel();
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            BindLabels();
            catalogueRevision = owner.LabelCatalogueRevision;
            RefreshControls();
        }

        public void Close()
        {
            colorPicker.Cancel();
            if (labelName.touchScreenKeyboard != null) labelName.touchScreenKeyboard.active = false;
            labelName.DeactivateInputField();
            labelName.SetTextWithoutNotify("");
            panel.SetActive(false);
        }

        public void ApplyOrRevert()
        {
            EnsureLoaded();
            owner.RefreshSelection();
            application.Bind(owner.CurrentScene, owner.CurrentColumn, owner.CurrentSite);
            if (!owner.Ready || !preset.HasActions) return;
            try
            {
                if (application.ApplyOrRevert(preset)) SetError("");
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
            {
                SetError(exception.Message);
            }

            RefreshControls();
        }

        internal void SelectionChanged(bool sceneChanged = false)
        {
            if (sceneChanged)
            {
                resetCatalogue = true;
                catalogueRevision = int.MinValue;
            }
        }

        private void BindLabels()
        {
            labelList.Bind(owner.SceneLabels.Concat(preset.Labels.Select(value => value.Label)), preset.GetLabelAction, SetLabel);
            resetCatalogue = false;
        }

        private void AddLabel()
        {
            string value = labelName.text.Trim();
            if (value.Length == 0) return;
            SetLabel(value, QuestPresetLabelAction.Add);
            labelList.Include(new[] { value });
            labelName.SetTextWithoutNotify("");
            RefreshControls();
        }

        private void SetLabel(string label, QuestPresetLabelAction action)
        {
            if (preset.SetLabelAction(label, action)) Changed();
        }

        private void Changed()
        {
            application.Invalidate();
            SetError("");
            if (writable)
            {
                try
                {
                    File.WriteAllText(DefaultPath, JsonUtility.ToJson(preset, true));
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                    SetError("Unable to save the site preset. " + exception.Message);
                }
            }

            RefreshControls();
            labelList.Refresh();
        }

        private void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;
            try
            {
                if (!File.Exists(DefaultPath)) return;
                var saved = JsonUtility.FromJson<QuestSitePreset>(File.ReadAllText(DefaultPath));
                if (saved == null) throw new ArgumentException("The saved site preset is empty.");
                saved.Validate();
                preset = saved;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                writable = false;
                SetError("Unable to load the saved site preset. Changes will only last for this session. " + exception.Message);
            }
        }

        private void RefreshControls()
        {
            highlightOn.SetIsOnWithoutNotify(preset.Highlight == QuestPresetBoolean.On);
            highlightOff.SetIsOnWithoutNotify(preset.Highlight == QuestPresetBoolean.Off);
            blacklistOn.SetIsOnWithoutNotify(preset.Blacklist == QuestPresetBoolean.On);
            blacklistOff.SetIsOnWithoutNotify(preset.Blacklist == QuestPresetBoolean.Off);
            colorEnabled.SetIsOnWithoutNotify(preset.SetColor);
            colorSample.color = preset.Color;
            applyText.text = application.CanRevert ? "Revert" : "Apply";
            apply.interactable = owner.Ready && preset.HasActions;
            newLabel.interactable = !string.IsNullOrWhiteSpace(labelName.text);
        }

        private void SetError(string message)
        {
            error.text = message;
            error.transform.parent.gameObject.SetActive(message.Length > 0);
        }

        private void OnApplicationFocus(bool value) => focused = value;
        private void OnApplicationPause(bool value) => paused = value;

        private void OnDisable()
        {
            shortcut?.Dispose();
            shortcut = null;
            application.Dispose();
        }

        private void OnDestroy()
        {
            application.Dispose();
            if (labelName is QuestTextInputField input) input.KeyboardError -= SetError;
        }
    }
}
