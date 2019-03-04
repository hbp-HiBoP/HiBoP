using HBP.Data.Experience.Dataset;
using System.Collections.Generic;
using System;

namespace HBP.Data.Informations
{
    [Serializable]
    public class ChannelStruct
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
        #endregion
    }

    [Serializable]
    public class DataStruct
    {
        #region Properties
        public Dataset Dataset { get; set; }
        public string Data { get; set; }
        public List<Experience.Protocol.Bloc> Blocs { get; set; }
        #endregion

        #region Constructors
        public DataStruct(Dataset dataset, string data, List<Experience.Protocol.Bloc> blocs = null)
        {
            Dataset = dataset;
            Data = data;
            Blocs = blocs;
        }
        #endregion
    }
}