using HBP.Data.Anatomy;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI.Anatomy
{
    public class CoordinateSelector : ObjectSelector<Coordinate>
    {
        #region Properties
        [SerializeField] CoordinateList m_List;
        protected override SelectableList<Coordinate> List => m_List;
        #endregion
    }
}