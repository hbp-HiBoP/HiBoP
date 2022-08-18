using UnityEngine;
using UnityEngine.UI;
using HBP.Theme.Components;
using HBP.UI.Tools.Lists;

namespace HBP.UI.Main
{
    /// <summary>
    /// Component to display MRI in list.
    /// </summary>
    public class MRIItem : ActionnableItem<Core.Data.MRI>
    {
        #region Properties
        [SerializeField] Text m_NameText;
        [SerializeField] ThemeElement m_MRI;

        [SerializeField] Theme.State m_ErrorState;

        /// <summary>
        /// Object to display.
        /// </summary>
        public override Core.Data.MRI Object
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