using System;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HBP.Core.Data
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
        [DataMember(Name = "Color")] SerializableColor m_Color;
        /// <summary>
        /// Color of the electrode.
        /// </summary>
        [IgnoreDataMember] public Color Color { get; set; }

        /// <summary>
        /// Configurations of the electrode sites.
        /// </summary>
        [DataMember] public Dictionary<string,SiteConfiguration> ConfigurationBySite { get; set; }
        #endregion

        #region Constructors
        public ElectrodeConfiguration(Dictionary<string,SiteConfiguration> configurationBySite, Color color)
        {
            Color = color;
            ConfigurationBySite = configurationBySite;
        }
        public ElectrodeConfiguration() : this(new Dictionary<string, SiteConfiguration>(),new Color()) { }
        #endregion

        #region Public Methods
        public object Clone()
        {
            Dictionary<string, SiteConfiguration> configurationBySiteClone = new Dictionary<string, SiteConfiguration>();
            foreach (var item in ConfigurationBySite)
            {
                configurationBySiteClone.Add(item.Key, item.Value.Clone() as SiteConfiguration);
            }
            return new ElectrodeConfiguration(configurationBySiteClone, Color);
        }
        #endregion

        #region Serialization
        [OnSerializing]
        void OnSerializing(StreamingContext streamingContext)
        {
            m_Color = new SerializableColor(Color);
        }
        [OnDeserialized]
        void OnDeserialized(StreamingContext streamingContext)
        {
            Color = m_Color.ToColor();
        }
        #endregion
    }
}