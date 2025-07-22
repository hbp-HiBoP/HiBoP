using UnityEngine;
using UnityEngine.UI;
using HBP.UI.Tools;
using Cysharp.Threading.Tasks;
using HBP.Core.Tools;
using HBP.Core.Data;

namespace HBP.UI.Main
{
    /// <summary>
    /// Window to modify a protocol.
    /// </summary>
	public class ProtocolModifier : ObjectModifier<Protocol>
    {
        #region Properties
        [SerializeField] Toggle m_BasicProtocolTabToggle;
        [SerializeField] Toggle m_AdvancedProtocolTabToggle;
        [SerializeField] BasicProtocolSubModifier m_BasicProtocolSubModifier;
        [SerializeField] AdvancedProtocolSubModifier m_AdvancedProtocolSubModifier;

        private bool m_IsChangingTab = false;

        public override Protocol Object
        {
            get => base.Object;
            set
            {
                base.Object = value;
                SetDefaultEditionMode(value);
            }
        }

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
        public override void OK()
        {
            if (m_BasicProtocolSubModifier.gameObject.activeSelf) m_ObjectTemp.SetBasicProtocolFeatures();
            base.OK();
        }
        public async void OnChangeBasicToggle(bool value)
        {
            if (!value || m_IsChangingTab) return;

            m_IsChangingTab = true;

            m_AdvancedProtocolTabToggle.SetValue(true);

            var changeAdvancedToBasic = await CheckAdvancedState();
            var discardUnsavedChanges = await CheckUnsavedChanges();

            if (discardUnsavedChanges && changeAdvancedToBasic)
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

            var discardUnsavedChanges = await CheckUnsavedChanges();

            if (discardUnsavedChanges)
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
        protected override void SetFields(Protocol objectToDisplay)
        {
            base.SetFields();

            m_BasicProtocolSubModifier.Object = objectToDisplay;
            m_AdvancedProtocolSubModifier.Object = objectToDisplay;
        }
        protected void SetDefaultEditionMode(Protocol protocol)
        {
            var isAdvanced = protocol.IsAdvanced;
            m_BasicProtocolTabToggle.SetIsOnWithoutNotify(!isAdvanced);
            m_BasicProtocolSubModifier.gameObject.SetActive(!isAdvanced);
            m_AdvancedProtocolTabToggle.SetIsOnWithoutNotify(isAdvanced);
            m_AdvancedProtocolSubModifier.gameObject.SetActive(isAdvanced);
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
        protected async UniTask<bool> CheckAdvancedState()
        {
            if (m_ObjectTemp.IsAdvanced)
            {
                var result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Warning, "Change edition mode", "Switching to basic protocol may remove some advanced features (such as multiple sub-blocs, different windows for each sub-bloc or custom trials sorting methods).\n\nAre you sure you want to continue?", "Continue", "Cancel");
                if (result == 1) return false;
            }
            return true;
        }
        #endregion
    }
}