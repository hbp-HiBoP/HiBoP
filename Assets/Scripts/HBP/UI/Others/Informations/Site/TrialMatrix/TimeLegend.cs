using UnityEngine;

namespace HBP.UI.TrialMatrix
{
    public class TimeLegend : MonoBehaviour
    {
        #region Properties
        [SerializeField] GameObject m_TimeBlocPrefab;
        public Tools.CSharp.Window[] Limits
        {
            set
            {
                foreach (Tools.CSharp.Window limit in value)
                {
                    Add(limit);
                }
            }
        }
        #endregion

        #region Private Methods
        void Add(Tools.CSharp.Window limit)
        {
            TimeBloc timeBloc = Instantiate(m_TimeBlocPrefab, transform).GetComponent<TimeBloc>();
            timeBloc.Set(limit.Start,limit.End);
        }
        #endregion
    }
}