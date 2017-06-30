using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections.Generic;
using HBP.Data;
using HBP.UI.Anatomy;
using HBP.Data.Visualization;

namespace HBP.UI.Visualization
{
    public class VisualizationModifier : ItemModifier<Data.Visualization.Visualization>
    {
        #region Properties
        [SerializeField]
        GameObject m_GroupSelectionPrefab;
        GroupSelection m_GroupSelection;

        InputField m_NameInputField;
        TabGestion m_TabGestion;
        ColumnModifier m_ColumnModifier;
        PatientList m_VisualizationPatientsList;
        PatientList m_ProjectPatientsList;
        Button m_AddPatientButton, m_RemovePatientButton, m_AddGroupButton, m_SaveButton, m_SaveAsButton;
        #endregion

        #region Public Methods
        public void AddColumn()
        {
            ItemTemp.Columns.Add(new Column());
            m_TabGestion.AddTab();
        }
        public void RemoveColumn()
        {
            Toggle toggle = new List<Toggle>(m_TabGestion.ToggleGroup.ActiveToggles())[0];
            ItemTemp.Columns.RemoveAt(toggle.transform.GetSiblingIndex() - 1);
            m_TabGestion.RemoveTab();
        }
        public void SaveAs()
        {

        }
        public void AddPatients()
        {
            Data.Patient[] patientsToAdd = m_ProjectPatientsList.GetObjectsSelected();
            ItemTemp.AddPatient(patientsToAdd);
            m_ProjectPatientsList.DeactivateObject(patientsToAdd);
            m_VisualizationPatientsList.Add(patientsToAdd);
            SelectColumn();
        }
        public void AddGroups(Group[] groups)
        {
            List<Data.Patient> patientsToAdd = new List<Data.Patient>();
            foreach (Group group in groups)
            {
                foreach(Data.Patient patient in group.Patients)
                {
                    if(!m_VisualizationPatientsList.Objects.Contains(patient))
                    {
                        patientsToAdd.Add(patient);
                    }
                }
            }
            ItemTemp.AddPatient(patientsToAdd.ToArray());
            m_ProjectPatientsList.DeactivateObject(patientsToAdd.ToArray());
            m_VisualizationPatientsList.Add(patientsToAdd.ToArray());
            SelectColumn();
        }
        public void OpenGroupSelection()
        {
            SetInteractable(false);
            Transform groupsSelectionTransform = Instantiate(m_GroupSelectionPrefab, GameObject.Find("Windows").transform).GetComponent<Transform>();
            groupsSelectionTransform.localPosition = Vector3.zero;
            GroupSelection groupSelection = groupsSelectionTransform.GetComponent<GroupSelection>();
            groupSelection.Open();
            groupSelection.AddGroupsEvent.AddListener((groups) => AddGroups(groups));
            groupSelection.CloseEvent.AddListener(() => OnCloseGroupSelection());
        }
        public void RemovePatients()
        {
            Data.Patient[] patientsToRemove = m_VisualizationPatientsList.GetObjectsSelected();
            ItemTemp.RemovePatient(patientsToRemove);
            m_ProjectPatientsList.ActiveObject(patientsToRemove);
            m_VisualizationPatientsList.Remove(patientsToRemove);
            SelectColumn();
        }
        #endregion

        #region Private Methods
        protected void SwapColumns(int i1, int i2)
        {
            ItemTemp.SwapColumns(i1, i2);
        }
        protected override void SetFields(Data.Visualization.Visualization objectToDisplay)
        {
            SetName(objectToDisplay);
            SetPatients(objectToDisplay);
            SetTabs(objectToDisplay);
            SetColumns(objectToDisplay);
        }
        protected void SelectColumn()
        {
            List<Toggle> ActiveToggles = new List<Toggle>(m_TabGestion.ToggleGroup.ActiveToggles());
            if (ActiveToggles.Count > 0)
            {
                Column l_column = ItemTemp.Columns[ActiveToggles[0].transform.GetSiblingIndex() - 1];
                m_ColumnModifier.SetTab(l_column, ItemTemp.Patients.ToArray());
            }
        }
        protected override void SetWindow()
        {
            m_NameInputField = transform.Find("Content").Find("General").Find("Name").Find("InputField").GetComponent<InputField>();
            m_TabGestion = transform.Find("Content").Find("Columns").Find("Tabs").GetComponent<TabGestion>();
            m_ColumnModifier = transform.Find("Content").Find("Columns").Find("Column").GetComponent<ColumnModifier>();
            m_SaveButton = transform.Find("Content").Find("Buttons").Find("Save").GetComponent<Button>();
            m_SaveAsButton = transform.Find("Content").Find("Buttons").Find("SaveAs").GetComponent<Button>();
            m_VisualizationPatientsList = transform.Find("Content").Find("Patients").Find("Lists").Find("PatientToDisplay").Find("Container").Find("Scrollable").GetComponent<PatientList>();
            m_ProjectPatientsList = transform.Find("Content").Find("Patients").Find("Lists").Find("AllPatients").Find("Container").Find("Scrollable").GetComponent<PatientList>();
            m_GroupSelection = GetComponent<GroupSelection>();
            m_AddPatientButton = transform.Find("Content").Find("Patients").Find("Lists").Find("Buttons").Find("Add").GetComponent<Button>();
            m_RemovePatientButton = transform.Find("Content").Find("Patients").Find("Lists").Find("Buttons").Find("Remove").GetComponent<Button>();
            m_AddGroupButton = transform.Find("Content").Find("Patients").Find("Lists").Find("Buttons").Find("AddGroup").GetComponent<Button>();
        }
        protected override void SetInteractableFields(bool interactable)
        {
            m_AddPatientButton.interactable = interactable;
            m_RemovePatientButton.interactable = interactable;
            m_AddGroupButton.interactable = interactable;
            m_NameInputField.interactable = interactable;
            m_SaveButton.interactable = interactable;
            m_SaveAsButton.interactable = interactable;
        }
        protected void OnCloseGroupSelection()
        {
            SetInteractable(true);
            m_GroupSelection = null;
        }
        protected void SetName(Data.Visualization.Visualization objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_NameInputField.onValueChanged.AddListener((value) => objectToDisplay.Name = value);
        }
        protected void SetPatients(Data.Visualization.Visualization objectToDisplay)
        {
            m_VisualizationPatientsList.Display(objectToDisplay.Patients.ToArray());
            m_ProjectPatientsList.Display(ApplicationState.ProjectLoaded.Patients.ToArray(), ItemTemp.Patients.ToArray());
        }
        protected void SetTabs(Data.Visualization.Visualization objectToDisplay)
        {
            m_TabGestion.OnSwapColumnsEvent.AddListener((c1, c2) => SwapColumns(c1, c2));
            m_TabGestion.OnChangeSelectedTabEvent.AddListener(() => SelectColumn());
        }
        protected void SetColumns(Data.Visualization.Visualization objectToDisplay)
        {
            // Columns.
            if (objectToDisplay.Columns.Count == 0)
            {
                objectToDisplay.Columns.Add(new Column());
            }
            for (int i = 0; i < objectToDisplay.Columns.Count; i++)
            {
                m_TabGestion.AddTab();
            }
        }
        #endregion
    }
}