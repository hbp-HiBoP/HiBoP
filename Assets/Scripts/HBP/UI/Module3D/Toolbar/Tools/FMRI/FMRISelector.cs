using HBP.Module3D;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class FMRISelector : Tool
    {
        #region Properties
        /// <summary>
        /// Select the fMRI to be displayed
        /// </summary>
        [SerializeField] private Button m_AddFMRI;
        /// <summary>
        /// Remove the fMRI from the scene
        /// </summary>
        [SerializeField] private Button m_RemoveFMRI;
        #endregion

        #region Events
        /// <summary>
        /// Event called when changing the displayed fMRI
        /// </summary>
        public UnityEvent OnChangeFMRI = new UnityEvent();
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_AddFMRI.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                string path = FileBrowser.GetExistingFileName(new string[] { "nii", "img", "nii.gz" }, "Select an fMRI file");
                if (!string.IsNullOrEmpty(path))
                {
                    SelectedScene.FMRIManager.FMRI = new MRI3D(new Data.MRI("FMRI", path));
                }
                OnChangeFMRI.Invoke();
            });
            m_RemoveFMRI.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                SelectedScene.FMRIManager.FMRI = null;
                OnChangeFMRI.Invoke();
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_AddFMRI.interactable = false;
            m_RemoveFMRI.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool hasFMRI = SelectedScene.FMRIManager.FMRI != null;

            m_AddFMRI.interactable = !hasFMRI;
            m_RemoveFMRI.interactable = hasFMRI;
        }
        #endregion
    }
}