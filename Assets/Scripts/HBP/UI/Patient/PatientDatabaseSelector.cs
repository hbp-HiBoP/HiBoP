using System.Collections.Generic;
using Tools.Unity;
using UnityEngine;

namespace HBP.UI
{
    public class PatientDatabaseSelector : SavableWindow
    {
        #region Properties
        [SerializeField] FolderSelector m_DatabaseFolderSelector;
        [SerializeField] PatientListGestion m_DatabaseListGestion;
        Queue<Data.Patient> m_PatientToAdd = new Queue<Data.Patient>();
        #endregion

        //    public void Add()
        //    {
        //        IEnumerable<Patient> patientsToAdd = m_DatabaseListGestion.List.ObjectsSelected.DeepClone();
        //        m_ProjectListGestion.Add(patientsToAdd);
        //        m_ProjectListGestion.List.Select(patientsToAdd);
        //        m_DatabaseListGestion.Remove(patientsToAdd);
        //    }
        //    public void Remove()
        //    {
        //        m_DatabaseListGestion.Add(m_ProjectListGestion.List.ObjectsSelected);
        //        m_DatabaseListGestion.List.Select(m_ProjectListGestion.List.ObjectsSelected);
        //        m_ProjectListGestion.RemoveSelected();
        //    }

        //    IEnumerator c_DisplayDataBasePatients()
        //    {
        //        m_PatientToAdd = new Queue<Patient>();
        //        yield return Ninja.JumpToUnity;
        //        m_DatabaseListGestion.Objects = new List<Patient>();
        //        yield return Ninja.JumpBack;

        //        string[] patients = Patient.GetPatientsDirectories(m_DatabaseFolderSelector.Folder);
        //        for (int i = 0; i < patients.Length; i++)
        //        {
        //            Patient patient = new Patient(patients[i]);
        //            if (!m_ProjectListGestion.Objects.Contains(patient))
        //            {
        //                m_PatientToAdd.Enqueue(patient);
        //            }
        //        }
        //    }

        //    private void Update()
        //    {
        //        int patientLenght = m_PatientToAdd.Count();
        //        for (int i = 0; i < patientLenght; i++)
        //        {
        //            Patient patient = m_PatientToAdd.Dequeue();
        //            if (!m_DatabaseListGestion.Objects.Contains(patient)) m_DatabaseListGestion.Add(patient);
        //        }
        //    }

        //    protected override void Initialize()
        //    {
        //        // Database list.  
        //        m_DatabaseListGestion.Initialize(m_SubWindows);
        //        m_DatabaseFolderSelector.onValueChanged.AddListener((value) => this.StartCoroutineAsync(c_DisplayDataBasePatients()));
        //        m_DatabaseFolderSelector.Folder = ApplicationState.ProjectLoaded.Settings.PatientDatabase;

        //        base.Initialize();
        //    }
    }

}