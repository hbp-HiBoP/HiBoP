using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;

namespace HBP.Data.Visualisation
{
    /**
    * \class Column
    * \author Adrien Gannerie
    * \version 1.0
    * \date 10 janvier 2017
    * \brief Visualisation column.
    * 
    * \detail Visualisation column is a class which contains all the information for the display wanted for a column and contains:
    *   - \a Dataset.
    *   - \a DataLabel.
    *   - \a Protocol.
    *   - \a Bloc.
    */
    [DataContract]
    public class Column: ICloneable
    {
        #region Properties
        [DataMember(Name = "Dataset")]
        private string datasetID;
        /// <summary>
        /// Dataset to use in the visualisation Column.
        /// </summary>
        public Dataset Dataset
        {
            get { return ApplicationState.ProjectLoaded.Datasets.FirstOrDefault(p => p.ID == datasetID); }
            set { datasetID = value.ID; }
        }

        [DataMember(Name = "Label")]
        /// <summary>
        /// Label of the data to use in the visualisation Column.
        /// </summary>
        public string DataLabel { get; set; }

        [DataMember(Name = "Protocol")]
        private string protocolID;
        /// <summary>
        /// Protocol to use in the visualisation Column.
        /// </summary>
        public Protocol Protocol
        {
            get { return ApplicationState.ProjectLoaded.Protocols.FirstOrDefault(p => p.ID == protocolID); }
            set { protocolID = value.ID; }
        }

        [DataMember(Name = "Bloc")]
        private string blocID;
        /// <summary>
        /// Bloc of the Protocol to use in the visualisation Column.
        /// </summary>
        public Bloc Bloc
        {
            get { return Protocol.Blocs.ToList().Find(p => p.ID == blocID); }
            set { blocID = value.ID; }
        }

        [DataMember(Name = "RegionOfInterest")]
        private List<RegionOfInterest> regionOfInterest;
        /// <summary>
        /// Region of interest used in the visualisation Column.
        /// </summary>
        public ReadOnlyCollection<RegionOfInterest> RegionOfInterest { get { return new ReadOnlyCollection<Data.Visualisation.RegionOfInterest>(regionOfInterest); } private set { regionOfInterest = value.ToList(); } }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new Column instance.
        /// </summary>
        /// <param name="dataset">Dataset to use in the visualisation Column.</param>
        /// <param name="dataLabel">Label of the data to use in the visualisation Column.</param>
        /// <param name="protocol">Protocol to use in the visualisation Column.</param>
        /// <param name="bloc">Bloc of the Protocol to use in the visualisation Column.</param>
        public Column(Dataset dataset, string dataLabel, Protocol protocol, Bloc bloc, IEnumerable<RegionOfInterest> regionOfInterest)
        {
            Dataset = dataset;
            Protocol = protocol;
            Bloc = bloc;
            DataLabel = dataLabel;
            RegionOfInterest = new ReadOnlyCollection<RegionOfInterest>(regionOfInterest.ToArray());
        }
        /// <summary>
        /// Create a new Column instance with default values.
        /// </summary>
        public Column():this(new Dataset(), string.Empty,new Protocol(),new Bloc(), new Collection<Data.Visualisation.RegionOfInterest>())
        {
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Test if the visualisation Column is compatible with a Patient.
        /// </summary>
        /// <param name="patient">Patient to test.</param>
        /// <returns>\a True if is compatible and \a false otherwise.</returns>
        public bool IsCompatible(Patient patient)
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
        /// <summary>
        /// Test if the visualisaation Column is compatible with some Patients.
        /// </summary>
        /// <param name="patients">Patients to test.</param>
        /// <returns>\a True if is compatible and \a false otherwise.</returns>
        public bool IsCompatible(Patient[] patients)
        {
            bool result = true;
            foreach(Patient patient in patients)
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
        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Clone of this instance.</returns>
        public object Clone()
        {
            IEnumerable<RegionOfInterest> regionOfInterestCloned = from ROI in regionOfInterest select ROI.Clone() as RegionOfInterest;
            return new Column(Dataset.Clone() as Dataset, DataLabel.Clone() as string, Protocol.Clone() as Protocol, Bloc.Clone() as Bloc, regionOfInterestCloned);
        }
        #endregion
    }
}