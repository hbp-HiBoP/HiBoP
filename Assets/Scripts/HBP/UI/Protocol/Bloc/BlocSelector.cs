using HBP.UI.Experience.Protocol;
using HBP.Data.Experience.Protocol;
using UnityEngine;
using Tools.Unity.Lists;

namespace HBP.UI
{
    /// <summary>
    /// Window to select blocs.
    /// </summary>
    public class BlocSelector : ObjectSelector<Bloc>
    {
        #region Properties
        [SerializeField] BlocList m_List;
        /// <summary>
        /// UI blocs list.
        /// </summary>
        protected override SelectableList<Bloc> List => m_List;
        #endregion
    }
}