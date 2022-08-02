using UnityEngine;
using UnityEngine.Events;
using System.IO;
using CielaSpike;

namespace HBP.UI
{
    public class ProjectLoaderSaver : MonoBehaviour
    {
        #region Public Methods  
        public void Load(Core.Data.ProjectInfo projectInfo)
        {
            UnityEngine.Profiling.Profiler.BeginSample("1");
            Core.Data.Project projectToLoad = new Core.Data.Project();

            Core.Data.DataManager.Clear();
            Core.Data.Project projectLoaded = ApplicationState.ProjectLoaded;
            string projectLoadedLocation = ApplicationState.ProjectLoadedLocation;
            ApplicationState.ProjectLoaded = projectToLoad;
            ApplicationState.ProjectLoadedLocation = Directory.GetParent(projectInfo.Path).FullName;

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("2");

            GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
            ApplicationState.LoadingManager.Load(
                projectToLoad.c_Load(projectInfo, (progress, duration, text) => onChangeProgress.Invoke(progress, duration, text)),
                onChangeProgress,
                (taskState) =>
                {
                    if (taskState == TaskState.Done)
                    {
                        FindObjectOfType<MenuButtonState>().SetInteractables();
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
            ApplicationState.Module3D.SaveConfigurations();
            ApplicationState.ProjectLoadedLocation = path;
            GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
            ApplicationState.LoadingManager.Load(
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