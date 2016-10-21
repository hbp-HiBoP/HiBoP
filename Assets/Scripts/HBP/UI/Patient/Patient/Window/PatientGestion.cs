using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using CielaSpike;
using Tools.Unity;
using Tools.Unity.Lists;
using Tools.CSharp;
using d = HBP.Data.Patient;

namespace HBP.UI.Patient
{
    public class PatientGestion : ItemGestion<d.Patient>
    {
        #region Properties
        FolderSelector databaseFolderSelector;
        PatientList databaseList;
        #endregion

        #region Public Methods
        // General.
        public override void Save()
        {
            ApplicationState.ProjectLoaded.SetPatients(Items.ToArray());
            base.Save();
        }

        // Patient.
        public void Add()
        {
            d.Patient[] patientsToAdd = databaseList.GetObjectsSelected().DeepClone();
            AddItem(patientsToAdd);
            databaseList.DeactivateObject(patientsToAdd);
        }
        public override void Remove()
        {
            databaseList.ActiveObject(list.GetObjectsSelected());
            base.Remove();
        }
        #endregion

        #region Private Methods
        IEnumerator c_DisplayDataBasePatients()
        {
            yield return Ninja.JumpToUnity;
            databaseList.Clear();
            yield return Ninja.JumpBack;

            string[] patients = d.Patient.PatientsInDirectory(databaseFolderSelector.Folder);
            for (int i = 0; i < patients.Length; i++)
            {
                yield return Ninja.JumpBack;
                d.Patient patient = new d.Patient(patients[i]);
                yield return Ninja.JumpToUnity;
                databaseList.Add(patient, !Items.Contains(patient));
            }
        }
        protected override void SetWindow()
        {
            // Project list.
            list = transform.FindChild("Content").FindChild("Lists").FindChild("Project").FindChild("List").FindChild("Viewport").FindChild("Content").GetComponent<PatientList>();
            AddItem(ApplicationState.ProjectLoaded.Patients.ToArray());
            (list as SelectableListWithItemAction<d.Patient>).ActionEvent.AddListener((patient, i) => OpenModifier(patient, true));

            // Database list.            
            databaseFolderSelector = transform.FindChild("Content").FindChild("Lists").FindChild("Database").FindChild("FolderSelector").GetComponent<FolderSelector>();
            databaseFolderSelector.onValueChanged.AddListener((value) => this.StartCoroutineAsync(c_DisplayDataBasePatients()));
            databaseFolderSelector.Folder = ApplicationState.ProjectLoaded.Settings.PatientDatabase;
            databaseList = transform.FindChild("Content").FindChild("Lists").FindChild("Database").FindChild("List").FindChild("Viewport").FindChild("Content").GetComponent<PatientList>();
            databaseList.ActionEvent.AddListener((patient, i) => OpenModifier(patient, false));
        }
        #endregion
    }
}