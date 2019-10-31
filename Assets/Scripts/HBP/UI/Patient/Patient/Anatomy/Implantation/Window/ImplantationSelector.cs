using HBP.Data.Anatomy;
using HBP.UI.Anatomy;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    public class ImplantationSelector : ObjectSelector<Implantation>
    {
        #region Properties
        [SerializeField] ImplantationList m_List;
        protected override SelectableList<Implantation> List => m_List;
        #endregion
    }
}