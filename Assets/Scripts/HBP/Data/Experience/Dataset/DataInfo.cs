using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace HBP.Data.Experience.Dataset
{
    /**
    * \class DataInfo
    * \author Adrien Gannerie
    * \version 1.0
    * \date 09 janvier 2017
    * \brief DataInfo.
    * 
    * \details Class which define a DataInfo which contains : 
    *     - Name.
    *     - Patient.
    *     - Measure.
    *     - EEG file.
    *     - POS file.
    *     - Protocol.
    */
    [DataContract]
    public class DataInfo : ICloneable
    {
        #region Properties
        [DataMember]
        /// <summary>
        /// Name of the data.
        /// </summary>
        public string Name { get; set; }

        [DataMember(Name = "Patient")]
        private string m_PatientID;
        /// <summary>
        /// Patient who has passed the experiment.
        /// </summary>
        public Patient Patient { get; set; }

        [DataMember]
        /// <summary>
        /// Name of the measure in the EEG file : "EGG data" by default.
        /// </summary>
        public string Measure { get; set; }

        [DataMember]
        /// <summary>
        /// Path of the EEG file.
        /// </summary>
        public string EEG { get; set; }

        [DataMember]
        /// <summary>
        /// Path of the POS file.
        /// </summary>
        public string POS { get; set; }

        [DataMember(Name = "Protocol")]
        private string m_ProtocolID;
        /// <summary>
        /// Protocol used during the experiment.
        /// </summary>
        public Protocol.Protocol Protocol { get; set; }

        /// <summary>
        /// State of the DataInfo {OK, INCOMPLETE, ERROR}.
        /// </summary>
        public enum State
        {
            OK, INCOMPLETE, ERROR
        }
        /// <summary>
        /// Error type of the DataInfo.
        /// </summary>
        private enum Error
        {
            LabelEmpty, PatientEmpty, PatientNotVisualizable, MeasureEmpty, EEGEmpty, POSEmpty, ProtocolEmpty, ImplantationEmpty, MNIImplantationEmpty,
            ImplantationFileNotExist, ImplantationFileIsNotAGoodFile, MNIImplantationFileNotExist, MNIImplantationFileIsNotAGoodFile, EEGFileNotExist, POSFileNotExist,
            EEGFileNotAGoodFile, POSFileNotAGoodFile, ChannelAndPlotDoNotMatch, ChannelAndMNIPLotDoNotMatch, MeasureNotFound, CantReadMNIImplantationFile, CantReadImplantationFile
        }

        private Error[] ErrorTypes;
        public State[] States
        {
            get
            {
                return GetState(singlePatientError);
            }
        }

        private string additionnalMessage;
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new DataInfo instance.
        /// </summary>
        /// <param name="name">Name of the dataInfo.</param>
        /// <param name="patient">Patient who passed the experiment.</param>
        /// <param name="measure">Name of the measure in the EEG file.</param>
        /// <param name="eeg">EEG file path.</param>
        /// <param name="pos">POS file path.</param>
        /// <param name="protocol">Protocol used for the experiment.</param>
        public DataInfo(string name, Patient patient, string measure, string eeg, string pos, Protocol.Protocol protocol)
        {
            Name = name;
            Patient = patient;
            Measure = measure;
            EEG = eeg;
            POS = pos;
            Protocol = protocol;
        }
        /// <summary>
        /// Create a new DataInfo instance with default value.
        /// </summary>
        public DataInfo() : this(string.Empty,new Patient(),"EEG data", string.Empty, string.Empty,new Protocol.Protocol())
        {
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Update states.
        /// </summary>
        public void UpdateStates()
        {
            Enum.GetValues(Anatomy.ReferenceFrameType)
            multiPatientsError = GetError(true);
            singlePatientError = GetError(false);
        }
        public bool IsUsable(Anatomy.ReferenceFrameType referenceFrame)
        {

        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Clone of this instance.</returns>
        public object Clone()
        {
            return new DataInfo(Name.Clone() as string, Patient.Clone() as Patient, Measure.Clone() as string, EEG.Clone() as string, POS.Clone() as string, Protocol.Clone() as Protocol.Protocol);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Get type of the error.
        /// </summary>
        /// <param name="referenceFrame">\a True if Multi patients and \a false otherwise.</param>
        /// <returns>Error type.</returns>
        Error[] GetErrors(Anatomy.ReferenceFrameType referenceFrame)
        {
            List<Error> errors = new List<Error>();
            if (string.IsNullOrEmpty(Name)) errors.Add(Error.LabelEmpty);
            if (Patient == null) errors.Add(Error.PatientEmpty);
            if (string.IsNullOrEmpty(Measure)) errors.Add(Error.MeasureEmpty);
            if (string.IsNullOrEmpty(EEG)) errors.Add(Error.EEGEmpty);
            if (string.IsNullOrEmpty(POS)) errors.Add(Error.POSEmpty);
            if (Protocol == null) errors.Add(Error.ProtocolEmpty);
            if (!Patient.Brain.IsVisualizable(referenceFrame)) errors.Add(Error.PatientNotVisualizable);
                //Test if MNI implantation is empty.
                if (Patient.Brain.MNIBasedImplantation != string.Empty)
                {
                    // Create the file info MNI implantation.
                    FileInfo l_MNIimplantation = new FileInfo(Patient.Brain.MNIBasedImplantation);

                    //Test if the MNI implantation file exist.
                    if (l_MNIimplantation.Exists)
                    {
                        // Test if the MNI implantation file is a implantation file.
                        if (l_MNIimplantation.Extension == Anatomy.Implantation.EXTENSION)
                        {
                            // Create the file info POS
                            FileInfo l_pos = new FileInfo(POS);

                            // Test if the POS file exist.
                            if (l_pos.Exists)
                            {
                                // Test if the POS file is a pos file.
                                if (l_pos.Extension == Localizer.POS.EXTENSION)
                                {
                                    // Create the file info EEG.
                                    FileInfo l_eeg = new FileInfo(EEG);

                                    // Test if the EEG file exist.
                                    if (l_eeg.Exists)
                                    {
                                        // Test if the EEG path is a eeg file.
                                        if (l_eeg.Extension == Elan.EEG.EXTENSION)
                                        {
                                            // Read EEG File.
                                            Elan.ElanFile elanFile = new Elan.ElanFile(EEG);
                                            // Test EEG Measure
                                            if (elanFile.MeasureLabels.Contains(Measure))
                                            {
                                                // FIXME
                                                List<string> sitesList = new List<string>();
                                                foreach (Anatomy.Electrode electrode in Patient.Brain.Implantation.Electrodes)
                                                {
                                                    foreach (Anatomy.Site site in electrode.Sites)
                                                    {
                                                        sitesList.Add(site.Name);
                                                    }
                                                }
                                                string[] sites = sitesList.ToArray();
                                                //string[] sites = Patient.Brain.GetImplantation(MNI, ApplicationState.GeneralSettings.PlotNameAutomaticCorrectionType == Settings.GeneralSettings.PlotNameCorrectionTypeEnum.Active).GetPlotsName();
                                                string[] channels = (from channel in elanFile.Channels select channel.Label).ToArray();
                                                if (sites.Length != 0)
                                                {
                                                    foreach (string plot in sites)
                                                    {
                                                        if (!channels.Contains(plot))
                                                        {
                                                            error = Error.ChannelAndMNIPLotDoNotMatch;
                                                            additionnalMessage = "channel \"" + plot + "\"";
                                                            break;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    error = Error.CantReadMNIImplantationFile;
                                                }
                                            }
                                            else
                                            {
                                                error = Error.MeasureNotFound;

                                            }
                                        }
                                        else
                                        {
                                            error = Error.EEGFileNotAGoodFile;
                                        }
                                    }
                                    else
                                    {
                                        error = Error.EEGFileNotExist;
                                    }
                                }
                                else
                                {
                                    error = Error.POSFileNotAGoodFile;
                                }
                            }
                            else
                            {
                                error = Error.POSFileNotExist;
                            }

                        }
                        else
                        {
                            error = Error.MNIImplantationFileIsNotAGoodFile;
                        }
                    }
                    else
                    {
                        error = Error.MNIImplantationFileNotExist;
                    }
            }
            return error;
        }
        /// <summary>
        /// Get state of the dataInfo.
        /// </summary>
        /// <param name="error">Type of the error.</param>
        /// <returns>State enum.</returns>
        State GetState(Error error)
        {
            if(error == Error.None)
            {
                return State.OK;
            }
            else if(error == Error.ChannelAndPlotDoNotMatch)
            {
                return State.INCOMPLETE;
            }
            else
            {
                return State.ERROR;
            }
        }
        ///// <summary>
        ///// Get Error Message.
        ///// </summary>
        ///// <param name="error">Type of the error.</param>
        ///// <returns>Error message.</returns>
        //string GetErrorMessage(ErrorType error)
        //{
        //    string l_errorMessage = string.Empty;
        //    switch (error)
        //    {
        //        case ErrorType.None: l_errorMessage = "None error detected."; break;
        //        case ErrorType.LabelEmpty: l_errorMessage = "The label field is empty."; break;
        //        case ErrorType.PatientEmpty: l_errorMessage = "The patient field is empty."; break;
        //        case ErrorType.MeasureEmpty: l_errorMessage = "The measure field is empty."; break;
        //        case ErrorType.EEGEmpty: l_errorMessage = "The .eeg field is empty."; break;
        //        case ErrorType.POSEmpty: l_errorMessage = "The .pos field is empty."; break;
        //        case ErrorType.ImplantationEmpty: l_errorMessage = "The implantation path of the patient is empty."; break;
        //        case ErrorType.MNIImplantationEmpty: l_errorMessage = "The MNI implantation path of the patient is empty."; break;
        //        case ErrorType.ImplantationFileNotExist: l_errorMessage = "The implantation file does not  exist."; break;
        //        case ErrorType.ImplantationFileIsNotAGoodFile: l_errorMessage = "The implantation file isn't a .pts file."; break;
        //        case ErrorType.MNIImplantationFileNotExist: l_errorMessage = "The MNI implantation file does not exist."; break;
        //        case ErrorType.MNIImplantationFileIsNotAGoodFile: l_errorMessage = "The MNI implantation file isn't a .pts file"; break;
        //        case ErrorType.EEGFileNotExist: l_errorMessage = "The eeg file does not exist."; break;
        //        case ErrorType.EEGFileNotAGoodFile: l_errorMessage = "The eeg file isn't a .eeg file."; break;
        //        case ErrorType.POSFileNotExist : l_errorMessage = "The pos file does not exist."; break;
        //        case ErrorType.POSFileNotAGoodFile: l_errorMessage = "The pos file isn't a .pos file."; break;
        //        case ErrorType.ChannelAndPlotDoNotMatch: l_errorMessage = "The implantation and the channels do not match : " + additionnalMessage + " do not match with the implantation"; break;
        //        case ErrorType.ChannelAndMNIPLotDoNotMatch: l_errorMessage = "The implantation and the channels do not match : " + additionnalMessage + " do not match witch the MNI implantation"; break;
        //        case ErrorType.MeasureNotFound: l_errorMessage = "The measures in the eeg file and the measure field do not match"; break;
        //        case ErrorType.CantReadMNIImplantationFile: l_errorMessage = "Can't read the MNI implantation file."; break;
        //    }
        //    return l_errorMessage;
        //}
        #endregion

        #region Serialization
        [OnSerializing]
        void OnSerializing()
        {
            m_PatientID = Patient.ID;
            m_ProtocolID = Protocol.ID;
        }
        [OnDeserialized]
        void OnDeserialized()
        {
            Patient = ApplicationState.ProjectLoaded.Patients.FirstOrDefault(p => p.ID == m_PatientID);
            Protocol = ApplicationState.ProjectLoaded.Protocols.FirstOrDefault(p => p.ID == m_ProtocolID);
        }
        #endregion
    }
}