﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using HBP.Core.Errors;
using HBP.Core.Tools;

namespace HBP.Core.Data
{
    /// <summary>
    /// Class containing paths to CCEP data files.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><b>Name</b></term>
    /// <description>Name of the data.</description>
    /// </item>
    /// <item>
    /// <term><b>Patient</b></term>
    /// <description>Patient who has passed the experiment.</description>
    /// </item>
    /// <item>
    /// <term><b>Stimulated channel</b></term>
    /// <description>Stimulated channel.</description>
    /// </item>
    /// <item>
    /// <term><b>Data container</b></term>
    /// <description>Data container containing all the paths to functional data files.</description>
    /// </item>
    /// <item>
    /// <term><b>Dataset</b></term>
    /// <description>Dataset the dataInfo belongs to.</description>
    /// </item>
    /// <item>
    /// <term><b>IsOk</b></term>
    /// <description>True if the dataInfo is visualizable, False otherwise.</description>
    /// </item>
    /// <item>
    /// <term><b>Errors</b></term>
    /// <description>All dataInfo errors.</description>
    /// </item>
    /// <item>
    /// <term><b>OnRequestErrorCheck</b></term>
    /// <description>Callback executed when error checking is required.</description>
    /// </item>
    /// </list>
    /// </remarks>
    [DataContract, DisplayName("FMRI")]
    public class FMRIDataInfo : PatientDataInfo
    {
        #region Properties
        protected Error[] m_FMRIErrors = new Error[0];
        public override Error[] Errors
        {
            get
            {
                List<Error> errors = new List<Error>(base.Errors);
                errors.AddRange(m_FMRIErrors);
                return errors.Distinct().ToArray();
            }
        }

        protected Warning[] m_FMRIWarnings = new Warning[0];
        public override Warning[] Warnings
        {
            get
            {
                List<Warning> warnings = new List<Warning>(base.Warnings);
                warnings.AddRange(m_FMRIWarnings);
                return warnings.Distinct().ToArray();
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new CCEPDataInfo instance.
        /// </summary>
        /// <param name="name">Name of the CCEP dataInfo.</param>
        /// <param name="dataContainer">Data container of the CCEP dataInfo.</param>
        /// <param name="patient">Patient related to the data.</param>
        /// <param name="channel">Stimulated channel.</param>
        /// <param name="id">Unique identifier</param>
        public FMRIDataInfo(string name, Container.DataContainer dataContainer, Patient patient, string ID) : base(name, dataContainer, patient, ID)
        {
        }
        /// <summary>
        /// Create a new CCEPDataInfo instance.
        /// </summary>
        /// <param name="name">Name of the CCEP dataInfo.</param>
        /// <param name="dataContainer">Data container of the CCEP dataInfo.</param>
        /// <param name="patient">Patient related to the data.</param>
        /// <param name="channel">Stimulated channel.</param>
        public FMRIDataInfo(string name, Container.DataContainer dataContainer, Patient patient) : base(name, dataContainer, patient)
        {
        }
        /// <summary>
        /// Create a new CCEPDataInfo instance.
        /// </summary>
        public FMRIDataInfo() : this("Data", new Container.Elan(), ApplicationState.ProjectLoaded.Patients.FirstOrDefault())
        {

        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Clone of this instance.</returns>
        public override object Clone()
        {
            return new FMRIDataInfo(Name, DataContainer.Clone() as Container.DataContainer, Patient, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
        }
        #endregion

        #region Public Methods
        public override Error[] GetErrors(Protocol protocol)
        {
            List<Error> errors = new List<Error>(base.GetErrors(protocol));
            errors.AddRange(GetFMRIErrors(protocol));
            return errors.Distinct().ToArray();
        }
        /// <summary>
        /// Get all dataInfo errors related to CCEP.
        /// </summary>
        /// <param name="protocol"></param>
        /// <returns>CCEP related errors</returns>
        public virtual Error[] GetFMRIErrors(Protocol protocol)
        {
            List<Error> errors = new List<Error>();
            // TODO
            m_FMRIErrors = errors.ToArray();
            return m_FMRIErrors;
        }
        public override Warning[] GetWarnings(Protocol protocol)
        {
            List<Warning> warnings = new List<Warning>(base.GetWarnings(protocol));
            warnings.AddRange(GetFMRIWarnings(protocol));
            return warnings.Distinct().ToArray();
        }
        /// <summary>
        /// Get all dataInfo errors related to CCEP.
        /// </summary>
        /// <param name="protocol"></param>
        /// <returns>CCEP related errors</returns>
        public virtual Warning[] GetFMRIWarnings(Protocol protocol)
        {
            List<Warning> warnings = new List<Warning>();
            m_FMRIWarnings = warnings.ToArray();
            return m_FMRIWarnings;
        }
        #endregion
    }
}