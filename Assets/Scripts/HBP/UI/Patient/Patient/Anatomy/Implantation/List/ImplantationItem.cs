using UnityEngine;
using UnityEngine.UI;
using Tools.Unity.Lists;
using HBP.Data.Anatomy;
using NewTheme.Components;

namespace HBP.UI.Anatomy
{
    public class ImplantationItem : ActionnableItem<Implantation>
    {
        #region Properties
        [SerializeField] Text m_NameInputField;
        [SerializeField] ThemeElement m_ImplantationThemeElement;
        [SerializeField] ThemeElement m_MarsAtlasThemeElement;
        [SerializeField] State m_ErrorState;

        public override Implantation Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_NameInputField.text = value.Name;
                if (value.HasImplantation) m_ImplantationThemeElement.Set();
                else m_ImplantationThemeElement.Set(m_ErrorState);
                if (value.HasMarsAtlas) m_MarsAtlasThemeElement.Set();
                else m_MarsAtlasThemeElement.Set(m_ErrorState);
            }
        }
        #endregion
    }
}