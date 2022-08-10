using HBP.Display.Informations;
using System.Collections.Generic;
using System.Linq;
using Tools.CSharp;
using UnityEngine;
using data = HBP.Display.Informations.TrialMatrix.Grid;

namespace HBP.UI.Informations
{
    public class TrialMatrixZone : MonoBehaviour
    {
        #region Properties
        [SerializeField] TrialMatrix.Grid.TrialMatrixGrid m_TrialMatrixGrid;
        data.TrialMatrixGrid m_TrialMatrixGridData;
        Dictionary<data.TrialMatrixGrid.TrialMatrixData, Settings> m_SettingsByData;
        bool m_ShowWholeProtocolLastState;
        #endregion

        #region Public Methods
        public void Display(ChannelStruct[] channelStructs, Data[] dataStructs)
        {
            List<data.TrialMatrixGrid.TrialMatrixData> dataToDisplay = new List<data.TrialMatrixGrid.TrialMatrixData>();
            if (Core.Data.ApplicationState.UserPreferences.Visualization.TrialMatrix.ShowWholeProtocol)
            {
                foreach (var data in dataStructs)
                {
                    if (data is IEEGData)
                    {
                        if (!dataToDisplay.OfType<data.TrialMatrixGrid.IEEGTrialMatrixData>().Any(d => d.Name == data.Name && d.Dataset == data.Dataset))
                        {
                            dataToDisplay.Add(new data.TrialMatrixGrid.IEEGTrialMatrixData(data.Dataset, data.Name, data.Dataset.Protocol.Blocs));
                        }
                    }
                    else if (data is CCEPData ccepData)
                    {
                        if (!dataToDisplay.OfType<data.TrialMatrixGrid.CCEPTrialMatrixData>().Any(d => d.Name == data.Name && d.Dataset == data.Dataset && d.Source == ccepData.Source))
                        {
                            dataToDisplay.Add(new data.TrialMatrixGrid.CCEPTrialMatrixData(data.Dataset, data.Name, data.Dataset.Protocol.Blocs, ccepData.Source));
                        }
                    }
                }
            }
            else
            {
                foreach (var data in dataStructs)
                {
                    if (data is IEEGData)
                    {
                        data.TrialMatrixGrid.IEEGTrialMatrixData trialMatrixData = dataToDisplay.OfType<data.TrialMatrixGrid.IEEGTrialMatrixData>().FirstOrDefault(d => d.Name == data.Name && d.Dataset == data.Dataset); 
                        if(trialMatrixData == null)
                        {
                            dataToDisplay.Add(new data.TrialMatrixGrid.IEEGTrialMatrixData(data.Dataset, data.Name, new List<Core.Data.Bloc>() { data.Bloc }));
                        }
                        else
                        {
                            trialMatrixData.Blocs.Add(data.Bloc);
                        }
                    }
                    else if (data is CCEPData ccepData)
                    {
                        data.TrialMatrixGrid.CCEPTrialMatrixData trialMatrixData = dataToDisplay.OfType<data.TrialMatrixGrid.CCEPTrialMatrixData>().FirstOrDefault(d => d.Name == data.Name && d.Dataset == data.Dataset && d.Source == ccepData.Source);
                        if (trialMatrixData == null)
                        {
                            dataToDisplay.Add(new data.TrialMatrixGrid.CCEPTrialMatrixData(data.Dataset, data.Name,  new List<Core.Data.Bloc>() { data.Bloc }, ccepData.Source));
                        }
                        else
                        {
                            trialMatrixData.Blocs.Add(data.Bloc);
                        }
                    }
                }
            }
            SaveSettings();
            foreach (var data in dataToDisplay)
            {
                m_SettingsByData.AddIfAbsent(data, new Settings());
            }
            m_TrialMatrixGridData = new data.TrialMatrixGrid(channelStructs, dataToDisplay.ToArray());
            m_TrialMatrixGrid.gameObject.SetActive(true);
            m_TrialMatrixGrid.Display(m_TrialMatrixGridData);
            ApplySettings();
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            m_SettingsByData = new Dictionary<data.TrialMatrixGrid.TrialMatrixData, Settings>();
            m_TrialMatrixGrid.gameObject.SetActive(false);
        }
        void SaveSettings()
        {
            foreach (var data in m_TrialMatrixGrid.Data)
            {
                var settings = m_SettingsByData[data.GridData.DataStruct];
                settings.UseDefaultLimit = data.UseDefaultLimits;
                if (!settings.UseDefaultLimit)
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
                if (!settings.UseDefaultLimit)
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
            public Settings() : this(Vector2.zero, true)
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