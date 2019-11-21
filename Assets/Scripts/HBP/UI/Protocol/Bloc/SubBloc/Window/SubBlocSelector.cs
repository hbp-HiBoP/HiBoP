using HBP.UI.Experience.Protocol;
using HBP.Data.Experience.Protocol;
using UnityEngine;
using Tools.Unity.Lists;

namespace HBP.UI
{
    public class SubBlocSelector : ObjectSelector<SubBloc>
    {
        #region Properties
        [SerializeField] SubBlocList m_List;
        protected override SelectableList<SubBloc> List => m_List;
        #endregion
    }
}