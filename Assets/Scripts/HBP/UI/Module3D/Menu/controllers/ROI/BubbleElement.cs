

/**
 * \file    BubbleElement.cs
 * \author  Lance Florian
 * \date    21/04/2016
 * \brief   Define BubbleElement class
 */

// unity
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.Module3D
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
        public GenericEvent<int> SelectBubbleEvent = new GenericEvent<int>(); /**< event for selecting a bubble */
        public GenericEvent<int> CloseBubbleEvent = new GenericEvent<int>(); /**< event for closing the bubble */

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
                CloseBubbleEvent.Invoke(m_idBubble);
            });

            m_bubbleUI.GetComponent<Button>().onClick.AddListener(() =>
            {
                SelectBubbleEvent.Invoke(m_idBubble);
            });

            update_ID(m_idBubble);
        }

        public void clean()
        {
            if(m_bubbleUI != null)
                Destroy(m_bubbleUI);
        }

        public void update_ID(int id)
        {
            m_idBubble = id;
            m_bubbleUI.name = "Bubble element " + m_idBubble;
            m_bubbleUI.transform.Find("bubble name text").GetComponent<Text>().text = "Bubble " + m_idBubble;
            m_bubbleUI.SetActive(true);
        }

        public void set_selected_state(bool state)
        {
            m_isSelected = state;
            m_bubbleUI.GetComponent<Image>().color = (state ? Color.green : Color.black);
            m_bubbleUI.transform.Find("bubble name text").GetComponent<Text>().color = (state ? Color.black : Color.white);
            m_bubbleUI.transform.Find("bubble name text").GetComponent<Text>().fontStyle = (state ? FontStyle.Bold : FontStyle.Normal);
            m_bubbleUI.transform.Find("ray text").GetComponent<Text>().color = (state ? Color.black : Color.gray);
            m_bubbleUI.transform.Find("ray text").GetComponent<Text>().fontStyle = (state ? FontStyle.Bold : FontStyle.Normal);
        }

        public void update_ray_text(float ray)
        {
            m_bubbleUI.transform.Find("ray text").GetComponent<Text>().text = "R: " + ray.ToString("0.00") + "mm";
        }
    }
}