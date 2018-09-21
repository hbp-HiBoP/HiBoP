using System.Collections.Generic;
using System.Linq;
using HBP.Data.Experience.Protocol;
using Tools.Unity.Components;
using UnityEngine;

namespace HBP.UI.Experience.Protocol
{
    public class EventListGestion : ListGestion<Data.Experience.Protocol.Event>
    {
        #region Properties
        [SerializeField] new EventList List;
        public override List<Data.Experience.Protocol.Event> Items
        {
            get
            {
                return base.Items;
            }

            set
            {
                List.Initialize();
                base.Items = value;
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
            if (m_Items.Any((e) => e.Type == Data.Experience.Protocol.Event.TypeEnum.Main))
            {
                OpenModifier(new Data.Experience.Protocol.Event(Data.Experience.Protocol.Event.TypeEnum.Secondary), Interactable);
            }
            else base.Create();
        }
        public override void Remove(Data.Experience.Protocol.Event item)
        {
            base.Remove(item);
            if (m_Items.All((e) => e.Type != Data.Experience.Protocol.Event.TypeEnum.Main))
            {
                Data.Experience.Protocol.Event firstEvent = m_Items.FirstOrDefault();
                if (firstEvent != null) firstEvent.Type = Data.Experience.Protocol.Event.TypeEnum.Main;
            }
        }
        protected override void OnSaveModifier(ItemModifier<Data.Experience.Protocol.Event> modifier)
        {
            if(modifier.Item.Type == Data.Experience.Protocol.Event.TypeEnum.Main)
            {
                foreach (var item in Items)
                {
                    if(modifier.Item != item)
                    {
                        item.Type = Data.Experience.Protocol.Event.TypeEnum.Secondary;
                    }
                }
            }
            List.Refresh();
            base.OnSaveModifier(modifier);
        }
        #endregion
    }
}

