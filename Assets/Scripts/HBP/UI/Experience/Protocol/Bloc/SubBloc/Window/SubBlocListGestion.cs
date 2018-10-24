using System.Collections.Generic;
using System.Linq;
using Tools.Unity.Components;
using HBP.Data.Experience.Protocol;
using UnityEngine;

namespace HBP.UI.Experience.Protocol
{
    public class SubBlocListGestion : ListGestion<SubBloc>
    {
        #region Properties
        [SerializeField] new SubBlocList List;
        public override List<SubBloc> Items
        {
            get
            {
                return base.Items;
            }

            set
            {
                List.Initialize();
                base.Items = value;
                List.SortByName(SubBlocList.Sorting.Descending);
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.List = List;
            base.Initialize();
        }
        public override void Create()
        {
            if (m_Items.Any((e) => e.Type == Data.Enums.MainSecondaryEnum.Main))
            {
                OpenModifier(new SubBloc(Data.Enums.MainSecondaryEnum.Secondary), Interactable);
            }
            else base.Create();
        }
        public override void Remove(SubBloc item)
        {
            base.Remove(item);
            if (m_Items.All((e) => e.Type != Data.Enums.MainSecondaryEnum.Main))
            {
                SubBloc firstEvent = m_Items.FirstOrDefault();
                if (firstEvent != null) firstEvent.Type = Data.Enums.MainSecondaryEnum.Main;
            }
        }
        protected override void OnSaveModifier(ItemModifier<SubBloc> modifier)
        {
            if (modifier.Item.Type == Data.Enums.MainSecondaryEnum.Main)
            {
                foreach (var item in Items)
                {
                    if (modifier.Item != item)
                    {
                        item.Type = Data.Enums.MainSecondaryEnum.Secondary;
                    }
                }
            }
            List.Refresh();
            base.OnSaveModifier(modifier);
        }
        #endregion
    }
}