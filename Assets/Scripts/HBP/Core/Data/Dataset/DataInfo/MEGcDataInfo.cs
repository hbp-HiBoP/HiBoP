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
    [JsonObject(MemberSerialization.OptIn), Preserve, DisplayName("MEGc")]
    public class MEGcDataInfo : PatientDataInfo
    {
        #region Constructors
        /// <summary>
        /// Create a new CCEPDataInfo instance.
        /// </summary>
        /// <param name="name">Name of the CCEP dataInfo.</param>
        /// <param name="dataContainer">Data container of the CCEP dataInfo.</param>
        /// <param name="patient">Patient related to the data.</param>
        /// <param name="channel">Stimulated channel.</param>
        /// <param name="id">Unique identifier</param>
        public MEGcDataInfo(string name, Protocol protocol, Container.DataContainer dataContainer, IEnumerable<Error> errors, IEnumerable<Warning> warnings, Patient patient, string correspondingDatabaseID, string ID) : base(name, protocol, dataContainer, errors, warnings, patient, correspondingDatabaseID, ID)
        {
        }
        /// <summary>
        /// Create a new CCEPDataInfo instance.
        /// </summary>
        /// <param name="name">Name of the CCEP dataInfo.</param>
        /// <param name="dataContainer">Data container of the CCEP dataInfo.</param>
        /// <param name="patient">Patient related to the data.</param>
        /// <param name="channel">Stimulated channel.</param>
        public MEGcDataInfo(string name, Protocol protocol, Container.DataContainer dataContainer, IEnumerable<Error> errors, IEnumerable<Warning> warnings, Patient patient, string correspondingDatabaseID) : base(name, protocol, dataContainer, errors, warnings, patient, correspondingDatabaseID)
        {
        }
        /// <summary>
        /// Create a new CCEPDataInfo instance.
        /// </summary>
        public MEGcDataInfo() : this("Data", DatabaseManager.Database.Protocols.FirstOrDefault(), new Container.Elan(), new Error[0], new Warning[0], null, "")
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
            return new MEGcDataInfo(Name, Protocol, DataContainer.Clone() as Container.DataContainer, Errors, Warnings, Patient, CorrespondingDatabaseID, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
        }
        #endregion

        #region Public Methods
        protected override IEnumerable<Error> GetErrors()
        {
            List<Error> errors = new(base.GetErrors());
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
            List<Error> errors = new();
            return errors;
        }
        protected override IEnumerable<Warning> GetWarnings()
        {
            List<Warning> warnings = new(base.GetWarnings());
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
            List<Warning> warnings = new();
            return warnings;
        }
        #endregion
    }
}