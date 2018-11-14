using HBP.UI.Experience.Protocol;
using HBP.Data.Experience.Protocol;
using UnityEngine;

namespace HBP.UI
{
    public class BlocSelector : ObjectSelector<Bloc>
    {
        #region Properties
        [SerializeField] BlocList m_BlocList;
        #endregion

        #region Public Methods
        protected override void Initialize()
        {
            m_List = m_BlocList;
            base.Initialize();
        }
        #endregion
    }
}