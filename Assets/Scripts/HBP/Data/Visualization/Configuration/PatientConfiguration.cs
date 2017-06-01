using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HBP.Data.Anatomy;

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
        public Dictionary<Electrode,ElectrodeConfiguration> ConfigurationByElectrode { get; set; }

        [DataMember(Name = "Color")]
        SerializableColor m_Color;
        /// <summary>
        /// Color of the patient.
        /// </summary>
        public Color Color { get; set; }
        #endregion

        #region Constructors
        public PatientConfiguration(Dictionary<Electrode,ElectrodeConfiguration> configurationByElectrode, Color color, Patient patient)
        {
            m_Patient = patient;
            ConfigurationByElectrode = configurationByElectrode;
            Color = color;
        }
        public PatientConfiguration(Color color, Patient patient) : this(new Dictionary<Electrode, ElectrodeConfiguration>(), color, patient) { }
        public PatientConfiguration(Patient patient) : this(new Dictionary<Electrode, ElectrodeConfiguration>(), new Color(), patient) { }
        public PatientConfiguration() : this(new Patient()) { }
        #endregion

        #region Public Methods
        public object Clone()
        {
            Dictionary<Electrode, ElectrodeConfiguration> configurationByElectrodeClone = new Dictionary<Electrode, ElectrodeConfiguration>();
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
            m_ConfigurationByElectrodeName = new Dictionary<string, ElectrodeConfiguration>();
            foreach (var pair in ConfigurationByElectrode) m_ConfigurationByElectrodeName.Add(pair.Key.Name, pair.Value);
        }
        [OnDeserialized]
        void OnDeserialized(StreamingContext streamingContext)
        {
            Color = m_Color.ToColor();
            m_Patient = ApplicationState.ProjectLoaded.Patients.First((p) => p.ID == m_PatientID);
            ConfigurationByElectrode = new Dictionary<Electrode, ElectrodeConfiguration>();
            foreach (var pair in m_ConfigurationByElectrodeName)
            {
                Electrode electrode = m_Patient.Brain.Implantation.Electrodes.First((elec) => elec.Name == pair.Key);
                ConfigurationByElectrode.Add(electrode, pair.Value);
            }
        }
        #endregion
    }
}