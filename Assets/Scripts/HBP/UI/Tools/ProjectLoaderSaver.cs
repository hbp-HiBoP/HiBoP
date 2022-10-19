using UnityEngine;
using UnityEngine.Events;
using System.IO;
using ThirdParty.CielaSpike;
using HBP.Core.Tools;
using HBP.Core.Data;
using HBP.Data.Module3D;
using System.Collections.Generic;
using HBP.Core.Interfaces;
using System.Linq;

namespace HBP.UI.Tools
{
    public class ProjectLoaderSaver : MonoBehaviour
    {
        #region Public Methods  
        public void Load(ProjectInfo projectInfo)
        {
            UnityEngine.Profiling.Profiler.BeginSample("1");
            Project projectToLoad = new Project();

            DataManager.Clear();
            Project projectLoaded = ApplicationState.ProjectLoaded;
            string projectLoadedLocation = ApplicationState.ProjectLoadedLocation;
            ApplicationState.ProjectLoaded = projectToLoad;
            ApplicationState.ProjectLoadedLocation = Directory.GetParent(projectInfo.Path).FullName;

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("2");

            GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
            LoadingManager.Load(
                projectToLoad.c_Load(projectInfo, (progress, duration, text) => onChangeProgress.Invoke(progress, duration, text)),
                onChangeProgress,
                (taskState) =>
                {
                    if (taskState == TaskState.Done)
                    {
                        FindObjectOfType<MenuButtonState>().SetInteractables();
                        Dictionary<string, List<string>> problematicData = ApplicationState.ProjectLoaded.CheckProjectIDs();
                        if (problematicData.Count > 0)
                        {
                            string result = "";
                            foreach (var kv in problematicData)
                            {
                                if (kv.Value.Count > 1) result += string.Format("{0}\n{1}\n\n", kv.Key, string.Join(", ", kv.Value));
                            }
                            DialogBoxManager.Open(DialogBoxManager.AlertType.WarningMultiOptions, "IDs issue", string.Format("Some IDs of this project are used by multiple different objects:\n\n{0}\nYou have two options: you can regenerate the IDs of problematic objects automatically, but this can unlink some of your objects (for example, some datasets may not be linked to the right protocol), or you can leave them as is but you may encounter issues and will need to fix the IDs manually later.\nWhat do you want to do?", result),
                                () => { }, "Regenerate IDs", () => { }, "Leave IDs as is");
                        }
                    }
                    else
                    {
                        ApplicationState.ProjectLoaded = projectLoaded;
                        ApplicationState.ProjectLoadedLocation = projectLoadedLocation;
                    }
                });
            UnityEngine.Profiling.Profiler.EndSample();
        }
        public void Save(string path)
        {
            Module3DMain.SaveConfigurations();
            ApplicationState.ProjectLoadedLocation = path;
            GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
            LoadingManager.Load(
                ApplicationState.ProjectLoaded.c_Save(path, (progress, duration, text) => onChangeProgress.Invoke(progress, duration, text)),
                onChangeProgress);
        }
        public void Save()
        {
            Save(ApplicationState.ProjectLoadedLocation);
        }
        public void SaveAndReload()
        {
            Save();
            FindObjectOfType<MenuButtonState>().SetInteractables();
        }
        #endregion
    }
}