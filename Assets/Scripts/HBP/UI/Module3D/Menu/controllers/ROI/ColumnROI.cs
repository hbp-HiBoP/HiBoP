
/**
 * \file    ColumnROI.cs
 * \author  Lance Florian
 * \date    21/04/2016
 * \brief   Define ColumnElement class
 */

// system
using System.Collections;
using System.Collections.Generic;

// unity
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace HBP.Module3D
{
    namespace Events
    {
        /// <summary>
        /// Send the path of the saved ROI
        /// </summary>
        public class ROISynchroEvent : UnityEvent<int, GameObject> { }
    }

    /// <summary>
    /// Column ROI
    /// </summary>
    public class ColumnROI : MonoBehaviour
    {
        public bool m_isDisplayed = false;


        public int m_idColumn = 0;      /**< id of the column */
        public int m_idROISelected = 0; /**< id of the selected ROI of the column */

        public GameObject m_menu = null; /**< column ROI menu gameobject */
        public List<GameObject> m_ROIList = new List<GameObject>(); /**< ROI elements of the column menu */

        private Transform m_ROIElementsParent = null; /**< parent of the ROI element */
        private MP3DScene m_mpScene = null;
        private Scrollbar m_scrollbar = null; /**< scrollbar of the ROI list */

        public GameObject m_colROIListParent = null;

        private string m_layerMask = ""; /**< layer mask of the column */

        // events
        public Events.ROISynchroEvent ROISynchroEvent = new Events.ROISynchroEvent(); /**< event for synchronizing the ROI of this columns with all others columns */
        public NoParamEvent SaveROIEvent = new NoParamEvent(); /**< event for saving the ROI and the plots states of the current column */
        public NoParamEvent LoadROIEvent = new NoParamEvent(); /**< event for loading the ROI and the plots states and add it in the current column */

        public void init(int columnId, GameObject menu, MP3DScene mpScene, Transform parentMenu)
        {
            m_idColumn = columnId;
            m_layerMask = "C" + m_idColumn + "_MP";
            m_menu = menu;
            m_mpScene = mpScene;

            string name = "MNI ROI left menu " + m_idColumn;

            // define names
            m_menu.name = name;
            m_menu.transform.SetParent(parentMenu);
            m_menu.transform.localScale = new Vector3(1, 1, 1);
            m_menu.transform.SetAsFirstSibling();

            // retrieve default ROI element
            m_ROIElementsParent = m_menu.transform.Find("panel").Find("ROI list parent").Find("ROI scrollable list panel").Find("scrollable list").Find("grid elements");
            m_scrollbar = m_ROIElementsParent.parent.parent.Find("list scrollbar").GetComponent<Scrollbar>();

            m_menu.transform.Find("panel").Find("ROI list parent").Find("add ROI button").GetComponent<Button>().onClick.AddListener(() =>
            {
                add_ROI();
            });

            m_menu.transform.Find("panel").Find("copy ROI parent").Find("copy ROI button").GetComponent<Button>().onClick.AddListener(() =>
            {
                ROISynchroEvent.Invoke(m_idColumn, selected_ROI_element().gameObject);
            });

            m_menu.transform.Find("panel").Find("save ROI parent").Find("save ROI button").GetComponent<Button>().onClick.AddListener(() =>
            {
                SaveROIEvent.Invoke();
            });

            m_menu.transform.Find("panel").Find("load ROI parent").Find("load ROI button").GetComponent<Button>().onClick.AddListener(() =>
            {
                LoadROIEvent.Invoke();
            });


            m_mpScene.CreateBubbleEvent.AddListener((position, idC) =>
            {
                if (idC == m_idColumn)
                {
                    selected_ROI_element().add_bubble(position);
                    m_mpScene.update_current_ROI(idC);
                }
            });

            m_mpScene.SelectBubbleEvent.AddListener((idC, idB) =>
            {
                if (idC == m_idColumn)
                    selected_ROI_element().select_bubble(idB);
            });

            m_mpScene.ChangeSizeBubbleEvent.AddListener((idC, idB, coeff) =>
            {
                if (idC == m_idColumn)
                {
                    selected_ROI_element().change_bubble_size(idB, coeff);
                    m_mpScene.update_current_ROI(idC);
                }
            });

            m_mpScene.RemoveBubbleEvent.AddListener((idC, idB) =>
            {
                if (idC == m_idColumn)
                {
                    selected_ROI_element().remove_bubble(idB);
                    m_mpScene.update_current_ROI(idC);
                }
            });

            // init the parent of the column ROIs
            m_colROIListParent = new GameObject("Col " + columnId);
            m_colROIListParent.transform.SetParent(mpScene.transform.Find("ROI list"));

            // init with 1 ROI
            add_ROI();
        }

        public void clean()
        {
            for(int ii = 0; ii < m_ROIList.Count; ++ii)
            {
                if (m_ROIList[ii] != null)
                {
                    m_ROIList[ii].GetComponent<ROIElement>().clean();
                    Destroy(m_ROIList[ii]);
                }
            }

            if(m_menu != null)
                Destroy(m_menu);
        }

        public void set_menu_active(bool active)
        {
            m_menu.SetActive(active);
        }

        /// <summary>
        /// Synchronize the current ROI with all the columns
        /// </summary>
        /// <param name="ROIElemGo"></param>
        public void synchro_ROI(GameObject ROIElemGo)
        {
            add_ROI();

            ROI currROI = ROIElemGo.GetComponent<ROIElement>().m_ROI.GetComponent<ROI>();
            for (int jj = 0; jj < currROI.bubbles_nb(); ++jj)
            {
                Bubble bubble = currROI.bubble(jj);
                m_ROIList[m_ROIList.Count-1].GetComponent<ROIElement>().add_bubble(bubble.transform.position, bubble.m_radius);                    
            }

            set_selected_ROI(m_ROIList.Count - 1);
        }

        /// <summary>
        /// Add a new ROI initialized with input bubbles
        /// </summary>
        /// <param name="bubblesPositions"></param>
        /// <param name="rays"></param>
        public void add_ROI(List<Vector3> bubblesPositions, List<float> rays, string ROIName)
        {
            // add a new ROI UI
            add_ROI();

            // add bubbles in this ROI UI
            for(int ii = 0; ii < bubblesPositions.Count; ++ii)
            {
                m_ROIList[m_ROIList.Count-1].GetComponent<ROIElement>().add_bubble(bubblesPositions[ii], rays[ii]);
            }
            
            // update the scene ROI
            m_mpScene.update_current_ROI(m_idColumn);

            // update ROI name
            m_ROIList[m_ROIList.Count - 1].GetComponent<ROIElement>().update_name(ROIName);

            // define the ROI as the current one
            set_selected_ROI(m_ROIList.Count - 1);
        }

        private void add_ROI()
        {
            int id = m_ROIList.Count;
            GameObject newROIElement = new GameObject("ROI UI " + id);
            m_ROIList.Add(newROIElement);

            newROIElement.transform.SetParent(transform);
            newROIElement.AddComponent<ROIElement>();
            newROIElement.GetComponent<ROIElement>().init(m_layerMask, id, Instantiate(m_ROIElementsParent.Find("default ROI panel").gameObject), m_mpScene, m_ROIElementsParent, m_colROIListParent.transform);

            // init listeners
            newROIElement.GetComponent<ROIElement>().m_closeROIEvent.AddListener((roiId) =>
            {
                remove_ROI(roiId);
            });

            newROIElement.GetComponent<ROIElement>().m_selectROIEvent.AddListener((roiId) =>
            {
                set_selected_ROI(roiId);
            });

            newROIElement.GetComponent<ROIElement>().m_closeBubbleEvent.AddListener(() =>
            {
                m_mpScene.update_current_ROI(m_idColumn);
            });

            // set the scrollbar down
            StartCoroutine("scrollabar_down");

            m_mpScene.data_.iEEGOutdated = true;
        }

        IEnumerator scrollabar_down()
        {
            yield return new WaitForSeconds(0.1f);
            m_scrollbar.value = 0;      
        }

        private void remove_ROI(int id)
        {
            if (id < m_idROISelected)
                m_idROISelected--;

            m_ROIList[id].GetComponent<ROIElement>().clean();
            Destroy(m_ROIList[id]);
            m_ROIList.RemoveAt(id);

            for (int ii = id; ii < m_ROIList.Count; ++ii)
            {
                m_ROIList[ii].GetComponent<ROIElement>().update_ID(ii);
            }

            StartCoroutine("update_scrollbar");

            m_mpScene.data_.iEEGOutdated = true;
        }

        IEnumerator update_scrollbar()
        {
            yield return new WaitForSeconds(0.05f);
            m_scrollbar.value = 0.01f;
        }

        public void set_selected_ROI(int idROI)
        {
            // update slected ROI id
            m_idROISelected = idROI;
            for (int ii = 0; ii < m_ROIList.Count; ++ii)
            {
                m_ROIList[ii].GetComponent<ROIElement>().set_selected_date((m_idROISelected == ii) ? true : false); // (only one ROI can be selected on a column)
            }

            // update the scene with the new selected ROI
            m_mpScene.update_ROI(m_idColumn, selected_ROI_element().associated_ROI());
        }

        public ROIElement selected_ROI_element()
        {
            return m_ROIList[m_idROISelected].GetComponent<ROIElement>();
        }

    }
}