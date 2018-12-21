using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HBP.Data.Visualization
{
    [DataContract]
    public class BaseConfiguration : ICloneable
    {
        #region Properties
        /// <summary>
        /// Size of the sites.
        /// </summary>
        [DataMember] public float SiteSize { get; set; }
        /// <summary>
        /// Region of interest.
        /// </summary>
        [DataMember] public List<RegionOfInterest> RegionsOfInterest { get; set; }
        /// <summary>
        /// Configuration of the sites.
        /// </summary>
        [DataMember] public Dictionary<string, SiteConfiguration> ConfigurationBySite { get; set; }
        #endregion

        #region Constructors
        public BaseConfiguration() : this(1, new RegionOfInterest[0], new Dictionary<string, SiteConfiguration>())
        {
        }
        public BaseConfiguration(float plotSize, IEnumerable<RegionOfInterest> regionsOfInterest, Dictionary<string,SiteConfiguration> configurationBySite)
        {
            SiteSize = plotSize;
            RegionsOfInterest = regionsOfInterest.ToList();
            ConfigurationBySite = configurationBySite;
        }
        #endregion

        #region Private Methods
        public object Clone()
        {
            return new BaseConfiguration(SiteSize, RegionsOfInterest.ToList(), ConfigurationBySite.ToDictionary((k) => k.Key,(k) => k.Value));
        }
        #endregion
    }
}