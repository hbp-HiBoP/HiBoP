using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.Theme;
using HBP.UI.Tools;
using HBP.UI.Tools.Lists;
using UnityEngine;

namespace HBP.UI.Database
{
    public class DatabaseDataInfoItem : ActionnableItem<DataInfo>
    {
        #region Properties
        [SerializeField] UnityEngine.UI.Text m_NameText;
        [SerializeField] UnityEngine.UI.Text m_TypeText;
        [SerializeField] ThemeElement m_StateThemeElement;
        [SerializeField] Tooltip m_ErrorText;

        [SerializeField] State m_OKState;
        [SerializeField] State m_WarningState;
        [SerializeField] State m_ErrorState;

        /// <summary>
        /// Object to display.
        /// </summary>
        public override DataInfo Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                SetInteractable();

                base.Object = value;
                m_NameText.text = value.Name + (value is CCEPDataInfo ccepDataInfo ? " (" + ccepDataInfo.StimulatedChannel + ")" : "");
                m_TypeText.text = value.GetType().GetDisplayName();

                var errors = Object.Errors;
                var warnings = Object.Warnings;
                if (errors.Count > 0 && warnings.Count > 0)
                    m_ErrorText.Text = Object.GetErrorsMessage() + "\n\n" + Object.GetWarningsMessage();
                else if (warnings.Count > 0)
                    m_ErrorText.Text = Object.GetWarningsMessage();
                else
                    m_ErrorText.Text = Object.GetErrorsMessage();

                m_StateThemeElement.Set(value.IsOk ? (warnings.Count > 0 ? m_WarningState : m_OKState) : m_ErrorState);

                SetNotInteractable();
            }
        }
        #endregion
    }
}