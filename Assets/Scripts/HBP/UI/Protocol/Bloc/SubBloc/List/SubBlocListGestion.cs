using System.Linq;
using UnityEngine;
using HBP.Core.Enums;

namespace HBP.UI.Experience.Protocol
{
    public class SubBlocListGestion : ListGestion<Core.Data.SubBloc>
    {
        #region Properties
        [SerializeField] protected SubBlocList m_List;
        public override Tools.Unity.Lists.ActionableList<Core.Data.SubBloc> List => m_List;

        [SerializeField] protected SubBlocCreator m_ObjectCreator;
        public override ObjectCreator<Core.Data.SubBloc> ObjectCreator => m_ObjectCreator;
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
        protected void OnAddObject(Core.Data.SubBloc obj)
        {
            if (List.Objects.Count == 0) m_ObjectCreator.Type = MainSecondaryEnum.Main;
            else m_ObjectCreator.Type = MainSecondaryEnum.Secondary;
        }
        protected void OnRemoveObject(Core.Data.SubBloc obj)
        {
            if (!List.Objects.Any((e) => e.Type == MainSecondaryEnum.Main))
            {
                Core.Data.SubBloc subBloc = List.Objects.FirstOrDefault();
                if (subBloc != null) subBloc.Type = MainSecondaryEnum.Main;
                List.UpdateObject(subBloc);
            }
            if (List.Objects.Count == 0) m_ObjectCreator.Type = MainSecondaryEnum.Main;
            else m_ObjectCreator.Type = MainSecondaryEnum.Secondary;
        }
        protected override void OnSaveModifier(Core.Data.SubBloc obj)
        {
            if (obj.Type == MainSecondaryEnum.Main)
            {
                foreach (var item in List.Objects.Where(s => s != obj))
                {
                    item.Type = MainSecondaryEnum.Secondary;
                }
            }
            base.OnSaveModifier(obj);
            List.Refresh();
        }
        protected override void OnObjectCreated(Core.Data.SubBloc obj)
        {
            if (obj.Type == MainSecondaryEnum.Main)
            {
                foreach (var item in List.Objects.Where(s => s != obj))
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