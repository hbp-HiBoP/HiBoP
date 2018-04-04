using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Linq;
using System.Collections.Generic;
using HBP.Data.Experience.Protocol;

namespace HBP.UI.TrialMatrix
{
    public class TrialMatrixList : MonoBehaviour
    {
        #region Properties
        [SerializeField] GameObject m_TrialMatrixPrefab;

        public List<TrialMatrix> TrialMatrix { get; set; }
        public GenericEvent<Vector2, Protocol> OnChangeLimits = new GenericEvent<Vector2, Protocol>();
        public GenericEvent<bool, Protocol> OnAutoLimits = new GenericEvent<bool, Protocol>();
        #endregion

        #region Public Methods
        public void Set(Dictionary<Protocol, Informations.TrialMatrixZone.ProtocolInformation> informationByProtocol, Texture2D colorMap)
        {
            Clear();
            foreach (var protocolPair in informationByProtocol)
            {
                RectTransform lineRectTransform = new GameObject(protocolPair.Key.Name, new System.Type[] { typeof(RectTransform), typeof(HorizontalLayoutGroup) }).GetComponent<RectTransform>();
                lineRectTransform.SetParent(transform);
                foreach (var sitePair in protocolPair.Value.TrialMatrixByDataInfoBySite)
                {
                    Data.TrialMatrix.TrialMatrix dataTrialMatrix = sitePair.Value.Values.First();
                    TrialMatrix trial = Instantiate(m_TrialMatrixPrefab, lineRectTransform).GetComponent<TrialMatrix>();
                    TrialMatrix.Add(trial);
                    trial.Set(dataTrialMatrix, protocolPair.Value.AutoLimits, protocolPair.Value.Limits, colorMap);
                    trial.OnAutoLimits.RemoveAllListeners();
                    trial.OnAutoLimits.AddListener((autoLimit) => OnAutoLimits.Invoke(autoLimit, dataTrialMatrix.Protocol));
                    trial.OnChangeLimits.RemoveAllListeners();
                    trial.OnChangeLimits.AddListener((limits) => OnChangeLimits.Invoke(limits, dataTrialMatrix.Protocol));
                }
            }
        }
        public void Clear()
        {
            int numberOfChildren = transform.childCount;
            for (int c = 0; c < numberOfChildren; c++)
            {
                Destroy(transform.GetChild(c).gameObject);
            }
            TrialMatrix = new List<TrialMatrix>();
        }
        #endregion
    }
}