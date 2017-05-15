using UnityEngine;
using Tools.Unity;

namespace HBP.UI.Visualization
{
    public class VisualizationLoader : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        GameObject m_LoadingCirclePrefab;
        LoadingCircle m_LoadingCircle;
        [SerializeField]
        GameObject m_PopUpPrefab;
        #endregion

        #region Public Methods
        public void Load(Data.Visualization.Visualization visualization)
        {
            visualization.OnErrorOccur.AddListener((message) => ErrorHandler(message));
            visualization.OnChangeLoadingProgress.AddListener((progress, duration, message) => ProgressHandler(progress, duration, message));
            m_LoadingCircle = Instantiate(m_LoadingCirclePrefab).GetComponent<LoadingCircle>();
            visualization.Load();
            Destroy(m_LoadingCircle.gameObject);
            ApplicationState.Module3D.AddVisualization(visualization);
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
        void ErrorHandler(string message)
        {
            GameObject popUpGameObject = Instantiate(m_PopUpPrefab);
            PopUp popUp = popUpGameObject.GetComponent<PopUp>();
            popUp.Show(message);
        }
        void ProgressHandler(float progress,float duration,string message)
        {
            m_LoadingCircle.Text = message;
            m_LoadingCircle.ChangePercentage(m_LoadingCircle.Progress, progress, duration);
        }
        #endregion
    }
}