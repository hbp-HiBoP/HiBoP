using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HBP.Data.Visualisation
{
    /**
    * \class PatientConfiguration
    * \author Adrien Gannerie
    * \version 1.0
    * \date 28 avril 2017
    * \brief Configuration of a patient.
    * 
    * \details PatientConfiguration is a class which define the configuration of a patient and contains:
    *   - \a Unique ID.
    *   - \a Color of the patient.
    *   - \a Configurations of the patient electrodes.
    */
    [DataContract]
    public class PatientConfiguration : ICloneable
    {
        #region Properties
        /// <summary>
        /// Unique ID of the patient.
        /// </summary>
        [DataMember]
        public string ID { get; set; }
        /// <summary>
        /// Configuration of the patient electrodes.
        /// </summary>
        [DataMember]
        public List<ElectrodeConfiguration> Electrodes { get; set; }

        [DataMember(Name = "Color")]
        SerializableColor m_Color;
        /// <summary>
        /// Color of the patient.
        /// </summary>
        public Color Color { get { return m_Color.ToColor(); } set { m_Color = new SerializableColor(value); } }
        #endregion

        #region Constructors
        public PatientConfiguration(string ID, IEnumerable<ElectrodeConfiguration> electrodes, Color color)
        {
            this.ID = ID;
            Electrodes = electrodes.ToList();
            Color = color;
        }
        public PatientConfiguration(): this(string.Empty,new ElectrodeConfiguration[0],new Color())
        {
        }
        #endregion

        #region Public Methods
        public object Clone()
        {
            IEnumerable<ElectrodeConfiguration> electrodesCloned = from electrode in Electrodes select electrode.Clone() as ElectrodeConfiguration;
            return new PatientConfiguration(ID.Clone() as string, electrodesCloned, Color);
        }
        #endregion
    }
}