using System.Collections.Generic;
using System.Linq;
using Tools.Unity.Components;
using d = HBP.Data.Experience.Protocol;
using UnityEngine;

namespace HBP.UI.Experience.Protocol
{
    public class EventListGestion : ListGestion<d.Event>
    {
        #region Properties
        [SerializeField] new EventList List;
        public override List<d.Event> Objects
        {
            get
            {
                return base.Objects;
            }

            set
            {
                List.Initialize();
                base.Objects = value;
                List.SortByName(EventList.Sorting.Descending);
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
            if (m_Objects.Any((e) => e.Type == Data.Enums.MainSecondaryEnum.Main))
            {
                OpenModifier(new d.Event(Data.Enums.MainSecondaryEnum.Secondary), Interactable);
            }
            else base.Create();
        }
        public override void Remove(d.Event item)
        {
            base.Remove(item);
            if (m_Objects.All((e) => e.Type != Data.Enums.MainSecondaryEnum.Main))
            {
                d.Event firstEvent = m_Objects.FirstOrDefault();
                if (firstEvent != null) firstEvent.Type = Data.Enums.MainSecondaryEnum.Main;
            }
        }
        protected override void OnSaveModifier(ItemModifier<d.Event> modifier)
        {
            if(modifier.Item.Type == Data.Enums.MainSecondaryEnum.Main)
            {
                foreach (var item in Objects)
                {
                    if(modifier.Item != item)
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

