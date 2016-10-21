
/**
 * \file    ColumnElement.cs
 * \author  Lance Florian
 * \date    21/04/2016
 * \brief   Define ColumnElement class
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
    /// Column ROI element of the ROI menu
    /// </summary>
    public class ColumnElement : MonoBehaviour
    {
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
        public SendIntValueEvent m_synchronizeEvent = new SendIntValueEvent(); /**< event for synchronizing the ROI of this columns with all others columns */
        public NoParamEvent m_saveROIEvent = new NoParamEvent(); /**< event for saving the ROI and the plots states of the current column */
        public NoParamEvent m_loadROIEvent = new NoParamEvent(); /**< event for loading the ROI and the plots states and add it in the current column */

        /// <summary>
        /// Initialize the column menu
        /// </summary>
        /// <param name="columnId"></param>
        /// <param name="menu"></param>
        /// <param name="mpScene"></param>
        /// <param name="parentMenu"></param>
        public void init(int columnId, GameObject menu, MP3DScene mpScene, Transform parentMenu)
        {
            m_idColumn = columnId;
            m_layerMask = "C" + m_idColumn + "_MP";
            m_menu = menu;
            m_mpScene = mpScene;

            string name = "MNI ROI left menu " + m_idColumn;

            // define names
            m_menu.name = name;
            m_menu.transform.SetParent(parentMenu.Find("ROI left menu list"));
            m_menu.transform.localScale = new Vector3(1, 1, 1);
            m_menu.transform.SetAsFirstSibling();

            // retrieve default ROI element
            m_ROIElementsParent = m_menu.transform.Find("panel").Find("ROI list parent").Find("ROI scrollable list panel").Find("scrollable list").Find("grid elements");
            m_scrollbar = m_ROIElementsParent.parent.parent.Find("list scrollbar").GetComponent<Scrollbar>();

            m_menu.transform.Find("panel").Find("ROI list parent").Find("add ROI button").GetComponent<Button>().onClick.AddListener(() =>
            {
                addROI();
            });

            m_menu.transform.Find("panel").Find("copy ROI parent").Find("copy ROI button").GetComponent<Button>().onClick.AddListener(() =>
            {
                m_synchronizeEvent.Invoke(m_idColumn);
            });

            m_menu.transform.Find("panel").Find("save ROI parent").Find("save ROI button").GetComponent<Button>().onClick.AddListener(() =>
            {
                m_saveROIEvent.Invoke();
            });


            m_menu.transform.Find("panel").Find("load ROI parent").Find("load ROI button").GetComponent<Button>().onClick.AddListener(() =>
            {
                m_loadROIEvent.Invoke();
            });


            m_mpScene.CreateBubbleEvent.AddListener((position, idC) =>
            {
                if (idC == m_idColumn)
                {
                    selectedROIElement().addBubble(position);
                    m_mpScene.updateCurrentROI(idC);
                }
            });

            m_mpScene.SelectBubbleEvent.AddListener((idC, idB) =>
            {
                if (idC == m_idColumn)
                    selectedROIElement().selectBubble(idB);
            });

            m_mpScene.ChangeSizeBubbleEvent.AddListener((idC, idB, coeff) =>
            {
                if (idC == m_idColumn)
                {
                    selectedROIElement().changeBubbleSize(idB, coeff);
                    m_mpScene.updateCurrentROI(idC);
                }
            });

            

            // init the parent of the column ROIs
            m_colROIListParent = new GameObject("Col " + columnId);
            m_colROIListParent.transform.SetParent(mpScene.transform.Find("ROI list"));

            // init with 1 ROI
            addROI();
        }

        /// <summary>
        /// Clean the column menu
        /// </summary>
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

        /// <summary>
        /// Define the acitvity of the column menu
        /// </summary>
        /// <param name="active"></param>
        public void SetMenuActive(bool active)
        {
            m_menu.SetActive(active);
        }

        /// <summary>
        /// Add a new ROI initialized with input bubbles
        /// </summary>
        /// <param name="bubblesPositions"></param>
        /// <param name="rays"></param>
        public void addROI(List<Vector3> bubblesPositions, List<float> rays, string ROIName)
        {
            // add a new ROI UI
            addROI();

            // add bubbles in this ROI UI
            for(int ii = 0; ii < bubblesPositions.Count; ++ii)
            {
                m_ROIList[m_ROIList.Count-1].GetComponent<ROIElement>().addBubble(bubblesPositions[ii], rays[ii]);
            }
            
            // update the scene ROI
            m_mpScene.updateCurrentROI(m_idColumn);

            // update ROI name
            m_ROIList[m_ROIList.Count - 1].GetComponent<ROIElement>().updateName(ROIName);

            // define the ROI as the current one
            setSelectedROI(m_ROIList.Count - 1);
        }

        /// <summary>
        /// Add a new ROI in the column
        /// </summary>
        private void addROI()
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
                removeROI(roiId);
            });

            newROIElement.GetComponent<ROIElement>().m_selectROIEvent.AddListener((roiId) =>
            {
                setSelectedROI(roiId);
            });

            newROIElement.GetComponent<ROIElement>().m_closeBubbleEvent.AddListener(() =>
            {
                m_mpScene.updateCurrentROI(m_idColumn);
            });

            // set the scrollbar down
            StartCoroutine("scrollabarDown");

            m_mpScene.data_.iEEGOutdated = true;
        }

        IEnumerator scrollabarDown()
        {
            yield return new WaitForSeconds(0.1f);
            m_scrollbar.value = 0;      
        }

        /// <summary>
        /// Remove the ROI from the column
        /// </summary>
        /// <param name="id"></param>
        private void removeROI(int id)
        {
            if (id < m_idROISelected)
                m_idROISelected--;

            m_ROIList[id].GetComponent<ROIElement>().clean();
            Destroy(m_ROIList[id]);
            m_ROIList.RemoveAt(id);

            for (int ii = id; ii < m_ROIList.Count; ++ii)
            {
                m_ROIList[ii].GetComponent<ROIElement>().updateID(ii);
            }

            StartCoroutine("updateScrollbar");

            m_mpScene.data_.iEEGOutdated = true;
        }

        IEnumerator updateScrollbar()
        {
            yield return new WaitForSeconds(0.05f);
            m_scrollbar.value = 0.01f;
        }

        /// <summary>
        /// Define the current selected ROI
        /// </summary>
        /// <param name="idROI"></param>
        public void setSelectedROI(int idROI)
        {
            // update slected ROI id
            m_idROISelected = idROI;
            for (int ii = 0; ii < m_ROIList.Count; ++ii)
            {
                m_ROIList[ii].GetComponent<ROIElement>().setSelectedState((m_idROISelected == ii) ? true : false); // (only one ROI can be selected on a column)
            }

            // update the scene with the new selected ROI
            m_mpScene.updateROI(m_idColumn, selectedROIElement().associatedROI());
        }

        public ROIElement selectedROIElement()
        {
            return m_ROIList[m_idROISelected].GetComponent<ROIElement>();
        }

    }
}