using UnityEngine;
using UnityEngine.Events;
using System;
using System.IO;
using System.Collections;
using CielaSpike;
using Tools.Unity;
using HBP.Data.General;

namespace HBP.UI
{
    public class ProjectLoaderSaver : MonoBehaviour
    {
        #region Public Methods  
        public void Load(ProjectInfo projectInfo)
        {
            this.StartCoroutineAsync(c_Load(projectInfo));
        }
        public void Save(string path)
        {
            this.StartCoroutineAsync(c_Save(path));
        }
        public void Save()
        {
            Save(ApplicationState.ProjectLoadedLocation);
        }
        public void SaveAndReload()
        {
            this.StartCoroutineAsync(c_SaveAndReload(ApplicationState.ProjectLoadedLocation));
        }
        #endregion

        #region Private Methods
        IEnumerator c_Load(ProjectInfo info)
        {
            Project oldProject = ApplicationState.ProjectLoaded;
            Project project = new Project();
            ApplicationState.ProjectLoaded = project;
            yield return Ninja.JumpToUnity;
            LoadingCircle loadingCircle = ApplicationState.LoadingManager.Open();
            UnityAction<float, float, string> OnChangeLoadingProgressAction = new UnityAction<float, float, string>((progress,time,message) => loadingCircle.ChangePercentage(progress, time, message));
            project.OnChangeLoadingProgress.AddListener(OnChangeLoadingProgressAction);
            Task loadingTask;
            yield return this.StartCoroutineAsync(project.c_Load(info), out loadingTask);
            switch (loadingTask.State)
            {
                case TaskState.Done:
                    yield return new WaitForSeconds(0.5f);
                    ApplicationState.ProjectLoaded = project;
                    ApplicationState.ProjectLoadedLocation = Directory.GetParent(info.Path).FullName;
                    // TODO
                    GameObject.FindGameObjectWithTag("Gestion").GetComponent<MenuButtonState>().SetInteractableButtons();
                    break;
                case TaskState.Error:
                    Exception exception = loadingTask.Exception;
                    ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, exception.ToString(), exception.Message);
                    ApplicationState.ProjectLoaded = oldProject;
                    break;
            }
            project.OnChangeLoadingProgress.RemoveListener(OnChangeLoadingProgressAction);
            loadingCircle.Close();
        }
        IEnumerator c_Save(string path)
        {
            yield return Ninja.JumpToUnity;
            LoadingCircle loadingCircle = ApplicationState.LoadingManager.Open();
            UnityAction<float, float, string> OnChangeSavingProgressAction = new UnityAction<float, float, string>((progress, time, message) => loadingCircle.ChangePercentage(progress, time, message));
            ApplicationState.ProjectLoaded.OnChangeSavingProgress.AddListener(OnChangeSavingProgressAction);
            Task savingTask;
            yield return this.StartCoroutineAsync(ApplicationState.ProjectLoaded.c_Save(path),out savingTask);
            switch (savingTask.State)
            {
                case TaskState.Done:
                    yield return new WaitForSeconds(0.5f);
                    break;
                case TaskState.Error:
                    Exception exception = savingTask.Exception;
                    ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, exception.ToString(), exception.Message);
                    break;
            }
            ApplicationState.ProjectLoaded.OnChangeSavingProgress.RemoveListener(OnChangeSavingProgressAction);
            loadingCircle.Close();
        }
        IEnumerator c_SaveAndReload(string path)
        {
            yield return c_Save(path);
            yield return Ninja.JumpToUnity;
            // TODO
            GameObject.FindGameObjectWithTag("Gestion").GetComponent<MenuButtonState>().SetInteractableButtons();
        }
        #endregion
    }
}