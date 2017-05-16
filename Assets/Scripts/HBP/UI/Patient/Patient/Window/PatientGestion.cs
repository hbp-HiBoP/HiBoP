using System.Linq;
using System.Collections;
using CielaSpike;
using Tools.Unity;
using Tools.Unity.Lists;
using Tools.CSharp;

namespace HBP.UI.Patient
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
            ApplicationState.CoroutineManager.Add(c_Save());
            base.Save();
        }

        // Patient.
        public void Add()
        {
            Data.Patient[] patientsToAdd = databaseList.GetObjectsSelected().DeepClone();
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

            string[] patients = Data.Patient.GetPatientsDirectories(databaseFolderSelector.Folder);
            for (int i = 0; i < patients.Length; i++)
            {
                yield return Ninja.JumpBack;
                Data.Patient patient = new Data.Patient(patients[i]);
                yield return Ninja.JumpToUnity;
                databaseList.Add(patient, !Items.Contains(patient));
            }
        }
        IEnumerator c_Save()
        {
            ApplicationState.ProjectLoaded.SetPatients(Items.ToArray());
            yield return true;
        }
        protected override void SetWindow()
        {
            // Project list.
            list = transform.FindChild("Content").FindChild("Lists").FindChild("Project").FindChild("List").FindChild("Viewport").FindChild("Content").GetComponent<PatientList>();
            AddItem(ApplicationState.ProjectLoaded.Patients.ToArray());
            (list as SelectableListWithItemAction<Data.Patient>).ActionEvent.AddListener((patient, i) => OpenModifier(patient, true));

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