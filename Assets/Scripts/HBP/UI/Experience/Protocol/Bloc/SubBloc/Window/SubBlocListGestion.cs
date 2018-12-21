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
        public override List<SubBloc> Objects
        {
            get
            {
                return base.Objects;
            }

            set
            {
                List.Initialize();
                base.Objects = value;
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
        public override void Remove(SubBloc item)
        {
            base.Remove(item);
            if (m_Objects.All((e) => e.Type != Data.Enums.MainSecondaryEnum.Main))
            {
                SubBloc firstEvent = m_Objects.FirstOrDefault();
                if (firstEvent != null) firstEvent.Type = Data.Enums.MainSecondaryEnum.Main;
            }
        }
        protected override void OnSaveModifier(ItemModifier<SubBloc> modifier)
        {
            if (modifier.Item.Type == Data.Enums.MainSecondaryEnum.Main)
            {
                foreach (var item in Objects)
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

        #region Protected Methods
        protected override void OnSaveCreator(CreatorWindow creatorWindow)
        {
            Data.Enums.CreationType type = creatorWindow.Type;
            SubBloc item = m_Objects.Any((e) => e.Type == Data.Enums.MainSecondaryEnum.Main) ? new SubBloc(Data.Enums.MainSecondaryEnum.Secondary) : new SubBloc(Data.Enums.MainSecondaryEnum.Main);
            switch (type)
            {
                case Data.Enums.CreationType.FromScratch:
                    OpenModifier(item, Interactable);
                    break;
                case Data.Enums.CreationType.FromExistingItem:
                    OpenSelector();
                    break;
                case Data.Enums.CreationType.FromFile:
                    if (LoadFromFile(out item))
                    {
                        OpenModifier(item, Interactable);
                    }
                    break;
            }
        }
        #endregion
    }
}