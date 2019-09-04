using HBP.UI.Anatomy;
using UnityEngine;

namespace HBP.UI
{
    public class ImplantationSelector : ObjectSelector<Data.Anatomy.Implantation>
    {
        #region Properties
        [SerializeField] ImplantationList m_ImplantationList;
        #endregion

        #region Public Methods
        protected override void Initialize()
        {
            m_List = m_ImplantationList;
            base.Initialize();
        }
        #endregion
    }
}