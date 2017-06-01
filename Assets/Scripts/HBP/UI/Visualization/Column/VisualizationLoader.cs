using System;
using System.Collections;
using UnityEngine.Events;
using UnityEngine;
using Tools.Unity;
using CielaSpike;

namespace HBP.UI.Visualization
{
    public class VisualizationLoader : MonoBehaviour
    {
        #region Public Methods
        public void Load(Data.Visualization.Visualization visualization)
        {
            this.StartCoroutineAsync(c_Load(visualization));
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            // TODO
            ApplicationState.Module3D.OnLoadSinglePatientSceneFromMultiPatientsScene.AddListener((visualization, patient) => LoadSPSceneFromMP(visualization, patient));
        }
        void LoadSPSceneFromMP(Data.Visualization.MultiPatientsVisualization visualization, Data.Patient patient)
        {
            // TODO
            Data.Visualization.SinglePatientVisualization singlePatientVisualization = Data.Visualization.SinglePatientVisualization.LoadFromMultiPatients(visualization, patient);
            ApplicationState.Module3D.AddVisualization(singlePatientVisualization);
        }
        IEnumerator c_Load(Data.Visualization.Visualization visualization)
        {
            yield return Ninja.JumpToUnity;
            LoadingCircle loadingCircle = ApplicationState.LoadingManager.Open();
            UnityAction<float, float, string> OnChangeLoadingProgressAction = new UnityAction<float, float, string>((progress, time, message) => loadingCircle.ChangePercentage(progress, time, message));
            visualization.OnChangeLoadingProgress.AddListener(OnChangeLoadingProgressAction);
            Task loadingTask;
            yield return this.StartCoroutineAsync(visualization.c_Load(), out loadingTask);
            switch (loadingTask.State)
            {
                case TaskState.Done:
                    yield return new WaitForSeconds(0.5f);
                    break;
                case TaskState.Error:
                    Exception exception = loadingTask.Exception;
                    ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, exception.ToString(), exception.Message);
                    break;
            }
            visualization.OnChangeLoadingProgress.RemoveListener(OnChangeLoadingProgressAction);
            loadingCircle.Close();
        }
        #endregion
    }
}