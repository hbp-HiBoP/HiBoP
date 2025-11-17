using HBP.Core.Data;
using HBP.UI.Tools;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class GroupFilterConditionSubModifier : SubModifier<GroupFilterCondition>
    {
        #region Properties
        protected List<object> m_FilteringObjects;
        public List<object> FilteringObjects
        {
            get => m_FilteringObjects;
            set
            {
                m_FilteringObjects = value;
            }
        }

        [SerializeField] FileSelector m_GroupFileSelector;
        [SerializeField] Text m_GroupInfoText;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                m_GroupFileSelector.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            
            // Configure the file selector for .group files
            m_GroupFileSelector.Extension = Group.GetExtensions()[0]; // "group"
            m_GroupFileSelector.Message = "Select a group file";
            
            // Listen for file selection changes
            m_GroupFileSelector.onValueChanged.AddListener(OnGroupFileChanged);
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(GroupFilterCondition objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            // Set the file path in the selector
            m_GroupFileSelector.File = objectToDisplay.GroupFilePath ?? "";
            
            // Update the group info display
            UpdateGroupInfo();
        }
        #endregion

        #region Private Methods
        private void OnGroupFileChanged(string filePath)
        {
            if (Object != null)
            {
                Object.GroupFilePath = filePath;
                UpdateGroupInfo();
            }
        }

        private void UpdateGroupInfo()
        {
            if (Object == null || string.IsNullOrEmpty(Object.GroupFilePath))
            {
                m_GroupInfoText.text = "No group file selected";
                return;
            }

            if (!File.Exists(Object.GroupFilePath))
            {
                m_GroupInfoText.text = $"File not found: {Path.GetFileName(Object.GroupFilePath)}";
                return;
            }

            var loadedGroup = Object.LoadedGroup;
            if (loadedGroup == null)
            {
                m_GroupInfoText.text = $"Failed to load: {Path.GetFileName(Object.GroupFilePath)}";
                return;
            }

            string patientCount = loadedGroup.PatientsID.Count == 1 ? "1 patient" : $"{loadedGroup.PatientsID.Count} patients";
            m_GroupInfoText.text = $"Group: {loadedGroup.Name} ({patientCount})";
        }
        #endregion
    }
}