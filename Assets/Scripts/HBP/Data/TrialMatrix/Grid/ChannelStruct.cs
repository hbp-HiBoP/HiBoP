using HBP.Data.Experience.Dataset;
using System.Collections.Generic;

namespace HBP.Data.Informations
{
    public struct ChannelStruct
    {
        public string Channel { get; set; }
        public Patient Patient { get; set; }

        public ChannelStruct(string channel, Patient patient)
        {
            Channel = channel;
            Patient = patient;
        }
    }

    public struct DataStruct
    {
        public Dataset Dataset { get; set; }
        public string Data { get; set; }
        public List<Experience.Protocol.Bloc> Blocs { get; set; }

        public DataStruct(Dataset dataset, string data, List<Experience.Protocol.Bloc> blocs = null)
        {
            Dataset = dataset;
            Data = data;
            Blocs = blocs;
        }

        public override bool Equals(object obj)
        {
            if(!(obj is DataStruct))
                return false;

            DataStruct dataStruct = (DataStruct)obj;
            return dataStruct.Dataset == Dataset && dataStruct.Data == Data;
        }
    }
}