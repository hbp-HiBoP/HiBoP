using System;
using System.Collections.Generic;
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
    [DataContract, DisplayName("CCEP")]
    public class CCEPDataInfo : PatientDataInfo, IEpochable
    {
        #region Properties
        /// <summary>
        /// Stimulated channel.
        /// </summary>
        [DataMember] public string StimulatedChannel { get; set; }

        protected Error[] m_CCEPErrors = new Error[0];
        public override Error[] Errors
        {
            get
            {
                List<Error> errors = new List<Error>(base.Errors);
                errors.AddRange(m_CCEPErrors);
                return errors.Distinct().ToArray();
            }
        }

        protected Warning[] m_CCEPWarnings = new Warning[0];
        public override Warning[] Warnings
        {
            get
            {
                List<Warning> warnings = new List<Warning>(base.Warnings);
                warnings.AddRange(m_CCEPWarnings);
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
        public CCEPDataInfo(string name, Container.DataContainer dataContainer, Patient patient, string channel, string ID) : base(name, dataContainer, patient, ID)
        {
            StimulatedChannel = channel;
        }
        /// <summary>
        /// Create a new CCEPDataInfo instance.
        /// </summary>
        /// <param name="name">Name of the CCEP dataInfo.</param>
        /// <param name="dataContainer">Data container of the CCEP dataInfo.</param>
        /// <param name="patient">Patient related to the data.</param>
        /// <param name="channel">Stimulated channel.</param>
        public CCEPDataInfo(string name, Container.DataContainer dataContainer, Patient patient, string channel) : base(name, dataContainer, patient)
        {
            StimulatedChannel = channel;
        }
        /// <summary>
        /// Create a new CCEPDataInfo instance.
        /// </summary>
        public CCEPDataInfo() : this("Data", new Container.Elan(), ApplicationState.ProjectLoaded.Patients.FirstOrDefault(), "Unknown")
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
            return new CCEPDataInfo(Name, DataContainer.Clone() as Container.DataContainer, Patient, StimulatedChannel, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if(copy is CCEPDataInfo ccepDataInfo)
            {
                StimulatedChannel = ccepDataInfo.StimulatedChannel;
            }
        }
        #endregion

        #region Public Methods
        public override Error[] GetErrors(Protocol protocol)
        {
            List<Error> errors = new List<Error>(base.GetErrors(protocol));
            errors.AddRange(GetCCEPErrors(protocol));
            return errors.Distinct().ToArray();
        }
        /// <summary>
        /// Get all dataInfo errors related to CCEP.
        /// </summary>
        /// <param name="protocol"></param>
        /// <returns>CCEP related errors</returns>
        public virtual Error[] GetCCEPErrors(Protocol protocol)
        {
            List<Error> errors = new List<Error>();
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
                    errors.Add(new BlocsCantBeEpochedError());
                }
            }
            if (!m_Patient.Sites.Any(site => site.Name == StimulatedChannel))
            {
                errors.Add(new ChannelNotFoundError());
            }
            m_CCEPErrors = errors.ToArray();
            return m_CCEPErrors;
        }
        public override Warning[] GetWarnings(Protocol protocol)
        {
            List<Warning> warnings = new List<Warning>(base.GetWarnings(protocol));
            warnings.AddRange(GetCCEPWarnings(protocol));
            return warnings.Distinct().ToArray();
        }
        /// <summary>
        /// Get all dataInfo errors related to CCEP.
        /// </summary>
        /// <param name="protocol"></param>
        /// <returns>CCEP related errors</returns>
        public virtual Warning[] GetCCEPWarnings(Protocol protocol)
        {
            List<Warning> warnings = new List<Warning>();
            m_CCEPWarnings = warnings.ToArray();
            return m_CCEPWarnings;
        }
        #endregion
    }
}