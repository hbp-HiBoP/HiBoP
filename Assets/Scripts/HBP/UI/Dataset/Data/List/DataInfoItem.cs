using UnityEngine;
using UnityEngine.UI;
using HBP.Theme.Components;
using Tools.Unity;
using Tools.CSharp;

namespace HBP.UI.Experience.Dataset
{
    /// <summary>
    /// Component to display dataInfo in list.
    /// </summary>
    public class DataInfoItem : Tools.Unity.Lists.ActionnableItem<Core.Data.DataInfo>
    {
        #region Properties
        [SerializeField] Text m_NameText;
        [SerializeField] Text m_PatientText;
        [SerializeField] Text m_TypeText;
        [SerializeField] ThemeElement m_StateThemeElement;
        [SerializeField] Tooltip m_ErrorText;

        [SerializeField] Theme.State m_OKState;
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
                m_ErrorText.Text = Object.GetErrorsMessage();
                m_StateThemeElement.Set(value.IsOk ? m_OKState : m_ErrorState);
            }
        }
        #endregion
    }
}