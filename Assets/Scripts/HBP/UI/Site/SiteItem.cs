using UnityEngine;
using UnityEngine.UI;
using Tools.Unity.Lists;
using NewTheme.Components;

namespace HBP.UI
{
    public class SiteItem : ActionnableItem<Data.Site>
    {
        #region Properties
        [SerializeField] Text m_NameInputField;
        [SerializeField] Text m_CoordinatesText;
        [SerializeField] Text m_TagsText;
        [SerializeField] ThemeElement m_CoordinatesThemeElement;
        [SerializeField] ThemeElement m_TagsThemeElement;
        [SerializeField] State m_ErrorState;

        public override Data.Site Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;

                m_NameInputField.text = value.Name;

                m_CoordinatesText.text = value.Coordinates.Count.ToString();
                if (value.Coordinates.Count > 0) m_CoordinatesThemeElement.Set();
                else m_CoordinatesThemeElement.Set(m_ErrorState);

                m_TagsText.text = value.Tags.Count.ToString();
                if (value.Tags.Count > 0) m_TagsThemeElement.Set();
                else m_TagsThemeElement.Set(m_ErrorState);
            }
        }
        #endregion
    }
}