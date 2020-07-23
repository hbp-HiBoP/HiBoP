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
        /// Configuration of the sites.
        /// </summary>
        [DataMember] public Dictionary<string, SiteConfiguration> ConfigurationBySite { get; set; }
        #endregion

        #region Constructors
        public BaseConfiguration() : this(new Dictionary<string, SiteConfiguration>())
        {
        }
        public BaseConfiguration(Dictionary<string,SiteConfiguration> configurationBySite) : base()
        {
            ConfigurationBySite = configurationBySite;
        }
        public BaseConfiguration(Dictionary<string, SiteConfiguration> configurationBySite, string ID) : base(ID)
        {
            ConfigurationBySite = configurationBySite;
        }
        #endregion

        #region Public Methods
        public override object Clone()
        {
            return new BaseConfiguration(ConfigurationBySite.ToDictionary((k) => k.Key,(k) => k.Value.Clone() as SiteConfiguration), ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is BaseConfiguration baseConfiguration)
            {
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