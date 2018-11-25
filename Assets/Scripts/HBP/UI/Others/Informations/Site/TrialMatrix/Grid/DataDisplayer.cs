using HBP.Data.TrialMatrix.Grid;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI.TrialMatrix.Grid
{
    public class DataDisplayer : MonoBehaviour
    {
        #region Properties
        [SerializeField] GameObject DataPrefab;
        #endregion

        #region Public Methods
        public void Set(Dictionary<DataStruct, Dictionary<ChannelStruct, HBP.Data.TrialMatrix.TrialMatrix>> trialMatrixByChannelAndData, Texture2D colormap)
        {
            foreach (var d in trialMatrixByChannelAndData.Keys)
            {
                AddData(d, trialMatrixByChannelAndData[d], colormap, new Vector2(80,120));
            }
        }
        #endregion

        #region Private Methods
        void AddData(DataStruct dataStruct, Dictionary<ChannelStruct, HBP.Data.TrialMatrix.TrialMatrix> trialMatrixByChannel, Texture2D colormap, Vector2 limits)
        {
            Data data = Instantiate(DataPrefab, transform).GetComponent<Data>();
            data.Set(dataStruct, trialMatrixByChannel, limits, colormap);
        }
        #endregion
    }
}