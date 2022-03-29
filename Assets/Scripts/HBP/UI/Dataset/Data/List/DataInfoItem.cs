using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Experience.Dataset;
using Theme.Components;
using Tools.Unity;
using Tools.CSharp;

namespace HBP.UI.Experience.Dataset
{
    /// <summary>
    /// Component to display dataInfo in list.
    /// </summary>
    public class DataInfoItem : Tools.Unity.Lists.ActionnableItem<DataInfo>
    {
        #region Properties
        [SerializeField] Text m_NameText;
        [SerializeField] Text m_PatientText;
        [SerializeField] Text m_TypeText;
        [SerializeField] ThemeElement m_StateThemeElement;
        [SerializeField] Tooltip m_ErrorText;

        [SerializeField] State m_OKState;
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
                base.Object = value;
                m_NameText.text = value.Name + (value is CCEPDataInfo ccepDataInfo ? " (" + ccepDataInfo.StimulatedChannel + ")" : "");
                if (value is PatientDataInfo patientDataInfo) m_PatientText.text = patientDataInfo.Patient.Name;
                else m_PatientText.text = "None";
                m_TypeText.text = value.GetType().GetDisplayName();
                m_ErrorText.Text = Object.GetErrorsMessage();
                m_StateThemeElement.Set(value.IsOk ? m_OKState : m_ErrorState);
            }
        }
        #endregion
    }
}