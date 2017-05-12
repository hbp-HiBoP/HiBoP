using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HBP.Data.Anatomy;

namespace HBP.Data.Visualization
{
    /**
    * \class PatientConfiguration
    * \author Adrien Gannerie
    * \version 1.0
    * \date 28 avril 2017
    * \brief Configuration of a patient.
    * 
    * \details PatientConfiguration is a class which define the configuration of a patient and contains:
    *   - \a Color of the patient.
    *   - \a Patient electrode configurations.
    */
    [DataContract]
    public class PatientConfiguration : ICloneable
    {
        #region Properties
        /// <summary>
        /// Configuration of the patient electrodes.
        /// </summary>
        [DataMember]
        public Dictionary<Electrode,ElectrodeConfiguration> ConfigurationByElectrode { get; set; }

        [DataMember(Name = "Color")]
        SerializableColor color;
        /// <summary>
        /// Color of the patient.
        /// </summary>
        public Color Color { get; set; }
        #endregion

        #region Constructors
        public PatientConfiguration(Dictionary<Electrode,ElectrodeConfiguration> configurationByElectrode, Color color)
        {
            ConfigurationByElectrode = configurationByElectrode;
            Color = color;
        }
        public PatientConfiguration(Color color) : this(new Dictionary<Electrode, ElectrodeConfiguration>(), color) { }
        public PatientConfiguration() : this(new Dictionary<Electrode, ElectrodeConfiguration>(), new Color()) { }
        #endregion

        #region Public Methods
        public object Clone()
        {
            IEnumerable<ElectrodeConfiguration> electrodesCloned = from electrode in ConfigurationByElectrode select electrode.Clone() as ElectrodeConfiguration;
            return new PatientConfiguration(ID.Clone() as string, electrodesCloned, Color);
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