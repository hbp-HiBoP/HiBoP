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
    public static class ProjectLoaderSaver
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
                await LoadingManager.LoadAsync((update, token) => projectToLoad.LoadAsync(projectInfo, update, token));
                await UniTask.SwitchToMainThread();
                InteractableStateManager.SetInteractables();
                UITools.CheckProjectIDAndAskForRegeneration().Forget();
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception e)
            {
                Debug.LogException(e);
                DialogBoxManager.OpenScrollable(Core.Enums.DialogBoxType.Error, "Unknown error", e.ToString()).Forget();
                ApplicationState.LoadedProject = projectLoaded;
                ApplicationState.LoadedProjectLocation = projectLoadedLocation;
            }
        }
        public async static UniTaskVoid Save(string path)
        {
            await SaveAsync(path);
        }
        public async static UniTaskVoid Save()
        {
            await SaveAsync();
        }
        public async static UniTaskVoid SaveAndReload()
        {
            await SaveAsync();
            InteractableStateManager.SetInteractables();
        }
        public async static UniTask SaveAsync()
        {
            await SaveAsync(ApplicationState.LoadedProjectLocation);
        }
        public async static UniTask SaveAsync(string path)
        {
            Module3DMain.SaveConfigurations();
            ApplicationState.LoadedProjectLocation = path;
            await LoadingManager.LoadAsync((update, token) => ApplicationState.LoadedProject.SaveAsync(path, update, token));
        }
        #endregion
    }
}