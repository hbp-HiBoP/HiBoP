using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.Data.Database;
using HBP.Data.Informations;
using HBP.UI.Informations;
using HBP.UI.Tools;
using System;
using System.Linq;
using UnityEngine;

namespace HBP.UI.Database
{
    public class TrialMatrixDisplayer : MonoBehaviour
    {
        #region Properties
        private Patient m_Patient;
        private string m_DataName;
        #endregion

        #region Public Methods
        public void Display(Patient patient, string dataName)
        {
            m_Patient = patient;
            m_DataName = dataName;
            LoadData(patient, dataName).Forget();
        }
        #endregion

        #region Private Methods
        private async UniTaskVoid LoadData(Patient patient, string dataName)
        {
            await LoadingManager.LoadAsync(update => LoadDataAsync(patient, dataName, update));
        }
        private async UniTask LoadDataAsync(Patient patient, string dataName, Action<float, float, LoadingText> updateProgress)
        {
            await UniTask.SwitchToThreadPool();
            var dataInfosToLoad = DatabaseManager.Database.DataInfos.OfType<IEEGDataInfo>().Where(d => d.Patient == patient && d.Name == dataName).ToList();
            int progress = 0;
            int length = dataInfosToLoad.Count;
            foreach (var dataInfo in dataInfosToLoad)
            {
                updateProgress((float)progress / length, 0, new LoadingText("Loading data for ", dataInfo.Protocol.Name, $" {++progress} / {length}"));
                DataManager.GetData(dataInfo);
            }
        }
        #endregion
    }
}