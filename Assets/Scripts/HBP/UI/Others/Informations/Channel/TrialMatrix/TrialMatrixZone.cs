using HBP.Data.Experience.Dataset;
using HBP.Data.Informations;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using data = HBP.Data.TrialMatrix.Grid;

namespace HBP.UI.Informations
{
    public class TrialMatrixZone : MonoBehaviour
    {
        #region Properties
        [SerializeField] TrialMatrix.Grid.TrialMatrixGrid m_TrialMatrixGrid;
        data.TrialMatrixGrid m_TrialMatrixGridData;
        Dictionary<Data, Settings> m_SettingsByData;
        bool m_ShowWholeProtocolLastState;
        #endregion

        #region Public Methods
        public void Display(ChannelStruct[] channelStructs, DataStruct[] dataStructs)
        {
            DataStruct[] dataToDisplay = new DataStruct[dataStructs.Length];
            if (ApplicationState.UserPreferences.Visualization.TrialMatrix.ShowWholeProtocol)
            {
                for (int d = 0; d < dataStructs.Length; d++)
                {
                    dataToDisplay[d] = new DataStruct(dataStructs[d].Dataset, dataStructs[d].Data, dataStructs[d].Dataset.Protocol.Blocs.Select(b => new BlocStruct(b)));
                }
            }
            else
            {
                for (int d = 0; d < dataStructs.Length; d++)
                {
                    dataToDisplay[d] = new DataStruct(dataStructs[d].Dataset, dataStructs[d].Data, dataStructs[d].Blocs.ToList());
                }
            }
            SaveSettings();
            foreach (var data in dataToDisplay)
            {
                Data key = new Data(data.Dataset, data.Data);
                if (!m_SettingsByData.ContainsKey(key))
                {
                    m_SettingsByData.Add(key, new Settings(new Vector2(), true));
                }
            }
            m_TrialMatrixGridData = new data.TrialMatrixGrid(channelStructs, dataToDisplay);
            m_TrialMatrixGrid.gameObject.SetActive(true);
            m_TrialMatrixGrid.Display(m_TrialMatrixGridData);
            ApplySettings();
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            m_SettingsByData = new Dictionary<Data, Settings>();
            m_TrialMatrixGrid.gameObject.SetActive(false);
            //m_TrialMatrixList.OnAutoLimitsChanged.AddListener(OnChangeAutoLimits);
            //m_TrialMatrixList.OnLimitsChanged.AddListener(OnChangeLimits);
        }
        void SaveSettings()
        {
            foreach (var data in m_TrialMatrixGrid.Data)
            {
                Data key = new Data(data.GridData.DataStruct.Dataset, data.GridData.DataStruct.Data);
                var settings = m_SettingsByData[key];
                settings.UseDefaultLimit = data.UseDefaultLimits;
                if(!settings.UseDefaultLimit)
                {
                    settings.Limits = data.Limits;
                }
                m_SettingsByData[key] = settings;
            }
        }
        void ApplySettings()
        {
            foreach (var data in m_TrialMatrixGrid.Data)
            {
                Data key = new Data(data.GridData.DataStruct.Dataset, data.GridData.DataStruct.Data);
                Settings settings = m_SettingsByData[key];
                if(!settings.UseDefaultLimit)
                {
                    data.Limits = settings.Limits;
                }
            }
        }
        #endregion

        #region Structs
        struct Settings
        {
            #region Properties
            public Vector2 Limits { get; set; }
            public bool UseDefaultLimit { get; set; }
            #endregion

            #region Constructors
            public Settings(Vector2 limits, bool useDefaultLimits)
            {
                Limits = limits;
                UseDefaultLimit = useDefaultLimits;
            }
            #endregion
        }
        struct Data
        {
            #region Properties
            public Dataset Dataset { get; set; }
            public string DataLabel { get; set;}
            #endregion

            #region Constructors
            public Data(Dataset dataset, string dataLabel)
            {
                Dataset = dataset;
                DataLabel = dataLabel;
            }
            #endregion
        }
        #endregion
    }
}