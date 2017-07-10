using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HBP.Data.Visualization
{
    /**
    * \class ColumnConfiguration
    * \author Adrien Gannerie
    * \version 1.0
    * \date 28 avril 2017
    * \brief Configuration of a column.
    * 
    * \details ColumnConfiguration§ is a class which define the configuration of a patient and contains:
    *   - \a Unique ID.
    *   - \a Color of the patient.
    *   - \a Configurations of the patient electrodes.
    */
    [DataContract]
    public class ColumnConfiguration : ICloneable
    {
        #region Properties
        [DataMember(Name = "ConfigurationByPatient")]
        Dictionary<string, PatientConfiguration> m_ConfigurationByPatientID;
        [IgnoreDataMember]
        /// <summary>
        /// Configuration by patient.
        /// </summary>
        public Dictionary<Patient, PatientConfiguration> ConfigurationByPatient { get; set; }

        /// <summary>
        /// Region of interest.
        /// </summary>
        //[DataMember]
        public List<RegionOfInterest> RegionOfInterest { get; set; }

        [DataMember(Name = "Site Gain")]
        private float m_Gain = 1.0f;
        /// <summary>
        /// IEEG Sites Gain
        /// </summary>
        public float Gain
        {
            get
            {
                return m_Gain;
            }
            set
            {
                m_Gain = value;
            }
        }

        [DataMember(Name = "Site Maximum Inflence")]
        private float m_MaximumInfluence = 15.0f;
        /// <summary>
        /// Maximum site influence
        /// </summary>
        public float MaximumInfluence
        {
            get
            {
                return m_MaximumInfluence;
            }
            set
            {
                m_MaximumInfluence = value;
            }
        }

        [DataMember(Name = "Transparency")]
        private float m_Alpha = 0.2f;
        /// <summary>
        /// IEEG Transparency
        /// </summary>
        public float Alpha
        {
            get
            {
                return m_Alpha;
            }
            set
            {
                m_Alpha = value;
            }
        }

        [DataMember(Name = "Span Min")]
        private float m_SpanMin = 0.0f;
        /// <summary>
        /// IEEG Span Min
        /// </summary>
        public float SpanMin
        {
            get
            {
                return m_SpanMin;
            }
            set
            {
                m_SpanMin = value;
            }
        }

        [DataMember(Name = "Middle")]
        private float m_Middle = 0.0f;
        /// <summary>
        /// IEEG Span Min
        /// </summary>
        public float Middle
        {
            get
            {
                return m_Middle;
            }
            set
            {
                m_Middle = value;
            }
        }

        [DataMember(Name = "Span Max")]
        private float m_SpanMax = 0.0f;
        /// <summary>
        /// IEEG Span Min
        /// </summary>
        public float SpanMax
        {
            get
            {
                return m_SpanMax;
            }
            set
            {
                m_SpanMax = value;
            }
        }

        [DataMember(Name = "Regions of Interest")]
        private List<Module3D.ROI> m_ROIs = new List<Module3D.ROI>();
        /// <summary>
        /// Regions of interest of this column
        /// </summary>
        public List<Module3D.ROI> ROIs
        {
            get
            {
                return m_ROIs;
            }
            set
            {
                m_ROIs = value;
            }
        }
        #endregion

        #region Constructor
        public ColumnConfiguration(Dictionary<Patient,PatientConfiguration> configurationByPatient, IEnumerable<RegionOfInterest> regionOfInterest)
        {
            ConfigurationByPatient = configurationByPatient;
            RegionOfInterest = regionOfInterest.ToList();
        }
        public ColumnConfiguration() : this (new Dictionary<Patient, PatientConfiguration>(), new RegionOfInterest[0])
        { 
        }
        #endregion

        #region Public Methods
        public object Clone()
        {
            Dictionary<Patient, PatientConfiguration> configurationByPatientClone = new Dictionary<Patient, PatientConfiguration>();
            foreach (var item in ConfigurationByPatient) configurationByPatientClone.Add(item.Key, item.Value.Clone() as PatientConfiguration);
            return new ColumnConfiguration(configurationByPatientClone, from ROI in RegionOfInterest select ROI.Clone() as RegionOfInterest);
        }
        #endregion

        #region Serialization
        [OnSerializing]
        void OnSerializing(StreamingContext streamingContext)
        {
            m_ConfigurationByPatientID = new Dictionary<string, PatientConfiguration>();
            foreach (var item in ConfigurationByPatient)
            {
                m_ConfigurationByPatientID.Add(item.Key.ID, item.Value);
            }
        }
        [OnSerialized]
        void OnSerialized(StreamingContext streamingContext)
        {
            m_ConfigurationByPatientID = null;
        }
        [OnDeserialized]
        void OnDeserialized(StreamingContext streamingContext)
        {
            foreach (var item in m_ConfigurationByPatientID)
            {
                ConfigurationByPatient.Add(ApplicationState.ProjectLoaded.Patients.First((elmt) => elmt.ID == item.Key), item.Value);
            }
        }
        #endregion
    }
}