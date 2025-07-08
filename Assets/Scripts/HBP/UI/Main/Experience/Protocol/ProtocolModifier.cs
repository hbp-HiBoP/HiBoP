using UnityEngine;
using UnityEngine.UI;
using HBP.UI.Tools;
using Cysharp.Threading.Tasks;
using HBP.Core.Tools;

namespace HBP.UI.Main
{
    /// <summary>
    /// Window to modify a protocol.
    /// </summary>
	public class ProtocolModifier : ObjectModifier<Core.Data.Protocol>
    {
        #region Properties
        [SerializeField] Toggle m_BasicProtocolTabToggle;
        [SerializeField] Toggle m_AdvancedProtocolTabToggle;
        [SerializeField] BasicProtocolSubModifier m_BasicProtocolSubModifier;
        [SerializeField] AdvancedProtocolSubModifier m_AdvancedProtocolSubModifier;

        private bool m_IsChangingTab = false;

        /// <summary>
        /// True if interactable, False otherwise.
        /// </summary>
        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;

                m_BasicProtocolSubModifier.Interactable = value;
                m_AdvancedProtocolSubModifier.Interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public async void OnChangeBasicToggle(bool value)
        {
            if (!value || m_IsChangingTab) return;

            m_IsChangingTab = true;

            m_AdvancedProtocolTabToggle.SetValue(true);

            var swap = await CheckUnsavedChanges();

            if (swap)
            {
                m_BasicProtocolTabToggle.SetValue(true);
                m_BasicProtocolSubModifier.gameObject.SetActive(true);
                m_AdvancedProtocolSubModifier.gameObject.SetActive(false);
            }

            m_IsChangingTab = false;

            Refresh();
        }
        public async void OnChangeAdvancedToggle(bool value)
        {
            if (!value || m_IsChangingTab) return;

            m_IsChangingTab = true;

            m_BasicProtocolTabToggle.SetValue(true);

            var swap = await CheckUnsavedChanges();

            if (swap)
            {
                m_AdvancedProtocolTabToggle.SetValue(true);
                m_AdvancedProtocolSubModifier.gameObject.SetActive(true);
                m_BasicProtocolSubModifier.gameObject.SetActive(false);
            }

            m_IsChangingTab = false;

            Refresh();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Initialize the window.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            m_BasicProtocolSubModifier.Initialize();
            m_AdvancedProtocolSubModifier.Initialize();

            m_BasicProtocolSubModifier.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_BasicProtocolSubModifier.WindowsReferencer.OnCloseWindow.AddListener(WindowsReferencer.Remove);
            m_AdvancedProtocolSubModifier.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_AdvancedProtocolSubModifier.WindowsReferencer.OnCloseWindow.AddListener(WindowsReferencer.Remove);
        }
        /// <summary>
        /// Set the fields
        /// </summary>
        /// <param name="objectToDisplay">Protocol to display</param>
        protected override void SetFields(Core.Data.Protocol objectToDisplay)
        {
            base.SetFields();

            m_BasicProtocolSubModifier.Object = objectToDisplay;
            m_AdvancedProtocolSubModifier.Object = objectToDisplay;
        }
        protected async UniTask<bool> CheckUnsavedChanges()
        {
            if (WindowsReferencer.Windows.Count > 0)
            {
                var result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Warning, "Change edition mode", "Protocol edition mode will be changed, all unsaved changes will be lost.", "Continue", "Cancel");
                if (result == 1) return false;

                WindowsReferencer.CloseAll();
            }
            return true;
        }
        #endregion
    }
}