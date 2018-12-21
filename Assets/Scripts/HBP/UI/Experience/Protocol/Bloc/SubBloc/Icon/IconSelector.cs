using HBP.UI.Experience.Protocol;
using d = HBP.Data.Experience.Protocol;
using UnityEngine;

namespace HBP.UI
{
    public class IconSelector : ObjectSelector<d.Icon>
    {
        #region Properties
        [SerializeField] IconList m_IconList;
        #endregion

        #region Public Methods
        protected override void Initialize()
        {
            m_List = m_IconList;
            base.Initialize();
        }
        #endregion
    }
}