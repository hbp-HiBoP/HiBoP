using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;

namespace HBP.UI.Tools
{
    /// <summary>
    /// Supplies the IME properties still requested by uGUI InputField through
    /// BaseInput. Text editing itself uses uGUI's native text event queue.
    /// Serialized as the UI module's inputOverride on the Input Manager prefab.
    /// </summary>
    public sealed class InputFieldBackend : BaseInput
    {
        [SerializeField] private InputSystemUIInputModule m_Module;
        private Keyboard m_Keyboard;
        private string m_Composition = "";
        private Vector2 m_CompositionCursor;
        private IMECompositionMode m_Mode;

        public override string compositionString => m_Composition;
        public override bool touchSupported => Touchscreen.current != null;

        public override IMECompositionMode imeCompositionMode
        {
            get => m_Mode;
            set
            {
                m_Mode = value;
                m_Keyboard?.SetIMEEnabled(value == IMECompositionMode.On);
                if (value != IMECompositionMode.On) m_Composition = "";
            }
        }

        public override Vector2 compositionCursorPos
        {
            get => m_CompositionCursor;
            set
            {
                m_CompositionCursor = value;
                m_Keyboard?.SetIMECursorPosition(value);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (m_Module != null) m_Module.inputOverride = this;
            InputSystem.onDeviceChange += OnDeviceChange;
            SetKeyboard(Keyboard.current);
        }

        protected override void OnDisable()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
            SetKeyboard(null);
            base.OnDisable();
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device is Keyboard) SetKeyboard(Keyboard.current);
        }

        private void SetKeyboard(Keyboard keyboard)
        {
            if (m_Keyboard != null) m_Keyboard.onIMECompositionChange -= OnComposition;
            m_Keyboard = keyboard;
            m_Composition = "";
            if (m_Keyboard != null) m_Keyboard.onIMECompositionChange += OnComposition;
        }

        private void OnComposition(IMECompositionString composition) => m_Composition = composition.ToString();

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) m_Composition = "";
        }
    }
}
