using System.Linq;
using Tools.Unity.Components;
using HBP.Data.Experience.Protocol;
using UnityEngine;

namespace HBP.UI.Experience.Protocol
{
    public class SubBlocListGestion : ListGestion<SubBloc>
    {
        #region Properties
        [SerializeField] protected SubBlocList m_List;
        public override Tools.Unity.Lists.ActionableList<SubBloc> List => m_List;

        [SerializeField] protected SubBlocCreator m_ObjectCreator;
        public override ObjectCreator<SubBloc> ObjectCreator => m_ObjectCreator;
        #endregion

        #region Protected Methods
        protected override void Initialize()
        {
            base.Initialize();
            List.OnRemoveObject.AddListener(OnRemoveObject);
            List.OnAddObject.AddListener(OnAddObject);
            if (List.Objects.Count == 0) m_ObjectCreator.Type = Data.Enums.MainSecondaryEnum.Main;
            else m_ObjectCreator.Type = Data.Enums.MainSecondaryEnum.Secondary;
        }
        protected void OnAddObject(SubBloc obj)
        {
            if (List.Objects.Count == 0) m_ObjectCreator.Type = Data.Enums.MainSecondaryEnum.Main;
            else m_ObjectCreator.Type = Data.Enums.MainSecondaryEnum.Secondary;
        }
        protected void OnRemoveObject(SubBloc obj)
        {
            if (!List.Objects.Any((e) => e.Type == Data.Enums.MainSecondaryEnum.Main))
            {
                SubBloc subBloc = List.Objects.FirstOrDefault();
                if (subBloc != null) subBloc.Type = Data.Enums.MainSecondaryEnum.Main;
                List.UpdateObject(subBloc);
            }
            if (List.Objects.Count == 0) m_ObjectCreator.Type = Data.Enums.MainSecondaryEnum.Main;
            else m_ObjectCreator.Type = Data.Enums.MainSecondaryEnum.Secondary;
        }
        protected override void OnSaveModifier(SubBloc obj)
        {
            if (obj.Type == Data.Enums.MainSecondaryEnum.Main)
            {
                foreach (var item in List.Objects.Where(s => s != obj))
                {
                    item.Type = Data.Enums.MainSecondaryEnum.Secondary;
                }
            }
            base.OnSaveModifier(obj);
            List.Refresh();
        }
        protected override void OnObjectCreated(SubBloc obj)
        {
            if (obj.Type == Data.Enums.MainSecondaryEnum.Main)
            {
                foreach (var item in List.Objects.Where(s => s != obj))
                {
                    item.Type = Data.Enums.MainSecondaryEnum.Secondary;
                }
            }
            base.OnObjectCreated(obj);
            List.Refresh();
        }
        #endregion
    }
}