using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace HBP.Data.Visualisation
{
    /**
    * \class MultiPatientsVisualisationData
    * \author Adrien Gannerie
    * \version 1.0
    * \date 11 janvier 2017
    * \brief Class which contains the multi-patients visualisation data.
    * 
    * \details Mutli-visualisation data is a class which contains the data of the multi-patients visualisation and herite from the VisualisationData class.
    */
    public class MultiPatientsVisualisationData : VisualisationData
    {
        #region Properties
        List<Patient> patients;
        /// <summary>
        /// Patients in the visualisation Data.
        /// </summary>
        public ReadOnlyCollection<Patient> Patients
        {
            get { return new ReadOnlyCollection<Patient>(patients); }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new multi-patients visualisations data instance.
        /// </summary>
        /// <param name="patients">Patients in the visualisation data.</param>
        /// <param name="columns">Columns in the visualisation data.</param>
        public MultiPatientsVisualisationData(Patient[] patients, ColumnData[] columns)  : base(columns)
        {
            this.patients = patients.ToList();
            List<Anatomy.PlotID> plotsID = new List<Anatomy.PlotID>();
            foreach(Patient patient in patients)
            {
                plotsID.AddRange(patient.GetImplantation(true, ApplicationState.GeneralSettings.PlotNameAutomaticCorrectionType == Settings.GeneralSettings.PlotNameCorrectionTypeEnum.Active));
            }
            PlotsID = plotsID;
        }
        /// <summary>
        /// Create a new multi-patients visualisations data instance with default values.
        /// </summary>
        public MultiPatientsVisualisationData() : this(new Patient[0], new ColumnData[0])
        {

        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Add patient to the multi-patients visualisation data.
        /// </summary>
        /// <param name="patient">Patient to add.</param>
        public void AddPatient(Patient patient)
        {
            patients.Add(patient);
        }
        /// <summary>
        /// Add patients to the multi-patients visualisation data.
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
        /// Remove patient to the multi-patients visualisation data.
        /// </summary>
        /// <param name="patient">Patient to remove.</param>
        public void RemovePatient(Patient patient)
        {
            patients.Remove(patient);
        }
        /// <summary>
        /// Remove patients to the multi-patients visualisation data.
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
        /// Get the implantation of the patients.
        /// </summary>
        /// <returns>Patients plots label sort by the patients order in the multi-patients visualisation data.</returns>
        public string[] GetImplantation()
        {
            return (from patient in patients select patient.Brain.MNIReferenceFrameImplantation).ToArray();
        }
        #endregion
    }
}