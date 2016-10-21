using UnityEngine;
using System;
using System.Linq;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;

namespace HBP.Data.Visualisation
{
    /// <summary>
    /// Visualisation column.
    /// </summary>
    [Serializable]
    public class Column
    {
        #region Properties
        [SerializeField]
        private string datasetID;
        public string DatasetID
        {
            get { return datasetID; }
            set { datasetID = value; }
        }
        public Dataset Dataset
        {
            get { return ApplicationState.ProjectLoaded.Datasets.FirstOrDefault(p => p.ID == datasetID); }
            set { datasetID = value.ID; }
        }

        [SerializeField]
        private string dataLabel;
        public string DataLabel
        {
            get { return dataLabel; }
            set { dataLabel = value; }
        }

        [SerializeField]
        private string protocolID;
        public string ProtocolID
        {
            get { return protocolID; }
            set { protocolID = value; }
        }
        public Protocol Protocol
        {
            get { return ApplicationState.ProjectLoaded.Protocols.FirstOrDefault(p => p.ID == protocolID); }
            set { protocolID = value.ID; }
        }

        [SerializeField]
        private string blocID;
        public string BlocID
        {
            get { return blocID; }
            set { blocID = value; }
        }
        public Bloc Bloc
        {
            get { return Protocol.Blocs.ToList().Find(p => p.ID == blocID); }
            set { blocID = value.ID; }
        }
        #endregion

        #region Constructors
        public Column(Dataset dataset, string dataLabel, Protocol protocol, Bloc bloc)
        {
            Dataset = dataset;
            Protocol = protocol;
            Bloc = bloc;
            DataLabel = dataLabel;
        }
        public Column():this(new Dataset(), string.Empty,new Protocol(),new Bloc())
        {
        }
        #endregion

        #region Public Methods
        public bool IsCompatible(Patient.Patient patient)
        {
            bool result = false;
            foreach (DataInfo dataInfo in Dataset.Data)
            {
                if (dataInfo.Name == DataLabel && dataInfo.Protocol == Protocol && dataInfo.Patient == patient && dataInfo.UsableInMultiPatients)
                {
                    result = true;
                    break;
                }
            }
            return result;
        }
        public bool IsCompatible(Patient.Patient[] patients)
        {
            bool result = true;
            foreach(Patient.Patient patient in patients)
            {
                if(!IsCompatible(patient))
                {
                    result = false;
                    break;
                }
            }
            return result;
        }
        #endregion

        #region Operators
        public object Clone()
        {
            return new Column(Dataset.Clone() as Dataset, DataLabel.Clone() as string, Protocol.Clone() as Protocol, Bloc.Clone() as Bloc);
        }
        #endregion
    }
}
