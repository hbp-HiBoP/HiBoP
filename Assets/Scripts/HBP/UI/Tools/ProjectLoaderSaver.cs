using UnityEngine.Events;
using System.IO;
using ThirdParty.CielaSpike;
using HBP.Core.Tools;
using HBP.Core.Data;
using HBP.Data.Module3D;
using HBP.UI.Main;

namespace HBP.UI.Tools
{
    public class ProjectLoaderSaver
    {
        #region Public Methods  
        public static void Load(ProjectInfo projectInfo)
        {
            Project projectToLoad = new();

            DataManager.Clear();
            Project projectLoaded = ApplicationState.ProjectLoaded;
            string projectLoadedLocation = ApplicationState.ProjectLoadedLocation;
            ApplicationState.ProjectLoaded = projectToLoad;
            ApplicationState.ProjectLoadedLocation = Directory.GetParent(projectInfo.Path).FullName; 

            GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
            LoadingManager.Load(
                projectToLoad.c_Load(projectInfo, (progress, duration, text) => onChangeProgress.Invoke(progress, duration, text)),
                onChangeProgress,
                (taskState) =>
                {
                    if (taskState == TaskState.Done)
                    {
                        InteractableStateManager.SetInteractables();
                        UITools.CheckProjectIDAndAskForRegeneration();
                    }
                    else
                    {
                        ApplicationState.ProjectLoaded = projectLoaded;
                        ApplicationState.ProjectLoadedLocation = projectLoadedLocation;
                    }
                });
        }
        public static void Save(string path)
        {
            Module3DMain.SaveConfigurations();
            ApplicationState.ProjectLoadedLocation = path;
            GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
            LoadingManager.Load(
                ApplicationState.ProjectLoaded.c_Save(path, (progress, duration, text) => onChangeProgress.Invoke(progress, duration, text)),
                onChangeProgress);
        }
        public static void Save()
        {
            Save(ApplicationState.ProjectLoadedLocation);
        }
        public static void SaveAndReload()
        {
            Save();
            InteractableStateManager.SetInteractables();
        }
        #endregion
    }
}