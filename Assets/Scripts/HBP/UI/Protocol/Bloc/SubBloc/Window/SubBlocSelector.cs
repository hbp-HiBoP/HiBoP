using HBP.UI.Experience.Protocol;
using UnityEngine;
using Tools.Unity.Lists;

namespace HBP.UI
{
    /// <summary>
    /// Window to select subBlocs.
    /// </summary>
    public class SubBlocSelector : ObjectSelector<Core.Data.SubBloc>
    {
        #region Properties
        [SerializeField] SubBlocList m_List;
        /// <summary>
        /// UI subBlocs list.
        /// </summary>
        protected override SelectableList<Core.Data.SubBloc> List => m_List;
        #endregion
    }
}