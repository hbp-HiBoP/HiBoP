/**
 * \file    LatencyButton.cs
 * \author  Lance Florian
 * \date    25/03/2016
 * \brief   Define LatencyButton class
 */

// unity
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace HBP.VISU3D
{
    namespace Events
    {
        /// <summary>
        /// Send the id of the latency file 
        /// </summary>
        public class ChooseLatencyFile : UnityEvent<int> { }
    }

    /// <summary>
    /// Manage the behaviour of the latency buttons
    /// </summary>
    public class LatencyButton : MonoBehaviour
    {
        #region members

        public int id = -1;
        private Events.ChooseLatencyFile m_chooseLatencyFile = new Events.ChooseLatencyFile();
        public Events.ChooseLatencyFile ChooseLatencyFile { get { return m_chooseLatencyFile; } }
        #endregion members

        #region others

        public void init(int idButton)
        {
            id = idButton;
            Button button = GetComponent<Button>();
            button.onClick.AddListener(
            delegate
            {
                m_chooseLatencyFile.Invoke(id);
            });
        }


        #endregion others
    }
}