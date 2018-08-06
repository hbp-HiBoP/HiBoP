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
    public class PatientGestion : ItemGestion<Patient>
    {
        #region Properties
        [SerializeField] FolderSelector m_DatabaseFolderSelector;
        [SerializeField] PatientList m_DatabaseList;
        [SerializeField] PatientList m_ProjectList;
        [SerializeField] Text m_DatabaseCounter;
        Queue<Patient> m_PatientToAdd;

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
                m_DatabaseList.Interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Save()
        {
            if (DataManager.HasData)
            {
                ApplicationState.DialogBoxManager.Open(Tools.Unity.DialogBoxManager.AlertType.WarningMultiOptions, "Reload required", "Some data have already been loaded. Your changes will not be applied unless you reload.\n\nWould you like to reload ?", () =>
                {
                    ApplicationState.ProjectLoaded.SetPatients(Items.ToArray());
                    base.Save();
                    DataManager.Clear();
                    ApplicationState.Module3D.ReloadScenes();
                });
            }
            else
            {
                ApplicationState.ProjectLoaded.SetPatients(Items.ToArray());
                base.Save();
            }
        }
        public void Add()
        {
            IEnumerable<Patient> patientsToAdd = m_DatabaseList.ObjectsSelected.DeepClone();
            AddItem(patientsToAdd);
            m_List.Select(patientsToAdd);
            m_DatabaseList.Remove(patientsToAdd);
            m_DatabaseCounter.text = m_DatabaseList.ObjectsSelected.Length.ToString();
        }
        public override void Remove()
        {
            m_DatabaseList.Add(m_List.ObjectsSelected);
            m_DatabaseList.Select(m_List.ObjectsSelected);
            base.Remove();
            m_DatabaseCounter.text = m_DatabaseList.ObjectsSelected.Length.ToString();
        }
        #endregion

        #region Private Methods
        IEnumerator c_DisplayDataBasePatients()
        {
            m_PatientToAdd = new Queue<Patient>();
            yield return Ninja.JumpToUnity;
            m_DatabaseList.Objects = new Patient[0];
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
                if (!Items.Contains(patient)) m_DatabaseList.Add(patient);
            }
        }
        protected override void Initialize()
        {
            // Database list.  
            m_DatabaseList.Initialize();
            m_DatabaseFolderSelector.onValueChanged.AddListener((value) => this.StartCoroutineAsync(c_DisplayDataBasePatients()));
            m_DatabaseFolderSelector.Folder = ApplicationState.ProjectLoaded.Settings.PatientDatabase;
            m_DatabaseList.OnAction.AddListener((patient, i) => OpenModifier(patient, false));
            m_DatabaseList.OnSelectionChanged.AddListener(() => m_DatabaseCounter.text = m_DatabaseList.NumberOfItemSelected.ToString());

            // Project list.
            m_List = m_ProjectList;
            base.Initialize();
            AddItem(ApplicationState.ProjectLoaded.Patients.ToArray());
        }
        #endregion
    }
}