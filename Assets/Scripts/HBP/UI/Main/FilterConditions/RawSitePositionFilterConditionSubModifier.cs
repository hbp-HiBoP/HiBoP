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
    public class RawSitePositionFilterConditionSubModifier : SubModifier<RawSitePositionFilterCondition>
    {
        #region Properties

        [SerializeField] Dropdown m_AxisDropdown;
        [SerializeField] Dropdown m_ComparisonTypeDropdown;
        [SerializeField] InputField m_ValueInputField;
        [SerializeField] InputField m_MinInputField;
        [SerializeField] InputField m_MaxInputField;

        protected List<object> m_FilteringObjects;

        public List<object> FilteringObjects
        {
            get => m_FilteringObjects;
            set => m_FilteringObjects = value;
        }

        #endregion

        #region Public Methods

        public override void Initialize()
        {
            base.Initialize();

            m_AxisDropdown.onValueChanged.AddListener(OnChangeAxis);
            m_ComparisonTypeDropdown.onValueChanged.AddListener(OnChangeComparisonType);
            m_ValueInputField.onEndEdit.AddListener(OnValueChanged);
            m_MinInputField.onEndEdit.AddListener(OnMinChanged);
            m_MaxInputField.onEndEdit.AddListener(OnMaxChanged);
        }

        #endregion

        #region Private Methods

        protected override void SetFields(RawSitePositionFilterCondition objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            m_AxisDropdown.Set(typeof(RawSitePositionFilterCondition.AxisType), (int)objectToDisplay.Axis);
            m_ComparisonTypeDropdown.Set(typeof(NumberComparisonType), (int)objectToDisplay.ComparisonType);
            m_ValueInputField.text = objectToDisplay.Value.ToString(CultureInfo.InvariantCulture);
            m_MinInputField.text = objectToDisplay.Min.ToString(CultureInfo.InvariantCulture);
            m_MaxInputField.text = objectToDisplay.Max.ToString(CultureInfo.InvariantCulture);

            UpdateFieldVisibility(objectToDisplay.ComparisonType);
        }

        private void OnChangeAxis(int value)
        {
            Object.Axis = (RawSitePositionFilterCondition.AxisType)value;
        }

        private void OnChangeComparisonType(int value)
        {
            var type = (NumberComparisonType)value;
            Object.ComparisonType = type;
            UpdateFieldVisibility(type);
        }

        private void OnValueChanged(string value)
        {
            if (float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out float result))
            {
                Object.Value = result;
            }
        }

        private void OnMinChanged(string value)
        {
            if (float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out float result))
            {
                Object.Min = result;
            }
        }

        private void OnMaxChanged(string value)
        {
            if (float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out float result))
            {
                Object.Max = result;
            }
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
