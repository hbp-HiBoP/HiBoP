using UnityEngine;
using UnityEngine.Profiling;
using System.Collections.Generic;
using UnityEngine.Events;
using HBP.Data.Experience.Protocol;
using UnityEngine.UI;

namespace HBP.UI.TrialMatrix
{
    public class TrialMatrixList : MonoBehaviour
    {
        #region Properties
        [SerializeField] GameObject m_trialMatrixPrefab;

        public List<TrialMatrix> TrialMatrix { get; set; }

        public GenericEvent<Vector2, Protocol> OnChangeLimits = new GenericEvent<Vector2, Protocol>();
        public GenericEvent<bool, Protocol> OnAutoLimits = new GenericEvent<bool, Protocol>();
        #endregion

        #region Public Methods
        public void Set(Data.TrialMatrix.TrialMatrix[][] trialMatrix, Dictionary<Protocol,bool> autoLimitsByProtocol, Dictionary<Protocol,Vector2> limitsByProtocol)
        {
            Clear();
            for (int lineIndex = 0; lineIndex < trialMatrix.Length; lineIndex++)
            {
                RectTransform lineRectTransform = new GameObject("line n°" + (lineIndex + 1), new System.Type[] { typeof(RectTransform), typeof(HorizontalLayoutGroup) }).GetComponent<RectTransform>();
                lineRectTransform.SetParent(transform);
                for (int columnIndex = 0; columnIndex < trialMatrix[lineIndex].Length; columnIndex++)
                {
                    Data.TrialMatrix.TrialMatrix dataTrialMatrix = trialMatrix[lineIndex][columnIndex];
                    TrialMatrix trial = Instantiate(m_trialMatrixPrefab, lineRectTransform).GetComponent<TrialMatrix>();
                    TrialMatrix.Add(trial);
                    trial.Set(dataTrialMatrix, autoLimitsByProtocol[dataTrialMatrix.Protocol], limitsByProtocol[dataTrialMatrix.Protocol]);
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