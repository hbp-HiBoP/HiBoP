using UnityEngine;

namespace HBP.UI.TrialMatrix
{
    public class TimeLegend : MonoBehaviour
    {
        #region Properties
        [SerializeField] GameObject m_TimeBlocPrefab;
        public Vector2[] Limits
        {
            set
            {
                foreach (Vector2 limit in value)
                {
                    Add(limit);
                }
            }
        }
        #endregion

        #region Private Methods
        void Add(Vector2 limit)
        {
            TimeBloc timeBloc = Instantiate(m_TimeBlocPrefab, transform).GetComponent<TimeBloc>();
            timeBloc.Set(limit.x,limit.y);
        }
        #endregion
    }
}