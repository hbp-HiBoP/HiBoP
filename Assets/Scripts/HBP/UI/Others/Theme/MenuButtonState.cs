using UnityEngine;

public class MenuButtonState : MonoBehaviour
{
    #region Properties
    [SerializeField]
    ButtonGestion[] m_buttons;
    #endregion

    #region Public Methods
    public void SetInteractableButtons()
    {
        foreach(ButtonGestion b in m_buttons)
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

    void SetInteractableButton(ButtonGestion buttonGestion)
    {
        bool l_interactable = true;
        if(buttonGestion.NeedProject)
        {
            if(ApplicationState.ProjectLoaded == null)
            {
                l_interactable = false;
            }
        }
        if(buttonGestion.NeedPatients)
        {
            if(ApplicationState.ProjectLoaded != null && ApplicationState.ProjectLoaded.Patients.Count == 0)
            {
                l_interactable = false;
            }
        }
        if(buttonGestion.NeedGroups)
        {
            if(ApplicationState.ProjectLoaded != null && ApplicationState.ProjectLoaded.Groups.Count == 0)
            {
                l_interactable = false;
            }
        }
        if(buttonGestion.NeedProtocols)
        {
            if(ApplicationState.ProjectLoaded != null && ApplicationState.ProjectLoaded.Protocols.Count == 0)
            {
                l_interactable = false;
            }
        }
        if (buttonGestion.NeedDataset)
        {
            if(ApplicationState.ProjectLoaded != null && ApplicationState.ProjectLoaded.Datasets.Count == 0)
            {
                l_interactable = false;
            }
        }
        buttonGestion.interactable = l_interactable;
        //Debug.Log("Name: " + buttonGestion.name + " Interactable: " + l_interactable);
    }
    #endregion
}
