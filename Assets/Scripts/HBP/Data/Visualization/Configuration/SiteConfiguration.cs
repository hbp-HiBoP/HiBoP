using UnityEngine;
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
        SerializableColor color;
        /// <summary>
        /// Color of the site.
        /// </summary>
        public Color Color { get ; set; }

        [IgnoreDataMember]
        public float[] Values { get; set; }
        #endregion

        #region Constructors
        public SiteConfiguration(float[] values, bool isMasked, bool isExcluded, bool isBlacklisted, bool isHighlighted, bool isMarked, Color color)
        {
            Values = values;
            IsMasked = IsMasked;
            IsExcluded = isExcluded;
            IsBlacklisted = isBlacklisted;
            IsHighlighted = isHighlighted;
            IsMarked = isMarked;
            Color = color;
        }
        public SiteConfiguration(Color color) : this(new float[0], false, false, false, false, false, color) { }
        public SiteConfiguration() : this(new Color()) { }
        #endregion

        #region Public Methods
        public object Clone()
        {
            return new SiteConfiguration(Values,IsMasked, IsExcluded, IsBlacklisted, IsHighlighted, IsMarked, Color);
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