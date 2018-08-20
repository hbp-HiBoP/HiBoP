using System.Linq;
using HBP.Data.Experience.Protocol;
using HBP.UI.Experience.Protocol;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    public class IconGestion : Gestion<Icon, SubBloc>
    {
        #region Properties
        [SerializeField] IconList m_List;
        protected override SelectableListWithItemAction<Icon> List { get { return m_List; } }
        #endregion

        #region Public Methods
        public override void Set(SubBloc subBloc)
        {
            base.Set(subBloc);
            List.Objects = m_ParentObject.Icons.ToArray();
            m_List.SortByName(IconList.Sorting.Descending);
        }
        public override void Save()
        {
            m_ParentObject.Icons = List.Objects.ToList();
        }
        #endregion

        #region Private Methods
        protected override void OnSaveModifier(ItemModifier<Data.Experience.Protocol.Icon> modifier)
        {
            base.OnSaveModifier(modifier);
            m_List.SortByNone();
        }
        #endregion
    }
}