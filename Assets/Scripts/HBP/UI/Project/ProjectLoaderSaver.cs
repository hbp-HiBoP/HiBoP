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
            DataManager.Clear();
            this.StartCoroutineAsync(c_Load(projectInfo));
        }
        public void Save(string path)
        {
            ApplicationState.Module3D.SaveConfigurations();
            ApplicationState.ProjectLoadedLocation = path;
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

        #region Coroutines
        public IEnumerator c_Load(ProjectInfo info)
        {
            Project oldProject = ApplicationState.ProjectLoaded;
            Project project = new Project();
            ApplicationState.ProjectLoaded = project;
            yield return Ninja.JumpToUnity;
            LoadingCircle loadingCircle = ApplicationState.LoadingManager.Open();
            GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
            onChangeProgress.AddListener((progress,time,message) => loadingCircle.ChangePercentage(progress / 2.0f, time, message));
            Task loadingTask;
            yield return this.StartCoroutineAsync(project.c_Load(info, onChangeProgress), out loadingTask);
            switch (loadingTask.State)
            {
                case TaskState.Done:
                    yield return new WaitForSeconds(0.5f);
                    ApplicationState.ProjectLoaded = project;
                    ApplicationState.ProjectLoadedLocation = Directory.GetParent(info.Path).FullName;
                    FindObjectOfType<MenuButtonState>().SetInteractables();
                    break;
                case TaskState.Error:
                    Exception exception = loadingTask.Exception;
                    ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, exception.ToString(), exception.Message);
                    ApplicationState.ProjectLoaded = oldProject;
                    break;
            }
            loadingCircle.Close();
        }
        public IEnumerator c_Save(string path)
        {
            yield return Ninja.JumpToUnity;
            LoadingCircle loadingCircle = ApplicationState.LoadingManager.Open();
            GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
            onChangeProgress.AddListener((progress, time, message) => loadingCircle.ChangePercentage(progress, time, message));
            Task savingTask;
            yield return this.StartCoroutineAsync(ApplicationState.ProjectLoaded.c_Save(path,onChangeProgress),out savingTask);
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
            loadingCircle.Close();
        }
        IEnumerator c_SaveAndReload(string path)
        {
            yield return c_Save(path);
            yield return Ninja.JumpToUnity;
            // TODO
            GameObject.FindObjectOfType<MenuButtonState>().SetInteractables();
        }
        #endregion
    }
}