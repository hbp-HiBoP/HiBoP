namespace HBP.Data.Experience.Dataset
{
    /**
    * \class DataInfoWithDatasetLabel
    * \author Adrien Gannerie
    * \version 1.0
    * \date 09 janvier 2017
    * \brief DataInfo with dataset label.
    * 
    * \details Class which combine the dataInfo and the dataset label. 
    */
    public struct DataInfoWithDatasetLabel
    {
        #region Properties
        DataInfo dataInfo;
        /// <summary>
        /// DataInfo.
        /// </summary>
        public DataInfo DataInfo
        {
            get { return dataInfo; }
            set { dataInfo = value; }
        }

        string datasetLabel;
        /// <summary>
        /// Label of the Dataset.
        /// </summary>
        public string DatasetLabel
        {
            get { return datasetLabel; }
            set { datasetLabel = value; }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new dataInfo with dataset label.
        /// </summary>
        /// <param name="datasetLabel"></param>
        /// <param name="dataInfo"></param>
        public DataInfoWithDatasetLabel(string datasetLabel, DataInfo dataInfo)
        {
            this.dataInfo = dataInfo;
            this.datasetLabel = datasetLabel;
        }
        #endregion
    }
}
