using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HBP.Data.Visualization
{
    /**
    * \class PatientConfiguration
    * \author Adrien Gannerie
    * \version 1.0
    * \date 28 avril 2017
    * \brief Configuration of a patient.
    * 
    * \details PatientConfiguration is a class which define the configuration of a patient and contains:
    *   - \a Color of the patient.
    *   - \a Patient electrode configurations.
    */
    [DataContract]
    public class PatientConfiguration : ICloneable
    {
        #region Properties
        [DataMember(Name = "Patient")]
        string m_PatientID;
        Patient m_Patient;

        [DataMember(Name = "ConfigurationByElectrode")]
        Dictionary <string, ElectrodeConfiguration> m_ConfigurationByElectrodeName;
        /// <summary>
        /// Configuration of the patient electrodes.
        /// </summary>
        [IgnoreDataMember]
        public Dictionary<string,ElectrodeConfiguration> ConfigurationByElectrode { get; set; }

        [DataMember(Name = "Color")]
        SerializableColor m_Color;
        /// <summary>
        /// Color of the patient.
        /// </summary>
        public Color Color { get; set; }
        #endregion

        #region Constructors
        public PatientConfiguration(Dictionary<string,ElectrodeConfiguration> configurationByElectrode, Color color, Patient patient)
        {
            m_Patient = patient;
            ConfigurationByElectrode = configurationByElectrode;
            Color = color;
        }
        public PatientConfiguration() : this(new Dictionary<string, ElectrodeConfiguration>(),new Color(),new Patient()) { }
        #endregion

        #region Public Methods
        public object Clone()
        {
            Dictionary<string, ElectrodeConfiguration> configurationByElectrodeClone = new Dictionary<string, ElectrodeConfiguration>();
            foreach (var item in ConfigurationByElectrode)
            {
                configurationByElectrodeClone.Add(item.Key, item.Value.Clone() as ElectrodeConfiguration);
            }
            return new PatientConfiguration(configurationByElectrodeClone, Color, m_Patient);
        }
        #endregion

        #region Serialization
        [OnSerializing]
        void OnSerializing(StreamingContext streamingContext)
        {
            m_Color = new SerializableColor(Color);
            m_PatientID = m_Patient.ID;
        }
        [OnDeserialized]
        void OnDeserialized(StreamingContext streamingContext)
        {
            Color = m_Color.ToColor();
            m_Patient = ApplicationState.ProjectLoaded.Patients.First((p) => p.ID == m_PatientID);
        }
        #endregion
    }
}