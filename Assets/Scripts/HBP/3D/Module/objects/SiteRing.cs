
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
        private GameObject m_Ring1 = null;
        private GameObject m_Ring2 = null;
        private GameObject m_Ring3 = null;

        private float m_RotationSpeed = 100f;

        [SerializeField]
        private GameObject m_SelectRingPrefab;
        #endregion

        #region Private Methods
        void Awake()
        {
            m_Ring1 = Instantiate(m_SelectRingPrefab);
            m_Ring1.name = "Ring 1";
            m_Ring1.transform.SetParent(transform);
            m_Ring1.transform.localPosition = Vector3.zero;
            m_Ring1.GetComponent<MeshFilter>().mesh = Geometry.CreateTube(1.7f);
            m_Ring1.SetActive(false);
            m_Ring1.transform.localEulerAngles = new Vector3(90, 0, 0);

            m_Ring2 = Instantiate(m_SelectRingPrefab);
            m_Ring2.name = "Ring 2";
            m_Ring2.transform.SetParent(transform);
            m_Ring2.transform.localPosition = Vector3.zero;
            m_Ring2.GetComponent<MeshFilter>().mesh = Geometry.CreateTube(1.5f);
            m_Ring2.SetActive(false);
            m_Ring2.transform.localEulerAngles = new Vector3(0, 90, 0);

            m_Ring3 = Instantiate(m_SelectRingPrefab);
            m_Ring3.name = "Ring 3";
            m_Ring3.transform.SetParent(transform);
            m_Ring3.transform.localPosition = Vector3.zero;
            m_Ring3.GetComponent<MeshFilter>().mesh = Geometry.CreateTube(1.3f);
            m_Ring3.SetActive(false);
            m_Ring3.transform.localEulerAngles = new Vector3(0, 0, 90);
        }
        void Update()
        {
            if(m_SelectedSite != null)
            {
                m_Ring1.transform.Rotate(new Vector3(1, 0, 0), m_RotationSpeed * Time.deltaTime);
                m_Ring2.transform.Rotate(new Vector3(1, 0, 0), m_RotationSpeed * Time.deltaTime);
                m_Ring3.transform.Rotate(new Vector3(1, 0, 0), m_RotationSpeed * Time.deltaTime);
            }

            if (m_SelectedSite)
            {
                SetMaterial(SharedMaterials.Ring.Selected);
            }
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
                m_Ring1.transform.position = m_Ring2.transform.position = m_Ring3.transform.position = m_SelectedSite.transform.position;
            
            m_Ring1.SetActive(notNullPlot);
            m_Ring2.SetActive(notNullPlot);
            m_Ring3.SetActive(notNullPlot);

            if (scale.x < 1)
                scale = new Vector3(1, 1, 1);

            m_Ring1.transform.localScale = scale;
            m_Ring2.transform.localScale = scale;
            m_Ring3.transform.localScale = scale;
        }
        /// <summary>
        /// Define the layer of the rings GO
        /// </summary>
        /// <param name="layer"></param>
        public void SetLayer(string layer)
        {
            if(m_Ring1 != null)
                m_Ring1.layer = m_Ring2.layer = m_Ring3.layer = LayerMask.NameToLayer(layer);
        }
        /// <summary>
        /// Define the material of the rings renderers
        /// </summary>
        /// <param name="material"></param>
        public void SetMaterial(Material material)
        {
            m_Ring1.GetComponent<MeshRenderer>().sharedMaterial = material;
            m_Ring2.GetComponent<MeshRenderer>().sharedMaterial = material;
            m_Ring3.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
        #endregion
    }
}