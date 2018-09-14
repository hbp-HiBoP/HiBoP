using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using HBP.Data;

namespace HBP.UI.Anatomy
{
	/// <summary>
	/// Display/Modify group.
	/// </summary>
	public class GroupModifier : ItemModifier<Group> 
	{
        #region Properties
        [SerializeField] InputField m_NameInputField;

        [SerializeField] Button m_AddButton, m_RemoveButton;
        [SerializeField] PatientListGestion m_GroupPatientListGestion, m_ProjectPatientListGestion;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;

                m_NameInputField.interactable = value;

                m_GroupPatientListGestion.Interactable = false;
                m_ProjectPatientListGestion.Interactable = false;

                m_RemoveButton.interactable = value;
                m_AddButton.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public void AddPatients()
		{
            m_GroupPatientListGestion.Add(m_ProjectPatientListGestion.List.ObjectsSelected);
            m_ProjectPatientListGestion.RemoveSelected();
        }
        public void RemovePatients()
		{
            m_ProjectPatientListGestion.Add(m_GroupPatientListGestion.List.ObjectsSelected);
            m_GroupPatientListGestion.RemoveSelected();
        }
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onValueChanged.RemoveAllListeners();
            m_NameInputField.onValueChanged.AddListener((value) => ItemTemp.Name = value);

            m_ProjectPatientListGestion.Initialize(m_SubWindows);
            m_GroupPatientListGestion.Initialize(m_SubWindows);
        }
        protected override void SetFields(Group objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_ProjectPatientListGestion.Items = ApplicationState.ProjectLoaded.Patients.Where(p => !objectToDisplay.Patients.Contains(p)).ToList();
            m_GroupPatientListGestion.Items = objectToDisplay.Patients;
        }
        #endregion
    }
}