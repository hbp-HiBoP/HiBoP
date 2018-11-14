using HBP.Data.Experience.Dataset;
using HBP.UI.Experience.Dataset;
using UnityEngine;

namespace HBP.UI
{
    public class DatasetSelector : ObjectSelector<Dataset>
    {
        #region Properties
        [SerializeField] DatasetList m_DatasetList;
        #endregion

        #region Public Methods
        protected override void Initialize()
        {
            m_List = m_DatasetList;
            base.Initialize();
        }
        #endregion
    }
}

