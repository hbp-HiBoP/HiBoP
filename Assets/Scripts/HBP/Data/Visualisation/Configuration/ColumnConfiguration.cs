using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HBP.Data.Visualisation
{
    /**
    * \class ColumnConfiguration
    * \author Adrien Gannerie
    * \version 1.0
    * \date 28 avril 2017
    * \brief Configuration of a column.
    * 
    * \details ColumnConfiguration§ is a class which define the configuration of a patient and contains:
    *   - \a Unique ID.
    *   - \a Color of the patient.
    *   - \a Configurations of the patient electrodes.
    */
    [DataContract]
    public class ColumnConfiguration : ICloneable
    {
        #region Properties
        /// <summary>
        /// Patients configurations.
        /// </summary>
        [DataMember]
        public List<PatientConfiguration> Patients { get; set; }
        /// <summary>
        /// Region of interest.
        /// </summary>
        [DataMember]
        public List<RegionOfInterest> RegionOfInterest { get; set; }
        #endregion

        #region Constructor
        public ColumnConfiguration(IEnumerable<PatientConfiguration> patients, IEnumerable<RegionOfInterest> regionOfInterest)
        {
            Patients = patients.ToList();
            RegionOfInterest = regionOfInterest.ToList();
        }
        public ColumnConfiguration() : this (new PatientConfiguration[0],new RegionOfInterest[0])
        { 
        }
        #endregion

        #region Public Methods
        public object Clone()
        {
            return new ColumnConfiguration(from patient in Patients select patient.Clone() as PatientConfiguration, from ROI in RegionOfInterest select ROI.Clone() as RegionOfInterest);
        }
        #endregion
    }
}