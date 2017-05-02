using System.Linq;
using System.Collections.Generic;
using HBP.Data.Experience.Dataset;
using System.Runtime.Serialization;
using System;

namespace HBP.Data.Visualisation
{
    /**
    * \class SinglePatientVisualisation
    * \author Adrien Gannerie
    * \version 1.0
    * \date 11 janvier 2017
    * \brief Class which define a visualisation with the single patient.
    * 
    * \details Single patient visualisation is a class which contains the informations of a single patient visualisation and herite from the Visualisation class.
    */
    [DataContract]
    public class SinglePatientVisualisation : Visualisation
    {
        #region Properties
        public const string Extension = ".singleVisualisation";
        [DataMember(Name = "Patient",Order = 3)]
        private string patientID = string.Empty;
        /// <summary>
        /// Patient of the single patient visualisation.
        /// </summary>
        public Patient Patient
        {
            get
            {
                Patient tmp = ApplicationState.ProjectLoaded.Patients.FirstOrDefault(p => p.ID == patientID);
                if (tmp == null) tmp = new Patient();
                return tmp;
            }
            set { patientID = value.ID; }
        }
        public override bool IsVisualisable
        {
            get
            {
                // Initialize
                bool result = true;

                // Test
                if (Patient != null && Columns.Count > 0)
                {
                    foreach (Column column in Columns)
                    {
                        if (!column.IsCompatible(Patient))
                        {
                            result = false;
                            break;
                        }
                    }
                }
                else
                {
                    result = false;
                }
                return result;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new single patient visualisation instance.
        /// </summary>
        /// <param name="name">Name of single patient visualisation.</param>
        /// <param name="columns">Columns of single patient visualisation.</param>
        /// <param name="patient">Patient of the single patient visualisation.</param>
        /// <param name="id">Unique ID of the single patient visualisation.</param>
        public SinglePatientVisualisation(string name,IEnumerable<Column> columns, Patient patient, string id) : base(name,columns,id)
        {
            Patient = patient;
        }
        /// <summary>
        /// Create a new single patient visualisation instance.
        /// </summary>
        /// <param name="name">Name of single patient visualisation.</param>
        /// <param name="columns">Columns of single patient visualisation.</param>
        /// <param name="patient">Patient of the single patient visualisation.</param>
        public SinglePatientVisualisation(string name,IEnumerable<Column> columns,Patient patient) : base (name,columns)
        {
            Patient = patient;
        }
        /// <summary>
        /// Create a new single patient visualisation instance with default value.
        /// </summary>
        public SinglePatientVisualisation() : base()
        {
            Patient = new Patient();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Get the DataInfo used by the column.
        /// </summary>
        /// <param name="column">Column to get the dataInfo.</param>
        /// <returns>DataInfo used by the column.</returns>
        public override DataInfo[] GetDataInfo(Column column)
        {
            DataInfo[] dataInfo = (from data in column.Dataset.Data where (column.DataLabel == data.Name && Patient == data.Patient && column.Protocol == data.Protocol && data.Protocol.Blocs.Contains(column.Bloc)) select data).ToArray();
            return dataInfo;
        }
        /// <summary>
        /// Get the implantation of the patients in the Visualisation.
        /// </summary>
        /// <returns>Plots label of the implantion sort by patients order.</returns>
        public string GetImplantation()
        {
            return Patient.Brain.PatientReferenceFrameImplantation;
        }
        /// <summary>
        /// Load a single patient visualisation from a multi-patients visualisation.
        /// </summary>
        /// <param name="multiPatientsVisualisation">Multi patients visualisation.</param>
        /// <param name="patient">Patient of the single patient visualisation.</param>
        /// <returns>Single patient visualisation loaded from the multi-patients visualisation.</returns>
        public static SinglePatientVisualisation LoadFromMultiPatients(MultiPatientsVisualisation multiPatientsVisualisation,int patient)
        {
            return new SinglePatientVisualisation(multiPatientsVisualisation.Name, multiPatientsVisualisation.Columns, multiPatientsVisualisation.Patients[patient]);
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone this single patient visualisation instance.
        /// </summary>
        /// <returns>Single patient visualisation clone.</returns>
        public override object Clone()
        {
            return new SinglePatientVisualisation(Name.Clone() as string, from column in Columns select column.Clone() as Column, Patient);
        }
        /// <summary>
        /// Copy a single patient viusalisation instance to this instance.
        /// </summary>
        /// <param name="copy">Single patient visualisation instance to copy.</param>
        public override void Copy(object copy)
        {
            base.Copy(copy);
            Patient = (copy as SinglePatientVisualisation).Patient;
        }
        #endregion
    }
}