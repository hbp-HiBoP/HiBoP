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
        [DataMember(Name = "Color")]
        SerializableColor color;

        /// <summary>
        /// Color of the electrode.
        /// </summary>
        [IgnoreDataMember]
        public Color Color { get; set; }

        /// <summary>
        /// Configurations of the electrode sites.
        /// </summary>
        [DataMember]
        public Dictionary<Site,SiteConfiguration> ConfigurationBySite { get; set; }
        #endregion

        #region Constructors
        public ElectrodeConfiguration(Dictionary<Site,SiteConfiguration> configurationBySite, Color color)
        {
            Color = color;
            ConfigurationBySite = configurationBySite;
        }
        public ElectrodeConfiguration(Color color) : this(new Dictionary<Site, SiteConfiguration>(), color) { }
        public ElectrodeConfiguration() : this(new Dictionary<Site, SiteConfiguration>(), new Color()) { }
        #endregion

        #region Public Methods
        public object Clone()
        {
            IEnumerable<SiteConfiguration> sitesCloned = from site in ConfigurationBySite select site.Clone() as SiteConfiguration;
            return new ElectrodeConfiguration(ID.Clone() as string, Color, sitesCloned);
        }
        #endregion

        #region Serialization
        [OnSerializing]
        void OnSerializing(StreamingContext streamingContext)
        {
            color = new SerializableColor(Color);
        }
        [OnDeserialized]
        void OnDeserialized(StreamingContext streamingContext)
        {
            Color = color.ToColor();
        }
        #endregion
    }
}