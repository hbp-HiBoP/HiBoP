using HBP.Data.Experience.Dataset;
using HBP.UI.Experience.Dataset;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    public class DatasetSelector : ObjectSelector<Dataset>
    {
        #region Properties
        [SerializeField] DatasetList m_List;
        protected override SelectableList<Dataset> List => m_List;
        #endregion
    }
}

