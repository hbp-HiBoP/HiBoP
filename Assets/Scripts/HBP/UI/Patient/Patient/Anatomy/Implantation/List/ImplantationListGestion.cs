using HBP.Data.Anatomy;
using Tools.Unity.Components;
using UnityEngine;

namespace HBP.UI.Anatomy
{
    public class ImplantationListGestion : ListGestion<Implantation>
    {
        #region Properties
        [SerializeField] protected ImplantationList m_List;
        public override Tools.Unity.Lists.SelectableListWithItemAction<Implantation> List => m_List;

        [SerializeField] protected ImplantationCreator m_ObjectCreator;
        public override ObjectCreator<Implantation> ObjectCreator => m_ObjectCreator;
        #endregion
    }
}