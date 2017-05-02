
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

        private Site m_selectedPlot = null;
        private GameObject m_ring1 = null;
        private GameObject m_ring2 = null;
        private GameObject m_ring3 = null;

        private float m_speedRotation = 100f;

        #endregion

        #region Private Methods

        void Awake()
        {
            m_ring1 = Instantiate(GlobalGOPreloaded.SelectRing);
            m_ring1.name = "ring 1";
            m_ring1.transform.SetParent(transform);
            m_ring1.GetComponent<MeshFilter>().mesh = Geometry.create_tube(1.7f);
            m_ring1.SetActive(false);
            m_ring1.transform.localEulerAngles = new Vector3(90, 0, 0);

            m_ring2 = Instantiate(GlobalGOPreloaded.SelectRing);
            m_ring2.name = "ring 2";
            m_ring2.transform.SetParent(transform);            
            m_ring2.GetComponent<MeshFilter>().mesh = Geometry.create_tube(1.5f);
            m_ring2.SetActive(false);
            m_ring2.transform.localEulerAngles = new Vector3(0, 90, 0);

            m_ring3 = Instantiate(GlobalGOPreloaded.SelectRing);
            m_ring3.name = "ring 3";
            m_ring3.transform.SetParent(transform);
            m_ring3.GetComponent<MeshFilter>().mesh = Geometry.create_tube(1.3f);
            m_ring3.SetActive(false);
            m_ring3.transform.localEulerAngles = new Vector3(0, 0, 90);
        }

        void Update()
        {
            if(m_selectedPlot != null)
            {
                m_ring1.transform.Rotate(new Vector3(1, 0, 0), m_speedRotation * Time.deltaTime);
                m_ring2.transform.Rotate(new Vector3(1, 0, 0), m_speedRotation * Time.deltaTime);
                m_ring3.transform.Rotate(new Vector3(1, 0, 0), m_speedRotation * Time.deltaTime);
            }

            if (m_selectedPlot)
            {
                set_material(SharedMaterials.Ring.Selected);
            }
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Define the current selected site and apply a scale on the GO transforms
        /// </summary>
        /// <param name="site"></param>
        /// <param name="scale"></param>
        public void set_selected_site(Site site, Vector3 scale)
        {
            m_selectedPlot = site;
            bool notNullPlot = m_selectedPlot != null;

            if (notNullPlot)
                m_ring1.transform.position = m_ring2.transform.position = m_ring3.transform.position = m_selectedPlot.transform.position;
            
            m_ring1.SetActive(notNullPlot);
            m_ring2.SetActive(notNullPlot);
            m_ring3.SetActive(notNullPlot);

            if (scale.x < 1)
                scale = new Vector3(1, 1, 1);

            m_ring1.transform.localScale = scale;
            m_ring2.transform.localScale = scale;
            m_ring3.transform.localScale = scale;
        }

        /// <summary>
        /// Define the layer of the rings GO
        /// </summary>
        /// <param name="layer"></param>
        public void set_layer(string layer)
        {
            if(m_ring1 != null)
                m_ring1.layer = m_ring2.layer = m_ring3.layer = LayerMask.NameToLayer(layer);
        }

        /// <summary>
        /// Define the material of the rings renderers
        /// </summary>
        /// <param name="material"></param>
        public void set_material(Material material)
        {
            m_ring1.GetComponent<MeshRenderer>().sharedMaterial = material;
            m_ring2.GetComponent<MeshRenderer>().sharedMaterial = material;
            m_ring3.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
        #endregion
    }
}