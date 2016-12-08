using UnityEngine;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using HBP.Data.Patient;

namespace HBP.Data.Experience.Dataset
{
    [Serializable]
    public class DataInfo : ICloneable
    {
        #region Properties
        [SerializeField]
        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        [SerializeField]
        private string patientID;
        public string PatientID
        {
            get { return patientID; }
            set { patientID = value; }
        }
        public Patient.Patient Patient
        {
            get { return ApplicationState.ProjectLoaded.Patients.FirstOrDefault(p => p.ID == patientID); }
            set { patientID = value.ID; }
        }

        [SerializeField]
        private string measure;
        public string Measure
        {
            get { return measure; }
            set { measure = value; }
        }

        [SerializeField]
        private string eeg;
        public string EEG
        {
            get { return eeg; }
            set { eeg = value; }
        }

        [SerializeField]
        private string pos;
        public string POS
        {
            get { return pos; }
            set { pos = value; }
        }

        [SerializeField]
        private string protocolID;
        public Protocol.Protocol Protocol
        {
            get { return ApplicationState.ProjectLoaded.Protocols.FirstOrDefault(p => p.ID == protocolID); }
            set { protocolID = value.ID; }
        }

        public enum StateEnum
        {
            OK, INCOMPLETE, ERROR
        }
        private enum ErrorTypeEnum
        {
            None, LabelEmpty, PatientEmpty, MeasureEmpty, EEGEmpty, POSEmpty, ProtocolEmpty, ImplantationEmpty, MNIImplantationEmpty,
            ImplantationFileNotExist, ImplantationFileIsNotAGoodFile, MNIImplantationFileNotExist, MNIImplantationFileIsNotAGoodFile, EEGFileNotExist, POSFileNotExist,
            EEGFileNotAGoodFile, POSFileNotAGoodFile, ChannelAndPlotDoNotMatch, ChannelAndMNIPLotDoNotMatch, MeasureNotFound, CantReadMNIImplantationFile, CantReadImplantationFile
        }

        private ErrorTypeEnum singlePatientError;
        public StateEnum SinglePatientState
        {
            get
            {
                return GetState(singlePatientError);
            }
        }

        private ErrorTypeEnum multiPatientsError;
        public StateEnum MultiPatientsState
        {
            get
            {
                return GetState(multiPatientsError);
            }
        }

        public bool UsableInSinglePatient
        {
            get { return SinglePatientState != StateEnum.ERROR; }
        }
        public bool UsableInMultiPatients
        {
            get { return MultiPatientsState != StateEnum.ERROR; }
        }
        private string additionnalMessage;

        #endregion

        #region Constructors
        public DataInfo(string name, Patient.Patient patient, string measure, string eeg, string pos, Protocol.Protocol protocol)
        {
            Name = name;
            Patient = patient;
            Measure = measure;
            EEG = eeg;
            POS = pos;
            Protocol = protocol;
        }
        public DataInfo() : this(string.Empty,new Patient.Patient(),"EEG data", string.Empty, string.Empty,new Protocol.Protocol())
        {
        }
        #endregion

        #region Public Methods
        public void UpdateStates()
        {
            singlePatientError = GetErrorSP();
            multiPatientsError = GetErrorMP();
        }
        #endregion

        #region Operators
        public object Clone()
        {
            return new DataInfo(Name.Clone() as string, Patient.Clone() as Patient.Patient, Measure.Clone() as string, EEG.Clone() as string, POS.Clone() as string, Protocol.Clone() as Protocol.Protocol);
        }
        #endregion

