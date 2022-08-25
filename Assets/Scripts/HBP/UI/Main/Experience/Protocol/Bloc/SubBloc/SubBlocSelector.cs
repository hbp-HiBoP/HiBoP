using UnityEngine;
using HBP.UI.Tools.Lists;
using HBP.UI.Tools;

namespace HBP.UI.Main
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