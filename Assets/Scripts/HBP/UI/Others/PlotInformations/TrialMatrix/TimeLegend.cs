using UnityEngine;

namespace HBP.UI.TrialMatrix
{
    public class TimeLegend : MonoBehaviour
    {
        #region Properties
        [SerializeField] GameObject m_TimeBlocPrefab;
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
            GameObject l_timeBlocGameObject = Instantiate(m_TimeBlocPrefab);
            TimeBloc l_timeBloc = l_timeBlocGameObject.GetComponent<TimeBloc>();
            l_timeBlocGameObject.GetComponent<RectTransform>().SetParent(transform);
            l_timeBloc.Set(limit.x,limit.y);
        }
        #endregion
    }
}
