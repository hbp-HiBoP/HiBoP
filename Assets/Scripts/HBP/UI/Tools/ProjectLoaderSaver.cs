using System.IO;
using HBP.Core.Tools;
using HBP.Core.Data;
using HBP.Data.Module3D;
using HBP.UI.Main;
using System;
using System.Threading.Tasks;

namespace HBP.UI.Tools
{
    public class ProjectLoaderSaver
    {
        #region Public Methods  
        public async static void Load(ProjectInfo projectInfo)
        {
            await LoadAsync(projectInfo);
        }
        public async static Task LoadAsync(ProjectInfo projectInfo)
        {
            Project projectToLoad = new();

            DataManager.Clear();
            Project projectLoaded = ApplicationState.LoadedProject;
            string projectLoadedLocation = ApplicationState.LoadedProjectLocation;
            ApplicationState.LoadedProject = projectToLoad;
            ApplicationState.LoadedProjectLocation = Directory.GetParent(projectInfo.Path).FullName;

            try
            {
                await LoadingManager.LoadAsync(update => projectToLoad.LoadAsync(projectInfo, update));
                InteractableStateManager.SetInteractables();
                UITools.CheckProjectIDAndAskForRegeneration();
            }
            catch (Exception)
            {
                ApplicationState.LoadedProject = projectLoaded;
                ApplicationState.LoadedProjectLocation = projectLoadedLocation;
            }
        }
        public static void Save(string path)
        {
            Module3DMain.SaveConfigurations();
            ApplicationState.LoadedProjectLocation = path;
            LoadingManager.Load(update => ApplicationState.LoadedProject.SaveAsync(path, update));
        }
        public static void Save()
        {
            Save(ApplicationState.LoadedProjectLocation);
        }
        public static void SaveAndReload()
        {
            Save();
            InteractableStateManager.SetInteractables();
        }
        #endregion
    }
}