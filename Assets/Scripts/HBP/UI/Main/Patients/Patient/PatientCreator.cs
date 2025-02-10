using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Interfaces;
using HBP.UI.Tools;
using System.Collections.Generic;
using System.Linq;

namespace HBP.UI.Main
{
    /// <summary>
    /// Component to create a new patient.
    /// </summary>
    public class PatientCreator : ObjectCreator<Patient>
    {
        #region Private Methods
        protected override async void SaveSelector(ObjectSelector<Patient> selector, bool generateNewIDs = true)
        {
            if (!generateNewIDs)
            {
                var selectedPatients = ExistingObjects.Where(o => selector.ObjectsSelected.Contains(o)).ToList();
                int numberOfExistingObjects = selectedPatients.Count;

                if (numberOfExistingObjects > 0)
                {
                    string message;
                    if (numberOfExistingObjects == 1)
                    {
                        message = $"Patient '{selectedPatients[0].Name}' will be overridden. Are you sure you want to override it?";
                    }
                    else
                    {
                        var patientNames = selectedPatients.Take(5).Select(p => p.Name).ToList();
                        string nameList = string.Join(", ", patientNames);
                        if (numberOfExistingObjects > 5)
                        {
                            nameList += ", ...";
                        }
                        message = $"{numberOfExistingObjects} patients will be overridden ({nameList}). Are you sure you want to override them?";
                    }

                    int result = await DialogBoxManager.OpenAsync(DialogBoxType.Warning, "Override patients", message, "Override", "Cancel");
                    if (result == 1) return;
                }
            }

            base.SaveSelector(selector, generateNewIDs);
        }

        protected async override UniTaskVoid LoadFromDirectory()
        {
#if UNITY_STANDALONE_OSX
            FileBrowser.GetExistingDirectoryNamesAsync(async (paths) =>
            {
                if (paths.Length > 0)
                {
                    ILoadableFromDirectory<Patient> loadable = new Patient();
                    var patients = await LoadingManager.LoadAsync(update => loadable.LoadFromDirectory(paths, update));
                    var length = patients.Count();
                    if (length > 0)
                    {
                        if (length == 1)
                        {
                            var patient = patients.First();
                            if (ExistingObjects.Contains(patient))
                            {
                                int result = await DialogBoxManager.OpenAsync(DialogBoxType.Warning, "Override patient", $"Patient {patient.Name} will be overridden. Are you sure you want to override it?", "Override", "Cancel");
                                if (result == 0) OnObjectCreated.Invoke(patient);
                            }
                        }
                        else
                            OpenSelector(patients, true, false, false);
                    }
                }
            });
#else
            string[] paths = FileBrowser.GetExistingDirectoryNames();
            if (paths.Length > 0)
            {
                ILoadableFromDirectory<Patient> loadable = new Patient();
                var patients = await LoadingManager.LoadAsync(update => loadable.LoadFromDirectory(paths, update));
                await UniTask.SwitchToMainThread();
                var length = patients.Count();
                if (length > 0)
                {
                    if (length == 1)
                    {
                        var patient = patients.First();
                        if (ExistingObjects.Contains(patient))
                        {
                            int result = await DialogBoxManager.OpenAsync(DialogBoxType.Warning, "Override patient", $"Patient {patient.Name} will be overridden. Are you sure you want to override it?", "Override", "Cancel");
                            if (result == 0) OnObjectCreated.Invoke(patient);
                        }
                    }
                    else
                        OpenSelector(patients, true, false, false);
                }
            }
#endif
        }
        #endregion
    }
}