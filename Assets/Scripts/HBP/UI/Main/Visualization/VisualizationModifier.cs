using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using HBP.Core.Data;
using HBP.Data.Module3D;
using HBP.UI.Tools;
using HBP.Core.Tools;

namespace HBP.UI.Main
{
    /// <summary>
    /// Window to modify visualization.
    /// </summary>
    public class VisualizationModifier : ObjectModifier<Visualization>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;

        [SerializeField] PatientListGestion m_PatientListGestion;
        [SerializeField] Button m_AddPatientButton, m_RemovePatientButton, m_AddGroupButton, m_RemoveGroupButton;

        [SerializeField] TabManager m_TabGestion;
        [SerializeField] ColumnModifier m_ColumnModifier;

        bool m_NeedToUpdate = false;
        /// <summary>
        /// True if interactable, False otherwise.
        /// </summary>
        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;

                m_NameInputField.interactable = value;

                m_AddPatientButton.interactable = value;
                m_RemovePatientButton.interactable = value;

                m_AddGroupButton.interactable = value;
                m_RemoveGroupButton.interactable = value;

                m_PatientListGestion.Interactable = value;
                m_PatientListGestion.Modifiable = false;

                m_ColumnModifier.Interactable = value;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Save the modifications.
        /// </summary>
        public override void OK()
        {
            if (Module3DMain.Visualizations.Contains(Object))
            {
                DialogBoxManager.Open(DialogBoxManager.AlertType.WarningMultiOptions, "Visualization already open", "The visualization you are trying to modify is already open. This visualization needs to be closed before saving the changes.\n\nWould you like to close it and save the changes ?", () =>
                {
                    Module3DMain.RemoveScene(Object);
                    base.OK();
                },
                "Close & Save");
            }
            else
            {
                base.OK();
            }
        }
        /// <summary>
        /// Add patients to the visualization.
        /// </summary>
        public void AddPatients()
        {
            ObjectSelector<Patient> selector = WindowsManager.OpenSelector(ApplicationState.ProjectLoaded.Patients.Where(p => !m_ObjectTemp.Patients.Contains(p)));
            selector.OnOk.AddListener(() => m_PatientListGestion.List.Add(selector.ObjectsSelected));
            WindowsReferencer.Add(selector);
        }
        /// <summary>
        /// Add groups to the visualization.
        /// </summary>
        public void AddGroups()
        {
            ObjectSelector<Group> selector = WindowsManager.OpenSelector(ApplicationState.ProjectLoaded.Groups);
            selector.OnOk.AddListener(() => AddGroups(selector.ObjectsSelected));
            WindowsReferencer.Add(selector);
        }
        /// <summary>
        /// Remove groups to the visualization.
        /// </summary>
        public void RemoveGroups()
        {
            ObjectSelector<Group> selector = WindowsManager.OpenSelector(ApplicationState.ProjectLoaded.Groups);
            selector.OnOk.AddListener(() => RemoveGroups(selector.ObjectsSelected));
            WindowsReferencer.Add(selector);
        }
        /// <summary>
        /// Add column to the visualization.
        /// </summary>
        public void AddColumn()
        {
            Column column = new IEEGColumn("Column n°" + (ObjectTemp.Columns.Count + 1), new BaseConfiguration(), ObjectTemp.Patients);
            ObjectTemp.Columns.Add(column);
            m_TabGestion.AddTab(column.Name, -1, true);
            m_ColumnModifier.Object = column;
        }
        /// <summary>
        /// Remove column to the visualization.
        /// </summary>
        public void RemoveColumn()
        {
            int index = m_TabGestion.ActiveTabIndex;
            ObjectTemp.Columns.RemoveAt(index);
            m_TabGestion.RemoveTab(index);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Initialize the window.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            // General.
            m_NameInputField.onEndEdit.AddListener((value) => ObjectTemp.Name = value);

            // Patient.
            m_PatientListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_PatientListGestion.List.OnAddObject.AddListener(AddPatient);
            m_PatientListGestion.List.OnRemoveObject.AddListener(RemovePatient);

            // Tabs.
            m_TabGestion.OnSwapColumns.AddListener((column1, column2) => ObjectTemp.SwapColumns(column1, column2));
            m_TabGestion.OnActiveTabChanged.AddListener(SelectColumn);

            // Column Modifier.
            m_ColumnModifier.OnChangeName.AddListener(m_TabGestion.ChangeTabTitle);
            m_ColumnModifier.OnChangeColumn.AddListener(SelectColumn);
        }
        /// <summary>
        /// Set the fields.
        /// </summary>
        /// <param name="objectToDisplay">Visualization to modify</param>
        protected override void SetFields(Visualization objectToDisplay)
        {
            m_NameInputField.text = ObjectTemp.Name;

            m_PatientListGestion.List.Set(m_ObjectTemp.Patients);


            if (objectToDisplay.Columns.Count > 0)
            {
                for (int i = 0; i < objectToDisplay.Columns.Count; i++)
                {
                    m_TabGestion.AddTab(objectToDisplay.Columns[i].Name);
                }
                m_TabGestion.ActiveTabIndex = 0;
            }
        }
        /// <summary>
        /// Add groups to the visualization.
        /// </summary>
        /// <param name="groups">Groups to add</param>
        protected void AddGroups(IEnumerable<Group> groups)
        {
            m_PatientListGestion.List.Add(groups.SelectMany(g => g.Patients).Distinct());
        }
        /// <summary>
        /// Remove groups to the visualization.
        /// </summary>
        /// <param name="groups">Groups to remove</param>
        protected void RemoveGroups(IEnumerable<Group> groups)
        {
            m_PatientListGestion.List.Remove(groups.SelectMany(g => g.Patients).Distinct());
        }
        /// <summary>
        /// Select column.
        /// </summary>
        protected void SelectColumn()
        {
            int index = m_TabGestion.ActiveTabIndex;
            if (index >= 0)
            {
                if (!m_ColumnModifier.gameObject.activeSelf)
                {
                    m_ColumnModifier.gameObject.SetActive(true);
                }
                m_ColumnModifier.Patients = ObjectTemp.Patients.ToArray();
                m_ColumnModifier.Object = ObjectTemp.Columns[index];
            }
            else
            {
                if (m_ColumnModifier.gameObject.activeSelf)
                {
                    m_ColumnModifier.gameObject.SetActive(false);
                }
            }
        }
        /// <summary>
        /// Select the specified column.
        /// </summary>
        /// <param name="column">Column selected</param>
        protected void SelectColumn(Column column)
        {
            if (ObjectTemp != null)
            {
                ObjectTemp.Columns[m_TabGestion.ActiveTabIndex] = column;
            }
        }
        /// <summary>
        /// Remove patient from the visualization.
        /// </summary>
        /// <param name="patient">Patient removed</param>
        protected void RemovePatient(Patient patient)
        {
            m_ObjectTemp.Patients.Remove(patient);
            m_NeedToUpdate = true;
        }
        /// <summary>
        /// Add patient from the visualization.
        /// </summary>
        /// <param name="patient"></param>
        protected void AddPatient(Patient patient)
        {
            m_ObjectTemp.Patients.AddIfAbsent(patient);
            m_NeedToUpdate = true;
        }
        private void Update()
        {
            if(m_NeedToUpdate)
            {
                SelectColumn();
                m_NeedToUpdate = false;
            }
        }
        #endregion
    }
}