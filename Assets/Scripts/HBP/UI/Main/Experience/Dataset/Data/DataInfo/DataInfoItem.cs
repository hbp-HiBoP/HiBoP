using UnityEngine;
using HBP.Theme;
using HBP.UI.Tools.Lists;
using HBP.UI.Tools;
using HBP.Core.Tools;

namespace HBP.UI.Main
{
    /// <summary>
    /// Component to display dataInfo in list.
    /// </summary>
    public class DataInfoItem : ActionnableItem<Core.Data.DataInfo>
    {
        #region Properties
        [SerializeField] UnityEngine.UI.Text m_NameText;
        [SerializeField] UnityEngine.UI.Text m_PatientText;
        [SerializeField] UnityEngine.UI.Text m_TypeText;
        [SerializeField] ThemeElement m_StateThemeElement;
        [SerializeField] Tooltip m_ErrorText;

        [SerializeField] Theme.State m_OKState;
        [SerializeField] Theme.State m_WarningState;
        [SerializeField] Theme.State m_ErrorState;

        /// <summary>
        /// Object to display.
        /// </summary>
        public override Core.Data.DataInfo Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_NameText.text = value.Name + (value is Core.Data.CCEPDataInfo ccepDataInfo ? " (" + ccepDataInfo.StimulatedChannel + ")" : "");
                if (value is Core.Data.PatientDataInfo patientDataInfo) m_PatientText.text = patientDataInfo.Patient.Name;
                else m_PatientText.text = "None";
                m_TypeText.text = value.GetType().GetDisplayName();

                var errors = Object.Errors;
                var warnings = Object.Warnings;
                if (errors.Length > 0 && warnings.Length > 0)
                    m_ErrorText.Text = Object.GetErrorsMessage() + "\n\n" + Object.GetWarningsMessage();
                else if (warnings.Length > 0)
                    m_ErrorText.Text = Object.GetWarningsMessage();
                else
                    m_ErrorText.Text = Object.GetErrorsMessage();

                m_StateThemeElement.Set(value.IsOk ? (warnings.Length > 0 ? m_WarningState : m_OKState) : m_ErrorState);
            }
        }
        #endregion
    }
}