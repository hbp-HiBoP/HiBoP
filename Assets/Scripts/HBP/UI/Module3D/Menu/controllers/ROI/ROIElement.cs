/**
 * \file    ROIElement.cs
 * \author  Lance Florian
 * \date    21/04/2016
 * \brief   Define ROIElement class
 */

// system
using System.Collections;
using System.Collections.Generic;

// unity
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Module3D
{
    /// <summary>
    /// ROI element in the Column ROI scroll list
    /// </summary>
    public class ROIElement : MonoBehaviour
    {
        // ROI
        public string m_layerMask;  /**< layer of the ROI */
        public bool m_isMinimize = false; /**< is the UI minimized ? */
        public int m_idROI = 0; /**< id of the ROI element */
        public GameObject m_ROI = null; /**< associated ROI gameobject */
        public GameObject m_ROIUI = null; /**< ROI UI gameobject */
        public Scrollbar m_scrollbar = null; /**< scrollbar of the bubbles */

        // bubbles
        public float m_defaultRay = 3f; /**< default ray of the bubbles */
        public int m_idSelectedBubble = 0; /**< id of the current selected bubble */
        private GameObject m_defaultBubbleUI = null; /**< default bubble element */
        private Transform m_bubblesUIParent = null; /**< parent of the bubble element */
        public List<GameObject> m_bubblesUIList = new List<GameObject>(); /**< bubbles UI list */

        // events
        public NoParamEvent m_closeBubbleEvent = new NoParamEvent(); /**< event for closing bubble */
        public SendIntValueEvent m_closeROIEvent = new SendIntValueEvent(); /**< event for closing ROI */
        public SendIntValueEvent m_selectROIEvent = new SendIntValueEvent(); /**< event for selecting a ROI */

        // scene
        MultiPatients3DScene m_mpScene = null;

        public void init(string layerMask, int idROI, GameObject ROIElement, MultiPatients3DScene mpScene, Transform parentROIUI, Transform parentROI)
        {
            m_layerMask = layerMask;
            m_idROI = idROI;
            m_mpScene = mpScene;

            // init UI ROI element
            m_ROIUI = ROIElement;
            m_ROIUI.transform.SetParent(parentROIUI);
            m_ROIUI.transform.localScale = new Vector3(1, 1, 1);
            Vector3 currPos = m_ROIUI.transform.localPosition;
            m_ROIUI.transform.localPosition = new Vector3(currPos.x, currPos.y, 0);

            // init ROI
            m_ROI = new GameObject("ROI " + idROI);
            m_ROI.AddComponent<ROI>();
            m_ROI.transform.SetParent(parentROI);

            // retrieve default ROI element
            m_bubblesUIParent = m_ROIUI.transform.Find("bubbles scrollable list panel").Find("scrollable list").Find("grid elements");
            m_scrollbar = m_bubblesUIParent.parent.parent.Find("list scrollbar").GetComponent<Scrollbar>();
            m_defaultBubbleUI = m_bubblesUIParent.Find("default bubble element").gameObject;

            // listeners
            m_ROIUI.transform.Find("minimize button").GetComponent<Button>().onClick.AddListener(() =>
            {
                switch_minimize_state();
            });

            m_ROIUI.transform.Find("close button").GetComponent<Button>().onClick.AddListener(() =>
            {
                m_closeROIEvent.Invoke(m_idROI);
            });

            m_ROIUI.transform.Find("select button").GetComponent<Button>().onClick.AddListener(() =>
            {
                m_selectROIEvent.Invoke(m_idROI);
            });

            m_ROIUI.transform.Find("title input field").GetComponent<InputField>().onEndEdit.AddListener((text) =>
            {
                m_ROI.GetComponent<ROI>().m_ROIname = text;
            });
            

            update_ID(idROI);
        }

        public void clean()
        {
            // destroy bubbles
            for (int ii = 0; ii < m_bubblesUIList.Count; ++ii)
            {
                if (m_bubblesUIList[ii] != null)
                {
                    m_bubblesUIList[ii].GetComponent<BubbleElement>().clean();
                    Destroy(m_bubblesUIList[ii]);
                }
            }

            // destroy ROI
            if(m_ROIUI != null)
                Destroy(m_ROIUI);

            if (m_ROI != null)
            {
                m_ROI.GetComponent<ROI>().clean();
                Destroy(m_ROI);
            }
        }

        /// <summary>
        /// Update the ROI with a new ID
        /// </summary>
        /// <param name="id"></param>
        public void update_ID(int id)
        {
            m_idROI = id;
            m_ROIUI.name = "ROI element " + m_idROI;
            m_ROIUI.transform.Find("ROI name text").GetComponent<Text>().text = "ROI " + m_idROI;
            m_ROIUI.SetActive(true);
        }

        /// <summary>
        /// Update the name of the ROI
        /// </summary>
        /// <param name="name"></param>
        public void update_name(string name)
        {
            m_ROIUI.transform.Find("title input field").GetComponent<InputField>().text = name;
            m_ROI.GetComponent<ROI>().m_ROIname = name;
        }


        /// <summary>
        /// Switch the minimized state of the ROI
        /// </summary>
        private void switch_minimize_state()
        {
            m_isMinimize = !m_isMinimize;
            m_ROIUI.transform.Find("minimize button").Find("Text").GetComponent<Text>().text = m_isMinimize ? "+" : "-";
            m_ROIUI.transform.Find("bubbles scrollable list panel").gameObject.SetActive(!m_isMinimize);
            m_ROIUI.GetComponent<LayoutElement>().minHeight = m_isMinimize ? 50 : 130;
        }

        /// <summary>
        /// Define the selected state of the ROI
        /// </summary>
        /// <param name="selected"></param>
        public void set_selected_date(bool selected)
        {
            // update UI
            m_ROIUI.GetComponent<Image>().color = selected ? Color.green : Color.black;
            m_ROIUI.transform.Find("select button").gameObject.SetActive(!selected);
            m_ROIUI.transform.Find("close button").GetComponent<Button>().interactable = !selected;
            m_ROIUI.transform.Find("ROI name text").GetComponent<Text>().color = (selected ? Color.black : Color.white);
            m_ROIUI.transform.Find("ROI name text").GetComponent<Text>().fontStyle = (selected ? FontStyle.Bold : FontStyle.Normal);
        }

        /// <summary>
        /// Add a new bubble in the ROI
        /// </summary>
        /// <param name="position"></param>
        /// <param name="ray"></param>
        public void add_bubble(Vector3 position, float ray = -1f)
        {
            int id = m_bubblesUIList.Count;
            GameObject newBubbleUI = new GameObject("Bubble element " + id);
            m_bubblesUIList.Add(newBubbleUI);
            newBubbleUI.transform.SetParent(transform);
            newBubbleUI.AddComponent<BubbleElement>();
            newBubbleUI.GetComponent<BubbleElement>().init(id, Instantiate(m_defaultBubbleUI), m_bubblesUIParent);
            newBubbleUI.GetComponent<BubbleElement>().update_ray_text((ray < 0f) ? m_defaultRay : ray);

            // add bubble in the ROI
            m_ROI.GetComponent<ROI>().add_bubble(m_layerMask, "Bubble " + id, position, (ray < 0f) ? m_defaultRay : ray);

            // init listeners
            newBubbleUI.GetComponent<BubbleElement>().CloseBubbleEvent.AddListener((bubbleId) =>
            {
                remove_bubble(bubbleId);
                m_closeBubbleEvent.Invoke();
            });
            newBubbleUI.GetComponent<BubbleElement>().SelectBubbleEvent.AddListener((bubbleId) =>
            {
                m_bubblesUIList[m_idSelectedBubble].GetComponent<BubbleElement>().set_selected_state(false);
                m_idSelectedBubble = bubbleId;
                m_ROI.GetComponent<ROI>().select_bubble(m_idSelectedBubble);
                m_bubblesUIList[m_idSelectedBubble].GetComponent<BubbleElement>().set_selected_state(true);
            });

            // unselect bubbles
            for (int ii = 0; ii < m_bubblesUIList.Count; ++ii)
                m_bubblesUIList[ii].GetComponent<BubbleElement>().set_selected_state(false);                
            m_idSelectedBubble = m_bubblesUIList.Count - 1;

            // select the new created bubble            
            m_bubblesUIList[m_idSelectedBubble].GetComponent<BubbleElement>().set_selected_state(true);
            m_ROI.GetComponent<ROI>().select_bubble(m_idSelectedBubble);

            // set the scrollbar down
            StartCoroutine("scrollbar_down");

            m_mpScene.SceneInformation.IsiEEGOutdated = true;
        }

        IEnumerator scrollbar_down()
        {
            yield return new WaitForSeconds(0.05f);
            m_scrollbar.value = 0;
        }

        /// <summary>
        /// Remove the input bubble of the ROI
        /// </summary>
        /// <param name="id"></param>
        public void remove_bubble(int id)
        {
            // ROI
            m_ROI.GetComponent<ROI>().remove_bubble(id);

            // ROI UI
            if (m_idSelectedBubble > id)
                m_idSelectedBubble--;
            else if (m_idSelectedBubble == id)
                m_idSelectedBubble = -1;

            m_bubblesUIList[id].GetComponent<BubbleElement>().clean();
            Destroy(m_bubblesUIList[id]);
            m_bubblesUIList.RemoveAt(id);

            for (int ii = id; ii < m_bubblesUIList.Count; ++ii)
            {
                m_bubblesUIList[ii].GetComponent<BubbleElement>().update_ID(ii);
            }

            // update selection
            if (m_bubblesUIList.Count > 0)
            {
                if (m_idSelectedBubble == -1 )
                    m_idSelectedBubble = m_bubblesUIList.Count - 1;

                m_bubblesUIList[m_idSelectedBubble].GetComponent<BubbleElement>().set_selected_state(true);
            }

            StartCoroutine("update_scrollbar");
            m_mpScene.SceneInformation.IsiEEGOutdated = true;
        }


        IEnumerator update_scrollbar()
        {
            yield return new WaitForSeconds(0.05f);
            m_scrollbar.value = 0.01f;
        }

        /// <summary>
        /// Select the bubble corresponding to the input id
        /// </summary>
        /// <param name="idBubble"></param>
        public void select_bubble(int idBubble)
        {
            m_bubblesUIList[m_idSelectedBubble].GetComponent<BubbleElement>().set_selected_state(false);
            m_idSelectedBubble = idBubble;
            m_bubblesUIList[m_idSelectedBubble].GetComponent<BubbleElement>().set_selected_state(true);
            m_ROI.GetComponent<ROI>().select_bubble(m_idSelectedBubble);
        }

        /// <summary>
        /// Apply a coeff to the size of the bubble coppresonding to the input id
        /// </summary>
        /// <param name="idBubble"></param>
        /// <param name="coeff"></param>
        public void change_bubble_size(int idBubble, float coeff)
        {
            if (m_bubblesUIList.Count == 0)
                return;

            m_ROI.GetComponent<ROI>().change_bubble_size(idBubble, coeff);
            m_bubblesUIList[idBubble].GetComponent<BubbleElement>().update_ray_text(m_ROI.GetComponent<ROI>().bubble(idBubble).m_radius);

            m_mpScene.SceneInformation.IsiEEGOutdated = true;
        }

        /// <summary>
        /// Return the ROI associated to the UI
        /// </summary>
        /// <returns></returns>
        public ROI associated_ROI()
        {
            return m_ROI.GetComponent<ROI>();
        }
    }
}