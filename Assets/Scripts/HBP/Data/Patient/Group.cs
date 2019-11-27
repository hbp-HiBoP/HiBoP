using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Tools.Unity;
using System.IO;

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
    public class Group : BaseData, ILoadable<Group>, INameable
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
        [DataMember(Name = "Patients",Order = 3)] List<string> m_PatientsID = new List<string>();
        /// <summary>
        /// Patients of the group.
        /// </summary>
        public List<Patient> Patients { get; set; }
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
            Patients = patients.ToList();
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="HBP.Data.Group">Group</see> class.
        /// </summary>
        /// <param name="name">Name of the group.</param>
        /// <param name="patients">Patients of the group.</param>
        public Group(string name, IEnumerable<Patient> patients): base()
        {
            Name = name;
            Patients = patients.ToList();
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="HBP.Data.Group">Group</see> class.
        /// </summary>
        public Group() : this("New Group",new Patient[0])
		{
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
                Patients = group.Patients.ToList();
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

        #region Serialization
        protected override void OnSerializing()
        {
            base.OnSerializing();
            m_PatientsID = Patients.Select(p => p.ID).ToList();
        }
        protected override void OnDeserialized()
        {
            base.OnDeserialized();
            Patients = m_PatientsID.Select(id => ApplicationState.ProjectLoaded.Patients.FirstOrDefault(p => p.ID == id)).ToList();
        }
        #endregion
    }
}