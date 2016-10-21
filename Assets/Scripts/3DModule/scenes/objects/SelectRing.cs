
/**
 * \file    SelectRing.cs
 * \author  Lance Florian
 * \date    03/05/2016
 * \brief   Define SelectRing classe
 */

// system
using System;
using System.Text;
using System.Collections.Generic;

// unity
using UnityEngine;

namespace HBP.VISU3D
{
    /// <summary>
    /// A dual rotated ring
    /// </summary>
    public class SelectRing : MonoBehaviour
    {
        #region members

        private Plot m_selectedPlot = null;
        private GameObject m_ring1 = null;
        private GameObject m_ring2 = null;
        private GameObject m_ring3 = null;

        private float m_speedRotation = 100f;

        #endregion members

        #region mono_behaviour

        /// <summary>
        /// This function is always called before any Start functions and also just after a prefab is instantiated. (If a GameObject is inactive during start up Awake is not called until it is made active.)
        /// </summary>
        void Awake()
        {
            m_ring1 = Instantiate(BaseGameObjects.Ring);
            m_ring1.name = "ring 1";
            m_ring1.transform.SetParent(transform);
            m_ring1.GetComponent<MeshFilter>().mesh = Geometry.createTube(1.7f);
            m_ring1.SetActive(false);
            m_ring1.transform.localEulerAngles = new Vector3(90, 0, 0);

            m_ring2 = Instantiate(BaseGameObjects.Ring);
            m_ring2.name = "ring 2";
            m_ring2.transform.SetParent(transform);            
            m_ring2.GetComponent<MeshFilter>().mesh = Geometry.createTube(1.5f);
            m_ring2.SetActive(false);
            m_ring2.transform.localEulerAngles = new Vector3(0, 90, 0);

            m_ring3 = Instantiate(BaseGameObjects.Ring);
            m_ring3.name = "ring 3";
            m_ring3.transform.SetParent(transform);
            m_ring3.GetComponent<MeshFilter>().mesh = Geometry.createTube(1.3f);
            m_ring3.SetActive(false);
            m_ring3.transform.localEulerAngles = new Vector3(0, 0, 90);
        }

        /// <summary>
        /// Update is called once per frame. It is the main workhorse function for frame updates.
        /// </summary>
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
                setMaterial(SharedMaterials.Ring.Selected);
                //setMaterial(m_selectedPlot.GetComponent<Renderer>().sharedMaterial);
            }
        }

        #endregion mono_behaviour

        /// <summary>
        /// Set the current selected plot
        /// </summary>
        /// <param name="plot"></param>
        public void setSelectedPlot(Plot plot, Vector3 scale)
        {
            m_selectedPlot = plot;
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

            //if (notNullPlot)
            //    setMaterial(plot.GetComponent<Renderer>().sharedMaterial);
        }

        /// <summary>
        /// Define the layer
        /// </summary>
        /// <param name="layer"></param>
        public void setLayer(string layer)
        {
            m_ring1.layer = m_ring2.layer = m_ring3.layer = LayerMask.NameToLayer(layer);
        }

        /// <summary>
        /// Define the material
        /// </summary>
        /// <param name="material"></param>
        public void setMaterial(Material material)
        {
            m_ring1.GetComponent<MeshRenderer>().sharedMaterial = material;
            m_ring2.GetComponent<MeshRenderer>().sharedMaterial = material;
            m_ring3.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

    }
}