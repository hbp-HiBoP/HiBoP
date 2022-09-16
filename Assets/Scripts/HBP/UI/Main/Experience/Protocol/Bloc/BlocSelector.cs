using UnityEngine;
using HBP.UI.Tools.Lists;
using HBP.UI.Tools;

namespace HBP.UI.Main
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