using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI.Extensions;
using HBP.Core.Data;
using HBP.Core.Tools;
using System.Linq;

namespace HBP.Data.Informations
{
    [Serializable]
    public class ChannelStruct : BaseData
    {
        #region Properties
        public string Channel { get; set; }
        public Patient Patient { get; set; }
        #endregion

        #region Constructors
        public ChannelStruct(string channel, Patient patient)
        {
            Channel = channel;
            Patient = patient;
        }
        public ChannelStruct(Core.Object3D.Site site)
        {
            Channel = site.Information.Name;
            Patient = site.Information.Patient;
        }
        #endregion

        #region Public Methods
        public override object Clone()
        {
            return new ChannelStruct(Channel, Patient);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is ChannelStruct channelStruct)
            {
                Channel = channelStruct.Channel;
                Patient = channelStruct.Patient;
            }
        }
        #endregion
    }

    [Serializable]
    public class Data : BaseData
    {
        #region Properties
        public Dataset Dataset { get; set; }
        public string Name { get; set; }
        public Bloc Bloc { get; set; }
        #endregion

        #region Constructors
        public Data(Dataset dataset, string data, Bloc bloc)
        {
            Dataset = dataset;
            Name = data;
            Bloc = bloc;
        }
        #endregion

        #region Public Methods
        public override object Clone()
        {
            return new Data(Dataset, Name, Bloc);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is Data data)
            {
                Dataset = data.Dataset;
                Name = data.Name;
                Bloc = data.Bloc;
            }
        }
        #endregion
    }

    [Serializable]
    public class CCEPData : Data
    {
        #region Properties
        public ChannelStruct Source { get; set; }
        #endregion

        #region Constructors
        public CCEPData(Dataset dataset, string data, ChannelStruct source, Bloc bloc) : base(dataset, data, bloc)
        {
            Source = source;
        }
        #endregion

        #region Public Methods
        public override object Clone()
        {
            return new CCEPData(Dataset, Name, Source, Bloc);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is CCEPData ccepData)
            {
                Source = ccepData.Source;
            }
        }
        #endregion
    }

    [Serializable]
    public class IEEGData : Data
    {
        #region Constructors
        public IEEGData(Dataset dataset, string data, Bloc bloc) : base(dataset, data, bloc)
        {
        }
        #endregion

        #region Public Methods
        public override object Clone()
        {
            return new IEEGData(Dataset, Name, Bloc);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is IEEGData ieegData)
            {
                // No specific properties to copy for IEEGData
            }
        }
        #endregion
    }

    [Serializable]
    public class MEGData : Data
    {
        #region Properties
        public TimeWindow Window { get; set; }
        #endregion

        #region Constructors
        public MEGData(Dataset dataset, string data, TimeWindow window) : base(dataset, data, null)
        {
            Window = window;
        }
        #endregion

        #region Public Methods
        public override object Clone()
        {
            return new MEGData(Dataset, Name, Window);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is MEGData megData)
            {
                Window = megData.Window;
            }
        }
        #endregion
    }

    [Serializable]
    public class ChannelStructsGroup : BaseData
    {
        #region Properties
        public string Name { get; set; }
        public List<ChannelStruct> Channels { get; set; }
        #endregion

        #region Constructors
        public ChannelStructsGroup(string name, IEnumerable<ChannelStruct> channels)
        {
            Name = name;
            Channels = channels.ToList();
        }
        #endregion

        #region Public Methods
        public override object Clone()
        {
            return new ChannelStructsGroup(Name, Channels.DeepClone());
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is ChannelStructsGroup group)
            {
                Name = group.Name;
                Channels = group.Channels.ToList();
            }
        }
        #endregion
    }

    [Serializable]
    public class Column : BaseData
    {
        public string Name { get; set; }
        public Data Data { get; set; }
        public List<ChannelStructsGroup> ChannelGroups { get; set; }

        #region Constructors
        public Column(string name, Data data, IEnumerable<ChannelStructsGroup> channelGroups)
        {
            Name = name;
            Data = data;
            ChannelGroups = channelGroups.ToList();
        }
        #endregion

        #region Public Methods
        public override object Clone()
        {
            return new Column(Name, Data, ChannelGroups.DeepClone());
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is Column column)
            {
                Name = column.Name;
                Data = column.Data;
                ChannelGroups = column.ChannelGroups.ToList();
            }
        }
        #endregion
    }

    [Serializable]
    public class SceneData : BaseData
    {
        #region Properties
        public List<Column> Columns { get; set; }
        #endregion

        #region Constructors
        public SceneData(IEnumerable<Column> columns)
        {
            Columns = columns.ToList();
        }
        #endregion

        #region Public Methods
        public override object Clone()
        {
            return new SceneData(Columns.DeepClone());
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is SceneData sceneData)
            {
                Columns = sceneData.Columns.ToList();
            }
        }
        #endregion
    }

    [Serializable] public class ChannelsEvent : UnityEngine.Events.UnityEvent<ChannelStruct[]> { }
}