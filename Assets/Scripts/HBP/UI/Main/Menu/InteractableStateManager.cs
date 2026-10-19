using UnityEngine;
using HBP.Core.Tools;
using HBP.Core.Database;

namespace HBP.UI.Main
{
    public class InteractableStateManager : Manager<InteractableStateManager>
    {
        #region Properties

        private InteractableConditions[] m_Interactables;

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

        protected override void Initialization()
        {
            base.Initialization();
            m_Interactables = FindObjectsByType<InteractableConditions>(FindObjectsInactive.Include);
        }

        void Start()
        {
            SetInteractables();
        }

        void SetInteractable(InteractableConditions interactableConditions)
        {
            bool interactable = true;
            if (interactableConditions.NeedProject)
            {
                if (ApplicationState.LoadedProject == null)
                {
                    interactable = false;
                }
            }

            if (interactableConditions.NeedPatient)
            {
                if (ApplicationState.LoadedProject != null && ApplicationState.LoadedProject.Patients.Count == 0)
                {
                    interactable = false;
                }
            }

            if (interactableConditions.NeedGroup)
            {
                if (ApplicationState.LoadedProject != null && ApplicationState.LoadedProject.Groups.Count == 0)
                {
                    interactable = false;
                }
            }

            if (interactableConditions.NeedProtocol)
            {
                if (DatabaseManager.Database.Protocols.Count == 0)
                {
                    interactable = false;
                }
            }

            if (interactableConditions.NeedDataset)
            {
                if (ApplicationState.LoadedProject != null && ApplicationState.LoadedProject.Datasets.Count == 0)
                {
                    interactable = false;
                }
            }

            interactableConditions.interactable = interactable;
        }

        #endregion
    }
}
