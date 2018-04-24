using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Experience.Dataset;

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
		[SerializeField] Image m_StateImage;
        [SerializeField] Text m_ErrorText;

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
                m_StateImage.color = value.isOk ? ApplicationState.UserPreferences.Theme.General.OK : ApplicationState.UserPreferences.Theme.General.Error;
            }
        }
        #endregion
    }
}