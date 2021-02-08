using Tools.CSharp;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.Module3D
{
    /// <summary>
    /// Class containing the parameters for the activity of an anatomical column
    /// </summary>
    public class AnatomyDataParameters
    {
        #region Properties
        /// <summary>
        /// Minimum distance for a site to influence a vertex of the mesh
        /// </summary>
        private const float MIN_INFLUENCE = 0.0f;
        /// <summary>
        /// Maximum distance for a site to influence a vertex of the mesh
        /// </summary>
        private const float MAX_INFLUENCE = 50.0f;
        private float m_InfluenceDistance = 15.0f;
        /// <summary>
        /// Distance for a site to influence a vertex of the mesh
        /// </summary>
        public float InfluenceDistance
        {
            get
            {
                return m_InfluenceDistance;
            }
            set
            {
                float val = Mathf.Clamp(value, MIN_INFLUENCE, MAX_INFLUENCE);
                if (m_InfluenceDistance != val)
                {
                    m_InfluenceDistance = val;
                    OnUpdateInfluenceDistance.Invoke();
                }
            }
        }

        private float m_Gain = 1.0f;
        /// <summary>
        /// Gain for the size of the sites
        /// </summary>
        public float Gain
        {
            get
            {
                return m_Gain;
            }
            set
            {
                if (m_Gain != value)
                {
                    m_Gain = value;
                    OnUpdateGain.Invoke();
                }
            }
        }
        #endregion
        
        #region Events
        /// <summary>
        /// Event called when updating the sphere gain
        /// </summary>
        public UnityEvent OnUpdateGain = new UnityEvent();
        /// <summary>
        /// Event called when updating the maximum influence
        /// </summary>
        public UnityEvent OnUpdateInfluenceDistance = new UnityEvent();
        #endregion
    }
}