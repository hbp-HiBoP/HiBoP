using HBP.Data.Experience.Protocol;
using HBP.UI.Experience.Protocol;
using UnityEngine;

namespace HBP.UI
{
    public class ProtocolSelector : ObjectSelector<Protocol>
    {
        #region Properties
        [SerializeField] ProtocolList m_ProtocolList;
        #endregion

        #region Public Methods
        protected override void Initialize()
        {
            m_List = m_ProtocolList;
            base.Initialize();
        }
        #endregion
    }
}

