using HBP.Data.Experience.Dataset;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI.Extensions;
using System.Collections.ObjectModel;
using System.Linq;

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

        [SerializeField] bool m_IsBlacklisted;
        public bool IsBlacklisted
        {
            get
            {
                return m_IsBlacklisted;
            }
            set
            {
                SetPropertyUtility.SetStruct(ref m_IsBlacklisted, value);
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
        public ChannelStruct(string channel, Patient patient, bool isBlacklisted)
        {
            Channel = channel;
            Patient = patient;
            IsBlacklisted = isBlacklisted;
        }
        public ChannelStruct(Module3D.Site site)
        {
            Channel = site.Information.Name;
            Patient = site.Information.Patient;
            IsBlacklisted = site.State.IsBlackListed;
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

        [SerializeField] List<BlocStruct> m_Blocs = new List<BlocStruct>();
        public ReadOnlyCollection<BlocStruct> Blocs
        {
            get
            {
                return new ReadOnlyCollection<BlocStruct>(m_Blocs);
            }
        }
        #endregion

        #region Constructors
        public DataStruct(Dataset dataset, string data, IEnumerable<BlocStruct> blocs = null)
        {
            m_Dataset = dataset;
            m_Data = data;
            if(blocs != null)
            {
                SetBlocs(blocs);
            }
        }
        #endregion

        #region Public Methods
        public void AddBloc(BlocStruct bloc)
        {
            if(!m_Blocs.Contains(bloc))
            {
                m_Blocs.Add(bloc);
            }
        }
        public void RemoveBloc(BlocStruct bloc)
        {
            m_Blocs.Remove(bloc);
        }
        public void SetBlocs(IEnumerable<BlocStruct> blocs)
        {
            m_Blocs = blocs.ToList();
        }

        public override bool  Equals(object obj)
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

    [Serializable]
    public class CCEPDataStruct : DataStruct
    {
        #region Properties
        [SerializeField] ChannelStruct m_Source;
        public ChannelStruct Source
        {
            get
            {
                return m_Source;
            }
            set
            {
                SetPropertyUtility.SetClass(ref m_Source, value);
            }
        }
        #endregion

        #region Constructors
        public CCEPDataStruct(Dataset dataset, string data, ChannelStruct channel, IEnumerable<BlocStruct> blocs = null) : base(dataset, data, blocs)
        {
            Source = channel;
        }
        #endregion

        #region Public Methods
        public override bool Equals(object obj)
        {
            return Equals(obj as CCEPDataStruct);
        }
        public bool Equals(CCEPDataStruct other)
        {
            return base.Equals(other) && EqualityComparer<ChannelStruct>.Default.Equals(Source, other.Source);
        }
        public override int GetHashCode()
        {
            var hashCode = 139406400 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<ChannelStruct>.Default.GetHashCode(Source);
            return hashCode;
        }
        public static bool operator ==(CCEPDataStruct struct1, CCEPDataStruct struct2)
        {
            return EqualityComparer<CCEPDataStruct>.Default.Equals(struct1, struct2);
        }
        public static bool operator !=(CCEPDataStruct struct1, CCEPDataStruct struct2)
        {
            return !(struct1 == struct2);
        }
        #endregion
    }

    [Serializable]
    public class IEEGDataStruct : DataStruct
    {
        #region Constructors
        public IEEGDataStruct(Dataset dataset, string data, IEnumerable<BlocStruct> blocs = null) : base(dataset, data, blocs)
        {
        }
        #endregion

        #region Public Methods
        public bool Equals(IEEGDataStruct other)
        {
            return base.Equals(other);
        }
        #endregion
    }

    [Serializable]
    public class ROIStruct : IEquatable<ROIStruct>
    {
        #region Properties
        public string Name { get; set; }
        public List<ChannelStruct> Channels { get; set; }
        #endregion

        #region Constructors
        public ROIStruct(string name, IEnumerable<ChannelStruct> channels)
        {
            Name = name;
            Channels = new List<ChannelStruct>(channels);
        }
        #endregion

        #region Public Methods
        public override bool Equals(object obj)
        {
            return Equals(obj as ROIStruct);
        }
        public bool Equals(ROIStruct other)
        {
            bool notNull = other != null;
            if(notNull)
            {
                bool sameName = Name == other.Name;
                bool collection = EqualityComparer<List<ChannelStruct>>.Default.Equals(Channels, other.Channels);
            }
            return other != null &&
                   Name == other.Name &&
                   EqualityComparer<List<ChannelStruct>>.Default.Equals(Channels, other.Channels);
        }
        public override int GetHashCode()
        {
            var hashCode = 252110562;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<List<ChannelStruct>>.Default.GetHashCode(Channels);
            return hashCode;
        }
        public static bool operator ==(ROIStruct struct1, ROIStruct struct2)
        {
            return EqualityComparer<ROIStruct>.Default.Equals(struct1, struct2);
        } 
        public static bool operator !=(ROIStruct struct1, ROIStruct struct2)
        {
            return !(struct1 == struct2);
        }
        #endregion
    }

    [Serializable]
    public class SceneROIStruct : IEquatable<SceneROIStruct>
    {
        #region Properties
        public string Name { get; set; }
        public Dictionary<DataStruct,List<ChannelStruct>> ChannelsByData { get; set; }
        #endregion

        #region Constructors
        public SceneROIStruct(string name, Dictionary<DataStruct, List<ChannelStruct>> channelsByData)
        {
            Name = name;
            ChannelsByData = new Dictionary<DataStruct, List<ChannelStruct>>(channelsByData);
        }
        #endregion

        #region Public Methods
        public override bool Equals(object obj)
        {
            return Equals(obj as SceneROIStruct);
        }
        public bool Equals(SceneROIStruct other)
        {
            bool notNull = other != null;
            if (notNull)
            {
                bool sameName = Name == other.Name;
                bool collection = EqualityComparer<Dictionary<DataStruct, List<ChannelStruct>>>.Default.Equals(ChannelsByData, other.ChannelsByData);
            }
            return other != null &&
                   Name == other.Name &&
                   EqualityComparer<Dictionary<DataStruct, List<ChannelStruct>>>.Default.Equals(ChannelsByData, other.ChannelsByData);
        }
        public override int GetHashCode()
        {
            var hashCode = 252110562;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<Dictionary<DataStruct, List<ChannelStruct>>>.Default.GetHashCode(ChannelsByData);
            return hashCode;
        }
        public static bool operator ==(SceneROIStruct struct1, SceneROIStruct struct2)
        {
            return EqualityComparer<SceneROIStruct>.Default.Equals(struct1, struct2);
        }
        public static bool operator !=(SceneROIStruct struct1, SceneROIStruct struct2)
        {
            return !(struct1 == struct2);
        }
        #endregion
    }

    [Serializable]
    public class BlocStruct : IEquatable<BlocStruct>
    {
        #region Properties
        [SerializeField] Experience.Protocol.Bloc m_Bloc;
        public Experience.Protocol.Bloc Bloc
        {
            get
            {
                return m_Bloc;
            }
            set
            {
                SetPropertyUtility.SetClass(ref m_Bloc, value);
            }
        }

        [SerializeField] List<ROIStruct> m_ROIs = new List<ROIStruct>();
        public ReadOnlyCollection<ROIStruct> ROIs
        {
            get
            {
                return new ReadOnlyCollection<ROIStruct>(m_ROIs);
            }
        }
        #endregion

        #region Constructors
        public BlocStruct(Experience.Protocol.Bloc bloc, IEnumerable<ROIStruct> ROI = null)
        {
            Bloc = bloc;
            if(ROI != null)
            {
                SetROIs(ROI);
            }
        }
        #endregion

        #region Public Methods
        public void AddROI(ROIStruct ROI)
        {
            if (!m_ROIs.Contains(ROI))
            {
                m_ROIs.Add(ROI);
            }
        }
        public void RemoveROI(ROIStruct ROI)
        {
            m_ROIs.Remove(ROI);
        }
        public void SetROIs(IEnumerable<ROIStruct> ROIs)
        {
            m_ROIs = ROIs.ToList();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as BlocStruct);
        }
        public bool Equals(BlocStruct other)
        {
            return other != null &&
                   Bloc == other.Bloc &&
                   EqualityComparer<ReadOnlyCollection<ROIStruct>>.Default.Equals(ROIs, other.ROIs);
        }
        public override int GetHashCode()
        {
            var hashCode = 252110562;
            hashCode = hashCode * -1521134295 + EqualityComparer<Experience.Protocol.Bloc>.Default.GetHashCode(Bloc);
            hashCode = hashCode * -1521134295 + EqualityComparer<IEnumerable<ROIStruct>>.Default.GetHashCode(ROIs);
            return hashCode;
        }
        public static bool operator ==(BlocStruct struct1, BlocStruct struct2)
        {
            return EqualityComparer<BlocStruct>.Default.Equals(struct1, struct2);
        }
        public static bool operator !=(BlocStruct struct1, BlocStruct struct2)
        {
            return !(struct1 == struct2);
        }
        #endregion
    }
}