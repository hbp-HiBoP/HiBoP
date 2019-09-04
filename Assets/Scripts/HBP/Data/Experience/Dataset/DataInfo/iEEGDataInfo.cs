using HBP.Errors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace HBP.Data.Experience.Dataset
{
    [DataContract, DisplayName("iEEG")]
    public class iEEGDataInfo : PatientDataInfo, IEpochable
    {
        #region Properties
        /// <summary>
        /// Normalization Type.
        /// </summary>
        public enum NormalizationType
        {
            None, SubTrial, Trial, SubBloc, Bloc, Protocol, Auto
        }

        [DataMember(Name = "Normalization")]
        /// <summary>
        /// Normalization of the Data.
        /// </summary>
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
        public iEEGDataInfo(string name, Container.DataContainer dataContainer, Patient patient, NormalizationType normalization, string id) : base(name, dataContainer, patient,id)
        {
            Normalization = normalization;
        }
        public iEEGDataInfo() : this("Data", new Container.Elan(), ApplicationState.ProjectLoaded.Patients.FirstOrDefault(), NormalizationType.Auto, Guid.NewGuid().ToString())
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
            return new iEEGDataInfo(Name, DataContainer.Clone() as Container.DataContainer, Patient, Normalization, ID);
        }
        public override void Copy(object obj)
        {
            base.Copy(obj);
            if(obj is iEEGDataInfo iEEGdataInfo)
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
                    files = new string[] { edfDataContainer.Path };
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
                else
                {
                    throw new Exception("Invalid data container type");
                }
                Tools.CSharp.EEG.File file = new Tools.CSharp.EEG.File(type, false, files);
                List<Tools.CSharp.EEG.Trigger> triggers = file.Triggers;
                if (!protocol.Blocs.All(bloc => bloc.MainSubBloc.MainEvent.Codes.Any(code => triggers.Any(t => t.Code == code))))
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