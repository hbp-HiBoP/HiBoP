using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HBP.Data.Visualisation
{
    /**
    * \class ElectrodeConfiguration
    * \author Adrien Gannerie
    * \version 1.0
    * \date 28 avril 2017
    * \brief Configuration of a electrode.
    * 
    * \details ElectrodeConfiguration is a class which define the configuration of a electrode and contains:
    *   - \a Unique ID.
    *   - \a Configuration of the sites.
    *   - \a Color of the electrode.
    */
    [DataContract]
    public class ElectrodeConfiguration : ICloneable
    {
        #region Properties
        /// <summary>
        /// Unique ID of the electrode.
        /// </summary>
        [DataMember]
        public string ID { get; set; }

        [DataMember(Name = "Color")]
        SerializableColor m_Color;
        /// <summary>
        /// Color of the electrode.
        /// </summary>
        public Color Color { get { return m_Color.ToColor(); } set { m_Color = new SerializableColor(value); } }

        /// <summary>
        /// Configurations of the electrode sites.
        /// </summary>
        [DataMember]
        public List<SiteConfiguration> Sites { get; set; }
        #endregion

        #region Constructors
        public ElectrodeConfiguration(string ID, Color color,IEnumerable<SiteConfiguration> sites)
        {
            this.ID = ID;
            Color = color;
            Sites = sites.ToList();
        }
        public ElectrodeConfiguration() : this(string.Empty, new Color(), new SiteConfiguration[0])
        {

        }
        #endregion

        #region Public Methods
        public object Clone()
        {
            IEnumerable<SiteConfiguration> sitesCloned = from site in Sites select site.Clone() as SiteConfiguration;
            return new ElectrodeConfiguration(ID.Clone() as string, Color, sitesCloned);
        }
        #endregion
    }
}