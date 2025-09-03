using HBP.UI.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Toolbar
{
    public class ExportActivityToNifti : Tool
    {
        #region Properties
        [SerializeField] private Button m_OpenWindowButton;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_OpenWindowButton.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                WindowsManager.Open("Export activity to nifti window", null);
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_OpenWindowButton.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool isGeneratorUpToDate = SelectedScene.IsGeneratorUpToDate;

            m_OpenWindowButton.interactable = isGeneratorUpToDate;
        }
        #endregion
    }
}