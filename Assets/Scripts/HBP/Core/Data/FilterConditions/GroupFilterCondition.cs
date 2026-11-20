using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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

                Group group = LoadedGroup;
                if (group == null)
                    return $"Group file '{Path.GetFileNameWithoutExtension(GroupFilePath)}' not loaded";

                int count = group.PatientsID.Count;
                string patientCount = count == 1 ? "1 patient" : $"{count} patients";
                return $"The patient {(IsNot ? "is not" : "is")} in group '{group.Name}' ({patientCount})";
            }
        }

        private string m_GroupFilePath;
        private Group m_LoadedGroup;

        [JsonProperty("GroupFilePath")]
        public string GroupFilePath
        {
            get => m_GroupFilePath;
            set
            {
                if (m_GroupFilePath == value)
                    return;

                m_GroupFilePath = value;
                m_LoadedGroup = null;
            }
        }

        /// <summary>
        /// The loaded group from the file path. Loads on first access.
        /// </summary>
        [JsonIgnore] public Group LoadedGroup
        {
            get
            {
                if (m_LoadedGroup == null && File.Exists(GroupFilePath))
                {
                    try
                    {
                        // Filters use the IDs from the file, including patients outside the current project.
                        m_LoadedGroup = Group.ReadFromFile(GroupFilePath, requireAllPatients: false);
                    }
                    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
                    {
                        // The filter description and editor already report that the file could not be loaded.
                        return null;
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
                List<string> patientIDs = LoadedGroup?.PatientsID;
                if (patientIDs == null || patientIDs.Count == 0)
                    return false;

                bool isInGroup = patientIDs.Contains(patient.ID);
                return IsNot ? !isInGroup : isInGroup;
            }

            return false;
        }

        #endregion
    }
}
