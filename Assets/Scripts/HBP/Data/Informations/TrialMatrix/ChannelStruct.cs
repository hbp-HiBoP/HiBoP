﻿using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI.Extensions;

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

        [SerializeField] Core.Data.Patient m_Patient;
        public Core.Data.Patient Patient
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
        public ChannelStruct(string channel, Core.Data.Patient patient, bool isBlacklisted)
        {
            Channel = channel;
            Patient = patient;
            IsBlacklisted = isBlacklisted;
        }
        public ChannelStruct(Core.Object3D.Site site)
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
                   EqualityComparer<Core.Data.Patient>.Default.Equals(Patient, other.Patient);
        }
        public override int GetHashCode()
        {
            var hashCode = 252110562;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Channel);
            hashCode = hashCode * -1521134295 + EqualityComparer<Core.Data.Patient>.Default.GetHashCode(Patient);
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
    public class Data : IEquatable<Data>
    {
        #region Properties
        [SerializeField] Core.Data.Dataset m_Dataset;
        public Core.Data.Dataset Dataset
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

        [SerializeField] string m_Name;
        public string Name
        {
            get
            {
                return m_Name;
            }
            set
            {
                SetPropertyUtility.SetClass(ref m_Name, value);
            }
        }

        [SerializeField] Core.Data.Bloc m_Bloc;
        public Core.Data.Bloc Bloc
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
        #endregion

        #region Constructors
        public Data(Core.Data.Dataset dataset, string data, Core.Data.Bloc bloc)
        {
            m_Dataset = dataset;
            m_Name = data;
            m_Bloc = bloc;
        }
        #endregion

        #region Public Methods
        public override bool Equals(object obj)
        {
            return Equals(obj as Data);
        }
        public bool Equals(Data other)
        {
            return other != null &&
                   EqualityComparer<Core.Data.Dataset>.Default.Equals(Dataset, other.Dataset) &&
                   Name == other.Name;
        }
        public override int GetHashCode()
        {
            var hashCode = 139406400;
            hashCode = hashCode * -1521134295 + EqualityComparer<Core.Data.Dataset>.Default.GetHashCode(Dataset);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            return hashCode;
        }
        public static bool operator ==(Data struct1, Data struct2)
        {
            return EqualityComparer<Data>.Default.Equals(struct1, struct2);
        }
        public static bool operator !=(Data struct1, Data struct2)
        {
            return !(struct1 == struct2);
        }
        #endregion
    }

    [Serializable]
    public class CCEPData : Data
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
        public CCEPData(Core.Data.Dataset dataset, string data, ChannelStruct source, Core.Data.Bloc bloc) : base(dataset, data, bloc)
        {
            Source = source;
        }
        #endregion

        #region Public Methods
        public override bool Equals(object obj)
        {
            return Equals(obj as CCEPData);
        }
        public bool Equals(CCEPData other)
        {
            return base.Equals(other) && Source.Equals(other.Source);
        }
        public override int GetHashCode()
        {
            var hashCode = 139406400 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + Source.GetHashCode();
            return hashCode;
        }
        public static bool operator ==(CCEPData struct1, CCEPData struct2)
        {
            return EqualityComparer<CCEPData>.Default.Equals(struct1, struct2);
        }
        public static bool operator !=(CCEPData struct1, CCEPData struct2)
        {
            return !(struct1 == struct2);
        }
        #endregion
    }

    [Serializable]
    public class IEEGData : Data
    {
        #region Constructors
        public IEEGData(Core.Data.Dataset dataset, string data, Core.Data.Bloc bloc) : base(dataset, data, bloc)
        {
        }
        #endregion

        #region Public Methods
        public bool Equals(IEEGData other)
        {
            return base.Equals(other);
        }
        #endregion
    }

    [Serializable]
    public class MEGData : Data
    {
        #region Properties
        [SerializeField] Core.Tools.TimeWindow m_Window;
        public Core.Tools.TimeWindow Window
        {
            get
            {
                return m_Window;
            }
            set
            {
                SetPropertyUtility.SetStruct(ref m_Window, value);
            }
        }
        #endregion

        #region Constructors
        public MEGData(Core.Data.Dataset dataset, string data, Core.Tools.TimeWindow window) : base(dataset, data, null)
        {
            Window = window;
        }
        #endregion

        #region Public Methods
        public bool Equals(MEGData other)
        {
            return base.Equals(other);
        }
        #endregion
    }

    [Serializable]
    public class ChannelStructsGroup : IEquatable<ChannelStructsGroup>
    {
        #region Properties
        [SerializeField] string m_Name;
        public string Name
        {
            get
            {
                return m_Name;
            }
            set
            {
                m_Name = value;
            }
        }

        [SerializeField] List<ChannelStruct> m_Channels;
        public List<ChannelStruct> Channels
        {
            get
            {
                return m_Channels;
            }
            set
            {
                m_Channels = value;
            }
        }
        #endregion

        #region Constructors
        public ChannelStructsGroup(string name, IEnumerable<ChannelStruct> channels)
        {
            Name = name;
            Channels = new List<ChannelStruct>(channels);
        }
        #endregion

        #region Public Methods
        public override bool Equals(object obj)
        {
            return Equals(obj as ChannelStructsGroup);
        }
        public bool Equals(ChannelStructsGroup other)
        {
            bool notNull = other != null;
            if (notNull)
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
        public static bool operator ==(ChannelStructsGroup struct1, ChannelStructsGroup struct2)
        {
            return EqualityComparer<ChannelStructsGroup>.Default.Equals(struct1, struct2);
        }
        public static bool operator !=(ChannelStructsGroup struct1, ChannelStructsGroup struct2)
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
        public Dictionary<Data, List<ChannelStruct>> ChannelsByData { get; set; }
        #endregion

        #region Constructors
        public SceneROIStruct(string name, Dictionary<Data, List<ChannelStruct>> channelsByData)
        {
            Name = name;
            ChannelsByData = new Dictionary<Data, List<ChannelStruct>>(channelsByData);
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
                bool collection = EqualityComparer<Dictionary<Data, List<ChannelStruct>>>.Default.Equals(ChannelsByData, other.ChannelsByData);
            }
            return other != null &&
                   Name == other.Name &&
                   EqualityComparer<Dictionary<Data, List<ChannelStruct>>>.Default.Equals(ChannelsByData, other.ChannelsByData);
        }
        public override int GetHashCode()
        {
            var hashCode = 252110562;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<Dictionary<Data, List<ChannelStruct>>>.Default.GetHashCode(ChannelsByData);
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
    public class Column : IEquatable<Column>
    {
        public string Name;
        public Data Data;
        public List<ChannelStructsGroup> ChannelGroups;

        public Column(string name, Data data, List<ChannelStructsGroup> channelGroups)
        {
            Name = name;
            Data = data;
            ChannelGroups = new List<ChannelStructsGroup>(channelGroups);
        }

        #region Public Methods
        public override bool Equals(object obj)
        {
            return Equals(obj as Column);
        }
        public bool Equals(Column other)
        {
            return other != null &&
                   Name == other.Name &&
                   Data == other.Data;
        }
        public override int GetHashCode()
        {
            var hashCode = 139406400;
            hashCode = hashCode * -1521134295 + EqualityComparer<Data>.Default.GetHashCode(Data);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            return hashCode;
        }
        public static bool operator ==(Column struct1, Column struct2)
        {
            return EqualityComparer<Column>.Default.Equals(struct1, struct2);
        }
        public static bool operator !=(Column struct1, Column struct2)
        {
            return !(struct1 == struct2);
        }
        #endregion
    }

    [Serializable]
    public class SceneData
    {
        public List<Column> Columns;

        public SceneData(List<Column> columns)
        {
            Columns = columns;
        }
    }

    [Serializable] public class ChannelsEvent : UnityEngine.Events.UnityEvent<ChannelStruct[]> { }
}