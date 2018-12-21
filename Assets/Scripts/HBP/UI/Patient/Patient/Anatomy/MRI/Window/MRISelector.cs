using HBP.UI.Anatomy;
using UnityEngine;

namespace HBP.UI
{
    public class MRISelector : ObjectSelector<Data.Anatomy.MRI>
    {
        #region Properties
        [SerializeField] MRIList m_MRIList;
        #endregion

        #region Public Methods
        protected override void Initialize()
        {
            m_List = m_MRIList;
            base.Initialize();
        }
        #endregion
    }
}