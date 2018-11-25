using System;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI.TrialMatrix.Grid
{
    public class BlocHeaderDisplayer : MonoBehaviour
    {
        #region Properties
        List<BlocHeader> m_Headers = new List<BlocHeader>();
        public BlocHeader[] Headers
        {
            get
            {
                return m_Headers.ToArray();
            }
        }

        [SerializeField] GameObject m_HeaderPrefab;
        #endregion

        #region Public Methods
        public void Set(Tuple<HBP.Data.Experience.Protocol.Bloc,float>[] blocsAndFlexibleHeight )
        {
            Clear();
            foreach (var pair in blocsAndFlexibleHeight)
            {
                BlocHeader header = Instantiate(m_HeaderPrefab, transform).GetComponent<BlocHeader>();
                header.Bloc = pair.Item1;
                header.FlexibleHeight = pair.Item2; 
                m_Headers.Add(header);
            }
        }
        #endregion

        #region Private Methods
        void Clear()
        {
            foreach (var header in m_Headers)
            {
                Destroy(header.gameObject);
            }
            m_Headers = new List<BlocHeader>();
        }
        #endregion
    }
}