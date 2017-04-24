using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;
using HBP.Data.Experience.Dataset;
using System;

namespace HBP.Data.Visualisation
{
    /**
    * \class MultiPatientsVisualisation
    * \author Adrien Gannerie
    * \version 1.0
    * \date 11 janvier 2017
    * \brief Multi-patients visualisation.
    * 
    * \details Multi-patients visualisation is a class which herite from the Visualisation class and it's used for visualise some columns with some patients on the MNI brain.
    */
    [DataContract]
    public class MultiPatientsVisualisation : Visualisation
    {
        #region Properties
        public const string Extension = ".multiVisualisation";
        [DataMember(Name = "Patients",Order = 3)]
        private List<string> patientsID;
        /// <summary>
        /// Patients used in the Visualisation.
        /// </summary>
        public ReadOnlyCollection<Patient> Patients
        {
            get { return new ReadOnlyCollection<Patient>((from patient in ApplicationState.ProjectLoaded.Patients where patientsID.Contains(patient.ID) select patient).ToList()); }
        }
        public override bool IsVisualisable
        {
            get
            {
                // Initialize
                bool result = true;

                // Test
                if (Patients.Count > 0 && Columns.Count > 0)
                {
                    foreach (Column column in Columns)
                    {
                        if (!column.IsCompatible(Patients.ToArray()))
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
        /// Create a new multi-patients visualisation.
        /// </summary>
        /// <param name="name">Name of the visualisation.</param>
        /// <param name="columns">Columns used in the visualisation.</param>
        /// <param name="patients">Patients used in the vusualisation.</param>
        /// <param name="id">Unique ID.</param>
        public MultiPatientsVisualisation(string name,List<Column> columns, List<Patient> patients, string id) : base(name,columns,id)
        {
            SetPatients(patients.ToArray());
        }
        /// <summary>
        /// Create a new multi-patients visualisation.
        /// </summary>
        /// <param name="name">Name of the visualisation.</param>
        /// <param name="columns">Columns used in the visualisation.</param>
        /// <param name="patients">Patients used in the visualisation.</param>
        public MultiPatientsVisualisation(string name,List<Column> columns, List<Patient> patients) : base (name,columns)
        {
            SetPatients(patients.ToArray());
        }
        /// <summary>
        /// Create a new multi-patients visualisation with no patients inside.
        /// </summary>
        public MultiPatientsVisualisation() : base()
        {
            RemoveAllPatients();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Add a patient to the visualisation.
        /// </summary>
        /// <param name="patient">Patient to add.</param>
        public void AddPatient(Patient patient)
        {
            patientsID.Add(patient.ID);
        }
        /// <summary>
        /// Add patients to the visualisation.
        /// </summary>
        /// <param name="patients">Patients to add.</param>
        public void AddPatient(Patient[] patients)
        {
            foreach(Patient patient in patients)
            {
                AddPatient(patient);
            }
        }
        /// <summary>
        /// Remove a patient to the visualisation.
        /// </summary>
        /// <param name="patient">Patient to remove.</param>
        public void RemovePatient(Patient patient)
        {
            patientsID.Remove(patient.ID);
        }
        /// <summary>
        /// Remove patients to the visualisation.
        /// </summary>
        /// <param name="patients">Patients to remove.</param>
        public void RemovePatient(Patient[] patients)
        {
            foreach (Patient patient in patients)
            {
                RemovePatient(patient);
            }
        }
        /// <summary>
        /// Set patients in the visualisations.
        /// </summary>
        /// <param name="patients">Patients to set in the visualisation.</param>
        public void SetPatients(Patient[] patients)
        {
            patientsID = (from patient in patients select patient.ID).ToList();
        }
        /// <summary>
        /// Remove all patients in the visualisation.
        /// </summary>
        public void RemoveAllPatients()
        {
            patientsID = new List<string>();
        }
        /// <summary>
        /// Get the DataInfo used by the column.
        /// </summary>
        /// <param name="column">Column to get the dataInfo.</param>
        /// <returns>DataInfo used by the column.</returns>
        public override DataInfo[] GetDataInfo(Column column)
        {
            DataInfo[] dataInfo = (from data in column.Dataset.Data where (column.DataLabel == data.Name && Patients.Contains(data.Patient) && column.Protocol == data.Protocol && data.Protocol.Blocs.Contains(column.Bloc)) select data).ToArray();
            return dataInfo;
        }
        /// <summary>
        /// Get the DataInfo used by the column for a specific Patient.
        /// </summary>
        /// <param name="patient">Patient concerned.</param>
        /// <param name="column">Column concerned.</param>
        /// <returns>DataInfo used by the column for the specific Patient.</returns>
        public DataInfo GetDataInfo(Patient patient, Column column)
        {
            DataInfo[] dataInfo = (from data in column.Dataset.Data where (column.DataLabel == data.Name && data.Patient == patient && column.Protocol == data.Protocol && data.Protocol.Blocs.Contains(column.Bloc)) select data).ToArray();
            return dataInfo[0];
        }
        /// <summary>
        /// Get the implantation of the patients in the Visualisation.
        /// </summary>
        /// <returns>Plots label of the implantion sort by patients order.</returns>
        public string[] GetImplantation()
        {
            return (from patient in Patients select patient.Brain.MNIReferenceFrameImplantation).ToArray();
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone this multi-patients visualisation instance.
        /// </summary>
        /// <returns>Multi-patients visualisation clone.</returns>
        public override object Clone()
        {
            return new MultiPatientsVisualisation(Name, new List<Column>(Columns), new List<Patient>(Patients), ID);
        }
        /// <summary>
        /// Copy a multi-patients viusalisation instance to this instance.
        /// </summary>
        /// <param name="copy">Multi-patients visualisation instance to copy.</param>
        public override void Copy(object copy)
        {
            base.Copy(copy);
            MultiPatientsVisualisation visu = copy as MultiPatientsVisualisation;
            SetPatients(visu.Patients.ToArray());
        }
        #endregion
    }
}