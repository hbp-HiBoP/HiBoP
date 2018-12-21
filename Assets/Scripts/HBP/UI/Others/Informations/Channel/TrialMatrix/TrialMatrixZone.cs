using HBP.Data.Experience.Dataset;
using HBP.Data.Informations;
using System.Collections.Generic;
using UnityEngine;
using data = HBP.Data.TrialMatrix.Grid;

namespace HBP.UI.Informations
{
    public class TrialMatrixZone : MonoBehaviour
    {
        #region Properties
        Texture2D m_Colormap;
        public Texture2D Colormap
        {
            get
            {
                return m_Colormap;
            }
            set
            {
                if(m_Colormap != value)
                {
                    m_Colormap = value;
                    m_TrialMatrixGrid.Colormap = value;
                }
            }
        }

        [SerializeField] TrialMatrix.Grid.TrialMatrixGrid m_TrialMatrixGrid;
        data.TrialMatrixGrid m_TrialMatrixGridData;
        Dictionary<Data, Settings> m_SettingsByData;
        #endregion

        #region Public Methods
        public void Display(ChannelStruct[] channelStructs, DataStruct[] dataStructs)
        {
            SaveSettings();
            foreach (var data in dataStructs)
            {
                Data key = new Data(data.Dataset, data.Data);
                if (!m_SettingsByData.ContainsKey(key))
                {
                    m_SettingsByData.Add(key, new Settings(new Vector2(), true));
                }
            }
            m_TrialMatrixGridData = new data.TrialMatrixGrid(channelStructs, dataStructs);
            m_TrialMatrixGrid.Display(m_TrialMatrixGridData);
            ApplySettings();
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            m_SettingsByData = new Dictionary<Data, Settings>();

            //m_TrialMatrixList.OnAutoLimitsChanged.AddListener(OnChangeAutoLimits);
            //m_TrialMatrixList.OnLimitsChanged.AddListener(OnChangeLimits);
        }
        void SaveSettings()
        {
            foreach (var data in m_TrialMatrixGrid.Data)
            {
                Data key = new Data(data.GridData.DataStruct.Dataset, data.GridData.DataStruct.Data);
                var settings = m_SettingsByData[key];
                settings.UsePrecalculatedLimits = data.UsePrecalculatedLimits;
                if(!settings.UsePrecalculatedLimits)
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
                if(!settings.UsePrecalculatedLimits)
                {
                    data.Limits = settings.Limits;
                }
            }
        }
        #endregion

        #region Structs
        public struct Settings
        {
            #region Properties
            public Vector2 Limits { get; set; }
            public bool UsePrecalculatedLimits { get; set; }
            #endregion

            #region Constructors
            public Settings(Vector2 limits, bool useAutoLimits)
            {
                Limits = limits;
                UsePrecalculatedLimits = useAutoLimits;
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