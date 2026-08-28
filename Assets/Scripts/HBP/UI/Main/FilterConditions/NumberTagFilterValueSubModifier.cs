using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Tools;
using HBP.UI.Tools;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class NumberTagFilterValueSubModifier : SubModifier<NumberTagFilterValue>
    {
        #region Properties

        [SerializeField] Dropdown m_TypeDropdown;
        [SerializeField] InputField m_ValueInputField;
        [SerializeField] InputField m_MinInputField;
        [SerializeField] InputField m_MaxInputField;

        #endregion

        #region Public Methods

        public override void Initialize()
        {
            base.Initialize();

            m_TypeDropdown.onValueChanged.AddListener(OnChangeType);
            m_ValueInputField.onEndEdit.AddListener(value => Object.Value = float.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture));
            m_MinInputField.onEndEdit.AddListener(value => Object.Min = float.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture));
            m_MaxInputField.onEndEdit.AddListener(value => Object.Max = float.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture));
        }

        #endregion

        #region Protected Methods

        protected override void SetFields(NumberTagFilterValue objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            m_TypeDropdown.Set(typeof(NumberComparisonType), (int)objectToDisplay.Type);
            m_ValueInputField.text = objectToDisplay.Value.ToString();
            m_MinInputField.text = objectToDisplay.Min.ToString();
            m_MaxInputField.text = objectToDisplay.Max.ToString();
        }

        private void OnChangeType(int value)
        {
            var type = (NumberComparisonType)value;
            Object.Type = type;

            m_ValueInputField.transform.parent.gameObject.SetActive(type != NumberComparisonType.Range);
            m_MinInputField.transform.parent.gameObject.SetActive(type == NumberComparisonType.Range);
            m_MaxInputField.transform.parent.gameObject.SetActive(type == NumberComparisonType.Range);
        }

        #endregion
    }
}
