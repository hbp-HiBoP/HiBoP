

/**
 * \file    BubbleElement.cs
 * \author  Lance Florian
 * \date    21/04/2016
 * \brief   Define BubbleElement class
 */


// system
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// unity
using UnityEngine;
using UnityEngine.UI;

namespace HBP.VISU3D
{
    /// <summary>
    /// Bubble element in the ROI  element scroll list
    /// </summary>
    public class BubbleElement : MonoBehaviour
    {
        public bool m_isSelected = false; /**< is the bubble selected ? */
        public int m_idBubble = 0; /**< id of the bubble element */

        public GameObject m_bubbleUI = null; /**< bubble UI gameobject */

        // events
        public SendIntValueEvent m_selectBubbleEvent = new SendIntValueEvent(); /**< event for selecting a bubble */
        public SendIntValueEvent m_closeBubbleEvent = new SendIntValueEvent(); /**< event for closing the bubble */

        /// <summary>
        /// Initialize the bubble
        /// </summary>
        /// <param name="idBubble"></param>
        /// <param name="bubbleUI"></param>
        /// <param name="parentBubbleUI"></param>
        public void init(int idBubble, GameObject bubbleUI, Transform parentBubbleUI)
        {
            m_idBubble = idBubble;
            m_bubbleUI = bubbleUI;
            m_bubbleUI.transform.SetParent(parentBubbleUI);
            m_bubbleUI.transform.localScale = new Vector3(1, 1, 1);
            Vector3 currPos = m_bubbleUI.transform.localPosition;
            m_bubbleUI.transform.localPosition = new Vector3(currPos.x, currPos.y, 0);

            // listeners
            m_bubbleUI.transform.Find("close button").GetComponent<Button>().onClick.AddListener(() =>
            {
                m_closeBubbleEvent.Invoke(m_idBubble);
            });

            m_bubbleUI.GetComponent<Button>().onClick.AddListener(() =>
            {
                m_selectBubbleEvent.Invoke(m_idBubble);
            });

            updateID(m_idBubble);
        }

        /// <summary>
        /// Clean the bubble element
        /// </summary>
        public void clean()
        {
            if(m_bubbleUI != null)
                Destroy(m_bubbleUI);
        }

        /// <summary>
        /// Update the bubble with a new ID
        /// </summary>
        /// <param name="id"></param>
        public void updateID(int id)
        {
            m_idBubble = id;
            m_bubbleUI.name = "Bubble element " + m_idBubble;
            m_bubbleUI.transform.Find("bubble name text").GetComponent<Text>().text = "Bubble " + m_idBubble;
            m_bubbleUI.SetActive(true);
        }

        /// <summary>
        /// Set the selected state of the bubble
        /// </summary>
        /// <param name="state"></param>
        public void setSelectedState(bool state)
        {
            m_isSelected = state;
            m_bubbleUI.GetComponent<Image>().color = (state ? Color.green : Color.black);
            m_bubbleUI.transform.Find("bubble name text").GetComponent<Text>().color = (state ? Color.black : Color.white);
            m_bubbleUI.transform.Find("bubble name text").GetComponent<Text>().fontStyle = (state ? FontStyle.Bold : FontStyle.Normal);
            m_bubbleUI.transform.Find("ray text").GetComponent<Text>().color = (state ? Color.black : Color.gray);
            m_bubbleUI.transform.Find("ray text").GetComponent<Text>().fontStyle = (state ? FontStyle.Bold : FontStyle.Normal);
        }

        /// <summary>
        /// Update the ray text
        /// </summary>
        /// <param name="ray"></param>
        public void updateRayText(float ray)
        {
            m_bubbleUI.transform.Find("ray text").GetComponent<Text>().text = "R: " + ray.ToString("0.00") + "mm";
        }
    }
}