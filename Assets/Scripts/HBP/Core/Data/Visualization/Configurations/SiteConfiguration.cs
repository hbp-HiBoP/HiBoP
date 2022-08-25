using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

namespace HBP.Core.Data
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
    public class SiteConfiguration : BaseData
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
        [DataMember(Name = "Color")] private SerializableColor m_Color;
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
        [DataMember] public string[] Labels { get; set; }
        #endregion

        #region Constructors
        public SiteConfiguration() : this(false, false, Object3D.SiteState.DefaultColor, new string[0])
        {
        }
        public SiteConfiguration(bool isBlacklisted, bool isHighlighted, Color color, IEnumerable<string> labels) : base()
        {
            IsBlacklisted = isBlacklisted;
            IsHighlighted = isHighlighted;
            Color = color;
            Labels = labels.ToArray();
        }
        public SiteConfiguration(bool isBlacklisted, bool isHighlighted, Color color, IEnumerable<string> labels, string ID) : base(ID)
        {
            IsBlacklisted = isBlacklisted;
            IsHighlighted = isHighlighted;
            Color = color;
            Labels = labels.ToArray();
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new SiteConfiguration(IsBlacklisted, IsHighlighted, Color, Labels, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if(copy is SiteConfiguration siteConfiguration)
            {
                IsBlacklisted = siteConfiguration.IsBlacklisted;
                IsHighlighted = siteConfiguration.IsHighlighted;
                Color = siteConfiguration.Color;
                Labels = siteConfiguration.Labels;
            }
        }
        #endregion
    }
}