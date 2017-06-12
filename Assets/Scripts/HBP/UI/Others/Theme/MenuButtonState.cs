using UnityEngine;

namespace HBP.UI
{
    public class MenuButtonState : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        InteractableConditions[] m_buttons;
        #endregion

        #region Public Methods
        public void SetInteractableButtons()
        {
            foreach (InteractableConditions b in m_buttons)
            {
                SetInteractableButton(b);
            }
        }
        #endregion

        #region Private Methods
        void Start()
        {
            SetInteractableButtons();
        }

        void SetInteractableButton(InteractableConditions buttonGestion)
        {
            bool l_interactable = true;
            if (buttonGestion.NeedProject)
            {
                if (ApplicationState.ProjectLoaded == null)
                {
                    l_interactable = false;
                }
            }
            if (buttonGestion.NeedPatient)
            {
                if (ApplicationState.ProjectLoaded != null && ApplicationState.ProjectLoaded.Patients.Count == 0)
                {
                    l_interactable = false;
                }
            }
            if (buttonGestion.NeedGroup)
            {
                if (ApplicationState.ProjectLoaded != null && ApplicationState.ProjectLoaded.Groups.Count == 0)
                {
                    l_interactable = false;
                }
            }
            if (buttonGestion.NeedProtocol)
            {
                if (ApplicationState.ProjectLoaded != null && ApplicationState.ProjectLoaded.Protocols.Count == 0)
                {
                    l_interactable = false;
                }
            }
            if (buttonGestion.NeedDataset)
            {
                if (ApplicationState.ProjectLoaded != null && ApplicationState.ProjectLoaded.Datasets.Count == 0)
                {
                    l_interactable = false;
                }
            }
            buttonGestion.interactable = l_interactable;
            //Debug.Log("Name: " + buttonGestion.name + " Interactable: " + l_interactable);
        }
        #endregion
    }
}