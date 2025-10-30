using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Tools;
using HBP.UI.Tools;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class ActivityFilterConditionSubModifier : SubModifier<ActivityFilterCondition>
    {
        #region Properties
        [SerializeField] Dropdown m_MeasureTypeDropdown;
        [SerializeField] Dropdown m_ComparisonTypeDropdown;
        [SerializeField] InputField m_ValueInputField;
        [SerializeField] InputField m_MinInputField;
        [SerializeField] InputField m_MaxInputField;

        protected List<object> m_FilteringObjects;
        public List<object> FilteringObjects
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

            m_MeasureTypeDropdown.onValueChanged.AddListener(OnChangeMeasureType);
            m_ComparisonTypeDropdown.onValueChanged.AddListener(OnChangeComparisonType);
            m_ValueInputField.onEndEdit.AddListener(value => Object.Value = float.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture));
            m_MinInputField.onEndEdit.AddListener(value => Object.Min = float.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture));
            m_MaxInputField.onEndEdit.AddListener(value => Object.Max = float.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture));
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(ActivityFilterCondition objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            m_MeasureTypeDropdown.Set(typeof(MeasureType), (int)objectToDisplay.MeasureType);
            m_ComparisonTypeDropdown.Set(typeof(NumberComparisonType), (int)objectToDisplay.ComparisonType);
            m_ValueInputField.text = objectToDisplay.Value.ToString();
            m_MinInputField.text = objectToDisplay.Min.ToString();
            m_MaxInputField.text = objectToDisplay.Max.ToString();

            UpdateFieldVisibility(objectToDisplay.ComparisonType);
        }

        private void OnChangeMeasureType(int value)
        {
            Object.MeasureType = (MeasureType)value;
        }

        private void OnChangeComparisonType(int value)
        {
            var type = (NumberComparisonType)value;
            Object.ComparisonType = type;
            UpdateFieldVisibility(type);
        }

        private void UpdateFieldVisibility(NumberComparisonType type)
        {
            m_ValueInputField.transform.parent.gameObject.SetActive(type != NumberComparisonType.Range);
            m_MinInputField.transform.parent.gameObject.SetActive(type == NumberComparisonType.Range);
            m_MaxInputField.transform.parent.gameObject.SetActive(type == NumberComparisonType.Range);
        }
        #endregion
    }
}