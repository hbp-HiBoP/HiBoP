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
        [DataMember]
        public List<RegionOfInterest> RegionOfInterest { get; set; }
        #endregion

        #region Constructor
        public ColumnConfiguration(Dictionary<string,SiteConfiguration> configurationBySite, IEnumerable<RegionOfInterest> regionOfInterest)
        {
            ConfigurationBySite = configurationBySite;
            RegionOfInterest = regionOfInterest.ToList();
        }
        public ColumnConfiguration() : this (new Dictionary<string, SiteConfiguration>(), new RegionOfInterest[0])
        { 
        }
        #endregion

        #region Public Methods
        public object Clone()
        {
            Dictionary<string, SiteConfiguration> configurationBySiteClone = new Dictionary<string, SiteConfiguration>();
            foreach (var item in ConfigurationBySite) configurationBySiteClone.Add(item.Key, item.Value.Clone() as SiteConfiguration);
            return new ColumnConfiguration(configurationBySiteClone, from ROI in RegionOfInterest select ROI.Clone() as RegionOfInterest);
        }
        #endregion
    }
}