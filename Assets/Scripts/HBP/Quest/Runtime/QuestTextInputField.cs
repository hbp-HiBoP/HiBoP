using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HBP.Quest
{
    /// <summary>
    /// Opens the OpenXR system keyboard explicitly on Android. UGUI's legacy Quest
    /// detection only recognizes the original device name and skips it on Quest 3.
    /// </summary>
    public sealed class QuestTextInputField : InputField
    {
        private string originalText;
        public event Action<string> KeyboardError;
        private static bool UsesSystemKeyboard => Application.platform == RuntimePlatform.Android;

        public override void OnSelect(BaseEventData eventData)
        {
            if (!UsesSystemKeyboard)
            {
                base.OnSelect(eventData);
                return;
            }

            bool activate = shouldActivateOnSelect;
            shouldActivateOnSelect = false;
            base.OnSelect(eventData);
            shouldActivateOnSelect = activate;
            if (activate) OpenKeyboard();
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!UsesSystemKeyboard)
            {
                base.OnPointerDown(eventData);
                return;
            }

            if (eventData.button != PointerEventData.InputButton.Left || !IsActive() || !IsInteractable()) return;
            EventSystem.current?.SetSelectedGameObject(gameObject, eventData);
            OpenKeyboard();
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (!UsesSystemKeyboard)
            {
                base.OnPointerClick(eventData);
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Left) OpenKeyboard();
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            if (!UsesSystemKeyboard)
            {
                base.OnSubmit(eventData);
                return;
            }

            OpenKeyboard();
        }

        public override void OnUpdateSelected(BaseEventData eventData)
        {
            if (!UsesSystemKeyboard) base.OnUpdateSelected(eventData);
        }

        private void OpenKeyboard()
        {
            if (!IsActive() || !IsInteractable() || readOnly || m_Keyboard != null) return;
            if (!TouchScreenKeyboard.isSupported)
            {
                Debug.LogWarning("[QuestTextInputField] System keyboard is unsupported on " + SystemInfo.deviceModel, this);
                KeyboardError?.Invoke("The system keyboard is unavailable. Create new labels on Desktop.");
                return;
            }

            originalText = text;
            try
            {
                // Do not depend on UGUI's in-place editing/device-name heuristics.
                TouchScreenKeyboard.hideInput = false;
                m_Keyboard = TouchScreenKeyboard.Open(text, keyboardType, false, multiLine, inputType == InputType.Password, false, "", characterLimit);
                Debug.Log("[QuestTextInputField] System keyboard requested on " + SystemInfo.deviceModel, this);
                if (m_Keyboard == null) KeyboardError?.Invoke("Unable to open the system keyboard. Try again or use Desktop.");
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[QuestTextInputField] System keyboard failed: " + exception.GetType().Name, this);
                KeyboardError?.Invoke("Unable to open the system keyboard. Try again or use Desktop.");
            }
        }

        protected override void LateUpdate()
        {
            if (!UsesSystemKeyboard)
            {
                base.LateUpdate();
                return;
            }

            if (m_Keyboard == null) return;
            var status = m_Keyboard.status;
            text = status == TouchScreenKeyboard.Status.Canceled ? originalText : m_Keyboard.text;
            if (status == TouchScreenKeyboard.Status.Visible) return;
            Debug.Log("[QuestTextInputField] System keyboard ended: " + status, this);
            CloseKeyboard();
            if (status == TouchScreenKeyboard.Status.Done) onSubmit.Invoke(text);
            onEndEdit.Invoke(text);
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            if (UsesSystemKeyboard && m_Keyboard != null)
            {
                text = m_Keyboard.status == TouchScreenKeyboard.Status.Canceled ? originalText : m_Keyboard.text;
                CloseKeyboard();
                onEndEdit.Invoke(text);
            }

            base.OnDeselect(eventData);
        }

        private void CloseKeyboard()
        {
            if (m_Keyboard == null) return;
            m_Keyboard.active = false;
            m_Keyboard = null;
        }

        protected override void OnDisable()
        {
            CloseKeyboard();
            base.OnDisable();
        }
    }
}
