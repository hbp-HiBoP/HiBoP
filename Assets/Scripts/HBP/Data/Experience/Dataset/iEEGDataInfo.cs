using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace HBP.Data.Experience.Dataset
{
    [DataContract]
    public class iEEGDataInfo : PatientDataInfo
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

        protected ErrorType[] m_iEEGErrors = new ErrorType[0];

        public override ErrorType[] Errors
        {
            get
            {
                List<ErrorType> errors = new List<ErrorType>(base.Errors);
                errors.AddRange(m_iEEGErrors);
                return errors.Distinct().ToArray();
            }
        }
        #endregion

        #region Constructors
        public  iEEGDataInfo(string name, DataContainer dataContainer, Patient patient, NormalizationType normalization, string id) : base(name, dataContainer, patient,id)
        {
            Normalization = normalization;
        }
        public iEEGDataInfo() : this("Data", new ElanDataContainer(), ApplicationState.ProjectLoaded.Patients.FirstOrDefault(), NormalizationType.Auto, Guid.NewGuid().ToString())
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
            return new iEEGDataInfo(Name, DataContainer, Patient, Normalization, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            iEEGDataInfo dataInfo = copy as iEEGDataInfo;
            Normalization = dataInfo.Normalization;
        }
        #endregion

        #region Public Methods
        public override ErrorType[] GetErrors(Protocol.Protocol protocol)
        {
            List<ErrorType> errors = new List<ErrorType>(base.GetErrors(protocol));
            errors.AddRange(GetiEEGErrors(protocol));
            return errors.Distinct().ToArray();
        }

        public virtual ErrorType[] GetiEEGErrors(Protocol.Protocol protocol)
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
            }
            m_iEEGErrors = errors.ToArray();
            return m_iEEGErrors;
        }
        #endregion

    }
}