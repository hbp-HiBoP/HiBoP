using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class AtlasState : Tool
    {
        #region Properties
        /// <summary>
        /// Displays the IBC atlas
        /// </summary>
        [SerializeField] private Toggle m_IBCToggle;
        /// <summary>
        /// Displays the JuBrain atlas
        /// </summary>
        [SerializeField] private Toggle m_JubrainToggle;
        /// <summary>
        /// Displays the MarsAtlas
        /// </summary>
        [SerializeField] private Toggle m_MarsAtlasToggle;
        #endregion

        #region Public Methods
        /// <summary>
        /// Add the listener to this tool
        /// </summary>
        public override void Initialize()
        {
            m_IBCToggle.onValueChanged.AddListener((isOn) =>
            {
                if (ListenerLock) return;

                SelectedScene.FMRIManager.DisplayIBCContrasts = isOn;
            });
            m_JubrainToggle.onValueChanged.AddListener((isOn) =>
            {
                if (ListenerLock) return;

                SelectedScene.AtlasManager.DisplayJuBrainAtlas = isOn;
            });
            m_MarsAtlasToggle.onValueChanged.AddListener((isOn) =>
            {
                if (ListenerLock) return;

                SelectedScene.AtlasManager.DisplayMarsAtlas = isOn;
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_IBCToggle.isOn = false;
            m_IBCToggle.interactable = false;
            m_JubrainToggle.isOn = false;
            m_JubrainToggle.interactable = false;
            m_MarsAtlasToggle.isOn = false;
            m_MarsAtlasToggle.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool isIBCAvailable = ApplicationState.Module3D.IBCObjects.Loaded && SelectedScene.MeshManager.SelectedMesh.Type == Data.Enums.MeshType.MNI;
            bool isJuBrainAtlasAvailable = ApplicationState.Module3D.JuBrainAtlas.Loaded && SelectedScene.MeshManager.SelectedMesh.Type == Data.Enums.MeshType.MNI;
            bool canUseMarsAtlas = ApplicationState.Module3D.MarsAtlas.Loaded && (SelectedScene.MeshManager.SelectedMesh.IsMarsAtlasLoaded || SelectedScene.MeshManager.SelectedMesh.Type == Data.Enums.MeshType.MNI);

            m_IBCToggle.interactable = isIBCAvailable;
            m_JubrainToggle.interactable = isJuBrainAtlasAvailable;
            m_MarsAtlasToggle.interactable = canUseMarsAtlas;
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            m_IBCToggle.isOn = SelectedScene.FMRIManager.DisplayIBCContrasts;
            m_JubrainToggle.isOn = SelectedScene.AtlasManager.DisplayJuBrainAtlas;
            m_MarsAtlasToggle.isOn = SelectedScene.AtlasManager.DisplayMarsAtlas;
        }
        #endregion
    }
}