using HBP.UI.Experience.Protocol;
using UnityEngine;
using HBP.UI.Lists;

namespace HBP.UI
{
    /// <summary>
    /// Window to select blocs.
    /// </summary>
    public class BlocSelector : ObjectSelector<Core.Data.Bloc>
    {
        #region Properties
        [SerializeField] BlocList m_List;
        /// <summary>
        /// UI blocs list.
        /// </summary>
        protected override SelectableList<Core.Data.Bloc> List => m_List;
        #endregion
    }
}