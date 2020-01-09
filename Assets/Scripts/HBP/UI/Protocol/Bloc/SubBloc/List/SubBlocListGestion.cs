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
        }
        protected void OnRemoveObject(SubBloc obj)
        {
            if (!List.Objects.Any((e) => e.Type == Data.Enums.MainSecondaryEnum.Main))
            {
                SubBloc firstSubBloc = List.Objects.FirstOrDefault();
                if (firstSubBloc != null) firstSubBloc.Type = Data.Enums.MainSecondaryEnum.Main;
                List.UpdateObject(firstSubBloc);
            }
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
        }
        #endregion
    }
}