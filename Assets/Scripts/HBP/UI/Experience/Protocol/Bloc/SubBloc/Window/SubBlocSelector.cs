using HBP.UI.Experience.Protocol;
using HBP.Data.Experience.Protocol;
using UnityEngine;

namespace HBP.UI
{
    public class SubBlocSelector : ObjectSelector<SubBloc>
    {
        #region Properties
        [SerializeField] SubBlocList m_SubBlocList;
        #endregion

        #region Public Methods
        protected override void Initialize()
        {
            m_List = m_SubBlocList;
            base.Initialize();
        }
        #endregion
    }
}