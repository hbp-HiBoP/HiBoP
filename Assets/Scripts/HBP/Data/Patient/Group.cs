﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Tools.Unity;
using System.IO;
using Tools.CSharp;
using System.Collections.ObjectModel;

namespace HBP.Data
{
    /// <summary>
    /// Contains all the data about a group of patients.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><b>Name</b></term>
    /// <description>Name of the group.</description>
    /// </item>
    /// <item>
    /// <term><b>Patients</b></term>
    /// <description>Patients of the group.</description>
    /// </item>
    /// </list>
    /// </remarks>
    [DataContract]
    public class Group : BaseData, ILoadable<Group>
    {
        #region Properties
        /// <summary>
        /// Extension of group files.
        /// </summary>
        public const string EXTENSION = ".group";
        /// <summary>
        /// <description>Name of the group.</description>
        /// </summary>
        [DataMember] public string Name { get; set; }
        /// <summary>
        /// IDs of the patients of the group.
        /// </summary>
        [DataMember(Name = "Patients",Order = 3)] List<string> m_Patients;
        /// <summary>
        /// Patients of the group.
        /// </summary>
        [IgnoreDataMember] public ReadOnlyCollection<Patient> Patients
        {
            get { return new ReadOnlyCollection<Patient>((from patient in ApplicationState.ProjectLoaded.Patients where m_Patients.Contains(patient.ID) select patient).ToArray()); }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="HBP.Data.Group">Group</see> class.
        /// </summary>
        /// <param name="name">Name of the group.</param>
        /// <param name="patients">Patients of the group.</param>
        /// <param name="id">Unique identifier to identify the group.</param>
        public Group(string name, IEnumerable<Patient> patients, string id) : base (id)
        {
            Name = name;
            Add(patients);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="HBP.Data.Group">Group</see> class.
        /// </summary>
        /// <param name="name">Name of the group.</param>
        /// <param name="patients">Patients of the group.</param>
        public Group(string name, IEnumerable<Patient> patients): base()
        {
            Name = name;
            Add(patients);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="HBP.Data.Group">Group</see> class.
        /// </summary>
        public Group() : this("New Group",new Patient[0])
		{
		}
        #endregion

        #region Public Methods
        public void Clear()
        {
            m_Patients.Clear();
        }
        /// <summary>
        /// Adds a patient to the group.
        /// </summary>
        /// <param name="patient">Patient to add.</param>
        public bool Add(Patient patient)
        {
            return m_Patients.AddIfAbsent(patient.ID);
        }
        /// <summary>
        /// Adds patients to the group.
        /// </summary>
        /// <param name="patient">Patients to add.</param>
        public bool Add(IEnumerable<Patient> patients)
        {
            bool result = true;
            foreach (Patient patient in patients)
            {
                result &= Add(patient);
            }
            return result;
        }
        /// <summary>
        /// Removes patient to the group.
        /// </summary>
        /// <param name="patient">Patient to remove.</param>
        public bool Remove(Patient patient)
        {
            return m_Patients.Remove(patient.ID);
        }
        /// <summary>
        /// Removes patients to the group.
        /// </summary>
        /// <param name="patient">Patients to remove.</param>
        public bool Remove(IEnumerable<Patient> patients)
        {
            bool result = true;
            foreach (Patient patient in patients)
            {
                result &= Remove(patient);
            }
            return result;
        }
        #endregion

        #region Public Static Methods
        /// <summary>
        /// Gets the extension of the group files.
        /// </summary>
        /// <returns></returns>
        public static string GetExtension()
        {
            return EXTENSION[0] == '.' ? EXTENSION.Substring(1) : EXTENSION;
        }
        /// <summary>
        /// Loads group from group file.
        /// </summary>
        /// <param name="path">The specified path of the group file.</param>
        /// <param name="result">The group in the group file.</param>
        /// <returns><see langword="true"/> if the method worked successfully; otherwise, <see langword="false"/></returns>
        public static bool LoadFromFile(string path, out Group result)
        {
            try
            {
                result = ClassLoaderSaver.LoadFromJson<Group>(path);
                if (result != null) return true;
                else return false;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                throw new CanNotReadGroupFileException(Path.GetFileNameWithoutExtension(path));
            }
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone the instance.
        /// </summary>
        /// <returns>object cloned.</returns>
        public override object Clone()
        {
            return new Group(Name, Patients, ID);
        }
        /// <summary>
        /// Copy the instance.
        /// </summary>
        /// <param name="obj">instance to copy.</param>
        public override void Copy(object obj)
        {
            base.Copy(obj);
            if(obj is Group group)
            {
                Name = group.Name;
                Add(group.Patients);
            }
        }
        #endregion

        #region Interfaces
        /// <summary>
        /// Gets the extension of the group files.
        /// </summary>
        /// <returns></returns>
        string ILoadable<Group>.GetExtension()
        {
            return GetExtension();
        }
        /// <summary>
        /// Loads group from group file.
        /// </summary>
        /// <param name="path">The specified path of the group file.</param>
        /// <param name="result">The group in the group file.</param>
        /// <returns><see langword="true"/> if the method worked successfully; otherwise, <see langword="false"/></returns>
        bool ILoadable<Group>.LoadFromFile(string path, out Group result)
        {
            return LoadFromFile(path, out result);
        }
        #endregion
    }
}