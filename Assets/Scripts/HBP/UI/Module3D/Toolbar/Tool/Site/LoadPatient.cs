using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class LoadPatient : Tool
    {
        #region Properties
        [SerializeField]
        private Button m_Button;

        private HBP.Module3D.Site m_LastSelectedSite;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Button.onClick.AddListener(() =>
            {
                ApplicationState.Module3D.LoadSinglePatientSceneFromMultiPatientScene(SelectedScene.Visualization, SelectedScene.Visualization.Patients[SelectedScene.SelectedColumn.SelectedPatientID]);
            });
        }
        public override void DefaultState()
        {
            m_Button.interactable = false;
        }
        public override void UpdateInteractable()
        {
            bool isInteractable = (SelectedColumn.SelectedSite != null) && (SelectedScene.Type == Data.Enums.SceneType.MultiPatients);

            m_Button.interactable = isInteractable;
        }
        #endregion
    }
}