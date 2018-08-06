using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using HBP.Data;
using System.Collections.Generic;

namespace HBP.UI.Anatomy
{
	/// <summary>
	/// Display/Modify group.
	/// </summary>
	public class GroupModifier : ItemModifier<Group> 
	{
        #region Properties
        [SerializeField] GameObject m_PatientModifierPrefab;
        List<ItemModifier<Patient>> m_Modifiers = new List<ItemModifier<Patient>>();

        [SerializeField] InputField m_NameInputField;

        [SerializeField] Button m_AddButton;
        [SerializeField] Button m_RemoveButton;

        [SerializeField] PatientList m_GroupPatientsList;
        [SerializeField] Text m_GroupPatientsCounter;

        [SerializeField] PatientList m_ProjectPatientsList;
        [SerializeField] Text m_ProjectPatientsCounter;
        #endregion

        #region Public Methods
        public void AddPatients()
		{
            Patient[] patients = m_ProjectPatientsList.ObjectsSelected;
            ItemTemp.AddPatient(patients);
            m_ProjectPatientsList.Remove(patients);
            m_GroupPatientsList.Add(patients);
            m_GroupPatientsList.Select(patients);
            m_ProjectPatientsCounter.text = m_ProjectPatientsList.ObjectsSelected.Length.ToString();
            m_GroupPatientsCounter.text = m_GroupPatientsList.ObjectsSelected.Length.ToString();
        }
        public void RemovePatients()
		{
            Data.Patient[] patients = m_GroupPatientsList.ObjectsSelected;
            ItemTemp.RemovePatient(patients);
            m_GroupPatientsList.Remove(patients);
            m_ProjectPatientsList.Add(patients);
            m_ProjectPatientsList.Select(patients);
            m_ProjectPatientsCounter.text = m_ProjectPatientsList.ObjectsSelected.Length.ToString();
            m_GroupPatientsCounter.text = m_GroupPatientsList.ObjectsSelected.Length.ToString();
        }
        public override void Close()
        {
            foreach (var modifier in m_Modifiers.ToArray()) modifier.Close();
            m_Modifiers.Clear();
            base.Close();
        }
        public void OpenPatientModifier(Patient patientToModify)
        {
            ItemModifier<Patient> patientModifier = ApplicationState.WindowsManager.OpenModifier(patientToModify, false);
            patientModifier.OnClose.AddListener(() => OnClosePatientModifier(patientModifier));
            m_Modifiers.Add(patientModifier);
        }
        #endregion

        #region Private Methods
        protected void OnClosePatientModifier(ItemModifier<Patient> modifier)
        {
            m_Modifiers.Remove(modifier);
        }
        protected override void SetFields(Group objectToDisplay)
        {
            m_NameInputField.text = ItemTemp.Name;
            m_ProjectPatientsList.Objects = (from p in ApplicationState.ProjectLoaded.Patients where !ItemTemp.Patients.Contains(p) select p).ToArray();
            m_GroupPatientsList.Objects = ItemTemp.Patients.ToArray();
            m_NameInputField.onValueChanged.AddListener((value) => ItemTemp.Name = value);
            m_GroupPatientsList.OnAction.AddListener((patient, i) => OpenPatientModifier(patient));
            m_GroupPatientsList.SortByName(PatientList.Sorting.Descending);
            m_ProjectPatientsList.OnAction.AddListener((patient, i) => OpenPatientModifier(patient));
            m_ProjectPatientsList.SortByName(PatientList.Sorting.Descending);
        }
        protected override void Initialize()
        {
            base.Initialize();
            m_ProjectPatientsList.OnSelectionChanged.AddListener(() => m_ProjectPatientsCounter.text = m_ProjectPatientsList.NumberOfItemSelected.ToString());
            m_GroupPatientsList.OnSelectionChanged.AddListener(() => m_GroupPatientsCounter.text = m_GroupPatientsList.NumberOfItemSelected.ToString());
        }
        protected override void SetInteractable(bool interactable)
        {
            base.SetInteractable(interactable);
            m_NameInputField.interactable = interactable;
            m_RemoveButton.interactable = interactable;
            m_AddButton.interactable = interactable;
        }
        #endregion
    }
}