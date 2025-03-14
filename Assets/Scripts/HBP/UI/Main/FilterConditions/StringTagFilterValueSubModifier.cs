using HBP.Core.Data;
using HBP.UI.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class StringTagFilterValueSubModifier : SubModifier<StringTagFilterValue>
    {
        #region Properties
        [SerializeField] InputField m_ValueInputField;
        [SerializeField] Toggle m_ExactMatchToggle;
        [SerializeField] Toggle m_CaseSensitiveToggle;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();

            m_ValueInputField.onEndEdit.AddListener(value => Object.Value = value);
            m_ExactMatchToggle.onValueChanged.AddListener(value => Object.ExactMatch = value);
            m_CaseSensitiveToggle.onValueChanged.AddListener(value => Object.CaseSensitive = value);
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(StringTagFilterValue objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            m_ValueInputField.text = objectToDisplay.Value;
            m_ExactMatchToggle.isOn = objectToDisplay.ExactMatch;
            m_CaseSensitiveToggle.isOn = objectToDisplay.CaseSensitive;
        }
        #endregion
    }
}