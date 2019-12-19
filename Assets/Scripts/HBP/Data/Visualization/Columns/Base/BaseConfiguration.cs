using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HBP.Data.Visualization
{
    [DataContract]
    public class BaseConfiguration : BaseData
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
        public BaseConfiguration(float plotSize, IEnumerable<RegionOfInterest> regionsOfInterest, Dictionary<string,SiteConfiguration> configurationBySite) : base()
        {
            SiteSize = plotSize;
            RegionsOfInterest = regionsOfInterest.ToList();
            ConfigurationBySite = configurationBySite;
        }
        public BaseConfiguration(float plotSize, IEnumerable<RegionOfInterest> regionsOfInterest, Dictionary<string, SiteConfiguration> configurationBySite, string ID) : base(ID)
        {
            SiteSize = plotSize;
            RegionsOfInterest = regionsOfInterest.ToList();
            ConfigurationBySite = configurationBySite;
        }
        #endregion

        #region Public Methods
        public override object Clone()
        {
            return new BaseConfiguration(SiteSize, RegionsOfInterest, ConfigurationBySite.ToDictionary((k) => k.Key,(k) => k.Value.Clone() as SiteConfiguration), ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if(copy is BaseConfiguration baseConfiguration)
            {
                SiteSize = baseConfiguration.SiteSize;
                RegionsOfInterest = baseConfiguration.RegionsOfInterest;
                ConfigurationBySite = baseConfiguration.ConfigurationBySite;
            }
        }
        public override void GenerateID()
        {
            base.GenerateID();
            foreach (var siteConfig in ConfigurationBySite.Values) siteConfig.GenerateID();
        }
        #endregion
    }
}