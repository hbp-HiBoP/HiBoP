using HBP.Core.Errors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using HBP.Core.Enums;
using HBP.Core.Tools;

namespace HBP.Core.Data
{
    /// <summary>
    /// Class containing paths to iEEG data files.
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
    [DataContract, DisplayName("iEEG")]
    public class IEEGDataInfo : PatientDataInfo, IEpochable
    {
        #region Properties
        /// <summary>
        /// Normalization of the Data.
        /// </summary>
        [DataMember(Name = "Normalization")]
        public NormalizationType Normalization { get; set; }

        protected Error[] m_iEEGErrors = new Error[0];
        public override Error[] Errors
        {
            get
            {
                List<Error> errors = new List<Error>(base.Errors);
                errors.AddRange(m_iEEGErrors);
                return errors.Distinct().ToArray();
            }
        }

        protected Warning[] m_iEEGWarnings = new Warning[0];
        public override Warning[] Warnings
        {
            get
            {
                List<Warning> warnings = new List<Warning>(base.Warnings);
                warnings.AddRange(m_iEEGWarnings);
                return warnings.Distinct().ToArray();
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new iEEG dataInfo instance.
        /// </summary>
        /// <param name="name">Name of the iEEG dataInfo.</param>
        /// <param name="dataContainer">Data container of the iEEG data.</param>
        /// <param name="patient">Patient related to the data.</param>
        /// <param name="normalization">Normalization of the iEEG data.</param>
        /// <param name="ID">Unique identifier</param>
        public IEEGDataInfo(string name, Container.DataContainer dataContainer, Patient patient, NormalizationType normalization, string ID) : base(name, dataContainer, patient,ID)
        {
            Normalization = normalization;
        }
        /// <summary>
        /// Create a new iEEG dataInfo instance.
        /// </summary>
        /// <param name="name">Name of the iEEG dataInfo.</param>
        /// <param name="dataContainer">Data container of the iEEG data.</param>
        /// <param name="patient">Patient related to the data.</param>
        /// <param name="normalization">Normalization of the iEEG data.</param>
        public IEEGDataInfo(string name, Container.DataContainer dataContainer, Patient patient, NormalizationType normalization) : base(name, dataContainer, patient)
        {
            Normalization = normalization;
        }
        /// <summary>
        /// Create a new iEEG dataInfo instance.
        /// </summary>
        public IEEGDataInfo() : this("Data", new Container.Elan(), ApplicationState.ProjectLoaded.Patients.FirstOrDefault(), NormalizationType.Auto)
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
            return new IEEGDataInfo(Name, DataContainer.Clone() as Container.DataContainer, Patient, Normalization, ID);
        }
        public override void Copy(object obj)
        {
            base.Copy(obj);
            if(obj is IEEGDataInfo iEEGdataInfo)
            {
                Normalization = iEEGdataInfo.Normalization;
            }
        }
        #endregion

        #region Public Methods
        public override Error[] GetErrors(Protocol protocol)
        {
            List<Error> errors = new List<Error>(base.GetErrors(protocol));
            errors.AddRange(GetiEEGErrors(protocol));
            return errors.Distinct().ToArray();
        }
        /// <summary>
        /// Get all dataInfo errors related to iEEG.
        /// </summary>
        /// <param name="protocol">Protocol of the dataset the dataInfo belongs to.</param>
        /// <returns>iEEG related errors</returns>
        public virtual Error[] GetiEEGErrors(Protocol protocol)
        {
            List<Error> errors = new List<Error>();
            m_iEEGErrors = errors.ToArray();
            return m_iEEGErrors;
        }
        public override Warning[] GetWarnings(Protocol protocol)
        {
            List<Warning> warnings = new List<Warning>(base.GetWarnings(protocol));
            warnings.AddRange(GetiEEGWarnings(protocol));
            return warnings.Distinct().ToArray();
        }
        public virtual Warning[] GetiEEGWarnings(Protocol protocol)
        {
            List<Warning> warnings = new List<Warning>();
            if (m_DataContainer.IsOk)
            {
                DLL.EEG.File.FileType type;
                string[] files;
                if (m_DataContainer is Container.BrainVision brainVisionDataContainer)
                {
                    type = DLL.EEG.File.FileType.BrainVision;
                    files = new string[] { brainVisionDataContainer.Header };
                }
                else if (m_DataContainer is Container.EDF edfDataContainer)
                {
                    type = DLL.EEG.File.FileType.EDF;
                    files = new string[] { edfDataContainer.File };
                }
                else if (m_DataContainer is Container.Elan elanDataContainer)
                {
                    type = DLL.EEG.File.FileType.ELAN;
                    files = new string[] { elanDataContainer.EEG, elanDataContainer.POS, elanDataContainer.Notes };
                }
                else if (m_DataContainer is Container.Micromed micromedDataContainer)
                {
                    type = DLL.EEG.File.FileType.Micromed;
                    files = new string[] { micromedDataContainer.Path };
                }
                else if (m_DataContainer is Container.FIF fifDataContainer)
                {
                    type = DLL.EEG.File.FileType.FIF;
                    files = new string[] { fifDataContainer.File };
                }
                else
                {
                    throw new Exception("Invalid data container type");
                }
                DLL.EEG.File file = new DLL.EEG.File(type, false, files);
                List<DLL.EEG.Trigger> triggers = file.Triggers;
                if (protocol.IsVisualizable && !protocol.Blocs.All(bloc => bloc.MainSubBloc.MainEvent.Codes.Any(code => triggers.Any(t => t.Code == code))))
                {
                    IEnumerable<string> blocsNotFound = protocol.Blocs.Where(bloc => !bloc.MainSubBloc.MainEvent.Codes.Any(code => triggers.Any(t => t.Code == code))).Select(bloc => bloc.Name);
                    warnings.Add(new BlocsCantBeEpochedWarning(string.Join(", ", blocsNotFound)));
                }
                List<DLL.EEG.Electrode> electrodes = file.Electrodes;
                if (!Patient.Sites.Any(s => electrodes.Any(e => e.Label == s.Name)))
                {
                    warnings.Add(new NoMatchingSiteWarning());
                }
            }
            m_iEEGWarnings = warnings.ToArray();
            return m_iEEGWarnings;
        }
        #endregion
    }
}