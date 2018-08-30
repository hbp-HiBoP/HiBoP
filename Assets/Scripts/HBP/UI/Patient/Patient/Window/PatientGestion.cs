using System.Linq;
using System.Collections;
using CielaSpike;
using Tools.Unity;
using Tools.CSharp;
using HBP.Data;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI.Anatomy
{
    public class PatientGestion : SavableWindow
    {
        #region Properties
        [SerializeField] FolderSelector m_DatabaseFolderSelector;
        [SerializeField] PatientListGestion m_DatabaseListGestion;
        [SerializeField] Text m_DatabaseCounter;
        Queue<Patient> m_PatientToAdd;

        [SerializeField] PatientListGestion m_ProjectListGestion;
        [SerializeField] Text m_ProjectCounter;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;

                m_DatabaseFolderSelector.interactable = value;
                m_DatabaseListGestion.Interactable = false;

                m_ProjectListGestion.Interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Save()
        {
            if (DataManager.HasData)
            {
                ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.WarningMultiOptions, "Reload required", "Some data have already been loaded. Your changes will not be applied unless you reload.\n\nWould you like to reload ?", () =>
                {
                    ApplicationState.ProjectLoaded.SetPatients(m_ProjectListGestion.Items);
                    base.Save();
                    DataManager.Clear();
                    ApplicationState.Module3D.ReloadScenes();
                });
            }
            else
            {
                ApplicationState.ProjectLoaded.SetPatients(m_ProjectListGestion.Items);
                base.Save();
            }
        }
        public void Create()
        {
            m_ProjectListGestion.Create();
        }
        public void Add()
        {
            IEnumerable<Patient> patientsToAdd = m_DatabaseListGestion.List.ObjectsSelected.DeepClone();
            m_ProjectListGestion.Add(patientsToAdd);
            m_ProjectListGestion.List.Select(patientsToAdd);
            m_ProjectCounter.text = m_ProjectListGestion.List.ObjectsSelected.Length.ToString();
            m_DatabaseListGestion.Remove(patientsToAdd);
            m_DatabaseCounter.text = m_DatabaseListGestion.List.ObjectsSelected.Length.ToString();
        }
        public void Remove()
        {
            m_DatabaseListGestion.Add(m_ProjectListGestion.List.ObjectsSelected);
            m_DatabaseListGestion.List.Select(m_ProjectListGestion.List.ObjectsSelected);
            m_ProjectListGestion.RemoveSelected();
            m_DatabaseCounter.text = m_DatabaseListGestion.List.ObjectsSelected.Length.ToString();
            m_ProjectCounter.text = m_ProjectListGestion.List.ObjectsSelected.Length.ToString();
        }
        #endregion

        #region Private Methods
        IEnumerator c_DisplayDataBasePatients()
        {
            m_PatientToAdd = new Queue<Patient>();
            yield return Ninja.JumpToUnity;
            m_DatabaseListGestion.Items = new List<Patient>();
            yield return Ninja.JumpBack;

            string[] patients = Patient.GetPatientsDirectories(m_DatabaseFolderSelector.Folder);
            for (int i = 0; i < patients.Length; i++)
            {
                Patient patient = new Patient(patients[i]);
                m_PatientToAdd.Enqueue(patient);
            }
        }
        private void Update()
        {
            int patientLenght = m_PatientToAdd.Count();
            for (int i = 0; i < patientLenght; i++)
            {
                Patient patient = m_PatientToAdd.Dequeue();
                if (!m_DatabaseListGestion.Items.Contains(patient)) m_DatabaseListGestion.Add(patient);
            }
        }
        protected override void Initialize()
        {
            // Database list.  
            m_DatabaseListGestion.Initialize(m_SubWindows);
            m_DatabaseFolderSelector.onValueChanged.AddListener((value) => this.StartCoroutineAsync(c_DisplayDataBasePatients()));
            m_DatabaseFolderSelector.Folder = ApplicationState.ProjectLoaded.Settings.PatientDatabase;

            // Project list.
            m_ProjectListGestion.Initialize(m_SubWindows);
            m_ProjectListGestion.Items = ApplicationState.ProjectLoaded.Patients.ToList();
            base.Initialize();
        }
        #endregion
    }
}