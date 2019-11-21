using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    public class CoordinateSelector : ObjectSelector<Data.Coordinate>
    {
        #region Properties
        [SerializeField] CoordinateList m_List;
        protected override SelectableList<Data.Coordinate> List => m_List;
        #endregion
    }
}