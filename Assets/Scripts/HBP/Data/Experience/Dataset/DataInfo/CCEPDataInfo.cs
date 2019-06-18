using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace HBP.Data.Experience.Dataset
{
    [DataContract]
    public class CCEPDataInfo : PatientDataInfo
    {
        #region Properties
        [DataMember] public string Channel { get; set; }

        protected ErrorType[] m_CCEPErrors = new ErrorType[0];
        public override ErrorType[] Errors
        {
            get
            {
                List<ErrorType> errors = new List<ErrorType>(base.Errors);
                errors.AddRange(m_CCEPErrors);
                return errors.Distinct().ToArray();
            }
        }
        #endregion

        #region Contructors
        public CCEPDataInfo(string name, DataContainer dataContainer, Patient patient, string channel, string id) : base(name, dataContainer, patient, id)
        {
            Channel = channel;
        }
        public CCEPDataInfo() : this("Data", new ElanDataContainer(), ApplicationState.ProjectLoaded.Patients.FirstOrDefault(), "", Guid.NewGuid().ToString())
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
            return new CCEPDataInfo(Name, DataContainer, Patient, Channel, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            CCEPDataInfo dataInfo = copy as CCEPDataInfo;
            Channel = dataInfo.Channel;
        }
        #endregion

        #region Public Methods
        public override ErrorType[] GetErrors(Protocol.Protocol protocol)
        {
            List<ErrorType> errors = new List<ErrorType>(base.GetErrors(protocol));
            errors.AddRange(GetCCEPErrors(protocol));
            return errors.Distinct().ToArray();
        }
        public virtual ErrorType[] GetCCEPErrors(Protocol.Protocol protocol)
        {
            List<ErrorType> errors = new List<ErrorType>();
            if (m_DataContainer.IsOk)
            {
                Tools.CSharp.EEG.File file = new Tools.CSharp.EEG.File(m_DataContainer.Type, false, m_DataContainer.DataFilesPaths);
                List<Tools.CSharp.EEG.Trigger> triggers = file.Triggers;
                if (!protocol.Blocs.All(bloc => bloc.MainSubBloc.MainEvent.Codes.Any(code => triggers.Any(t => t.Code == code))))
                {
                    errors.Add(ErrorType.BlocsCantBeEpoched);
                }
                List<Tools.CSharp.EEG.Electrode> electrodes = file.Electrodes;
                if (electrodes.All(e => e.Label != Channel))
                {
                    errors.Add(ErrorType.ChannelNotFound);
                }
            }
            m_CCEPErrors = errors.ToArray();
            return m_CCEPErrors;
        }
        #endregion
    }
}