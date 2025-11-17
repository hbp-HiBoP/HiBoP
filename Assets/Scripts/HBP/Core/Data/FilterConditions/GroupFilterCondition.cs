using HBP.Core.Data;
using Newtonsoft.Json;
using System.ComponentModel;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), Preserve, DisplayName("Group file"), SortingOrder(7), FilterCondition(typeof(Patient))]
    public class GroupFilterCondition : BaseFilterCondition
    {
        #region Properties
        public override string Description 
        { 
            get 
            {
                if (string.IsNullOrEmpty(GroupFilePath))
                    return "No group file selected";

                if (LoadedGroup == null)
                    return $"Group file '{Path.GetFileNameWithoutExtension(GroupFilePath)}' not loaded";

                string patientCount = LoadedGroup.PatientsID.Count == 1 ? "1 patient" : $"{LoadedGroup.PatientsID.Count} patients";
                return $"The patient {(IsNot ? "is not" : "is")} in group '{LoadedGroup.Name}' ({patientCount})";
            }
        }

        [JsonProperty("GroupFilePath")] public string GroupFilePath { get; set; }

        [JsonIgnore] private Group m_LoadedGroup;

        /// <summary>
        /// The loaded group from the file path. Loads on first access.
        /// </summary>
        [JsonIgnore] public Group LoadedGroup
        {
            get
            {
                if (m_LoadedGroup == null && !string.IsNullOrEmpty(GroupFilePath))
                {
                    if (File.Exists(GroupFilePath) && Group.LoadFromFile(GroupFilePath, out Group loadedGroup))
                    {
                        m_LoadedGroup = loadedGroup;
                    }
                }
                return m_LoadedGroup;
            }
        }
        #endregion

        #region Constructors
        public GroupFilterCondition() : this("", false)
        {
        }
        public GroupFilterCondition(string groupFilePath, bool isNot) : base(isNot)
        {
            GroupFilePath = groupFilePath;
        }
        public GroupFilterCondition(string groupFilePath, bool isNot, string ID) : base(isNot, ID)
        {
            GroupFilePath = groupFilePath;
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new GroupFilterCondition(GroupFilePath, IsNot, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is GroupFilterCondition groupFilterCondition)
            {
                GroupFilePath = groupFilterCondition.GroupFilePath;
                m_LoadedGroup = null;
            }
        }
        #endregion

        #region Public Methods
        public override bool Check(object obj)
        {
            if (obj is Patient patient)
            {
                var group = LoadedGroup;
                if (group == null || group.PatientsID == null || group.PatientsID.Count == 0)
                    return false;

                bool isInGroup = group.PatientsID.Contains(patient.ID);
                return IsNot ? !isInGroup : isInGroup;
            }
            return false;
        }
        #endregion
    }
}