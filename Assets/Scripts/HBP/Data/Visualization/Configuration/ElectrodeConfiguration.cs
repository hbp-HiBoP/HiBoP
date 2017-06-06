using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HBP.Data.Anatomy;

namespace HBP.Data.Visualization
{
    /**
    * \class ElectrodeConfiguration
    * \author Adrien Gannerie
    * \version 1.0
    * \date 28 avril 2017
    * \brief Configuration of a electrode.
    * 
    * \details ElectrodeConfiguration is a class which define the configuration of a electrode and contains:
    *   - \a Unique ID.
    *   - \a Configuration of the sites.
    *   - \a Color of the electrode.
    */
    [DataContract]
    public class ElectrodeConfiguration : ICloneable
    {
        #region Properties
        [DataMember(Name = "Patient")]
        string m_PatientID;
        Patient m_Patient;

        [DataMember(Name = "Color")]
        SerializableColor m_Color;
        /// <summary>
        /// Color of the electrode.
        /// </summary>
        [IgnoreDataMember]
        public Color Color { get; set; }

        [DataMember(Name = "ConfigurationBySite")]
        Dictionary <string, SiteConfiguration> m_ConfigurationBySiteName;
        /// <summary>
        /// Configurations of the electrode sites.
        /// </summary>
        public Dictionary<Site,SiteConfiguration> ConfigurationBySite { get; set; }
        #endregion

        #region Constructors
        public ElectrodeConfiguration(Dictionary<Site,SiteConfiguration> configurationBySite, Color color, Patient patient)
        {
            m_Patient = patient;
            Color = color;
            ConfigurationBySite = configurationBySite;
        }
        public ElectrodeConfiguration(Color color, Patient patient) : this(new Dictionary<Site, SiteConfiguration>(), color, patient) { }
        public ElectrodeConfiguration(Patient patient) : this(new Dictionary<Site, SiteConfiguration>(), new Color(), patient) { }
        public ElectrodeConfiguration() : this(new Patient()) { }
        #endregion

        #region Public Methods
        public object Clone()
        {
            Dictionary<Site, SiteConfiguration> configurationBySiteClone = new Dictionary<Site, SiteConfiguration>();
            foreach (var item in ConfigurationBySite)
            {
                configurationBySiteClone.Add(item.Key, item.Value.Clone() as SiteConfiguration);
            }
            return new ElectrodeConfiguration(configurationBySiteClone, Color, m_Patient);
        }
        #endregion

        #region Serialization
        [OnSerializing]
        void OnSerializing(StreamingContext streamingContext)
        {
            m_Color = new SerializableColor(Color);
            m_PatientID = m_Patient.ID;
            m_ConfigurationBySiteName = new Dictionary<string, SiteConfiguration>();
            foreach (var pair in ConfigurationBySite) m_ConfigurationBySiteName.Add(pair.Key.Name, pair.Value);
        }
        [OnDeserialized]
        void OnDeserialized(StreamingContext streamingContext)
        {
            Color = m_Color.ToColor();
            m_Patient = ApplicationState.ProjectLoaded.Patients.First((p) => p.ID == m_PatientID);
            Electrode electrode = m_Patient.Brain.Implantation.Electrodes.First((elec) => elec.Name == Electrode.FindElectrodeName(m_ConfigurationBySiteName.FirstOrDefault().Key));
            ConfigurationBySite = new Dictionary<Site, SiteConfiguration>();
            foreach (var pair in m_ConfigurationBySiteName)
            {
                Site site = electrode.Sites.First((s) => s.Name == pair.Key);
                ConfigurationBySite.Add(site, pair.Value);
            }
        }
        #endregion
    }
}