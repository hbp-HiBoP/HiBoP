using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using HBP.Data.Visualization;

namespace HBP.UI.Visualization
{
    public class SingleVisualizationModifier : ItemModifier<SinglePatientVisualization>
    {
        #region Properties
        InputField nameInputField;
        TabGestion tabGestion;
        ColumnModifier columnModifier;
        Button saveButton, saveAsButton;

        Dropdown patientDropdown;
        Data.Patient[] patientsSorted = new Data.Patient[0];
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
        #endregion

        #region Private Methods
        protected void SwapColumns(int i1, int i2)
        {
            ItemTemp.SwapColumns(i1, i2);
        }
        protected override void SetFields(SinglePatientVisualization objectToDisplay)
        {
            // Name.
            nameInputField.text = objectToDisplay.Name;
            nameInputField.onValueChanged.AddListener((value) => objectToDisplay.Name = value);

            // Columns.
            if (ItemTemp.Columns.Count == 0)
            {
                ItemTemp.Columns.Add(new Column());
            }
            for (int i = 0; i < ItemTemp.Columns.Count; i++)
            {
                tabGestion.AddTab();
            }
            tabGestion.OnSwapColumnsEvent.AddListener((c1, c2) => SwapColumns(c1, c2));
            tabGestion.OnChangeSelectedTabEvent.AddListener(() => SelectColumn());

            int patientIndex = 0;
            patientsSorted = ApplicationState.ProjectLoaded.Patients.OrderBy(a => a.Name).ToArray();
            List<Dropdown.OptionData> patientOptions = new List<Dropdown.OptionData>(patientsSorted.Length);
            for (int i = 0; i < patientsSorted.Length; i++)
            {
                Data.Patient patient = patientsSorted[i];
                patientOptions.Add(new Dropdown.OptionData(patient.Name, null));
                if (objectToDisplay.Patient == patient) patientIndex = i;
            }
            patientDropdown.options = patientOptions;
            patientDropdown.value = patientIndex;
            OnChangePatient(patientsSorted[patientIndex]);
            patientDropdown.onValueChanged.AddListener((value) => OnChangePatient(patientsSorted[value]));
        }
        protected void SelectColumn()
        {
            List<Toggle> ActiveToggles = new List<Toggle>(tabGestion.ToggleGroup.ActiveToggles());
            if (ActiveToggles.Count > 0)
            {
                Column l_column = ItemTemp.Columns[ActiveToggles[0].transform.GetSiblingIndex() - 1];
                columnModifier.SetTab(l_column, new Data.Patient[] { (ItemTemp as SinglePatientVisualization).Patient }, true);
            }
        }
        protected override void SetWindow()
        {
            nameInputField = transform.FindChild("Content").FindChild("General").FindChild("Name").FindChild("InputField").GetComponent<InputField>();
            tabGestion = transform.FindChild("Content").FindChild("Columns").FindChild("Tabs").GetComponent<TabGestion>();
            columnModifier = transform.FindChild("Content").FindChild("Columns").FindChild("Column").GetComponent<ColumnModifier>();
            saveButton = transform.FindChild("Content").FindChild("Buttons").FindChild("Save").GetComponent<Button>();
            saveAsButton = transform.FindChild("Content").FindChild("Buttons").FindChild("SaveAs").GetComponent<Button>();
            patientDropdown = transform.FindChild("Content").FindChild("General").FindChild("Patient").FindChild("Dropdown").GetComponent<Dropdown>();
        }
        protected override void SetInteractableFields(bool interactable)
        {
            nameInputField.interactable = interactable;
            patientDropdown.interactable = interactable;
            saveButton.interactable = interactable;
            saveAsButton.interactable = interactable;
        }
        protected void OnChangePatient(Data.Patient patient)
        {
            (ItemTemp as SinglePatientVisualization).Patient = patient;
            SelectColumn();
        }
        #endregion
    }
}