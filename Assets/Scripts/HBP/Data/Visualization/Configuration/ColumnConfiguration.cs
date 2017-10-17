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
        [DataMember(Name = "ConfigurationBySite")]
        public Dictionary<string, SiteConfiguration> ConfigurationBySite;
        
        /// <summary>
        /// Region of interest.
        /// </summary>
        [DataMember(Name = "Regions Of Interest")]
        public List<RegionOfInterest> RegionsOfInterest { get; set; }

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
        #endregion

        #region Constructor
        public ColumnConfiguration(Dictionary<string,SiteConfiguration> configurationBySite, IEnumerable<RegionOfInterest> regionOfInterest)
        {
            ConfigurationBySite = configurationBySite;
            RegionsOfInterest = regionOfInterest.ToList();
        }
        public ColumnConfiguration() : this (new Dictionary<string, SiteConfiguration>(), new RegionOfInterest[0])
        { 
        }
        #endregion

        #region Public Methods
        public object Clone()
        {
            ColumnConfiguration configuration = new ColumnConfiguration();
            Dictionary<string, SiteConfiguration> configurationBySiteClone = new Dictionary<string, SiteConfiguration>();
            foreach (var item in ConfigurationBySite) configurationBySiteClone.Add(item.Key, item.Value.Clone() as SiteConfiguration);
            configuration.ConfigurationBySite = configurationBySiteClone;
            configuration.RegionsOfInterest = (from ROI in RegionsOfInterest select ROI).ToList();
            configuration.Gain = Gain;
            configuration.MaximumInfluence = MaximumInfluence;
            configuration.Alpha = Alpha;
            configuration.SpanMin = SpanMin;
            configuration.Middle = Middle;
            configuration.SpanMax = SpanMax;
            return configuration;
        }
        #endregion
    }
}