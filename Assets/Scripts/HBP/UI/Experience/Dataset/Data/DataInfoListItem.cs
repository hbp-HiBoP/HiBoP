using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Experience.Dataset;
using NewTheme.Components;

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
		[SerializeField] ThemeElement m_StateThemeElement;
        [SerializeField] Text m_ErrorText;

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
                m_PatientText.text = value.Patient.Name;
                m_ErrorText.text = Object.GetErrorsMessage();
                m_StateThemeElement.Set(value.isOk? m_OKState : m_ErrorState);
            }
        }
        #endregion
    }
}