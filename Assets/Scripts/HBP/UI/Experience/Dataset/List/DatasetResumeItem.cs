using HBP.Data.Experience.Dataset;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Experience.Dataset
{
    public class DatasetResumeItem : Tools.Unity.Lists.Item<Data.Experience.Dataset.Dataset.Resume>
    {
        #region Properties
        [SerializeField] Text m_LabelText;
        [SerializeField] Text m_NumberText;
        [SerializeField] Image m_StateImage;

        public override Data.Experience.Dataset.Dataset.Resume Object
        {
            get
            {
                return base.Object;
            }

            set
            {
                base.Object = value;
                m_LabelText.text = value.Label;
                m_NumberText.text = value.Number.ToString();
                switch (value.State)
                {
                    case Data.Experience.Dataset.Dataset.Resume.StateEnum.OK: m_StateImage.color = ApplicationState.UserPreferences.Theme.General.OK; break;
                    case Data.Experience.Dataset.Dataset.Resume.StateEnum.Warning: m_StateImage.color = ApplicationState.UserPreferences.Theme.General.Warning; break;
                    case Data.Experience.Dataset.Dataset.Resume.StateEnum.Error: m_StateImage.color = ApplicationState.UserPreferences.Theme.General.Error; break;
                }
            }
        }
        #endregion
    }
}