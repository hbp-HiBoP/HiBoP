using HBP.Core.Object3D;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.Data.Module3D
{
    /// <summary>
    /// This class defines a Sphere of a <see cref="ROI"/>
    /// </summary>
    public class Sphere : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Minimum value for the radius of the sphere
        /// </summary>
        private float m_MinRadiusSphere = 0.5f;
        /// <summary>
        /// Maximum value for the radius of the sphere
        /// </summary>
        private float m_MaxRadiusSphere = 100f;
        
        private Vector3 m_Position;
        /// <summary>
        /// Position of the center of the sphere
        /// </summary>
        public Vector3 Position
        {
            get
            {
                return m_Position;
            }
            set
            {
                m_Position = value;
                transform.localPosition = value;
            }
        }

        /// <summary>
        /// Variable used for the SmoothStep for the animation when displaying the ROI
        /// </summary>
        private float m_RadiusPercentage = 0.0f;
        /// <summary>
        /// Target radius at the end of the animation
        /// </summary>
        private float m_TargetRadius = 5.0f;

        private float m_Radius = 1.0f;
        /// <summary>
        /// Radius of the sphere
        /// </summary>
        public float Radius
        {
            get
            {
                return m_Radius;
            }
            set
            {
                m_Radius = value;
                if (m_Radius > m_MaxRadiusSphere)
                    m_Radius = m_MaxRadiusSphere;

                if (m_Radius < m_MinRadiusSphere)
                    m_Radius = m_MinRadiusSphere;

                if (m_RadiusPercentage >= 1.0f)
                {
                    m_TargetRadius = m_Radius;
                }
                transform.localScale = new Vector3(m_Radius, m_Radius, m_Radius);

                OnChangeRadius.Invoke();
            }
        }

        private bool m_Selected = false;
        /// <summary>
        /// Is this sphere currently selected ?
        /// </summary>
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

        #region Events
        /// <summary>
        /// Event called when changing the radius of the sphere
        /// </summary>
        public UnityEvent OnChangeRadius = new UnityEvent();
        #endregion

        #region Private Methods
        private void Update()
        {
            if (m_RadiusPercentage < 2.0f)
            {
                m_RadiusPercentage += 2 * Time.deltaTime;
                Radius = Mathf.SmoothStep(m_Radius, m_TargetRadius, m_RadiusPercentage);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the sphere
        /// </summary>
        /// <param name="layer">Layer on which the sphere is displayed</param>
        /// <param name="name">Name of the sphere</param>
        /// <param name="radius">Initial radius of the sphere</param>
        /// <param name="position">Initial position of the sphere</param>
        public void Initialize(int layer, string name, float radius, Vector3 position)
        {
            gameObject.layer = layer;
            Position = position;
            m_TargetRadius = radius;
            gameObject.SetActive(true);
            
            gameObject.GetComponent<MeshFilter>().sharedMesh = SharedMeshes.ROISphere;
        }
        /// <summary>
        /// Check if a collision occurs between the input ray and the sphere
        /// </summary>
        /// <param name="ray">Ray to check the collision with</param>
        /// <param name="hitInfo">Return value of the raycast</param>
        /// <returns>True if a collision occured</returns>
        public bool CheckCollision(Ray ray, out RaycastHit hitInfo)
        {
            return GetComponent<SphereCollider>().Raycast(ray, out hitInfo, Mathf.Infinity);
        }
        /// <summary>
        /// Start the growing animation
        /// </summary>
        public void StartAnimation()
        {
            m_RadiusPercentage = 0.0f;
            Radius = 1.0f;
        }
        #endregion
    }
}