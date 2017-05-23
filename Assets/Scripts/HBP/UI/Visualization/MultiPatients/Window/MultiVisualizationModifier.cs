using UnityEngine.UI;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using HBP.UI.Patient;
using HBP.Data.Visualization;
using HBP.Data;

namespace HBP.UI.Visualization
{
    public class MultiVisualizationModifier : ItemModifier<MultiPatientsVisualization>
    {
        #region Properties
        [SerializeField]
        GameObject groupSelectionPrefab;
        GroupSelection groupSelection;

        InputField nameInputField;
        TabGestion tabGestion;
        ColumnModifier columnModifier;
        PatientList visualizationPatientsList;
        PatientList projectPatientsList;
        Button addPatientButton, removePatientButton, addGroupButton, saveButton, saveAsButton;
        #endregion

        #region Public Methods
        public void AddColumn()
        {
            ItemTemp.Columns.Add(new Column());
            tabGestion.AddTab();
        }
        public void RemoveColumn()
        {
            Toggle l_Toggle = new List<Toggle>(tabGestion.ToggleGroup.ActiveToggles())[0];
            ItemTemp.Columns.RemoveAt(l_Toggle.transform.GetSiblingIndex() - 1);
            tabGestion.RemoveTab();
        }
        public void SaveAs()
        {

        }
        public void AddPatients()
        {
            Data.Patient[] patientsToAdd = projectPatientsList.GetObjectsSelected();
            ItemTemp.AddPatient(patientsToAdd);
            projectPatientsList.DeactivateObject(patientsToAdd);
            visualizationPatientsList.Add(patientsToAdd);
            SelectColumn();
        }
        public void AddGroups(Group[] groups)
        {
            List<Data.Patient> patientsToAdd = new List<Data.Patient>();
            foreach (Group group in groups)
            {
                foreach(Data.Patient patient in group.Patients)
                {
                    if(!visualizationPatientsList.Objects.Contains(patient))
                    {
                        patientsToAdd.Add(patient);
                    }
                }
            }
            ItemTemp.AddPatient(patientsToAdd.ToArray());
            projectPatientsList.DeactivateObject(patientsToAdd.ToArray());
            visualizationPatientsList.Add(patientsToAdd.ToArray());
            SelectColumn();
        }
        public void OpenGroupSelection()
        {
            SetInteractable(false);
            Transform groupsSelectionTransform = Instantiate(groupSelectionPrefab, GameObject.Find("Windows").transform).GetComponent<Transform>();
            groupsSelectionTransform.localPosition = Vector3.zero;
            GroupSelection groupSelection = groupsSelectionTransform.GetComponent<GroupSelection>();
            Debug.Log(groupSelection);
            groupSelection.Open();
            groupSelection.AddGroupsEvent.AddListener((groups) => AddGroups(groups));
            groupSelection.CloseEvent.AddListener(() => OnCloseGroupSelection());
        }
        public void RemovePatients()
        {
            Data.Patient[] patientsToRemove = visualizationPatientsList.GetObjectsSelected();
            ItemTemp.RemovePatient(patientsToRemove);
            projectPatientsList.ActiveObject(patientsToRemove);
            visualizationPatientsList.Remove(patientsToRemove);
            SelectColumn();
        }
        #endregion

        #region Private Methods
        protected void SwapColumns(int i1, int i2)
        {
            ItemTemp.SwapColumns(i1, i2);
        }
        protected override void SetFields(MultiPatientsVisualization objectToDisplay)
        {
            // Name.
            nameInputField.text = objectToDisplay.Name;
            nameInputField.onValueChanged.AddListener((value) => objectToDisplay.Name = value);

            tabGestion.OnSwapColumnsEvent.AddListener((c1, c2) => SwapColumns(c1, c2));
            tabGestion.OnChangeSelectedTabEvent.AddListener(() => SelectColumn());

            // Columns.
            if (ItemTemp.Columns.Count == 0)
            {
                ItemTemp.Columns.Add(new Column());
            }
            for (int i = 0; i < ItemTemp.Columns.Count; i++)
            {
                tabGestion.AddTab();
            }

            visualizationPatientsList.Display(objectToDisplay.Patients.ToArray());
            projectPatientsList.Display(ApplicationState.ProjectLoaded.Patients.ToArray(), objectToDisplay.Patients.ToArray());
        }
        protected void SelectColumn()
        {
            List<Toggle> ActiveToggles = new List<Toggle>(tabGestion.ToggleGroup.ActiveToggles());
            if (ActiveToggles.Count > 0)
            {
                Column l_column = ItemTemp.Columns[ActiveToggles[0].transform.GetSiblingIndex() - 1];
                columnModifier.SetTab(l_column, (ItemTemp as MultiPatientsVisualization).Patients.ToArray(), true);
            }
        }
        protected override void SetWindow()
        {
            nameInputField = transform.Find("Content").Find("General").Find("Name").Find("InputField").GetComponent<InputField>();
            tabGestion = transform.Find("Content").Find("Columns").Find("Tabs").GetComponent<TabGestion>();
            columnModifier = transform.Find("Content").Find("Columns").Find("Column").GetComponent<ColumnModifier>();
            saveButton = transform.Find("Content").Find("Buttons").Find("Save").GetComponent<Button>();
            saveAsButton = transform.Find("Content").Find("Buttons").Find("SaveAs").GetComponent<Button>();
            visualizationPatientsList = transform.Find("Content").Find("Patients").Find("Lists").Find("PatientToDisplay").Find("Container").Find("Scrollable").GetComponent<PatientList>();
            projectPatientsList = transform.Find("Content").Find("Patients").Find("Lists").Find("AllPatients").Find("Container").Find("Scrollable").GetComponent<PatientList>();
            groupSelection = GetComponent<GroupSelection>();
            addPatientButton = transform.Find("Content").Find("Patients").Find("Lists").Find("Buttons").Find("Add").GetComponent<Button>();
            removePatientButton = transform.Find("Content").Find("Patients").Find("Lists").Find("Buttons").Find("Remove").GetComponent<Button>();
            addGroupButton = transform.Find("Content").Find("Patients").Find("Lists").Find("Buttons").Find("AddGroup").GetComponent<Button>();
        }
        protected override void SetInteractableFields(bool interactable)
        {
            addPatientButton.interactable = interactable;
            removePatientButton.interactable = interactable;
            addGroupButton.interactable = interactable;
            nameInputField.interactable = interactable;
            saveButton.interactable = interactable;
            saveAsButton.interactable = interactable;
        }
        protected void OnCloseGroupSelection()
        {
            SetInteractable(true);
            groupSelection = null;
        }
        #endregion
    }
}