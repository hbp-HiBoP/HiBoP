using HBP.UI.Experience.Protocol;
using HBP.Data.Experience.Protocol;
using UnityEngine;
using Tools.Unity.Lists;

namespace HBP.UI
{
    public class BlocSelector : ObjectSelector<Bloc>
    {
        #region Properties
        [SerializeField] BlocList m_List;
        protected override SelectableList<Bloc> List => m_List;
        #endregion
    }
}