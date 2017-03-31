using System.Linq;


namespace HBP.Data.Visualisation
{
    /**
    * \class SinglePatientVisualisationData
    * \author Adrien Gannerie
    * \version 1.0
    * \date 11 janvier 2017
    * \brief Class which contains the single patient visualisation data.
    * 
    * \details Mutli-visualisation data is a class which contains the data of the multi-patients visualisation and herite from the VisualisationData class.
    */
    public class SinglePatientVisualisationData : VisualisationData
    {
        #region Properties
        /// <summary>
        /// Patient of the single patient visualisation data.
        /// </summary>
        public Patient Patient { get; set; }

        #endregion

        #region Constructors
        /// <summary>
        ///  Create a new single patient visualisation data instance.
        /// </summary>
        /// <param name="patient">Patient used in the single patient visualisation data.</param>
        /// <param name="columns">Columns used in the single patient visaulisation data.</param>
        public SinglePatientVisualisationData(Patient patient, ColumnData[] columns) : base(columns)
        {
            Patient = patient;
            PlotsID = patient.GetImplantation(false, ApplicationState.GeneralSettings.PlotNameAutomaticCorrectionType == Settings.GeneralSettings.PlotNameCorrectionTypeEnum.Active).ToList();
        }
        /// <summary>
        /// Create a new single patient visualisation data instance with default values.
        /// </summary>
        public SinglePatientVisualisationData() : this(new Patient(),new ColumnData[0])
        {

        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Get the implantation of the patients.
        /// </summary>
        /// <returns>Patients plots label sort by the patients order in the single patient visualisation data.</returns>
        public string GetImplantation()
        {
            return Patient.Brain.PatientReferenceFrameImplantation;
        }
        /// <summary>
        /// Load single patient visualisation data from multi-patients visualisation data.
        /// </summary>
        /// <param name="multiPatientsVisualisationData">Multi-patients visualisation data.</param>
        /// <param name="patientID">Index of the patient in the patients in the multi-patients visualisation.</param>
        /// <returns></returns>
        public static SinglePatientVisualisationData LoadFromMultiPatients(MultiPatientsVisualisationData multiPatientsVisualisationData,int patientID)
        {
            return new SinglePatientVisualisationData(multiPatientsVisualisationData.Patients[patientID], multiPatientsVisualisationData.Columns.ToArray());
        }        
        #endregion
    }
}