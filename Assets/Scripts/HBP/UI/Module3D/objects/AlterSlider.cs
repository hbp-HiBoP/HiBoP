
/**
 * \file    AlterSlider.cs
 * \author  Lance Florian
 * \date    09/02/2016
 * \brief   Define AlterSlider class
 */

// unity
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Module3D
{
    public class AlterSlider : MonoBehaviour
    {
        private Slider m_slider;

        // Use this for initialization
        void Start()
        {
            m_slider = gameObject.GetComponent<Slider>();
        }

        public void addValueToSlider(float value)
        {
            m_slider.value += value;
        }
    }
}