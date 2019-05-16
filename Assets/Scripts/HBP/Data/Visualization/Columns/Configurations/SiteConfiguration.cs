using System;
using System.Runtime.Serialization;
using UnityEngine;

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
        /// The site is blacklisted ?
        /// </summary>
        [DataMember] public bool IsBlacklisted { get; set; }
        /// <summary>
        /// The site is highlighted ?
        /// </summary>
        [DataMember] public bool IsHighlighted { get; set; }
        /// <summary>
        /// Color of the site
        /// </summary>
        [DataMember] private SerializableColor m_Color;
        public Color Color
        {
            get
            {
                return m_Color.ToColor();
            }
            set
            {
                m_Color = new SerializableColor(value);
            }
        }
        #endregion

        #region Constructors
        public SiteConfiguration() : this(false, false, Module3D.SiteState.DefaultColor)
        {
        }
        public SiteConfiguration(bool isBlacklisted, bool isHighlighted, Color color)
        {
            IsBlacklisted = isBlacklisted;
            IsHighlighted = isHighlighted;
            Color = color;
        }
        #endregion

        #region Public Methods
        public object Clone()
        {
            return new SiteConfiguration(IsBlacklisted, IsHighlighted, Color);
        }
        #endregion
    }
}