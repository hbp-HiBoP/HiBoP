namespace HBP.Data.Experience.Dataset
{
    public struct DataInfoWithDatasetLabel
    {
        public DataInfo DataInfo;
        public string DatasetLabel;

        public DataInfoWithDatasetLabel(string datasetLabel, DataInfo dataInfo)
        {
            DataInfo = dataInfo;
            DatasetLabel = datasetLabel;
        }
    }
}
