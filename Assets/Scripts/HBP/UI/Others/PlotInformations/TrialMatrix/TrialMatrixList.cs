using UnityEngine;
using UnityEngine.Profiling;
using System.Collections.Generic;

namespace HBP.UI.TrialMatrix
{
    public class TrialMatrixList : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        GameObject m_trialMatrixPrefab;

        List<TrialMatrix> m_trials = new List<TrialMatrix>();
        public TrialMatrix[] TrialMatrix { get { return m_trials.ToArray(); } }
        #endregion

        #region Public Methods
        public void Set(Data.TrialMatrix.TrialMatrix[][] trialMatrix)
        {
            Clear();
            Display(trialMatrix);
        }
        #endregion

        #region Private Methods
        void Display(Data.TrialMatrix.TrialMatrix[][] trialMatrix)
        {
            for (int c = 0; c < trialMatrix.Length; c++)
            {
                GameObject l_line = new GameObject();
                l_line.name = "line n°" + (c + 1);
                l_line.transform.SetParent(transform);
                l_line.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                for (int p = 0; p < trialMatrix[c].Length; p++)
                {
                    GameObject l_gameObject = Instantiate(m_trialMatrixPrefab);
                    l_gameObject.transform.SetParent(l_line.transform);
                    l_gameObject.transform.name = "Trial matrix n°"+(l_gameObject.transform.GetSiblingIndex() + 1).ToString();
                    TrialMatrix trial = l_gameObject.GetComponent<TrialMatrix>();
                    m_trials.Add(trial);
                    Profiler.BeginSample("SetTrial");
                    trial.Set(trialMatrix[c][p]);
                    Profiler.EndSample();
                    RectTransform l_rect = transform as RectTransform;
                }
            }
        }

        void Clear()
        {
            int iMax = transform.childCount;
            for (int i = 0; i < iMax; i++)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
            RectTransform l_rect = transform as RectTransform;
            m_trials = new List<TrialMatrix>();
        }
        #endregion
    }
}