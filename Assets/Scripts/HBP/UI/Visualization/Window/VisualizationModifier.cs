using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using HBP.Data;
using HBP.UI.Anatomy;
using HBP.Data.Visualization;
using System.Collections.ObjectModel;

namespace HBP.UI.Visualization
{
    public class VisualizationModifier : ItemModifier<Data.Visualization.Visualization>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;

        [SerializeField] PatientListGestion m_VisualizationPatientsListGestion, m_ProjectPatientsListGestion;
        [SerializeField] Button m_AddPatientButton, m_RemovePatientButton, m_AddGroupButton, m_RemoveGroupButton;

        [SerializeField] TabManager m_TabGestion;
        [SerializeField] ColumnModifier m_ColumnModifier;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;

                // General.
                m_NameInputField.interactable = value;

                // Patients.
                m_AddPatientButton.interactable = value;
                m_RemovePatientButton.interactable = value;

                m_AddGroupButton.interactable = value;
                m_RemoveGroupButton.interactable = value;

                m_VisualizationPatientsListGestion.Interactable = false;
                m_ProjectPatientsListGestion.Interactable = false;

                m_ColumnModifier.Interactable = value;
            }
        }
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
        public void AddPatients()
        {
            AddPatients(m_ProjectPatientsListGestion.List.ObjectsSelected);
        }
        public void RemovePatients()
        {
            RemovePatients(m_VisualizationPatientsListGestion.List.ObjectsSelected);
        }
        public void AddGroups()
        {
            GroupSelection groupSelection = ApplicationState.WindowsManager.Open<GroupSelection>("Add Group window", Interactable);
            groupSelection.OnSave.AddListener(() => AddGroups(groupSelection.SelectedGroups));
            groupSelection.OnClose.AddListener(() => m_SubWindows.Remove(groupSelection));
            m_SubWindows.Add(groupSelection);
        }
        public void RemoveGroups()
        {
            GroupSelection groupSelection = ApplicationState.WindowsManager.Open<GroupSelection>("Remove Group window", Interactable);
            groupSelection.OnSave.AddListener(() => RemoveGroups(groupSelection.SelectedGroups));
            groupSelection.OnClose.AddListener(() => m_SubWindows.Remove(groupSelection));
            m_SubWindows.Add(groupSelection);
        }
        public void AddColumn()
        {
            BaseColumn column = new IEEGColumn("Column n°"+(ItemTemp.Columns.Count + 1), new BaseConfiguration(), ItemTemp.Patients);
            ItemTemp.Columns.Add(column);
            m_TabGestion.AddTab(column.Name);
        }
        public void RemoveColumn()
        {
            int index = m_TabGestion.ActiveTabIndex;
            ItemTemp.Columns.RemoveAt(index);
            m_TabGestion.RemoveTab(index);
        }
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();

            // General.
            m_NameInputField.onEndEdit.AddListener((value) => ItemTemp.Name = value);

            // Patients.
            m_VisualizationPatientsListGestion.Initialize(m_SubWindows);
            m_ProjectPatientsListGestion.Initialize(m_SubWindows);

            // Tabs.
            m_TabGestion.OnSwapColumns.AddListener((column1, column2) => ItemTemp.SwapColumns(column1, column2));
            m_TabGestion.OnActiveTabChanged.AddListener(SelectColumn);

            // Column Modifier.
            m_ColumnModifier.OnChangeName.AddListener(m_TabGestion.ChangeTabTitle);
            m_ColumnModifier.OnChangeColumn.AddListener(column => ItemTemp.Columns[m_TabGestion.ActiveTabIndex] = column);
        }
        protected override void SetFields(Data.Visualization.Visualization objectToDisplay)
        {
            m_NameInputField.text = ItemTemp.Name;

            m_VisualizationPatientsListGestion.Objects = ItemTemp.Patients.ToList();

            ReadOnlyCollection<Patient> visualizationPatients = objectToDisplay.Patients;
            List<Patient> patients = new List<Patient>();
            foreach (var patient in ApplicationState.ProjectLoaded.Patients)
            {
                if (!visualizationPatients.Contains(patient))
                {
                    patients.Add(patient);
                }
            }
            m_ProjectPatientsListGestion.Objects = patients;

            if (objectToDisplay.Columns.Count > 0)
            {
                for (int i = 0; i < objectToDisplay.Columns.Count; i++)
                {
                    m_TabGestion.AddTab(objectToDisplay.Columns[i].Name);
                }
            }
        }


        protected void AddPatients(IEnumerable<Patient> patients)
        {
            ItemTemp.AddPatient(patients.ToArray());
            m_ProjectPatientsListGestion.Remove(patients.ToArray());
            m_VisualizationPatientsListGestion.Add(patients.ToArray());
            SelectColumn();
        }
        protected void RemovePatients(IEnumerable<Patient> patients)
        {
            ItemTemp.RemovePatient(patients.ToArray());
            m_ProjectPatientsListGestion.Add(patients.ToArray());
            m_VisualizationPatientsListGestion.Remove(patients.ToArray());
            SelectColumn();
        }
        protected void AddGroups(IEnumerable<Group> groups)
        {
            AddPatients(groups.SelectMany(g => g.Patients).Distinct().Where(p => !m_VisualizationPatientsListGestion.Objects.Contains(p)));
        }
        protected void RemoveGroups(IEnumerable<Group> groups)
        {
            RemovePatients(groups.SelectMany(g => g.Patients).Distinct().Where(p => m_VisualizationPatientsListGestion.Objects.Contains(p)));
        }
        protected void SelectColumn()
        {
            int index = m_TabGestion.ActiveTabIndex;
            if (index >= 0)
            {
                if (!m_ColumnModifier.gameObject.activeSelf)
                {
                    m_ColumnModifier.gameObject.SetActive(true);
                }
                m_ColumnModifier.Set(ItemTemp.Columns[index], ItemTemp.Patients);
            }
            else
            {
                if (m_ColumnModifier.gameObject.activeSelf)
                {
                    m_ColumnModifier.gameObject.SetActive(false);
                }
            }
        }
        #endregion
    }
}