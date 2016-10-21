using UnityEngine;
using UnityEngine.UI;

using System.Collections;

namespace HBP.VISU3D
{

    public class AlterSlider : MonoBehaviour
    {

        private Slider m_slider;

        // Use this for initialization
        void Start()
        {
            m_slider = gameObject.GetComponent<Slider>();
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void addValueToSlider(float value)
        {
            m_slider.value += value;
        }
    }
}