using UnityEngine;
using UnityEngine.Events;
using System.IO;
using ThirdParty.CielaSpike;
using HBP.Core.Tools;
using HBP.Core.Data;
using HBP.Display.Module3D;

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