        #region Private Methods
        ErrorTypeEnum GetError()
        {
            ErrorTypeEnum l_errorType = ErrorTypeEnum.None;

            // Test if label field is empty.
            if (name != string.Empty)
            {
                // Test if patient field is null.
                if (Patient != null)
                {
                    // Test if measure field is empty.
                    if (measure != string.Empty)
                    {
                        // Test if eeg field is empty.
                        if (eeg != string.Empty)
                        {
                            // Test if pos field is empty.
                            if (pos != string.Empty)
                            {
                                // Test if protocol is null.
                                if (Protocol != null)
                                {
                                    l_errorType = ErrorTypeEnum.None;
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
        ErrorTypeEnum GetErrorMP()
        {
            ErrorTypeEnum l_errorType = GetError();

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
                        if (l_MNIimplantation.Extension == Settings.FileExtension.Implantation)
                        {
                            // Create the file info POS
                            FileInfo l_pos = new FileInfo(pos);

                            // Test if the POS file exist.
                            if (l_pos.Exists)
                            {
                                // Test if the POS file is a pos file.
                                if (l_pos.Extension == Settings.FileExtension.POS)
                                {
                                    // Create the file info EEG.
                                    FileInfo l_eeg = new FileInfo(eeg);

                                    // Test if the EEG file exist.
                                    if (l_eeg.Exists)
                                    {
                                        // Test if the EEG path is a eeg file.
                                        if (l_eeg.Extension == Localizer.EEG.Extension)
                                        {
                                            // Read EEG File.
                                            Localizer.EEG l_EEG = new Localizer.EEG(EEG);

                                            // Test EEG Measure
                                            bool l_measureFound = false;
                                            foreach (Localizer.Measure measure in l_EEG.Header.Measures)
                                            {
                                                if (measure.Label == this.measure)
                                                {
                                                    l_measureFound = true;
                                                    break;
                                                }
                                            }
                                            if (!l_measureFound)
                                            {
                                                l_errorType = ErrorTypeEnum.MeasureNotFound;
                                            }

                                            // Read MNI Patient PTS file.
                                            Electrode[] l_MNIelectrodes = Electrode.readImplantationFile(Patient.Brain.MNIBasedImplantation);
                                            if(l_MNIelectrodes.Length != 0 && l_MNIelectrodes[0] != null && l_MNIelectrodes[0].Plots != null)
                                            {
                                                List<Plot> l_MNIplots = new List<Plot>(l_MNIelectrodes.Length * l_MNIelectrodes[0].Plots.Count);
                                                foreach (Electrode electrode in l_MNIelectrodes)
                                                {
                                                    l_MNIplots.AddRange(electrode.Plots);
                                                }

                                                // Test

                                                foreach (Localizer.Channel channel in l_EEG.Header.Channels)
                                                {
                                                    // With MNI PTS.
                                                    bool l_MNIFound = false;
                                                    foreach (Plot plot in l_MNIplots)
                                                    {
                                                        if (plot.Name == channel.Label)
                                                        {
                                                            l_MNIFound = true;
                                                        }
                                                    }
                                                    if (!l_MNIFound)
                                                    {
                                                        l_errorType = ErrorTypeEnum.ChannelAndMNIPLotDoNotMatch;
                                                        additionnalMessage = "channel \"" + channel.Label + "\"";
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
            return l_errorType;
        }
        ErrorTypeEnum GetErrorSP()
        {
            ErrorTypeEnum l_errorType = GetError();

            if (l_errorType == ErrorTypeEnum.None)
            {
                //Test if Implantation is empty.
                if (Patient.Brain.PatientBasedImplantation != string.Empty)
                {
                    //Create the file info implantation.
                    FileInfo l_implantation = new FileInfo(Patient.Brain.PatientBasedImplantation);

                    //Test if the implantation file exist.
                    if (l_implantation.Exists)
                    {
                        //Test if the implantatin file really is a implantation file.
                        if (l_implantation.Extension == Settings.FileExtension.Implantation)
                        {
                            // Create the file info POS
                            FileInfo l_pos = new FileInfo(pos);

                            // Test if the POS file exist.
                            if (l_pos.Exists)
                            {
                                // Test if the POS file is a pos file.
                                if (l_pos.Extension == Settings.FileExtension.POS)
                                {
                                    // Create the file info EEG.
                                    FileInfo l_eeg = new FileInfo(eeg);

                                    // Test if the EEG file exist.
                                    if (l_eeg.Exists)
                                    {
                                        // Test if the EEG path is a eeg file.
                                        if (l_eeg.Extension == Localizer.EEG.Extension)
                                        {
                                            // Read EEG File.
                                            Localizer.EEG l_EEG = new Localizer.EEG(EEG);

                                            // Test EEG Measure
                                            bool l_measureFound = false;
                                            foreach (Localizer.Measure measure in l_EEG.Header.Measures)
                                            {
                                                if (measure.Label == this.measure)
                                                {
                                                    l_measureFound = true;
                                                    break;
                                                }
                                            }
                                            if (!l_measureFound)
                                            {
                                                l_errorType = ErrorTypeEnum.MeasureNotFound;
                                            }

                                            // Read Single Patient PTS file.
                                            Electrode[] l_electrodes = Electrode.readImplantationFile(Patient.Brain.PatientBasedImplantation);
                                            if (l_electrodes.Length != 0 && l_electrodes[0] != null && l_electrodes[0].Plots != null)
                                            {
                                                List<Plot> l_plots = new List<Plot>(l_electrodes.Length * l_electrodes[0].Plots.Count);
                                                foreach (Electrode electrode in l_electrodes)
                                                {
                                                    l_plots.AddRange(electrode.Plots);
                                                }

                                                // Test
                                                foreach (Localizer.Channel channel in l_EEG.Header.Channels)
                                                {
                                                    // With single PTS.
                                                    bool l_singleFound = false;
                                                    foreach (Plot plot in l_plots)
                                                    {
                                                        if (plot.Name == channel.Label)
                                                        {
                                                            l_singleFound = true;
                                                        }
                                                    }

                                                    if (!l_singleFound)
                                                    {
                                                        l_errorType = ErrorTypeEnum.ChannelAndPlotDoNotMatch;
                                                        additionnalMessage = "channel \"" + channel.Label + "\"";
                                                        break;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                l_errorType = ErrorTypeEnum.CantReadImplantationFile;
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
                            l_errorType = ErrorTypeEnum.ImplantationFileIsNotAGoodFile;
                        }
                    }
                    else
                    {
                        l_errorType = ErrorTypeEnum.ImplantationFileNotExist;
                    }
                }
                else
                {
                    l_errorType = ErrorTypeEnum.ImplantationEmpty;
                }
            }
            return l_errorType;
        }
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