using HBP.Data.Experience.Dataset;

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
        public Experience.Protocol.Bloc[] Blocs { get; set; }

        public DataStruct(Dataset dataset, string data, Experience.Protocol.Bloc[] blocs = null)
        {
            Dataset = dataset;
            Data = data;
            Blocs = blocs;
        }
    }
}