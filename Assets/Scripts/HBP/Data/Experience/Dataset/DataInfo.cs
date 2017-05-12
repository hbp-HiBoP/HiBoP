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
        private string patientID;
        /// <summary>
        /// Patient who has passed the experiment.
        /// </summary>
        public Patient Patient
        {
            get { return ApplicationState.ProjectLoaded.Patients.FirstOrDefault(p => p.ID == patientID); }
            set { patientID = value.ID; }
        }

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
        private string protocolID;
        /// <summary>
        /// Protocol used during the experiment.
        /// </summary>
        public Protocol.Protocol Protocol
        {
            get { return ApplicationState.ProjectLoaded.Protocols.FirstOrDefault(p => p.ID == protocolID); }
            set { protocolID = value.ID; }
        }

        /// <summary>
        /// State of the DataInfo {OK, INCOMPLETE, ERROR}.
        /// </summary>
        public enum StateEnum
        {
            OK, INCOMPLETE, ERROR
        }
        /// <summary>
        /// Error type of the DataInfo.
        /// </summary>
        private enum ErrorTypeEnum
        {
            None, LabelEmpty, PatientEmpty, MeasureEmpty, EEGEmpty, POSEmpty, ProtocolEmpty, ImplantationEmpty, MNIImplantationEmpty,
            ImplantationFileNotExist, ImplantationFileIsNotAGoodFile, MNIImplantationFileNotExist, MNIImplantationFileIsNotAGoodFile, EEGFileNotExist, POSFileNotExist,
            EEGFileNotAGoodFile, POSFileNotAGoodFile, ChannelAndPlotDoNotMatch, ChannelAndMNIPLotDoNotMatch, MeasureNotFound, CantReadMNIImplantationFile, CantReadImplantationFile
        }

        private ErrorTypeEnum singlePatientError;
        /// <summary>
        /// Single Patient DataInfo state.
        /// </summary>
        public StateEnum SinglePatientState
        {
            get
            {
                return GetState(singlePatientError);
            }
        }

        private ErrorTypeEnum multiPatientsError;
        /// <summary>
        /// Multi Patients DataInfo state.
        /// </summary>
        public StateEnum MultiPatientsState
        {
            get
            {
                return GetState(multiPatientsError);
            }
        }

        /// <summary>
        /// Usable in single patient visualization.
        /// </summary>
        public bool UsableInSinglePatient
        {
            get { return SinglePatientState != StateEnum.ERROR; }
        }
        /// <summary>
        /// Usable in multi patients visualization.
        /// </summary>
        public bool UsableInMultiPatients
        {
            get { return MultiPatientsState != StateEnum.ERROR; }
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
            multiPatientsError = GetError(true);
            singlePatientError = GetError(false);
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
        /// <param name="MNI">\a True if Multi patients and \a false otherwise.</param>
        /// <returns>Error type.</returns>
        ErrorTypeEnum GetError(bool MNI)
        {
            ErrorTypeEnum l_errorType = ErrorTypeEnum.None;

            // Test if label field is empty.
            if (Name != string.Empty)
            {
                // Test if patient field is null.
                if (Patient != null)
                {
                    // Test if measure field is empty.
                    if (Measure != string.Empty)
                    {
                        // Test if eeg field is empty.
                        if (EEG != string.Empty)
                        {
                            // Test if pos field is empty.
                            if (POS != string.Empty)
                            {
                                // Test if protocol is null.
                                if (Protocol != null)
                                {
                                    l_errorType = ErrorTypeEnum.None;
                                    if (l_errorType == ErrorTypeEnum.None)
                                    {
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
                                                                        if(sites.Length != 0)
                                                                        {
                                                                            foreach(string plot in sites)
                                                                            {
                                                                                if(!channels.Contains(plot))
                                                                                {
                                                                                    l_errorType = ErrorTypeEnum.ChannelAndMNIPLotDoNotMatch;
                                                                                    additionnalMessage = "channel \"" + plot + "\"";
                                                                                    break;
                                                                                }
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            l_errorType = ErrorTypeEnum.CantReadMNIImplantationFile;
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        l_errorType = ErrorTypeEnum.MeasureNotFound;

                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    l_errorType = ErrorTypeEnum.EEGFileNotAGoodFile;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                l_errorType = ErrorTypeEnum.EEGFileNotExist;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            l_errorType = ErrorTypeEnum.POSFileNotAGoodFile;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        l_errorType = ErrorTypeEnum.POSFileNotExist;
                                                    }

                                                }
                                                else
                                                {
                                                    l_errorType = ErrorTypeEnum.MNIImplantationFileIsNotAGoodFile;
                                                }
                                            }
                                            else
                                            {
                                                l_errorType = ErrorTypeEnum.MNIImplantationFileNotExist;
                                            }
                                        }
                                        else
                                        {
                                            l_errorType = ErrorTypeEnum.MNIImplantationEmpty;
                                        }
                                    }
                                }
                                else
                                {
                                    l_errorType = ErrorTypeEnum.ProtocolEmpty;
                                }
                            }
                            else
                            {
                                l_errorType = ErrorTypeEnum.POSEmpty;
                            }
                        }
                        else
                        {
                            l_errorType = ErrorTypeEnum.EEGEmpty;
                        }
                    }
                    else
                    {
                        l_errorType = ErrorTypeEnum.MeasureEmpty;
                    }
                }
                else
                {
                    l_errorType = ErrorTypeEnum.PatientEmpty;
                }
            }
            else
            {
                l_errorType = ErrorTypeEnum.LabelEmpty;
            }
            return l_errorType;
        }
        /// <summary>
        /// Get Error Message.
        /// </summary>
        /// <param name="error">Type of the error.</param>
        /// <returns>Error message.</returns>
        string GetErrorMessage(ErrorTypeEnum error)
        {
            string l_errorMessage = string.Empty;
            switch (error)
            {
                case ErrorTypeEnum.None: l_errorMessage = "None error detected."; break;
                case ErrorTypeEnum.LabelEmpty: l_errorMessage = "The label field is empty."; break;
                case ErrorTypeEnum.PatientEmpty: l_errorMessage = "The patient field is empty."; break;
                case ErrorTypeEnum.MeasureEmpty: l_errorMessage = "The measure field is empty."; break;
                case ErrorTypeEnum.EEGEmpty: l_errorMessage = "The .eeg field is empty."; break;
                case ErrorTypeEnum.POSEmpty: l_errorMessage = "The .pos field is empty."; break;
                case ErrorTypeEnum.ImplantationEmpty: l_errorMessage = "The implantation path of the patient is empty."; break;
                case ErrorTypeEnum.MNIImplantationEmpty: l_errorMessage = "The MNI implantation path of the patient is empty."; break;
                case ErrorTypeEnum.ImplantationFileNotExist: l_errorMessage = "The implantation file does not  exist."; break;
                case ErrorTypeEnum.ImplantationFileIsNotAGoodFile: l_errorMessage = "The implantation file isn't a .pts file."; break;
                case ErrorTypeEnum.MNIImplantationFileNotExist: l_errorMessage = "The MNI implantation file does not exist."; break;
                case ErrorTypeEnum.MNIImplantationFileIsNotAGoodFile: l_errorMessage = "The MNI implantation file isn't a .pts file"; break;
                case ErrorTypeEnum.EEGFileNotExist: l_errorMessage = "The eeg file does not exist."; break;
                case ErrorTypeEnum.EEGFileNotAGoodFile: l_errorMessage = "The eeg file isn't a .eeg file."; break;
                case ErrorTypeEnum.POSFileNotExist : l_errorMessage = "The pos file does not exist."; break;
                case ErrorTypeEnum.POSFileNotAGoodFile: l_errorMessage = "The pos file isn't a .pos file."; break;
                case ErrorTypeEnum.ChannelAndPlotDoNotMatch: l_errorMessage = "The implantation and the channels do not match : " + additionnalMessage + " do not match with the implantation"; break;
                case ErrorTypeEnum.ChannelAndMNIPLotDoNotMatch: l_errorMessage = "The implantation and the channels do not match : " + additionnalMessage + " do not match witch the MNI implantation"; break;
                case ErrorTypeEnum.MeasureNotFound: l_errorMessage = "The measures in the eeg file and the measure field do not match"; break;
                case ErrorTypeEnum.CantReadMNIImplantationFile: l_errorMessage = "Can't read the MNI implantation file."; break;
            }
            return l_errorMessage;
        }
        /// <summary>
        /// Get state of the dataInfo.
        /// </summary>
        /// <param name="error">Type of the error.</param>
        /// <returns>State enum.</returns>
        StateEnum GetState(ErrorTypeEnum error)
        {
            if(error == ErrorTypeEnum.None)
            {
                return StateEnum.OK;
            }
            else if((error == ErrorTypeEnum.ChannelAndMNIPLotDoNotMatch) || (error == ErrorTypeEnum.ChannelAndPlotDoNotMatch))
            {
                return StateEnum.INCOMPLETE;
            }
            else
            {
                return StateEnum.ERROR;
            }
        }
        #endregion
    }
}