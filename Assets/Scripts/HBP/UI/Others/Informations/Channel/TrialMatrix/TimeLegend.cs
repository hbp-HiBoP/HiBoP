using UnityEngine;

namespace HBP.UI.TrialMatrix
{
    public class TimeLegend : MonoBehaviour
    {
        #region Properties
        [SerializeField] GameObject m_TimeBlocPrefab;
        public Core.Tools.TimeWindow[] Limits
        {
            set
            {
                Clear();
                foreach (Core.Tools.TimeWindow limit in value)
                {
                    Add(limit);
                }
            }
        }
        #endregion

        #region Private Methods
        void Clear()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
        void Add(Core.Tools.TimeWindow limit)
        {
            TimeBloc timeBloc = Instantiate(m_TimeBlocPrefab, transform).GetComponent<TimeBloc>();
            timeBloc.Set(limit.Start,limit.End);
        }
        #endregion
    }
}