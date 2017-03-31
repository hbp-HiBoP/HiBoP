using UnityEngine.UI;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using HBP.UI.Patient;
using HBP.Data.Visualisation;
using HBP.Data;

namespace HBP.UI
{
    public class MultiVisualisationModifier : ItemModifier<MultiPatientsVisualisation>
    {
        #region Properties
        [SerializeField]
        GameObject groupSelectionPrefab;
        GroupSelection groupSelection;

        InputField nameInputField;
        TabGestion tabGestion;
        ColumnModifier columnModifier;
        PatientList visualisationPatientsList;
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
            visualisationPatientsList.Add(patientsToAdd);
            SelectColumn();
        }
        public void AddGroups(Group[] groups)
        {
            List<Data.Patient> patientsToAdd = new List<Data.Patient>();
            foreach (Group group in groups)
            {
                foreach(Data.Patient patient in group.Patients)
                {
                    if(!visualisationPatientsList.Objects.Contains(patient))
                    {
                        patientsToAdd.Add(patient);
                    }
                }
            }
            ItemTemp.AddPatient(patientsToAdd.ToArray());
            projectPatientsList.DeactivateObject(patientsToAdd.ToArray());
            visualisationPatientsList.Add(patientsToAdd.ToArray());
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
            Data.Patient[] patientsToRemove = visualisationPatientsList.GetObjectsSelected();
            ItemTemp.RemovePatient(patientsToRemove);
            projectPatientsList.ActiveObject(patientsToRemove);
            visualisationPatientsList.Remove(patientsToRemove);
            SelectColumn();
        }
        #endregion

        #region Private Methods
        protected void SwapColumns(int i1, int i2)
        {
            ItemTemp.SwapColumns(i1, i2);
        }
        protected override void SetFields(MultiPatientsVisualisation objectToDisplay)
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

            visualisationPatientsList.Display(objectToDisplay.Patients.ToArray());
            projectPatientsList.Display(ApplicationState.ProjectLoaded.Patients.ToArray(), objectToDisplay.Patients.ToArray());
        }
        protected void SelectColumn()
        {
            List<Toggle> ActiveToggles = new List<Toggle>(tabGestion.ToggleGroup.ActiveToggles());
            if (ActiveToggles.Count > 0)
            {
                Column l_column = ItemTemp.Columns[ActiveToggles[0].transform.GetSiblingIndex() - 1];
                columnModifier.SetTab(l_column, (ItemTemp as MultiPatientsVisualisation).Patients.ToArray(), true);
            }
        }
        protected override void SetWindow()
        {
            nameInputField = transform.FindChild("Content").FindChild("General").FindChild("Name").FindChild("InputField").GetComponent<InputField>();
            tabGestion = transform.FindChild("Content").FindChild("Columns").FindChild("Tabs").GetComponent<TabGestion>();
            columnModifier = transform.FindChild("Content").FindChild("Columns").FindChild("Column").GetComponent<ColumnModifier>();
            saveButton = transform.FindChild("Content").FindChild("Buttons").FindChild("Save").GetComponent<Button>();
            saveAsButton = transform.FindChild("Content").FindChild("Buttons").FindChild("SaveAs").GetComponent<Button>();
            visualisationPatientsList = transform.Find("Content").FindChild("Patients").FindChild("Lists").FindChild("PatientToDisplay").FindChild("Container").FindChild("Scrollable").GetComponent<PatientList>();
            projectPatientsList = transform.Find("Content").FindChild("Patients").FindChild("Lists").FindChild("AllPatients").FindChild("Container").FindChild("Scrollable").GetComponent<PatientList>();
            groupSelection = GetComponent<GroupSelection>();
            addPatientButton = transform.Find("Content").FindChild("Patients").FindChild("Lists").FindChild("Buttons").FindChild("Add").GetComponent<Button>();
            removePatientButton = transform.Find("Content").FindChild("Patients").FindChild("Lists").FindChild("Buttons").FindChild("Remove").GetComponent<Button>();
            addGroupButton = transform.Find("Content").FindChild("Patients").FindChild("Lists").FindChild("Buttons").FindChild("AddGroup").GetComponent<Button>();
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