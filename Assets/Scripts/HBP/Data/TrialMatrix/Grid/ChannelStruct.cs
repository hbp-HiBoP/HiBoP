using HBP.Data.Experience.Dataset;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI.Extensions;
using System.Collections.ObjectModel;

namespace HBP.Data.Informations
{
    [Serializable]
    public class ChannelStruct
    {
        #region Properties
        [SerializeField] string m_Channel;
        public string Channel
        {
            get
            {
                return m_Channel;
            }
            set
            {
                SetPropertyUtility.SetClass(ref m_Channel, value);
            }
        }

        [SerializeField] Patient m_Patient;
        public Patient Patient
        {
            get
            {
                return m_Patient;
            }
            set
            {
                SetPropertyUtility.SetClass(ref m_Patient, value);
            }
        }
        #endregion

        #region Constructors
        public ChannelStruct(string channel, Patient patient)
        {
            Channel = channel;
            Patient = patient;
        }
        #endregion
    }

    [Serializable]
    public class DataStruct
    {
        #region Properties
        [SerializeField] Dataset m_Dataset;
        public Dataset Dataset
        {
            get
            {
                return m_Dataset;
            }
            set
            {
                SetPropertyUtility.SetClass(ref m_Dataset, value);
            }
        }
        [SerializeField] string m_Data;
        public string Data
        {
            get
            {
                return m_Data;
            }
            set
            {
                SetPropertyUtility.SetClass(ref m_Data, value);
            }
        }

        [SerializeField] List<Experience.Protocol.Bloc> m_Blocs;
        public ReadOnlyCollection<Experience.Protocol.Bloc> Blocs
        {
            get
            {
                return new ReadOnlyCollection<Experience.Protocol.Bloc>(m_Blocs);
            }
        }
        #endregion

        #region Constructors
        public DataStruct(Dataset dataset, string data, List<Experience.Protocol.Bloc> blocs = null)
        {
            m_Dataset = dataset;
            m_Data = data;
            m_Blocs = blocs;
        }
        #endregion

        #region Public Methods
        public void AddBloc(Experience.Protocol.Bloc bloc)
        {
            m_Blocs.Add(bloc);
        }
        public void RemoveBloc(Experience.Protocol.Bloc bloc)
        {
            m_Blocs.Remove(bloc);
        }
        public void SetBlocs(Experience.Protocol.Bloc[] blocs)
        {
            m_Blocs = new List<Experience.Protocol.Bloc>(blocs);
        }
        #endregion
    }
}