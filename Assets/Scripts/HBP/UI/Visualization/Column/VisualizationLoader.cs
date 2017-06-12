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
        void LoadSPSceneFromMP(Data.Visualization.Visualization visualization, Data.Patient patient)
        {
            // TODO
            //Data.Visualization.Visualization singlePatientVisualization = Data.Visualization.Visualization.LoadFromMultiPatients(visualization, patient);
            //ApplicationState.Module3D.AddVisualization(singlePatientVisualization);
        }
        IEnumerator c_Load(Data.Visualization.Visualization visualization)
        {
            yield return Ninja.JumpToUnity;
            LoadingCircle loadingCircle = ApplicationState.LoadingManager.Open();
            GenericEvent<float, float, string> OnChangeLoadingProgress = new GenericEvent<float, float, string>();
            OnChangeLoadingProgress.AddListener((progress, time, message) => loadingCircle.ChangePercentage(progress, time, message));
            Task loadingTask;
            yield return this.StartCoroutineAsync(visualization.c_Load(OnChangeLoadingProgress), out loadingTask);
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
            loadingCircle.Close();
        }
        #endregion
    }
}