using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace HBP.Data.Visualisation
{
    public class MultiPatientsVisualisationData : VisualisationData
    {
        #region Properties
        List<Patient.Patient> patients;
        public ReadOnlyCollection<Patient.Patient> Patients
        {
            get { return new ReadOnlyCollection<Patient.Patient>(patients); }
        }
        #endregion

        #region Constructors
        public MultiPatientsVisualisationData(List<Patient.Patient> patients, List<ColumnData> columns)  : base(columns)
        {
            this.patients = patients;
        }
        public MultiPatientsVisualisationData() : this(new List<Patient.Patient>(), new List<ColumnData>())
        {

        }
        #endregion

        #region Public Methods
        public void AddPatient(Patient.Patient patient)
        {
            patients.Add(patient);
        }
        public void AddPatient(Patient.Patient[] patients)
        {
            foreach(Patient.Patient patient in patients)
            {
                AddPatient(patient);
            }
        }
        public void RemovePatient(Patient.Patient patient)
        {
            patients.Remove(patient);
        }
        public void RemovePatient(Patient.Patient[] patients)
        {
            foreach (Patient.Patient patient in patients)
            {
                RemovePatient(patient);
            }
        }
        public string[] GetImplantation()
        {
            return (from patient in patients select patient.Brain.MNIBasedImplantation).ToArray();
        }
        #endregion
    }
}