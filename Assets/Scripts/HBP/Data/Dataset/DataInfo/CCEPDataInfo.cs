using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using HBP.Errors;

namespace HBP.Data.Experience.Dataset
{
    [DataContract, DisplayName("CCEP")]
    public class CCEPDataInfo : PatientDataInfo, IEpochable
    {
        #region Properties
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
        #endregion

        #region Constructors
        public CCEPDataInfo(string name, Container.DataContainer dataContainer, Patient patient, string channel, string ID) : base(name, dataContainer, patient, ID)
        {
            StimulatedChannel = channel;
        }
        public CCEPDataInfo(string name, Container.DataContainer dataContainer, Patient patient, string channel) : base(name, dataContainer, patient)
        {
            StimulatedChannel = channel;
        }
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
        public override Error[] GetErrors(Protocol.Protocol protocol)
        {
            List<Error> errors = new List<Error>(base.GetErrors(protocol));
            errors.AddRange(GetCCEPErrors(protocol));
            return errors.Distinct().ToArray();
        }
        public virtual Error[] GetCCEPErrors(Protocol.Protocol protocol)
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
                if (protocol.IsVisualizable && !protocol.Blocs.All(bloc => bloc.MainSubBloc.MainEvent.Codes.Any(code => triggers.Any(t => t.Code == code))))
                {
                    errors.Add(new BlocsCantBeEpochedError());
                }
                List<Tools.CSharp.EEG.Electrode> electrodes = file.Electrodes;
                if (electrodes.All(e => e.Label != StimulatedChannel))
                {
                    errors.Add(new ChannelNotFoundError());
                }
            }
            m_CCEPErrors = errors.ToArray();
            return m_CCEPErrors;
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
            public ChannelNotFoundError(string message): base("The specified channel could not be found in the data container", message)
            {

            }
            #endregion
        }
        #endregion
    }
}