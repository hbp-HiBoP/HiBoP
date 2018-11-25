using HBP.Data.Experience.Dataset;

namespace HBP.Data.TrialMatrix.Grid
{
    public struct ChannelStruct
    {
        public string Channel;
        public Patient Patient;

        public ChannelStruct(string channel, Patient patient)
        {
            Channel = channel;
            Patient = patient;
        }
    }

    public struct DataStruct
    {
        public Dataset Dataset;
        public string Data;

        public DataStruct(Dataset dataset, string data)
        {
            Dataset = dataset;
            Data = data;
        }
    }
}