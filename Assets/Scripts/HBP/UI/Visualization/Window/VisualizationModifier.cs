using UnityEngine;
using UnityEngine.UI;
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
        [SerializeField] GameObject m_RemoveGroupPrefab;
        [SerializeField] GameObject m_AddGroupPrefab;

        InputField m_NameInputField;
        TabGestion m_TabGestion;
        ColumnModifier m_ColumnModifier;
        PatientList m_VisualizationPatientsList;
        PatientList m_ProjectPatientsList;
        Button m_AddPatientButton, m_RemovePatientButton, m_AddGroupButton, m_SaveButton;
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
        public void AddPatients()
        {
            Patient[] patientsToAdd = m_ProjectPatientsList.ObjectsSelected;
            ItemTemp.AddPatient(patientsToAdd);
            m_ProjectPatientsList.Remove(patientsToAdd);
            m_VisualizationPatientsList.Add(patientsToAdd);
            SelectColumn();
        }
        public void AddGroups(Group[] groups)
        {
            List<Patient> patientsToAdd = new List<Patient>();
            foreach (Group group in groups)
            {
                foreach(Patient patient in group.Patients)
                {
                    if(!m_VisualizationPatientsList.Objects.Contains(patient))
                    {
                        patientsToAdd.Add(patient);
                    }
                }
            }
            ItemTemp.AddPatient(patientsToAdd.ToArray());
            m_ProjectPatientsList.Remove(patientsToAdd.ToArray());
            m_VisualizationPatientsList.Add(patientsToAdd.ToArray());
            SelectColumn();
        }
        public void RemoveGroups(Group[] groups)
        {
            List<Patient> patientsToRemove = new List<Patient>();
            foreach (Group group in groups)
            {
                foreach (Patient patient in group.Patients)
                {
                    if (m_VisualizationPatientsList.Objects.Contains(patient))
                    {
                        patientsToRemove.Add(patient);
                    }
                }
            }
            ItemTemp.RemovePatient(patientsToRemove.ToArray());
            m_ProjectPatientsList.Add(patientsToRemove.ToArray());
            m_VisualizationPatientsList.Remove(patientsToRemove.ToArray());
            SelectColumn();
        }
        public void OpenAddGroupWindow()
        {
            SetInteractable(false);
            Transform groupsSelectionTransform = Instantiate(m_AddGroupPrefab, GameObject.Find("Windows").transform).GetComponent<Transform>();
            groupsSelectionTransform.localPosition = Vector3.zero;
            GroupSelection groupSelection = groupsSelectionTransform.GetComponent<GroupSelection>();
            groupSelection.Open();
            groupSelection.GroupsSelectedEvent.AddListener((groups) => AddGroups(groups));
            groupSelection.CloseEvent.AddListener(() => OnCloseGroupSelection());
        }
        public void OpenRemoveGroupWindow()
        {
            SetInteractable(false);
            Transform groupsSelectionTransform = Instantiate(m_RemoveGroupPrefab, GameObject.Find("Windows").transform).GetComponent<Transform>();
            groupsSelectionTransform.localPosition = Vector3.zero;
            GroupSelection groupSelection = groupsSelectionTransform.GetComponent<GroupSelection>();
            groupSelection.Open();
            groupSelection.GroupsSelectedEvent.AddListener((groups) => RemoveGroups(groups));
            groupSelection.CloseEvent.AddListener(() => OnCloseGroupSelection());
        }
        public void RemovePatients()
        {
            Data.Patient[] patientsToRemove = m_VisualizationPatientsList.ObjectsSelected;
            ItemTemp.RemovePatient(patientsToRemove);
            m_ProjectPatientsList.Add(patientsToRemove);
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
            m_TabGestion = transform.Find("Content").Find("Columns").Find("Fields").Find("Tabs").GetComponent<TabGestion>();
            m_ColumnModifier = transform.Find("Content").Find("Columns").Find("Fields").Find("Column").GetComponent<ColumnModifier>();
            m_SaveButton = transform.Find("Content").Find("Buttons").Find("OK").GetComponent<Button>();
            m_VisualizationPatientsList = transform.Find("Content").Find("Patients").Find("Lists").Find("Visualization").Find("Display").Find("Viewport").Find("Content").GetComponent<PatientList>();
            m_ProjectPatientsList = transform.Find("Content").Find("Patients").Find("Lists").Find("Project").Find("Display").Find("Viewport").Find("Content").GetComponent<PatientList>();
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
        }
        protected void OnCloseGroupSelection()
        {
            SetInteractable(true);
        }
        protected void SetName(Data.Visualization.Visualization objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_NameInputField.onValueChanged.AddListener((value) => objectToDisplay.Name = value);
        }
        protected void SetPatients(Data.Visualization.Visualization objectToDisplay)
        {
            m_VisualizationPatientsList.Objects = objectToDisplay.Patients.ToArray();
            m_ProjectPatientsList.Objects = (from p in ApplicationState.ProjectLoaded.Patients where !objectToDisplay.Patients.Contains(p) select p).ToArray();
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