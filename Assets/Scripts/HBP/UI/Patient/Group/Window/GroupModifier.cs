﻿using UnityEngine;
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
        [SerializeField]
        GameObject m_PatientModifierPrefab;
        List<PatientModifier> m_Modifiers = new List<PatientModifier>();

        InputField m_NameInputField;
        Button m_SaveButton, m_AddButton, m_RemoveButton;
        PatientList m_GroupPatientsList, m_ProjectPatientsList;

        Text m_projectPatientsCounter;
        Text m_groupPatientsCounter;
        #endregion

        #region Public Methods
        public void AddPatients()
		{
            Patient[] patients = m_ProjectPatientsList.ObjectsSelected;
            ItemTemp.AddPatient(patients);
            m_ProjectPatientsList.Remove(patients);
            m_GroupPatientsList.Add(patients);
            m_GroupPatientsList.Select(patients);
            m_projectPatientsCounter.text = m_ProjectPatientsList.ObjectsSelected.Length.ToString();
            m_groupPatientsCounter.text = m_GroupPatientsList.ObjectsSelected.Length.ToString();
        }
        public void RemovePatients()
		{
            Data.Patient[] patients = m_GroupPatientsList.ObjectsSelected;
            ItemTemp.RemovePatient(patients);
            m_GroupPatientsList.Remove(patients);
            m_ProjectPatientsList.Add(patients);
            m_ProjectPatientsList.Select(patients);
            m_projectPatientsCounter.text = m_ProjectPatientsList.ObjectsSelected.Length.ToString();
            m_groupPatientsCounter.text = m_GroupPatientsList.ObjectsSelected.Length.ToString();
        }
        public override void Close()
        {
            foreach (var modifier in m_Modifiers.ToArray()) modifier.Close();
            m_Modifiers.Clear();
            base.Close();
        }
        public void OpenPatientModifier(Patient patientToModify)
        {
            RectTransform obj = Instantiate(m_PatientModifierPrefab).GetComponent<RectTransform>();
            obj.SetParent(GameObject.Find("Windows").transform);
            obj.localPosition = new Vector3(0, 0, 0);
            PatientModifier patientModifier = obj.GetComponent<PatientModifier>();
            patientModifier.Open(patientToModify, false);
            patientModifier.CloseEvent.AddListener(() => OnClosePatientModifier(patientModifier));
            m_Modifiers.Add(patientModifier);
        }
        #endregion

        #region Private Methods
        protected void OnClosePatientModifier(PatientModifier modifier)
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
        protected override void SetWindow()
        {
            m_NameInputField = transform.Find("Content").Find("Name").Find("InputField").GetComponent<InputField>();
            m_ProjectPatientsList = transform.Find("Content").Find("Patients").Find("Lists").Find("Project").Find("Display").GetComponent<PatientList>();
            m_GroupPatientsList = transform.Find("Content").Find("Patients").Find("Lists").Find("Group").Find("Display").GetComponent<PatientList>();
            m_SaveButton = transform.Find("Content").Find("Buttons").Find("OK").GetComponent<Button>();
            m_AddButton = transform.Find("Content").Find("Patients").Find("Lists").Find("Buttons").Find("Add").GetComponent<Button>();
            m_RemoveButton = transform.Find("Content").Find("Patients").Find("Lists").Find("Buttons").Find("Remove").GetComponent<Button>();

            m_projectPatientsCounter = transform.Find("Content").Find("Patients").Find("Lists").Find("Project").Find("ItemSelected").Find("Counter").GetComponent<Text>();
            m_groupPatientsCounter = transform.Find("Content").Find("Patients").Find("Lists").Find("Group").Find("ItemSelected").Find("Counter").GetComponent<Text>();

            m_ProjectPatientsList.OnSelectionChanged.AddListener((patient, i) => m_projectPatientsCounter.text = m_ProjectPatientsList.ObjectsSelected.Length.ToString());
            m_GroupPatientsList.OnSelectionChanged.AddListener((patient, i) => m_groupPatientsCounter.text = m_GroupPatientsList.ObjectsSelected.Length.ToString());
        }
        protected override void SetInteractableFields(bool interactable)
        {
            m_NameInputField.interactable = interactable;
            m_SaveButton.interactable = interactable;
            m_RemoveButton.interactable = interactable;
            m_AddButton.interactable = interactable;
        }
        #endregion
    }
}