using HBP.UI.Anatomy;
using UnityEngine;

namespace HBP.UI
{
    public class ConnectivitySelector : ObjectSelector<Data.Anatomy.Connectivity>
    {
        #region Properties
        [SerializeField] ConnectivityList m_ConnectivityList;
        #endregion

        #region Public Methods
        protected override void Initialize()
        {
            m_List = m_ConnectivityList;
            base.Initialize();
        }
        #endregion
    }
}