using HBP.Data.Anatomy;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using HBP.UI.TrialMatrix;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI.Informations
{
    public class SiteInformations : MonoBehaviour
    {
        #region Properties
        // Trial matrix
        [SerializeField] TrialMatrixList m_TrialMatrixList;
        Dictionary<Protocol, Vector2> m_LimitsByProtocol = new Dictionary<Protocol, Vector2>();
        Dictionary<Protocol, bool> m_AutoLimitsByProtocol = new Dictionary<Protocol, bool>();
        Dictionary<Protocol, Dictionary<Site, Dictionary<DataInfo, Data.TrialMatrix.TrialMatrix>>> m_TrialMatrixByProtocolBySiteByDataInfo = new Dictionary<Protocol, Dictionary<Site, Dictionary<DataInfo, Data.TrialMatrix.TrialMatrix>>>();
        bool m_LineSelectable = false;


        #endregion
    }
}