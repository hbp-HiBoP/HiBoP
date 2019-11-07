using System;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI
{
    public class EnumTagSubModifier : SubModifier<Data.EnumTag>
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
        protected override void SetFields(Data.EnumTag objectToDisplay)
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