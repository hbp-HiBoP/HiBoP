using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class ComputeActivity : Tool
    {
        #region Properties
        /// <summary>
        /// Trigger the computation of the projection of the iEEG activity
        /// </summary>
        [SerializeField] private Button m_Compute;
        /// <summary>
        /// Remove the projection of the iEEG activity
        /// </summary>
        [SerializeField] private Button m_Remove;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Compute.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                SelectedScene.ResetGenerators();
                SelectedScene.SceneInformation.GeneratorUpdateRequested = true;
                UpdateInteractable();
            });
            m_Remove.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                SelectedScene.ResetGenerators();
                UpdateInteractable();
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Compute.interactable = false;
            m_Remove.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool isGeneratorUpToDate = SelectedScene.IsGeneratorUpToDate;

            m_Compute.interactable = SelectedScene.CanComputeFunctionalValues;
            m_Remove.interactable = isGeneratorUpToDate;
        }
        #endregion
    }
}