using HBP.UI.Experience.Protocol;
using HBP.Data.Experience.Protocol;
using UnityEngine;
using Tools.Unity.Lists;

namespace HBP.UI
{
    /// <summary>
    /// Window to select subBlocs.
    /// </summary>
    public class SubBlocSelector : ObjectSelector<SubBloc>
    {
        #region Properties
        [SerializeField] SubBlocList m_List;
        /// <summary>
        /// UI subBlocs list.
        /// </summary>
        protected override SelectableList<SubBloc> List => m_List;
        #endregion
    }
}