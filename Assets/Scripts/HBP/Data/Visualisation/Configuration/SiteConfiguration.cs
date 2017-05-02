using UnityEngine;
using System;
using System.Runtime.Serialization;

namespace HBP.Data.Visualisation
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
        /// Unique ID of the site.
        /// </summary>
        [DataMember]
        public string ID { get; set; }

        /// <summary>
        /// The site is masked ?
        /// </summary>
        [DataMember]
        public bool IsMasked { get; set; }

        /// <summary>
        /// The site is excluded ?
        /// </summary>
        [DataMember]
        public bool IsExcluded { get; set; }

        /// <summary>
        /// The site is blacklisted ?
        /// </summary>
        [DataMember]
        public bool IsBlacklisted { get; set; }

        /// <summary>
        /// The site is highlighted ?
        /// </summary>
        [DataMember]
        public bool IsHighlighted { get; set; }

        /// <summary>
        /// The site is marked ?
        /// </summary>
        [DataMember]
        public bool IsMarked { get; set; }

        [DataMember(Name = "Color")]
        SerializableColor m_Color;
        /// <summary>
        /// Color of the site.
        /// </summary>
        public Color Color { get { return m_Color.ToColor(); } set { m_Color = new SerializableColor(value); } }
        #endregion

        #region Constructors
        public SiteConfiguration(string ID,bool isMasked, bool isExcluded, bool isBlacklisted, bool isHighlighted, bool isMarked, Color color)
        {
            this.ID = ID;
            IsMasked = IsMasked;
            IsExcluded = isExcluded;
            IsBlacklisted = isBlacklisted;
            IsHighlighted = isHighlighted;
            IsMarked = isMarked;
            Color = color;
        }
        public SiteConfiguration() : this(string.Empty,false,false,false,false,false,new Color())
        {

        }
        #endregion

        #region Public Methods
        public object Clone()
        {
            return new SiteConfiguration(ID.Clone() as string, IsMasked, IsExcluded, IsBlacklisted, IsHighlighted, IsMarked, Color);
        }
        #endregion
    }
}