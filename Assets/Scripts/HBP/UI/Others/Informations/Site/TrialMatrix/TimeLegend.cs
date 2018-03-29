using UnityEngine;

namespace HBP.UI.TrialMatrix
{
    public class TimeLegend : MonoBehaviour
    {
        #region Properties
        [SerializeField] GameObject m_TimeBlocPrefab;
        public RectTransform TimeBlocsRectTransform;
        #endregion

        #region Public Methods
        public void Set(Vector2[] limits)
        {
            foreach(Vector2 limit in limits)
            {
                AddTimeBloc(limit);
            }
        }
        #endregion

        #region Private Methods
        void AddTimeBloc(Vector2 limit)
        {
            TimeBloc timeBloc = Instantiate(m_TimeBlocPrefab, TimeBlocsRectTransform).GetComponent<TimeBloc>();
            timeBloc.Set(limit.x,limit.y);
        }
        #endregion
    }
}
