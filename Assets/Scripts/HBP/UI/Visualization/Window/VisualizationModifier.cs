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
        [SerializeField] GameObject m_PatientModifierPrefab;

        List<PatientModifier> m_PatientModifiers = new List<PatientModifier>();
        List<GroupSelection> m_GroupSelectionModifiers = new List<GroupSelection>();

        [SerializeField] InputField m_NameInputField;
        [SerializeField] TabGestion m_TabGestion;
        [SerializeField] ColumnModifier m_ColumnModifier;
        [SerializeField] PatientList m_VisualizationPatientsList;
        [SerializeField] PatientList m_ProjectPatientsList;
        [SerializeField] Button m_AddPatientButton, m_RemovePatientButton, m_AddGroupButton, m_RemoveGroupButton;
        #endregion

        #region Public Methods
        public override void Save()
        {
            if (Item.IsOpen)
            {
                ApplicationState.DialogBoxManager.Open(Tools.Unity.DialogBoxManager.AlertType.WarningMultiOptions, "Visualization already open", "The visualization you are trying to modify is already open. This visualization needs to be closed before saving the changes.\n\nWould you like to close it and save the changes ?", () =>
                {
                    ApplicationState.Module3D.RemoveScene(Item);
                    base.Save();
                },
                "Close & Save");
            }
            else
            {
                base.Save();
            }
        }
        public override void Close()
        {
            foreach (var modifier in m_PatientModifiers.ToArray()) modifier.Close();
            m_PatientModifiers.Clear();
            foreach (var modifier in m_GroupSelectionModifiers.ToArray()) modifier.Close();
            m_GroupSelectionModifiers.Clear();
            base.Close();
        }
        public void AddColumn()
        { 
            ItemTemp.Columns.Add(new Column(ItemTemp.Columns.Count + 1, ItemTemp.Patients, ApplicationState.ProjectLoaded.Datasets));
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
        public void OpenPatientModifier(Patient patientToModify)
        {
            PatientModifier patientModifier = PatientModifier.Open(patientToModify, Interactable) as PatientModifier;
            patientModifier.OnClose.AddListener(() => OnClosePatientModifier(patientModifier));
            m_PatientModifiers.Add(patientModifier);
        }
        public void OpenAddGroupWindow()
        {
            //GroupSelection groupSelection = GroupSelection.Open() as GroupSelection;
            //groupSelection.GroupsSelectedEvent.AddListener((groups) => AddGroups(groups));
            //groupSelection.OnClose.AddListener(() => OnCloseGroupSelection(groupSelection));
            //m_GroupSelectionModifiers.Add(groupSelection);
        }
        public void OpenRemoveGroupWindow()
        {
            //GroupSelection groupSelection = GroupSelection.Open() as GroupSelection;
            //groupSelection.GroupsSelectedEvent.AddListener((groups) => RemoveGroups(groups));
            //groupSelection.OnClose.AddListener(() => OnCloseGroupSelection(groupSelection));
            //m_GroupSelectionModifiers.Add(groupSelection);
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
        protected void OnClosePatientModifier(PatientModifier modifier)
        {
            m_PatientModifiers.Remove(modifier);
        }
        protected void OnCloseGroupSelection(GroupSelection groupSelection)
        {
            m_GroupSelectionModifiers.Remove(groupSelection);
        }
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
        protected void SetName(Data.Visualization.Visualization objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_NameInputField.onValueChanged.AddListener((value) => objectToDisplay.Name = value);
        }
        protected void SetPatients(Data.Visualization.Visualization objectToDisplay)
        {
            m_VisualizationPatientsList.Objects = objectToDisplay.Patients.ToArray();
            m_VisualizationPatientsList.OnAction.AddListener((patient, i) => OpenPatientModifier(patient));
            m_ProjectPatientsList.Objects = (from p in ApplicationState.ProjectLoaded.Patients where !objectToDisplay.Patients.Contains(p) select p).ToArray();
            m_VisualizationPatientsList.OnAction.RemoveAllListeners();
            m_VisualizationPatientsList.OnAction.AddListener((patient, action) => OpenPatientModifier(patient));
            m_ProjectPatientsList.OnAction.RemoveAllListeners();
            m_ProjectPatientsList.OnAction.AddListener((patient, action) => OpenPatientModifier(patient));
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
                objectToDisplay.Columns.Add(new Column(ItemTemp.Columns.Count + 1, ItemTemp.Patients, ApplicationState.ProjectLoaded.Datasets));
            }
            for (int i = 0; i < objectToDisplay.Columns.Count; i++)
            {
                m_TabGestion.AddTab();
            }
        }
        protected override void SetInteractable(bool interactable)
        {
            m_AddPatientButton.interactable = interactable;
            m_RemovePatientButton.interactable = interactable;
            m_AddGroupButton.interactable = interactable;
            m_NameInputField.interactable = interactable;
        }
        #endregion
    }
}