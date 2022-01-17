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
        /// IEEG Transparency
        /// </summary>
        [DataMember(Name = "Activity Alpha")] public float ActivityAlpha { get; set; }
        /// <summary>
        /// Configuration of the sites.
        /// </summary>
        [DataMember] public Dictionary<string, SiteConfiguration> ConfigurationBySite { get; set; }
        #endregion

        #region Constructors
        public BaseConfiguration() : this(0.8f, new Dictionary<string, SiteConfiguration>())
        {
        }
        public BaseConfiguration(float alpha, Dictionary<string,SiteConfiguration> configurationBySite) : base()
        {
            ActivityAlpha = alpha;
            ConfigurationBySite = configurationBySite;
        }
        public BaseConfiguration(float alpha, Dictionary<string, SiteConfiguration> configurationBySite, string ID) : base(ID)
        {
            ActivityAlpha = alpha;
            ConfigurationBySite = configurationBySite;
        }
        #endregion

        #region Public Methods
        public override object Clone()
        {
            return new BaseConfiguration(ActivityAlpha, ConfigurationBySite.ToDictionary((k) => k.Key,(k) => k.Value.Clone() as SiteConfiguration), ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is BaseConfiguration baseConfiguration)
            {
                ActivityAlpha = baseConfiguration.ActivityAlpha;
                ConfigurationBySite = baseConfiguration.ConfigurationBySite;
            }
        }
        public override void GenerateID()
        {
            base.GenerateID();
            foreach (var siteConfig in ConfigurationBySite.Values) siteConfig.GenerateID();
        }
        public override List<BaseData> GetAllIdentifiable()
        {
            List<BaseData> IDs = base.GetAllIdentifiable();
            foreach (var siteConfig in ConfigurationBySite.Values) IDs.AddRange(siteConfig.GetAllIdentifiable());
            return IDs;
        }
        #endregion
    }
}