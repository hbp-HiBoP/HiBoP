using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    public class MRISelector : ObjectSelector<Data.MRI>
    {
        #region Properties
        [SerializeField] MRIList m_List;
        protected override SelectableList<Data.MRI> List => m_List;
        #endregion
    }
}