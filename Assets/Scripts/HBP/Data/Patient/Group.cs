using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Tools.Unity;
using System.IO;

namespace HBP.Data
{
    /**
    * \class Group
    * \author Adrien Gannerie
    * \version 1.0
    * \date 04 janvier 2017
    * \brief Patients group.
    * 
    * \details Group which contains:
    *   - \a Name.
    *   - \a ID.
    *   - \a Patients.
    */
    [DataContract]
    public class Group : ICloneable , ICopiable, ILoadable, IIdentifiable
    {
        #region Properties
        public const string EXTENSION = ".group";
        /** Unique ID. */
        [DataMember]
        public string ID { get; set; }
        /** Group name.*/
        [DataMember]
        public string Name { get; set; }
        /** Patients. */
        [DataMember(Name = "Patients",Order = 3)]
        private List<string> patientsID;
        public List<Patient> Patients
        {
            get { return (from patient in ApplicationState.ProjectLoaded.Patients where patientsID.Contains(patient.ID) select patient).ToList(); }
            set { patientsID = (from patient in value select patient.ID).ToList(); }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Construct a new group instance.
        /// </summary>
        /// <param name="name">\a Name of the group.</param>
        /// <param name="patientsInTheGroup">\a Patients in the group.</param>
        /// <param name="id">\a ID of the group.</param>
        public Group(string name, Patient[] patientsInTheGroup,string id)
        {
            Name = name;
            Patients = patientsInTheGroup.ToList();
            ID = id;
        }
        /// <summary>
        /// Construct a new group instance with a unique ID.
        /// </summary>
        /// <param name="name">\a Name of the group.</param>
        /// <param name="patientsInTheGroup">\a Patients in the group.</param>
        public Group(string name, Patient[] patientsInTheGroup): this(name,patientsInTheGroup,Guid.NewGuid().ToString())
        {
        }
        /// <summary>
        /// Construct a new group instance with a default name and no patients.
        /// </summary>
        public Group() : this("New Group",new Patient[0])
		{
		}
		#endregion

		#region Public Methods
        public void Load(string path)
        {
            Group result;
            try
            {
                result = ClassLoaderSaver.LoadFromJson<Group>(path);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                throw new CanNotReadGroupFileException(Path.GetFileNameWithoutExtension(path));
            }
            Copy(result);
        }
        public string GetExtension()
        {
            return EXTENSION[0] == '.' ? EXTENSION.Substring(1) : EXTENSION;
        }
        /// <summary>
        /// Add patient.
        /// </summary>
        /// <param name="patient">\a Patient to add.</param>
        public void AddPatient(Patient patient)
        {
            if(!patientsID.Contains(patient.ID))
            {
                patientsID.Add(patient.ID);
            }
        }
        /// <summary>
        /// Add patients.
        /// </summary>
        /// <param name="patients">\a Patients to add.</param>
        public void AddPatient(Patient[] patients)
        {
            foreach (Patient patient in patients) AddPatient(patient);
        }
        /// <summary>
        /// Remove patient. 
        /// </summary>
        /// <param name="patient">\a Patient to remove.</param>
        public void RemovePatient(Patient patient)
        {
            patientsID.Remove(patient.ID);
        }
        /// <summary>
        /// Remove patients.
        /// </summary>
        /// <param name="patients">\a Patients to remove.</param>
        public void RemovePatient(Patient[] patients)
        {
            foreach (Patient patient in patients) RemovePatient(patient);
        }
		#endregion

        #region Operator
        /// <summary>
        /// Clone the instance.
        /// </summary>
        /// <returns>object cloned.</returns>
        public object Clone()
        {
            return new Group(Name, Patients.ToArray().Clone() as Patient[],ID);
        }
        /// <summary>
        /// Operator Equals.
        /// </summary>
        /// <param name="obj">Object to test.</param>
        /// <returns>\a True if equals and \a false otherwise.</returns>
        public override bool Equals(object obj)
        {
            Group p = obj as Group;
            if (p == null)
            {
                return false;
            }
            else
            {
                return ID == p.ID;
            }
        }
        /// <summary>
        /// Get hash code.
        /// </summary>
        /// <returns>HashCode.</returns>
        public override int GetHashCode()
        {
            return this.ID.GetHashCode();
        }
        /// <summary>
        /// Copy the instance.
        /// </summary>
        /// <param name="copy">instance to copy.</param>
        public void Copy(object copy)
        {
            Group group = copy as Group;
            Name = group.Name;
            Patients = group.Patients.ToList();
            ID = group.ID;
        }
        /// <summary>
        /// Operator equals.
        /// </summary>
        /// <param name="a">First group to compare.</param>
        /// <param name="b">Second group to compare.</param>
        /// <returns>\a True if equals and \a false otherwise.</returns>
        public static bool operator ==(Group a, Group b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            return a.Equals(b);
        }
        /// <summary>
        /// Operator not equals.
        /// </summary>
        /// <param name="a">First group to compare.</param>
        /// <param name="b">Second group to compare.</param>
        /// <returns>\a True if not equals and \a false otherwise.</returns>
        public static bool operator !=(Group a, Group b)
        {
            return !(a == b);
        }
        #endregion
    }
}