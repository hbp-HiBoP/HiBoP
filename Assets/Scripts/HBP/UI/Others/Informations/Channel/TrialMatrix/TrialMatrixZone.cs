using HBP.Data.Informations;
using System.Collections.Generic;
using System.Linq;
using Tools.CSharp;
using UnityEngine;
using data = HBP.Data.TrialMatrix.Grid;

namespace HBP.UI.Informations
{
    public class TrialMatrixZone : MonoBehaviour
    {
        #region Properties
        [SerializeField] TrialMatrix.Grid.TrialMatrixGrid m_TrialMatrixGrid;
        data.TrialMatrixGrid m_TrialMatrixGridData;
        Dictionary<DataStruct, Settings> m_SettingsByData;
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
                    DataStruct dataStruct = dataStructs[d];
                    if (dataStruct is IEEGDataStruct)
                    {
                        dataStruct = new IEEGDataStruct(dataStruct.Dataset, dataStruct.Data, dataStruct.Dataset.Protocol.Blocs.Select(b => new BlocStruct(b)));
                    }
                    else if (dataStruct is CCEPDataStruct ccepDataStruct)
                    {
                        dataStruct = new CCEPDataStruct(ccepDataStruct.Dataset, ccepDataStruct.Data, ccepDataStruct.Source, ccepDataStruct.Dataset.Protocol.Blocs.Select(b => new BlocStruct(b)));

                    }
                    dataToDisplay[d] = dataStruct;
                }
            }
            else
            {
                for (int d = 0; d < dataStructs.Length; d++)
                {
                    DataStruct dataStruct = dataStructs[d];
                    if (dataStruct is IEEGDataStruct)
                    {
                        dataStruct = new IEEGDataStruct(dataStruct.Dataset, dataStruct.Data, dataStruct.Blocs.ToList());
                    }
                    else if (dataStruct is CCEPDataStruct ccepDataStruct)
                    {
                        dataStruct = new CCEPDataStruct(ccepDataStruct.Dataset, ccepDataStruct.Data, ccepDataStruct.Source, ccepDataStruct.Blocs.ToList());

                    }
                    dataToDisplay[d] = dataStruct;
                }
            }
            SaveSettings();
            foreach (var data in dataToDisplay)
            {
                m_SettingsByData.AddIfAbsent(data, new Settings());
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
            m_SettingsByData = new Dictionary<DataStruct, Settings>();
            m_TrialMatrixGrid.gameObject.SetActive(false);
        }
        void SaveSettings()
        {
            foreach (var data in m_TrialMatrixGrid.Data)
            {
                var settings = m_SettingsByData[data.GridData.DataStruct];
                settings.UseDefaultLimit = data.UseDefaultLimits;
                if(!settings.UseDefaultLimit)
                {
                    settings.Limits = data.Limits;
                }
                m_SettingsByData[data.GridData.DataStruct] = settings;
            }
        }
        void ApplySettings()
        {
            foreach (var data in m_TrialMatrixGrid.Data)
            {
                Settings settings = m_SettingsByData[data.GridData.DataStruct];
                if(!settings.UseDefaultLimit)
                {
                    data.Limits = settings.Limits;
                }
            }
        }
        #endregion

        #region Structs
        class Settings
        {
            #region Properties
            public Vector2 Limits { get; set; }
            public bool UseDefaultLimit { get; set; }
            #endregion

            #region Constructors
            public Settings(): this(Vector2.zero, true)
            {

            }
            public Settings(Vector2 limits, bool useDefaultLimits)
            {
                Limits = limits;
                UseDefaultLimit = useDefaultLimits;
            }
            #endregion
        }       
        #endregion
    }
}