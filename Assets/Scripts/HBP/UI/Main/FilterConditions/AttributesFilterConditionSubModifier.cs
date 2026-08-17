using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.UI.Tools;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class AttributesFilterConditionSubModifier : SubModifier<AttributesFilterCondition>
    {
        #region Properties

        [SerializeField] Dropdown m_TypeDropdown;
        [SerializeField] GameObject m_LabelValueField;
        [SerializeField] InputField m_LabelValueInputField;
        [SerializeField] Toggle m_ExactMatchToggle;
        [SerializeField] Toggle m_CaseSensitiveToggle;
        [SerializeField] GameObject m_ColorField;
        [SerializeField] Button m_ColorPickerButton;
        [SerializeField] Image m_ColorPickedImage;

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

            m_TypeDropdown.onValueChanged.AddListener(OnChangeType);
            m_LabelValueInputField.onValueChanged.AddListener(OnChangeLabelValue);
            m_ExactMatchToggle.onValueChanged.AddListener(OnChangeExactMatch);
            m_CaseSensitiveToggle.onValueChanged.AddListener(OnChangeCaseSensitive);
            m_ColorPickerButton.onClick.AddListener(OnClickColorPicker);
        }

        #endregion

        #region Private Methods

        protected override void SetFields(AttributesFilterCondition objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            m_TypeDropdown.Set(typeof(AttributesFilterCondition.AttributeType), (int)objectToDisplay.Type);

            m_LabelValueInputField.text = objectToDisplay.LabelValue;
            m_ExactMatchToggle.isOn = objectToDisplay.ExactMatch;
            m_CaseSensitiveToggle.isOn = objectToDisplay.CaseSensitive;
            m_ColorPickedImage.color = objectToDisplay.Color;

            UpdateFieldVisibility(objectToDisplay.Type);
        }

        private void OnChangeType(int value)
        {
            Object.Type = (AttributesFilterCondition.AttributeType)value;
            UpdateFieldVisibility(Object.Type);
        }

        private void OnChangeLabelValue(string value)
        {
            Object.LabelValue = value;
        }

        private void OnChangeExactMatch(bool value)
        {
            Object.ExactMatch = value;
        }

        private void OnChangeCaseSensitive(bool value)
        {
            Object.CaseSensitive = value;
        }

        private async void OnClickColorPicker()
        {
            Object.Color = await ColorPickerManager.OpenColorPickerAsync(m_ColorPickedImage.color);
            m_ColorPickedImage.color = Object.Color;
        }

        private void UpdateFieldVisibility(AttributesFilterCondition.AttributeType type)
        {
            bool isLabel = type == AttributesFilterCondition.AttributeType.Label;
            bool isColor = type == AttributesFilterCondition.AttributeType.Color;

            m_LabelValueField.SetActive(isLabel);
            m_ColorField.SetActive(isColor);
        }

        #endregion
    }
}
