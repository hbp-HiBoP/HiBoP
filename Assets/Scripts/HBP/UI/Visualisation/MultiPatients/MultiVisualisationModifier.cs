using UnityEngine.UI;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using d = HBP.Data.Patient;
using HBP.UI.Patient;
using HBP.Data.Visualisation;

namespace HBP.UI
{
    public class MultiVisualisationModifier : ItemModifier<MultiPatientsVisualisation>
    {
        #region Properties
        InputField nameInputField;
        TabGestion tabGestion;
        ColumnModifier columnModifier;
        PatientList visualisationPatientsList;
        PatientList projectPatientsList;
        GroupSelection groupSelection;
        Button addPatientButton, removePatientButton, addGroupButton, saveButton, saveAsButton;
        #endregion

        #region Public Methods
        public void AddColumn()
        {
            ItemTemp.AddColumn(new Column());
            tabGestion.AddTab();
        }
        public void RemoveColumn()
        {
            Toggle l_Toggle = new List<Toggle>(tabGestion.ToggleGroup.ActiveToggles())[0];
            ItemTemp.RemoveColumn(l_Toggle.transform.GetSiblingIndex() - 1);
            tabGestion.RemoveTab();
        }
        public void SaveAs()
        {

        }
        public void AddPatients()
        {
            d.Patient[] patientsToAdd = projectPatientsList.GetObjectsSelected();
            ItemTemp.AddPatient(patientsToAdd);
            projectPatientsList.DeactivateObject(patientsToAdd);
            visualisationPatientsList.Add(patientsToAdd);
            SelectColumn();
        }
        public void AddGroup()
        {
            List<d.Patient> patientsToAdd = new List<d.Patient>();
            d.Group[] groups = groupSelection.GetGroupSelected();
            foreach (d.Group group in groups)
            {
                patientsToAdd.AddRange(group.Patients.ToArray());
            }
            ItemTemp.AddPatient(patientsToAdd.ToArray());
            projectPatientsList.DeactivateObject(patientsToAdd.ToArray());
            visualisationPatientsList.Add(patientsToAdd.ToArray());
            SelectColumn();
        }
        public void RemovePatients()
        {
            d.Patient[] patientsToRemove = visualisationPatientsList.GetObjectsSelected();
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
                ItemTemp.AddColumn(new Column());
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
        #endregion
    }
}