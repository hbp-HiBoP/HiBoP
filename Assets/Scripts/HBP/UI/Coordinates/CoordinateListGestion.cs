using Tools.Unity.Components;
using UnityEngine;
using System.Linq;
using Tools.Unity.Lists;

namespace HBP.UI
{
    public class CoordinateListGestion : ListGestion<Data.Coordinate>
    {
        #region Properties
        [SerializeField] protected CoordinateList m_List;
        public override SelectableListWithItemAction<Data.Coordinate> List => m_List;

        [SerializeField] protected CoordinateCreator m_ObjectCreator;
        public override ObjectCreator<Data.Coordinate> ObjectCreator => m_ObjectCreator;
        #endregion

        #region Public Methods
        protected override void OnSaveModifier(Data.Coordinate obj)
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
        #endregion
    }
}