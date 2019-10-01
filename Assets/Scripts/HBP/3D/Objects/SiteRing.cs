
/**
 * \file    SelectRing.cs
 * \author  Lance Florian
 * \date    03/05/2016
 * \brief   Define SelectRing classe
 */

// unity
using UnityEngine;

namespace HBP.Module3D
{
    /// <summary>
    /// A dual rotated ring
    /// </summary>
    public class SiteRing : MonoBehaviour
    {
        #region Properties
        private Site m_SelectedSite = null;
        private GameObject m_Ring = null;

        private float m_AnimationSpeed = 0.05f;
        private float m_AnimationMin = 0.40f;
        private float m_AnimationMax = 0.60f;
        private float m_AnimationCurrentStep = 0.45f;
        private bool m_AnimationDirection = true;
        private float m_Scale = 1.0f;

        [SerializeField]
        private GameObject m_SelectRingPrefab;
        #endregion

        #region Private Methods
        void Awake()
        {
            m_Ring = Instantiate(m_SelectRingPrefab);
            m_Ring.name = "Ring";
            m_Ring.transform.SetParent(transform);
            m_Ring.transform.localPosition = Vector3.zero;
            m_Ring.transform.localEulerAngles = new Vector3(90, 0, 0);
            m_Ring.SetActive(false);
            m_Ring.GetComponent<MeshRenderer>().sharedMaterial = SharedMaterials.Site.SelectionRing;
        }
        void Update()
        {
            UnityEngine.Profiling.Profiler.BeginSample("select ring");
            if(m_SelectedSite != null)
            {
                if (m_AnimationDirection)
                {
                    m_AnimationCurrentStep += m_AnimationSpeed * Time.deltaTime;
                    if (m_AnimationCurrentStep > m_AnimationMax)
                    {
                        m_AnimationCurrentStep = m_AnimationMax;
                        m_AnimationDirection = false;
                    }
                }
                else
                {
                    m_AnimationCurrentStep -= m_AnimationSpeed * Time.deltaTime;
                    if (m_AnimationCurrentStep < m_AnimationMin)
                    {
                        m_AnimationCurrentStep = m_AnimationMin;
                        m_AnimationDirection = true;
                    }
                }
                DestroyImmediate(m_Ring.GetComponent<MeshFilter>().mesh);
                m_Ring.GetComponent<MeshFilter>().mesh = Geometry.CreateTube(m_Scale, m_AnimationCurrentStep, 0.1f, 10);
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Define the current selected site and apply a scale on the GO transforms
        /// </summary>
        /// <param name="site"></param>
        /// <param name="scale"></param>
        public void SetSelectedSite(Site site, Vector3 scale)
        {
            m_SelectedSite = site;
            bool notNullPlot = m_SelectedSite != null;

            if (notNullPlot)
                transform.position = m_SelectedSite.transform.position;
            
            m_Ring.SetActive(notNullPlot);

            m_Scale = scale.x * 0.95f;

            DestroyImmediate(m_Ring.GetComponent<MeshFilter>().mesh);
            m_Ring.GetComponent<MeshFilter>().mesh = Geometry.CreateTube(m_Scale, m_AnimationCurrentStep, 0.1f, 20);
        }
        /// <summary>
        /// Define the layer of the rings GO
        /// </summary>
        /// <param name="layer"></param>
        public void SetLayer(string layer)
        {
            if(m_Ring != null)
                m_Ring.layer = LayerMask.NameToLayer(layer);
        }

        public void SelectRingFaceCamera(Camera camera)
        {
            if (m_SelectedSite)
            {
                transform.LookAt(camera.transform);
            }
        }
        #endregion
    }
}