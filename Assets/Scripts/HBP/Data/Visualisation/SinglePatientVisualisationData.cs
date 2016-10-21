using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace HBP.Data.Visualisation
{
    public class SinglePatientVisualisationData : VisualisationData
    {
        #region Properties
        Patient.Patient patient;
        public Patient.Patient Patient
        {
            get { return patient; }
            set { patient = value; }
        }
        #endregion

        #region Constructors
        public SinglePatientVisualisationData(Patient.Patient patient, List<ColumnData> columns)  : base(columns)
        {
            Patient = patient;
        }
        public SinglePatientVisualisationData() : this(new Data.Patient.Patient(),new List<ColumnData>())
        {

        }
        #endregion

        #region Public Methods
        public string GetImplantation()
        {
            return Patient.Brain.PatientBasedImplantation;
        }
        public static SinglePatientVisualisationData LoadFromMultiPatients(MultiPatientsVisualisationData multiPatientsVisualisationData,int patientID)
        {
            return new SinglePatientVisualisationData(multiPatientsVisualisationData.Patients[patientID], multiPatientsVisualisationData.Columns.ToList());
        }        
        #endregion
    }
}