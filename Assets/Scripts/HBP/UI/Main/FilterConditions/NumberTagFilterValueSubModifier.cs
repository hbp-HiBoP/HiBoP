using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.UI.Tools;
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
            m_ValueInputField.onEndEdit.AddListener(value => Object.Value = float.Parse(value));
            m_MinInputField.onEndEdit.AddListener(value => Object.Min = float.Parse(value));
            m_MaxInputField.onEndEdit.AddListener(value => Object.Max = float.Parse(value));
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(NumberTagFilterValue objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            m_TypeDropdown.Set(typeof(NumberTagFilterValue.ComparisonType), 0);
            m_ValueInputField.text = objectToDisplay.Value.ToString();
            m_MinInputField.text = objectToDisplay.Min.ToString();
            m_MaxInputField.text = objectToDisplay.Max.ToString();
        }
        private void OnChangeType(int value)
        {
            var type = (NumberTagFilterValue.ComparisonType)value;
            Object.Type = type;

            m_ValueInputField.transform.parent.gameObject.SetActive(type != NumberTagFilterValue.ComparisonType.Range);
            m_MinInputField.transform.parent.gameObject.SetActive(type == NumberTagFilterValue.ComparisonType.Range);
            m_MaxInputField.transform.parent.gameObject.SetActive(type == NumberTagFilterValue.ComparisonType.Range);
        }
        #endregion
    }
}