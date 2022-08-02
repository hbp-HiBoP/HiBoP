using System.Linq;
using UnityEngine;
using HBP.Core.Data.Enums;

namespace HBP.UI.Experience.Protocol
{
    public class EventListGestion : ListGestion<Core.Data.Event>
    {
        #region Properties
        [SerializeField] protected EventList m_List;
        public override Tools.Unity.Lists.ActionableList<Core.Data.Event> List => m_List;

        [SerializeField] protected EventCreator m_ObjectCreator;
        public override ObjectCreator<Core.Data.Event> ObjectCreator => m_ObjectCreator;
        #endregion

        #region Protected Methods
        protected override void Initialize()
        {
            base.Initialize();
            List.OnRemoveObject.AddListener(OnRemoveObject);
            List.OnAddObject.AddListener(OnAddObject);
            if (List.Objects.Count == 0) m_ObjectCreator.Type = MainSecondaryEnum.Main;
            else m_ObjectCreator.Type = MainSecondaryEnum.Secondary;
        }
        protected void OnAddObject(Core.Data.Event obj)
        {
            if (List.Objects.Count == 0) m_ObjectCreator.Type = MainSecondaryEnum.Main;
            else m_ObjectCreator.Type = MainSecondaryEnum.Secondary;
        }
        protected void OnRemoveObject(Core.Data.Event obj)
        {
            if (!List.Objects.Any((e) => e.Type == MainSecondaryEnum.Main))
            {
                Core.Data.Event firstEvent = List.Objects.FirstOrDefault();
                if (firstEvent != null) firstEvent.Type = MainSecondaryEnum.Main;
                List.UpdateObject(firstEvent);
            }
            if (List.Objects.Count == 0) m_ObjectCreator.Type = MainSecondaryEnum.Main;
            else m_ObjectCreator.Type = MainSecondaryEnum.Secondary;
        }
        protected override void OnSaveModifier(Core.Data.Event obj)
        {
            if(obj.Type == MainSecondaryEnum.Main)
            {
                foreach (var item in List.Objects.Where(e => e != obj))
                {
                    item.Type = MainSecondaryEnum.Secondary;
                }
            }
            base.OnSaveModifier(obj);
            List.Refresh();
        }
        protected override void OnObjectCreated(Core.Data.Event obj)
        {
            if (obj.Type == MainSecondaryEnum.Main)
            {
                foreach (var item in List.Objects.Where(e => e != obj))
                {
                    item.Type = MainSecondaryEnum.Secondary;
                }
            }
            base.OnObjectCreated(obj);
            List.Refresh();
        }
        #endregion
    }
}

