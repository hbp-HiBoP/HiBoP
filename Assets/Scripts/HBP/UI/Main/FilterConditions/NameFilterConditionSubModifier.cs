using HBP.Core.Data;
using HBP.UI.Tools;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class NameFilterConditionSubModifier : SubModifier<NameFilterCondition>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
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
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onValueChanged.AddListener(OnChangeName);
            m_ExactMatchToggle.onValueChanged.AddListener(OnChangeExactMatch);
            m_CaseSensitiveToggle.onValueChanged.AddListener(OnChangeCaseSensitive);
        }
        #endregion

        #region Private Methods
        protected override void SetFields(NameFilterCondition objectToDisplay)
        {
            base.SetFields(objectToDisplay);
            m_NameInputField.text = objectToDisplay.Name;
            m_ExactMatchToggle.isOn = objectToDisplay.ExactMatch;
            m_CaseSensitiveToggle.isOn = objectToDisplay.CaseSensitive;
        }
        void OnChangeName(string value)
        {
            Object.Name = value;
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