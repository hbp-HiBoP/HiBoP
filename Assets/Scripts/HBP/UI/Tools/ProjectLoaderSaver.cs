using System.IO;
using HBP.Core.Tools;
using HBP.Core.Data;
using HBP.Data.Module3D;
using HBP.UI.Main;
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HBP.UI.Tools
{
    public class ProjectLoaderSaver
    {
        #region Public Methods  
        public async static UniTaskVoid Load(ProjectInfo projectInfo)
        {
            await LoadAsync(projectInfo);
        }
        public async static UniTask LoadAsync(ProjectInfo projectInfo)
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
                await UniTask.SwitchToMainThread();
                InteractableStateManager.SetInteractables();
                UITools.CheckProjectIDAndAskForRegeneration().Forget();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, e.ToString(), e.Message).Forget();
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