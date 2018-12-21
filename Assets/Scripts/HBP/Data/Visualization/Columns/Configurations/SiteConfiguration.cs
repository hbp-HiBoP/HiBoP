using System;
using System.Runtime.Serialization;

namespace HBP.Data.Visualization
{
    /**
    * \class SiteConfiguration
    * \author Adrien Gannerie
    * \version 1.0
    * \date 28 avril 2017
    * \brief Configuration of a site.
    * 
    * \details SiteConfiguration is a class which define the configuration of a site and contains:
    *   - \a Unique ID.
    *   - \a IsMasked.
    *   - \a IsExcluded.
    *   - \a IsBlackListed.
    *   - \a IsHighligted.
    *   - \a IsMarked
    *   - \a Color.
    */
    [DataContract]
    public class SiteConfiguration : ICloneable
    {
        #region Properties
        /// <summary>
        /// The site is excluded ?
        /// </summary>
        [DataMember] public bool IsExcluded { get; set; }
        /// <summary>
        /// The site is blacklisted ?
        /// </summary>
        [DataMember] public bool IsBlacklisted { get; set; }
        /// <summary>
        /// The site is highlighted ?
        /// </summary>
        [DataMember] public bool IsHighlighted { get; set; }
        /// <summary>
        /// The site is marked ?
        /// </summary>
        [DataMember] public bool IsMarked { get; set; }
        /// <summary>
        /// The site is suspicious ?
        /// </summary>
        [DataMember] public bool IsSuspicious { get; set; }
        #endregion

        #region Constructors
        public SiteConfiguration() : this(false, false, false, false, false)
        {
        }
        public SiteConfiguration(bool isExcluded, bool isBlacklisted, bool isHighlighted, bool isMarked, bool isSuspicious)
        {
            IsExcluded = isExcluded;
            IsBlacklisted = isBlacklisted;
            IsHighlighted = isHighlighted;
            IsMarked = isMarked;
            IsSuspicious = isSuspicious;
        }
        #endregion

        #region Public Methods
        public object Clone()
        {
            return new SiteConfiguration(IsExcluded, IsBlacklisted, IsHighlighted, IsMarked, IsSuspicious);
        }
        public void LoadSerializedConfiguration(SiteConfiguration configuration)
        {
            IsBlacklisted = configuration.IsBlacklisted;
            IsExcluded = configuration.IsExcluded;
            IsHighlighted = configuration.IsHighlighted;
            IsMarked = configuration.IsMarked;
            IsSuspicious = configuration.IsSuspicious;
        }
        #endregion
    }
}