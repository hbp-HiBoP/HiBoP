using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Collections.Generic;
using HBP.Data.Anatomy;
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

        /// <summary>
        /// Configuration of the column.
        /// </summary>
        [DataMember(Name = "Configuration")]
        public ColumnConfiguration Configuration { get; set; }

        /// <summary>
        /// TimeLine which define the size,limits,events.
        /// </summary>
        public TimeLine TimeLine { get; set; }

        /// <summary>
        /// Iconic scenario which define the labels,images to display during the timeLine. 
        /// </summary>
        public IconicScenario IconicScenario { get; set; }

        /// <summary>
        /// Values by site.
        /// </summary>
        public Dictionary<Site,float[]> ValuesBySite { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new Column instance.
        /// </summary>
        /// <param name="dataset">Dataset to use in the visualisation Column.</param>
        /// <param name="dataLabel">Label of the data to use in the visualisation Column.</param>
        /// <param name="protocol">Protocol to use in the visualisation Column.</param>
        /// <param name="bloc">Bloc of the Protocol to use in the visualisation Column.</param>
        public Column(Dataset dataset, string dataLabel, Protocol protocol, Bloc bloc,ColumnConfiguration configuration)
        {
            Dataset = dataset;
            Protocol = protocol;
            Bloc = bloc;
            DataLabel = dataLabel;
            Configuration = configuration;
        }
        /// <summary>
        /// Create a new Column instance with default values.
        /// </summary>
        public Column():this(new Dataset(), string.Empty,new Protocol(),new Bloc(),new ColumnConfiguration())
        {
        }
        #endregion

        #region Public Methods
        public void Load(Patient patient)
        {
            patient.Brain.LoadImplantation(Implantation.ReferenceFrameType.Patient, true); // Read patient implantation.
            DataInfo dataInfo = Dataset.Data.Find((d) => d.Name == DataLabel && d.Patient == patient && d.Protocol == Protocol && d.Protocol.Blocs.Contains(Bloc)); // Find dataInfo.
            Elan.ElanFile elanFile = new Elan.ElanFile(dataInfo.EEG, true); // Instantiate new EEG file.
            bool elanFileReaded = elanFile.ReadChannel(); // Read the elanfile.
            if(elanFileReaded)
            {
                PatientConfiguration patientConfiguration = Configuration.Patients.Find((elmt) => elmt.ID == patient.ID); // Find the patient configuration.
                if (patientConfiguration == null) patientConfiguration = new PatientConfiguration(patient.ID, new ElectrodeConfiguration[0], new UnityEngine.Color()); // If patient configuration not find create a new patient configuration.

                foreach (Electrode electrode in patient.Brain.Implantation.Electrodes)
                {
                    ElectrodeConfiguration electrodeConfiguration = patientConfiguration.Electrodes.Find((elmt) => elmt.ID == electrode.Name); // Find electrode configuration.
                    foreach (Site site in electrode.Sites)
                    {
                        SiteConfiguration siteConfiguration = electrodeConfiguration.Sites.Find((elmt) => elmt.ID == site.Name); // Find site electrode configuration.
                        Elan.Track track = elanFile.FindTrack(dataInfo.Measure, site.Name);
                        float[] values = new float[elanFile.EEG.SampleNumber];
                        if(track.Measure < 0 || track.Channel < 0)
                        {
                            siteConfiguration.IsMasked = true;
                        }
                        else
                        {
                            siteConfiguration.IsMasked = false;
                            values = elanFile.EEG.GetFloatData(elanFile.FindTrack(dataInfo.Measure, site.Name));
                        }
                        ValuesBySite.Add(site, values);
                    }
                }
            }
            else
            {
                UnityEngine.Debug.LogError("Cannot read the elan file.");
            }
        }
        public void Load(IEnumerable<Patient> patients)
        {
            foreach (Patient patient in patients)
            {
                DataInfo dataInfo = Dataset.Data.Find((d) => d.Name == DataLabel && d.Patient == patient && d.Protocol == Protocol && d.Protocol.Blocs.Contains(Bloc));
                foreach (Electrode electrode in patient.Brain.MNIImplantation.Electrodes)
                {
                    foreach (Site site in electrode.Sites)
                    {
                        ValuesBySite.Add(site, new float[0]);
                    }
                }
            }
        }
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
            return new Column(Dataset.Clone() as Dataset, DataLabel.Clone() as string, Protocol.Clone() as Protocol, Bloc.Clone() as Bloc, Configuration.Clone() as ColumnConfiguration);
        }
        #endregion
    }
}