using UnityEngine;
using System.Runtime.Serialization;
using UnityEngine.Events;

namespace HBP.Data.Visualization
{
    [DataContract]
    public class Sphere : Volume
    {
        #region Properties
        [DataMember(Name = "Center")]
        SerializableVector3 m_Center;
        public Vector3 Center
        {
            get
            {
                return m_Center.ToVector3();
            }
            set
            {
                m_Center = new SerializableVector3(value);
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
        public Sphere(Vector3 center, float radius)
        {
            OnChangeCenter = new UnityEvent();
            OnChangeRadius = new UnityEvent();
            m_Center = new SerializableVector3(center);
            m_Radius = radius;
        }
        public override bool IsInVolume(Vector3 position)
        {
           return Vector3.Distance(Center, position) <= Radius;
        }
        public override object Clone()
        {
            return new Sphere(Center, Radius);
        }
        #endregion
    }
}