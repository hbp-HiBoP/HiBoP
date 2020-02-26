using UnityEngine;

namespace HBP.UI
{
    public class ExperienceMenu : Menu
    {
        #region Properties
        [SerializeField] InteractableConditions m_ProtocolsInteractableConditions;
        public InteractableConditions ProtocolsInteractableConditions { get { return m_ProtocolsInteractableConditions; } }
        [SerializeField] InteractableConditions m_DatasetsInteractableConditions;
        public InteractableConditions DatasetsInteractableConditions { get { return m_DatasetsInteractableConditions; } }
        #endregion

        #region Public Methods
        public void OpenProtocolGestion()
        {
            ApplicationState.WindowsManager.Open("Protocol gestion window", true);
        }
        public void OpenDatasetGestion()
        {
            ApplicationState.WindowsManager.Open("Dataset gestion window", true);
        }
        #endregion
    }
}