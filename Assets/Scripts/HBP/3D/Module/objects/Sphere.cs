



// unity
using System.Runtime.Serialization;
/**
* \file    Bubble.cs
* \author  Lance Florian
* \date    22/04/2016
* \brief   Define Bubble
*/
using UnityEngine;

namespace HBP.Module3D
{
    /// <summary>
    /// Define a bubble used in ROI
    /// </summary>
    [DataContract]
    public class Sphere : MonoBehaviour
    {
        #region Properties
        private float m_MinRaySphere = 0.5f;    
        private float m_MaxRaySphere = 100f;

        [DataMember(Name = "Position")]
        private SerializableVector3 m_Position;
        public Vector3 Position
        {
            get
            {
                return m_Position.ToVector3();
            }
            private set
            {
                m_Position = new SerializableVector3(value);
                transform.position = value;
            }
        }

        private float m_RadiusPercentage = 0.0f;
        private float m_TargetRadius = 5.0f;
        [DataMember(Name = "Radius")]
        private float m_Radius = 1.0f;
        public float Radius
        {
            get
            {
                return m_Radius;
            }
            set
            {
                m_Radius = value;
                if (m_Radius > m_MaxRaySphere)
                    m_Radius = m_MaxRaySphere;

                if (m_Radius < m_MinRaySphere)
                    m_Radius = m_MinRaySphere;

                transform.localScale = new Vector3(m_Radius, m_Radius, m_Radius);

                ApplicationState.Module3D.OnChangeROIVolumeRadius.Invoke();
            }
        }

        private bool m_Selected = false;
        public bool Selected
        {
            get
            {
                return m_Selected;
            }
            set
            {
                m_Selected = value;
                if (m_Selected)
                {
                    GetComponent<Renderer>().sharedMaterial = SharedMaterials.ROI.Selected;
                }
                else
                {
                    GetComponent<Renderer>().sharedMaterial = SharedMaterials.ROI.Normal;
                }
            }
        }
        #endregion

        #region Private Methods
        private void Update()
        {
            if (m_RadiusPercentage < 1.0f)
            {
                Radius = Mathf.SmoothStep(m_Radius, m_TargetRadius, m_RadiusPercentage);
                m_RadiusPercentage += 2 * Time.deltaTime;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Init the bubble
        /// </summary
        /// <param name="layer"> layer of the bubble gameobject </param>
        /// <param name="radius"></param>
        /// <param name="position"></param>
        public void Initialize(int layer, float radius, Vector3 position)
        {
            gameObject.layer = layer;
            Position = position;
            m_TargetRadius = radius;
            gameObject.SetActive(true);

            // add mesh
            gameObject.GetComponent<MeshFilter>().sharedMesh = SharedMeshes.ROISphere;
        }
        /// <summary>
        /// Check if a collision occurs with the input ray
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="hitInfo"></param>
        /// <returns></returns>
        public bool CheckCollision(Ray ray, out RaycastHit hitInfo)
        {
            return GetComponent<SphereCollider>().Raycast(ray, out hitInfo, Mathf.Infinity);
        }
        #endregion
    }
}