using HBP.Data.Anatomy;
using HBP.UI.Anatomy;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    public class MRISelector : ObjectSelector<Data.Anatomy.MRI>
    {
        #region Properties
        [SerializeField] MRIList m_List;
        protected override SelectableList<MRI> List => m_List;
        #endregion
    }
}