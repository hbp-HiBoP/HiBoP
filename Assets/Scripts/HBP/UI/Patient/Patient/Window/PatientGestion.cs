using System.Linq;
using System.Collections;
using CielaSpike;
using Tools.Unity;
using Tools.Unity.Lists;
using Tools.CSharp;

namespace HBP.UI.Anatomy
{
    public class PatientGestion : ItemGestion<Data.Patient>
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
            Data.Patient[] patientsToAdd = databaseList.GetObjectsSelected().DeepClone();
            AddItem(patientsToAdd);
            databaseList.Remove(patientsToAdd);
        }
        public override void Remove()
        {
            databaseList.Add(list.GetObjectsSelected());
            base.Remove();
        }
        #endregion

        #region Private Methods
        IEnumerator c_DisplayDataBasePatients()
        {
            yield return Ninja.JumpToUnity;
            databaseList.Clear();
            yield return Ninja.JumpBack;

            string[] patients = Data.Patient.GetPatientsDirectories(databaseFolderSelector.Folder);
            for (int i = 0; i < patients.Length; i++)
            {
                yield return Ninja.JumpBack;
                Data.Patient patient = new Data.Patient(patients[i]);
                yield return Ninja.JumpToUnity;
                databaseList.Add(patient, !Items.Contains(patient));
            }
        }
        protected override void SetWindow()
        {
            // Project list.
            list = transform.Find("Content").Find("Patients").Find("Project").Find("List").Find("Viewport").Find("Content").GetComponent<PatientList>();
            AddItem(ApplicationState.ProjectLoaded.Patients.ToArray());
            (list as SelectableListWithItemAction<Data.Patient>).ActionEvent.AddListener((patient, i) => OpenModifier(patient, true));

            // Database list.            
            databaseFolderSelector = transform.Find("Content").Find("Patients").Find("Database").Find("FolderSelector").GetComponent<FolderSelector>();
            databaseFolderSelector.onValueChanged.AddListener((value) => this.StartCoroutineAsync(c_DisplayDataBasePatients()));
            databaseFolderSelector.Folder = ApplicationState.ProjectLoaded.Settings.PatientDatabase;
            databaseList = transform.Find("Content").Find("Patients").Find("Database").Find("List").Find("Viewport").Find("Content").GetComponent<PatientList>();
            databaseList.ActionEvent.AddListener((patient, i) => OpenModifier(patient, false));
        }
        #endregion
    }
}