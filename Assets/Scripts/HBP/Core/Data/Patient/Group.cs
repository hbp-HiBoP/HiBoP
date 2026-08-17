using HBP.Core.Exceptions;
using HBP.Core.Interfaces;
using HBP.Core.Tools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.Scripting;

namespace HBP.Core.Data
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
    [JsonObject(MemberSerialization.OptIn), Preserve]
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
        [JsonProperty] public string Name { get; set; }

        /// <summary>
        /// IDs of the patients of the group.
        /// </summary>
        [JsonProperty("Patients", Order = 3)] List<string> m_PatientsID = new();

        /// <summary>
        /// Patients of the group.
        /// </summary>
        private List<Patient> m_Patients = new();

        public List<Patient> Patients
        {
            get => m_Patients;
            set
            {
                m_Patients = value?.Where(p => p != null).ToList() ?? new List<Patient>();
                m_PatientsID = m_Patients.Where(p => !string.IsNullOrEmpty(p.ID)).Select(p => p.ID).ToList();
            }
        }

        /// <summary>
        /// IDs of the patients of the group (read-only access).
        /// </summary>
        public List<string> PatientsID => m_PatientsID?.ToList() ?? new List<string>();

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="HBP.Data.Group">Group</see> class.
        /// </summary>
        /// <param name="name">Name of the group.</param>
        /// <param name="patients">Patients of the group.</param>
        /// <param name="id">Unique identifier to identify the group.</param>
        public Group(string name, IEnumerable<Patient> patients, string id) : base(id)
        {
            Name = name;
            Patients = patients.ToList();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HBP.Data.Group">Group</see> class.
        /// </summary>
        /// <param name="name">Name of the group.</param>
        /// <param name="patients">Patients of the group.</param>
        public Group(string name, IEnumerable<Patient> patients) : base()
        {
            Name = name;
            Patients = patients.ToList();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HBP.Data.Group">Group</see> class.
        /// </summary>
        public Group() : this("New Group", new Patient[0])
        {
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Gets the extension of the group files.
        /// </summary>
        /// <returns></returns>
        public static string[] GetExtensions()
        {
            return new string[] { EXTENSION[0] == '.' ? EXTENSION.Substring(1) : EXTENSION };
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
                if (result == null)
                {
                    return false;
                }

                Project project = ApplicationState.LoadedProject;
                IEnumerable<Patient> patients = project != null ? project.Patients : Array.Empty<Patient>();
                LoadingContext context = new(Array.Empty<BaseTag>(), Array.Empty<Protocol>(), patients);
                context.ResolveProject(patients, new[] { result }, Array.Empty<Dataset>(), Array.Empty<Visualization>());
                return true;
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
            if (obj is Group group)
            {
                Name = group.Name;
                Patients = group.Patients.ToList();
            }
        }

        #endregion

        #region Public Methods

        internal void ResolveReferences(LoadingContext context)
        {
            ResolvePatientReferences(context, true);
        }

        internal void ResolvePatientReferences(LoadingContext context, bool required)
        {
            m_Patients = (m_PatientsID ?? new List<string>()).Select(id => required ? context.ResolveRequired(context.PatientById, id, "patient", $"Group '{ID}'") : context.ResolveOptional(context.PatientById, id)).Where(patient => patient != null).ToList();
        }

        #endregion

        #region Interfaces

        /// <summary>
        /// Gets the extension of the group files.
        /// </summary>
        /// <returns></returns>
        string[] ILoadable<Group>.GetExtensions()
        {
            return GetExtensions();
        }

        /// <summary>
        /// Loads group from group file.
        /// </summary>
        /// <param name="path">The specified path of the group file.</param>
        /// <param name="result">The group in the group file.</param>
        /// <returns><see langword="true"/> if the method worked successfully; otherwise, <see langword="false"/></returns>
        bool ILoadable<Group>.LoadFromFile(string path, out Group[] result)
        {
            bool success = LoadFromFile(path, out Group group);
            result = new Group[] { group };
            return success;
        }

        #endregion

        #region Serialization

        protected override void OnSerializing()
        {
            base.OnSerializing();
            m_Patients = m_Patients?.Where(p => p != null).ToList() ?? new List<Patient>();
            m_PatientsID = m_Patients.Where(p => !string.IsNullOrEmpty(p.ID)).Select(p => p.ID).ToList();
        }

        protected override void OnDeserialized()
        {
            base.OnDeserialized();
            m_PatientsID ??= new List<string>();
            m_Patients = new List<Patient>();
        }

        #endregion
    }
}
