using UnityEngine;
using System.Runtime.Serialization;
using UnityEngine.Events;

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

        Vector3 m_Center;
        [DataMember]
        public Vector3 Center
        {
            get
            {
                return m_Center;
            }
            set
            {
                m_Center = value;
                OnChangeCenter.Invoke();
            }
        }
        [IgnoreDataMember]
        public UnityEvent OnChangeCenter { get; set; }

        float m_Radius;
        [DataMember]
        public float Radius
        {
            get
            {
                return m_Radius;
            }
            set
            {
                m_Radius = value;
                OnChangeRadius.Invoke();
            }
        }
        [IgnoreDataMember]
        public UnityEvent OnChangeRadius { get; set; }
        #endregion

        #region Public Methods
        public ROI(string name,Vector3 center,float radius)
        {
            OnChangeCenter = new UnityEvent();
            OnChangeName = new UnityEvent();
            OnChangeRadius = new UnityEvent();
            Name = name;
            Center = center;
            Radius = radius;
        }
        #endregion
    }
}