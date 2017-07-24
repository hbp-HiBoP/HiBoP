using System.Linq;
using System.Collections;
using CielaSpike;
using Tools.Unity;
using Tools.Unity.Lists;
using Tools.CSharp;
using HBP.Data;

namespace HBP.UI.Anatomy
{
    public class PatientGestion : ItemGestion<Data.Patient>
    {
        #region Properties
        FolderSelector m_DatabaseFolderSelector;
        PatientList m_DatabaseList;
        #endregion

        #region Public Methods
        public override void Save()
        {
            ApplicationState.ProjectLoaded.SetPatients(Items.ToArray());
            base.Save();
        }
        public void Add()
        {
            Data.Patient[] patientsToAdd = m_DatabaseList.ObjectsSelected.DeepClone();
            AddItem(patientsToAdd);
            m_List.Select(patientsToAdd);
            m_DatabaseList.Remove(patientsToAdd);
        }
        public override void Remove()
        {
            m_DatabaseList.Add(m_List.ObjectsSelected);
            m_DatabaseList.Select(m_List.ObjectsSelected);
            base.Remove();
        }
        public override void SetInteractable(bool interactable)
        {
            base.SetInteractable(interactable);
            m_DatabaseFolderSelector.interactable = interactable;
            m_DatabaseList.interactable = interactable;
        }
        #endregion

        #region Private Methods
        IEnumerator c_DisplayDataBasePatients()
        {
            yield return Ninja.JumpToUnity;
            m_DatabaseList.Objects = new Data.Patient[0];
            yield return Ninja.JumpBack;

            string[] patients = Data.Patient.GetPatientsDirectories(m_DatabaseFolderSelector.Folder);
            for (int i = 0; i < patients.Length; i++)
            {
                yield return Ninja.JumpBack;
                Data.Patient patient = new Data.Patient(patients[i]);
                yield return Ninja.JumpToUnity;
                if (!Items.Contains(patient)) m_DatabaseList.Add(patient);
            }
        }
        protected override void SetWindow()
        {
            // Project list.
            m_List = transform.Find("Content").Find("Patients").Find("Project").Find("List").Find("Viewport").Find("Content").GetComponent<PatientList>();
            AddItem(ApplicationState.ProjectLoaded.Patients.ToArray());
            (m_List as PatientList).OnAction.AddListener((patient, i) => OpenModifier(patient, true));

            // Database list.            
            m_DatabaseFolderSelector = transform.Find("Content").Find("Patients").Find("Database").Find("FolderSelector").GetComponent<FolderSelector>();
            m_DatabaseFolderSelector.onValueChanged.AddListener((value) => this.StartCoroutineAsync(c_DisplayDataBasePatients()));

            m_DatabaseFolderSelector.Folder = ApplicationState.ProjectLoaded.Settings.PatientDatabase;
            m_DatabaseList = transform.Find("Content").Find("Patients").Find("Database").Find("List").Find("Viewport").Find("Content").GetComponent<PatientList>();
            m_DatabaseList.OnAction.AddListener((patient, i) => OpenModifier(patient, false));
        }
        #endregion
    }
}