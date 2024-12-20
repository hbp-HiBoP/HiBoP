using UnityEngine;
using HBP.Core.Tools;

namespace HBP.UI.Tools
{
    public class MenuButtonState : Singleton<MenuButtonState>
    {
        #region Properties
        [SerializeField] InteractableConditions[] m_Interactables;
        #endregion

        #region Public Methods
        public static void SetInteractables()
        {
            foreach (InteractableConditions b in m_Instance.m_Interactables)
            {
                m_Instance.SetInteractable(b);
            }
        }
        #endregion

        #region Private Methods
        void Start()
        {
            SetInteractables();
        }

        void SetInteractable(InteractableConditions buttonGestion)
        {
            bool interactable = true;
            if (buttonGestion.NeedProject)
            {
                if (ApplicationState.ProjectLoaded == null)
                {
                    interactable = false;
                }
            }
            if (buttonGestion.NeedPatient)
            {
                if (ApplicationState.ProjectLoaded != null && ApplicationState.ProjectLoaded.Patients.Count == 0)
                {
                    interactable = false;
                }
            }
            if (buttonGestion.NeedGroup)
            {
                if (ApplicationState.ProjectLoaded != null && ApplicationState.ProjectLoaded.Groups.Count == 0)
                {
                    interactable = false;
                }
            }
            if (buttonGestion.NeedProtocol)
            {
                if (ApplicationState.ProjectLoaded != null && ApplicationState.ProjectLoaded.Protocols.Count == 0)
                {
                    interactable = false;
                }
            }
            if (buttonGestion.NeedDataset)
            {
                if (ApplicationState.ProjectLoaded != null && ApplicationState.ProjectLoaded.Datasets.Count == 0)
                {
                    interactable = false;
                }
            }
            buttonGestion.interactable = interactable;
        }
        #endregion
    }
}