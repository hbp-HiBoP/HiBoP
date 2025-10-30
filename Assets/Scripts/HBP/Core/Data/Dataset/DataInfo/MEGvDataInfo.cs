using HBP.Core.Errors;
using HBP.Data.Database;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    /// <summary>
    /// Class containing paths to MEG data files.
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
    [JsonObject(MemberSerialization.OptIn), Preserve, DisplayName("MEGv")]
    public class MEGvDataInfo : PatientDataInfo
    {
        #region Properties
        [JsonProperty("MaskDataContainer")] protected Container.Nifti m_MaskDataContainer;
        public Container.Nifti MaskDataContainer
        {
            get => m_MaskDataContainer;
            set => m_MaskDataContainer = value;
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
        public MEGvDataInfo(string name, Protocol protocol, Container.DataContainer dataContainer, Container.Nifti maskDataContainer, IEnumerable<Error> errors, IEnumerable<Warning> warnings, Patient patient, string correspondingDatabaseID, string ID) : base(name, protocol, dataContainer, errors, warnings, patient, correspondingDatabaseID, ID)
        {
            m_MaskDataContainer = maskDataContainer;
        }
        /// <summary>
        /// Create a new CCEPDataInfo instance.
        /// </summary>
        /// <param name="name">Name of the CCEP dataInfo.</param>
        /// <param name="dataContainer">Data container of the CCEP dataInfo.</param>
        /// <param name="patient">Patient related to the data.</param>
        /// <param name="channel">Stimulated channel.</param>
        public MEGvDataInfo(string name, Protocol protocol, Container.DataContainer dataContainer, Container.Nifti maskDataContainer, IEnumerable<Error> errors, IEnumerable<Warning> warnings, Patient patient, string correspondingDatabaseID) : base(name, protocol, dataContainer, errors, warnings, patient, correspondingDatabaseID)
        {
            m_MaskDataContainer = maskDataContainer;
        }
        /// <summary>
        /// Create a new CCEPDataInfo instance.
        /// </summary>
        public MEGvDataInfo() : this("Data", DatabaseManager.Database.Protocols.FirstOrDefault(), new Container.Nifti(), new Container.Nifti(), new Error[0], new Warning[0], null, "")
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
            return new MEGvDataInfo(Name, Protocol, DataContainer.Clone() as Container.DataContainer, MaskDataContainer.Clone() as Container.Nifti, Errors, Warnings, Patient, CorrespondingDatabaseID, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is MEGvDataInfo dataInfo)
            {
                m_MaskDataContainer = dataInfo.m_MaskDataContainer;
            }
        }
        #endregion

        #region Private Methods
        protected override IEnumerable<Error> GetErrors()
        {
            List<Error> errors = new List<Error>(base.GetErrors());
            errors.AddRange(GetMEGErrors());
            return errors;
        }
        /// <summary>
        /// Get all dataInfo errors related to CCEP.
        /// </summary>
        /// <param name="protocol"></param>
        /// <returns>CCEP related errors</returns>
        private IEnumerable<Error> GetMEGErrors()
        {
            List<Error> errors = new List<Error>();
            if (!string.IsNullOrEmpty(MaskDataContainer.File)) errors.AddRange(MaskDataContainer.GetErrors());
            return errors;
        }
        protected override IEnumerable<Warning> GetWarnings()
        {
            List<Warning> warnings = new List<Warning>(base.GetWarnings());
            warnings.AddRange(GetMEGWarnings());
            return warnings.Distinct().ToArray();
        }
        /// <summary>
        /// Get all dataInfo errors related to CCEP.
        /// </summary>
        /// <param name="protocol"></param>
        /// <returns>CCEP related errors</returns>
        private IEnumerable<Warning> GetMEGWarnings()
        {
            List<Warning> warnings = new List<Warning>();
            if (!string.IsNullOrEmpty(MaskDataContainer.File)) warnings.AddRange(MaskDataContainer.GetWarnings());
            return warnings;
        }
        #endregion
    }
}