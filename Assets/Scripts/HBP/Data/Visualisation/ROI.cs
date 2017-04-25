using UnityEngine;
using System.Runtime.Serialization;
using UnityEngine.Events;
using System.Collections.Generic;

namespace HBP.Data.Visualisation
{
    [DataContract]
    public class ROI
    {
        #region Properties
        string m_Name;
        [DataMember]
        public string Name
        {
            get
            {
                return m_Name;
            }
            set
            {
                m_Name = value;
                OnChangeName.Invoke();
            }
        }
        [IgnoreDataMember]
        public UnityEvent OnChangeName { get; set; }

        List<ROIBubble> m_Bubbles;
        [DataMember]
        public List<ROIBubble> Bubbles
        {
            get
            {
                return m_Bubbles;
            }
        }
        [IgnoreDataMember]
        public UnityEvent OnAddBubble { get; set; }
        public UnityEvent OnRemoveBubble { get; set; }
        #endregion

        #region Public Methods
        // Initialize empty ROI
        public ROI(string name)
        {
            OnChangeName = new UnityEvent();
            OnAddBubble = new UnityEvent();
            OnRemoveBubble = new UnityEvent();
            Name = name;
        }
        // Initialize with a first bubble
        public ROI(string name, Vector3 center)
        {
            OnChangeName = new UnityEvent();
            OnAddBubble = new UnityEvent();
            OnRemoveBubble = new UnityEvent();
            Name = name;
            AddBubble(center);
        }
        public void AddBubble(Vector3 center, float radius = 3.0f)
        {
            ROIBubble l_bubble = new ROIBubble(center, radius);
            m_Bubbles.Add(l_bubble);
            OnAddBubble.Invoke();
        }
        public void RemoveBubble(int index)
        {
            m_Bubbles.RemoveAt(index);
            OnRemoveBubble.Invoke();
        }
        #endregion
    }
}