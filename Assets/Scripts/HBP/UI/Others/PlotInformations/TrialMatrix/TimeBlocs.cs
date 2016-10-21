using UnityEngine;

namespace HBP.UI.TrialMatrix
{
    public class TimeBlocs : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        GameObject m_timeBloc;

        RectTransform m_rect;
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
            GameObject l_timeBlocGameObject = Instantiate(m_timeBloc);
            TimeBloc l_timeBloc = l_timeBlocGameObject.GetComponent<TimeBloc>();
            l_timeBlocGameObject.GetComponent<RectTransform>().SetParent(m_rect);
            l_timeBloc.Set(limit.x,limit.y);
        }

        void Awake()
        {
            m_rect = transform.GetChild(0).GetComponent<RectTransform>();
        }
        #endregion
    }
}
