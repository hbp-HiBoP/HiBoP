using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Experience.Dataset;
using NewTheme.Components;
using Tools.Unity;

namespace HBP.UI.Experience.Dataset
{
    /// <summary>
    /// The Script which manage the dataInfo list panel.
    /// </summary>
    public class DataInfoListItem : Tools.Unity.Lists.ActionnableItem<DataInfo>
    {
        #region Properties
        [SerializeField] Text m_NameText;
        [SerializeField] Text m_PatientText;
        [SerializeField] Text m_TypeText;
		[SerializeField] ThemeElement m_StateThemeElement;
        [SerializeField] Tooltip m_ErrorText;

        [SerializeField] State m_OKState;
        [SerializeField] State m_ErrorState;

        public override DataInfo Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_NameText.text = value.Name;
                //m_PatientText.text = value.Patient.Name;
                //m_TypeText.text = value.DataTypeString;
                m_ErrorText.Text = Object.GetErrorsMessage();
                m_StateThemeElement.Set(value.IsOk? m_OKState : m_ErrorState);
            }
        }
        #endregion
    }
}