using HBP.Data.Experience.Dataset;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI.Extensions;
using System.Collections.ObjectModel;

namespace HBP.Data.Informations
{
    [Serializable]
    public class ChannelStruct : IEquatable<ChannelStruct>
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

        public override bool Equals(object obj)
        {
            return Equals(obj as ChannelStruct);
        }
        public bool Equals(ChannelStruct other)
        {
            return other != null &&
                   Channel == other.Channel &&
                   EqualityComparer<Patient>.Default.Equals(Patient, other.Patient);
        }
        public override int GetHashCode()
        {
            var hashCode = 252110562;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Channel);
            hashCode = hashCode * -1521134295 + EqualityComparer<Patient>.Default.GetHashCode(Patient);
            return hashCode;
        }
        public static bool operator ==(ChannelStruct struct1, ChannelStruct struct2)
        {
            return EqualityComparer<ChannelStruct>.Default.Equals(struct1, struct2);
        }
        public static bool operator !=(ChannelStruct struct1, ChannelStruct struct2)
        {
            return !(struct1 == struct2);
        }
        #endregion
    }

    [Serializable]
    public class DataStruct : IEquatable<DataStruct>
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

        public override bool Equals(object obj)
        {
            return Equals(obj as DataStruct);
        }
        public bool Equals(DataStruct other)
        {
            return other != null &&
                   EqualityComparer<Dataset>.Default.Equals(Dataset, other.Dataset) &&
                   Data == other.Data;
        }
        public override int GetHashCode()
        {
            var hashCode = 139406400;
            hashCode = hashCode * -1521134295 + EqualityComparer<Dataset>.Default.GetHashCode(Dataset);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Data);
            return hashCode;
        }
        public static bool operator ==(DataStruct struct1, DataStruct struct2)
        {
            return EqualityComparer<DataStruct>.Default.Equals(struct1, struct2);
        }
        public static bool operator !=(DataStruct struct1, DataStruct struct2)
        {
            return !(struct1 == struct2);
        }
        #endregion
    }
}