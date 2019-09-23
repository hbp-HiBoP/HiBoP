using HBP.Data.Tags;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Tags
{
    public class EnumTagSubModifier : SubModifier<EnumTag>
    {
        #region Properties
        [SerializeField] InputField m_EnumInputField;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_EnumInputField.onValueChanged.AddListener(OnChangeEnum);
        }
        #endregion

        #region Private Methods
        protected override void SetFields(EnumTag objectToDisplay)
        {
            base.SetFields(objectToDisplay);
            m_EnumInputField.text = string.Join(",", objectToDisplay.Values);
        }
        #endregion

        #region Protected Methods
        protected void OnChangeEnum(string value)
        {
            Object.Values = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }
        #endregion
    }
}