using HBP.Errors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace HBP.Data.Experience.Dataset
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
        /// Normalization Type.
        /// </summary>
        public enum NormalizationType
        {
            None, SubTrial, Trial, SubBloc, Bloc, Protocol, Auto
        }

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
        public override Error[] GetErrors(Protocol.Protocol protocol)
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
        public virtual Error[] GetiEEGErrors(Protocol.Protocol protocol)
        {
            List<Error> errors = new List<Error>();
            if (m_DataContainer.IsOk)
            {
                Tools.CSharp.EEG.File.FileType type;
                string[] files;
                if (m_DataContainer is Container.BrainVision brainVisionDataContainer)
                {
                    type = Tools.CSharp.EEG.File.FileType.BrainVision;
                    files = new string[] { brainVisionDataContainer.Header };
                }
                else if (m_DataContainer is Container.EDF edfDataContainer)
                {
                    type = Tools.CSharp.EEG.File.FileType.EDF;
                    files = new string[] { edfDataContainer.File };
                }
                else if (m_DataContainer is Container.Elan elanDataContainer)
                {
                    type = Tools.CSharp.EEG.File.FileType.ELAN;
                    files = new string[] { elanDataContainer.EEG, elanDataContainer.POS, elanDataContainer.Notes };
                }
                else if (m_DataContainer is Container.Micromed micromedDataContainer)
                {
                    type = Tools.CSharp.EEG.File.FileType.Micromed;
                    files = new string[] { micromedDataContainer.Path };
                }
                else if (m_DataContainer is Container.FIF fifDataContainer)
                {
                    type = Tools.CSharp.EEG.File.FileType.FIF;
                    files = new string[] { fifDataContainer.File };
                }
                else
                {
                    throw new Exception("Invalid data container type");
                }
                Tools.CSharp.EEG.File file = new Tools.CSharp.EEG.File(type, false, files);
                List<Tools.CSharp.EEG.Trigger> triggers = file.Triggers;
                if (protocol.IsVisualizable && !protocol.Blocs.All(bloc => bloc.MainSubBloc.MainEvent.Codes.Any(code => triggers.Any(t => t.Code == code))))
                {
                    errors.Add(new BlocsCantBeEpochedError());
                }
            }
            m_iEEGErrors = errors.ToArray();
            return m_iEEGErrors;
        }
        #endregion

        #region Errors
        public class BlocsCantBeEpochedError : Error
        {
            #region Constructors
            public BlocsCantBeEpochedError() : this("")
            {

            }
            public BlocsCantBeEpochedError(string message) : base("One of the blocs of the protocol can't be epoched", message)
            {
                 
            }
            #endregion
        }
        public class ChannelNotFoundError : Error
        {
            #region Constructors
            public ChannelNotFoundError() : this("")
            {

            }
            public ChannelNotFoundError(string message) : base("The specified channel could not be found in the data container", message)
            {

            }
            #endregion
        }
        #endregion
    }
}