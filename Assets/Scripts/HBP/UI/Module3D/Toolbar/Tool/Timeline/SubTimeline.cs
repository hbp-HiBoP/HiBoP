using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class SubTimeline : MonoBehaviour
    {
        #region Properties
        [SerializeField] private Text m_MinText;
        [SerializeField] private Text m_MaxText;
        [SerializeField] private Text m_Current;
        private HBP.Module3D.Timeline m_Timeline;
        private HBP.Module3D.SubTimeline m_SubTimeline;
        #endregion

        #region Public Methods
        public void Initialize(HBP.Module3D.Timeline timeline, HBP.Module3D.SubTimeline subTimeline, float offset)
        {
            m_Timeline = timeline;
            m_SubTimeline = subTimeline;
            m_MinText.text = subTimeline.MinTime.ToString("N2") + timeline.Unit;
            m_MaxText.text = subTimeline.MaxTime.ToString("N2") + timeline.Unit;
            UpdateCurrentTime();
            float begin = (float)subTimeline.GlobalMinIndex / (timeline.Length - 1);
            float end =(float)subTimeline.GlobalMaxIndex / (timeline.Length - 1);
            GetComponent<RectTransform>().anchorMin = new Vector2(begin, 0);
            GetComponent<RectTransform>().anchorMax = new Vector2(end, 1);
            GetComponent<RectTransform>().sizeDelta = new Vector2(offset, 0);
        }
        public void UpdateCurrentTime()
        {
            if (m_Timeline.CurrentSubtimeline == m_SubTimeline)
            {
                m_Current.gameObject.SetActive(true);
                m_Current.text = m_SubTimeline.GetLocalIndex(m_Timeline.CurrentIndex) + " (" + m_SubTimeline.GetLocalTime(m_Timeline.CurrentIndex).ToString("N2") + m_Timeline.Unit + ")";
            }
            else
            {
                m_Current.gameObject.SetActive(false);
            }
        }
        #endregion
    }
}