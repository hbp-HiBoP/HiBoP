using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Enums;

namespace HBP.UI.Toolbar
{
    public class MoveSites : Tool
    {
        #region Properties
        /// <summary>
        /// Button to show the panel
        /// </summary>
        [SerializeField] private Button m_Button;
        /// <summary>
        /// Button to move all sites to the left hemisphere
        /// </summary>
        [SerializeField] private Button m_MoveToLeftHemisphere;
        /// <summary>
        /// Button to move all sites to the right hemisphere
        /// </summary>
        [SerializeField] private Button m_MoveToRightHemisphere;
        /// <summary>
        /// Button to reset the position of all sites
        /// </summary>
        [SerializeField] private Button m_Reset;
        #endregion

        #region Private Methods

        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_MoveToLeftHemisphere.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                Vector3 orientation = SelectedScene.MRIManager.SelectedMRI.Volume.GetOrientationVector(CutOrientation.Sagittal, false);
                Vector3 center = SelectedScene.MeshManager.MeshCenter;
                orientation = new Vector3(-orientation.x, orientation.y, orientation.z);
                center = new Vector3(-center.x, center.y, center.z);
                foreach (var column in SelectedScene.Columns)
                {
                    column.MoveAllSitesToTheSameSideOfAPlane(center, orientation);
                }
            });
            m_MoveToRightHemisphere.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                Vector3 orientation = SelectedScene.MRIManager.SelectedMRI.Volume.GetOrientationVector(CutOrientation.Sagittal, true);
                Vector3 center = SelectedScene.MeshManager.MeshCenter;
                orientation = new Vector3(-orientation.x, orientation.y, orientation.z);
                center = new Vector3(-center.x, center.y, center.z);
                foreach (var column in SelectedScene.Columns)
                {
                    column.MoveAllSitesToTheSameSideOfAPlane(center, orientation);
                }
            });
            m_Reset.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                foreach (var column in SelectedScene.Columns)
                {
                    column.ResetSitesPositions();
                }
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Button.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            m_Button.interactable = true;
        }
        #endregion
    }
}