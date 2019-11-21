using UnityEngine;
using UnityEngine.UI;
using Tools.Unity.Lists;
using NewTheme.Components;

namespace HBP.UI
{
    public class MRIItem : ActionnableItem<Data.MRI>
    {
        #region Properties
        [SerializeField] Text m_NameText;
        [SerializeField] ThemeElement m_MRI;

        [SerializeField] State m_ErrorState;

        public override Data.MRI Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_NameText.text = value.Name;
                if (Object.HasMRI) m_MRI.Set();
                else m_MRI.Set(m_ErrorState);
            }
        }
        #endregion
    }
}