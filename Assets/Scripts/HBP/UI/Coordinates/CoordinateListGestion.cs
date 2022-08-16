using UnityEngine;
using System.Linq;
using HBP.UI.Lists;

namespace HBP.UI
{
    public class CoordinateListGestion : ListGestion<Core.Data.Coordinate>
    {
        #region Properties
        [SerializeField] protected CoordinateList m_List;
        public override ActionableList<Core.Data.Coordinate> List => m_List;

        [SerializeField] protected CoordinateCreator m_ObjectCreator;
        public override ObjectCreator<Core.Data.Coordinate> ObjectCreator => m_ObjectCreator;
        #endregion

        #region Public Methods
        protected override void OnSaveModifier(Core.Data.Coordinate obj)
        {
            if (List.Objects.Any(c => c.ReferenceSystem == obj.ReferenceSystem && c != obj))
            {
                int count = 1;
                string referenceSystem = string.Format("{0}({1})", obj.ReferenceSystem, count);
                while (List.Objects.Any(c => c.ReferenceSystem == referenceSystem))
                {
                    count++;
                    referenceSystem = string.Format("{0}({1})", obj.ReferenceSystem, count);
                }
                obj.ReferenceSystem = referenceSystem;
            }
            base.OnSaveModifier(obj);
        }
        protected override void OnObjectCreated(Core.Data.Coordinate obj)
        {
            if (List.Objects.Any(c => c.ReferenceSystem == obj.ReferenceSystem && c != obj))
            {
                int count = 1;
                string referenceSystem = string.Format("{0}({1})", obj.ReferenceSystem, count);
                while (List.Objects.Any(c => c.ReferenceSystem == referenceSystem))
                {
                    count++;
                    referenceSystem = string.Format("{0}({1})", obj.ReferenceSystem, count);
                }
                obj.ReferenceSystem = referenceSystem;
            }
            base.OnObjectCreated(obj);
        }
        #endregion
    }
}