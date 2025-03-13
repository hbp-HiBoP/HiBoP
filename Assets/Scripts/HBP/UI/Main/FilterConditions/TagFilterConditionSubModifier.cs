using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.Data.Preferences;
using HBP.UI.Tools;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class TagFilterConditionSubModifier : SubModifier<TagFilterCondition>
    {
        #region Properties
        [SerializeField] Dropdown m_TargetDropdown;
        [SerializeField] Dropdown m_TagDropdown;
        [SerializeField] InputField m_ValueInputField;
        [SerializeField] Toggle m_ExactMatchToggle;
        [SerializeField] Toggle m_CaseSensitiveToggle;

        protected List<BaseData> m_FilteringObjects;
        public List<BaseData> FilteringObjects
        {
            get => m_FilteringObjects;
            set
            {
                m_FilteringObjects = value;
            }
        }

        private List<BaseTag> m_Tags = new();
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();

            m_TargetDropdown.onValueChanged.AddListener(OnChangeTarget);
            m_TagDropdown.onValueChanged.AddListener(OnChangeTag);
            m_ValueInputField.onValueChanged.AddListener(OnChangeName);
            m_ExactMatchToggle.onValueChanged.AddListener(OnChangeExactMatch);
            m_CaseSensitiveToggle.onValueChanged.AddListener(OnChangeCaseSensitive);
        }
        #endregion

        #region Private Methods
        protected override void SetFields(TagFilterCondition objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            m_TargetDropdown.SetValue((int)objectToDisplay.Target);

            var currentTag = m_Tags.FirstOrDefault(t => t == objectToDisplay.Tag);
            m_TagDropdown.SetValue(currentTag != null ? m_Tags.IndexOf(currentTag) : 0);

            m_ValueInputField.text = objectToDisplay.Value;
            m_ExactMatchToggle.isOn = objectToDisplay.ExactMatch;
            m_CaseSensitiveToggle.isOn = objectToDisplay.CaseSensitive;
        }
        void OnChangeTarget(int value)
        {
            Object.Target = (TagFilterCondition.TargetType)value;
            m_Tags = Object.Target switch
            {
                TagFilterCondition.TargetType.Patient => PersistentDataManager.Tags.GeneralTags.Concat(PersistentDataManager.Tags.PatientsTags).ToList(),
                TagFilterCondition.TargetType.Site => PersistentDataManager.Tags.GeneralTags.Concat(PersistentDataManager.Tags.SitesTags).ToList(),
                _ => PersistentDataManager.Tags.GeneralTags.ToList(),
            };
            m_TagDropdown.options = m_Tags.Select(t => new Dropdown.OptionData(t.Name)).ToList();
            m_TagDropdown.SetValue(0);
        }
        void OnChangeTag(int value)
        {
            Object.Tag = m_Tags[value];
        }
        void OnChangeName(string value)
        {
            Object.Value = value;
        }
        void OnChangeExactMatch(bool value)
        {
            Object.ExactMatch = value;
        }
        void OnChangeCaseSensitive(bool value)
        {
            Object.CaseSensitive = value;
        }
        #endregion
    }
}