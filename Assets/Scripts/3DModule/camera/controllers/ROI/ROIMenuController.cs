
/**
 * \file    ROIMenuController.cs
 * \author  Lance Florian
 * \date    21/04/2016
 * \brief   Define ROIMenuController class
 */


// system
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

// unity
using UnityEngine;
using UnityEngine.UI;

namespace HBP.VISU3D
{
    /// <summary>
    /// A class for managing the ROI menu for all the columns of the MNI scene
    /// </summary>
    public class ROIMenuController : MonoBehaviour//, UICameraOverlay
    {
        #region members
       
        // scene
        private MP3DScene m_mpScene = null; /**< MP scene */

        // canvas
        public Transform m_middlePanelT = null; /**< middle scene panel transform */

        // menus
        List<GameObject> m_mpColumnROI = new List<GameObject>();

        // parameters
        private bool m_displayMenu = false; /**< menu displayed ? */
        private int m_mpID = 0; /**< ID of the mp column menu to display */

        // right buttons menu
        private RightMenuController m_rightMenuController = null;

        #endregion members

        #region mono_behaviour

        /// <summary>
        /// Start is called before the first frame update only if the script instance is enabled.
        /// </summary>
        void Start()
        {
            // retrieve right menu controller
            m_rightMenuController = transform.parent.Find("right scene buttons").GetComponent<RightMenuController>();
        }

        #endregion mono_behaviour

        #region functions

        public void init(ScenesManager scenesManager)
        {
            m_mpScene = scenesManager.MPScene;

            // default initlization with one menu per scene
            addMenu();

            // listeners
            m_rightMenuController.m_roiMenuStateEvent.AddListener(() =>
            {
                switchUIVisibility();
            });

            m_mpScene.AskROIUpdateEvent.AddListener((idColumn) =>
            {
                if (idColumn == -1)
                {
                    for (int ii = 0; ii < m_mpColumnROI.Count; ++ii)
                    {
                        m_mpColumnROI[ii].GetComponent<ColumnElement>().setSelectedROI(0);
                    }
                }
                else
                {
                    m_mpColumnROI[idColumn].GetComponent<ColumnElement>().setSelectedROI(0);
                }

            });
        }

        /// <summary>
        /// Add a new column menu
        /// </summary>
        public void addMenu()
        {
            int id = m_mpColumnROI.Count;
            GameObject newROICol = new GameObject("Column " + id);
            m_mpColumnROI.Add(newROICol);
            newROICol.transform.SetParent(transform);
            newROICol.AddComponent<ColumnElement>();
            newROICol.GetComponent<ColumnElement>().init(id, Instantiate(BaseGameObjects.ROILeftMenu), m_mpScene, m_middlePanelT);

            newROICol.GetComponent<ColumnElement>().m_synchronizeEvent.AddListener((idColumn) =>
            {
                //List<GameObject> roiListToSynchronize = m_mpColumnROI[idColumn].GetComponent<ColumnElement>().m_ROIList;

                //for (int ii = 0; ii < m_mpColumnROI.Count; ++ii)
                //{
                //    if (ii == idColumn)
                //        continue;
                    

                //}
            });

            newROICol.GetComponent<ColumnElement>().m_saveROIEvent.AddListener(() =>
            {
                if (saveROI())
                    m_mpScene.displayScreenMessage("ROI successfully saved.", 2f, 200, 80);
                else
                    m_mpScene.displayScreenMessage("ERROR during ROI saving !", 2f, 200, 80);
            });

            newROICol.GetComponent<ColumnElement>().m_loadROIEvent.AddListener(() =>
            {
                if (loadROI())
                    m_mpScene.displayScreenMessage("ROI successfully loaded.", 2f, 200, 80);
                else
                    m_mpScene.displayScreenMessage("ERROR during ROI loading !", 2f, 200, 80);
            });
        }

        /// <summary>
        /// Remove the last column menu
        /// </summary>
        public void removeLastMenu()
        {
            // destroy the column gameobject menu
            int id = m_mpColumnROI.Count - 1;
            m_mpColumnROI[id].GetComponent<ColumnElement>().clean();
            Destroy(m_mpColumnROI[id]);

            // remove the element from the list
            m_mpColumnROI.RemoveAt(id);

            if(m_mpID >= m_mpColumnROI.Count)
                m_mpID = m_mpColumnROI.Count - 1;
        }


        /// <summary>
        /// Update the UI visibility with the current mode
        /// </summary>
        /// <param name="mode"></param>
        public void setUIActivity(Mode mode)
        {
            bool menuDisplayed = false;
            if (mode.m_sceneSp)
            {
                menuDisplayed = false;
            }
            else
            {                
                switch (mode.m_idMode)
                {
                    case Mode.ModesId.ROICreation:
                        menuDisplayed = true;
                        break;
                }
            }

            // set the state of the menu
            setUIVisibility(menuDisplayed);
        }

        /// <summary>
        /// Swith the UI visibility of the UI
        /// </summary>
        public void switchUIVisibility()
        {
            m_displayMenu = !m_displayMenu;
            updateUI();
        }

        /// <summary>
        /// Set the visibility of the UI
        /// </summary>
        /// <param name="visible"></param>
        private void setUIVisibility(bool visible)
        {
            m_displayMenu = visible;
            updateUI();
        }

        /// <summary>
        /// Define the current scene and column selected
        /// </summary>
        /// <param name="columnId"></param>
        public void defineCurrentMenu(int columnId = -1)
        {           
            if (columnId != -1)
            {
                m_mpID = columnId;
            }

            updateUI();
        }

        /// <summary>
        /// Update the menu UI activity with the current parameters
        /// </summary>
        private void updateUI()
        {
            // hide all the menus
            for (int ii = 0; ii < m_mpColumnROI.Count; ++ii)
                m_mpColumnROI[ii].GetComponent<ColumnElement>().SetMenuActive(false);

            // display the menu corresponding to the current scene and column
            if (m_displayMenu)
            {
                if(m_mpID < m_mpColumnROI.Count && m_mpID != -1)
                    m_mpColumnROI[m_mpID].GetComponent<ColumnElement>().SetMenuActive(true);
            }
        }

        /// <summary>
        /// Adapt the number of menues
        /// </summary>
        /// <param name="nbColumns"></param>
        public void defineColumnsNb(int nbColumns)
        {
            int currColNb = m_mpColumnROI.Count;
            for (int ii = 0; ii < currColNb; ++ii)
            {
                removeLastMenu();
            }

            for (int ii = 0; ii < nbColumns; ++ii)
            {
                addMenu();
            }
        }

        /// <summary>
        /// Save the current column ROI and plots states
        /// </summary>
        /// <returns></returns>
        public bool saveROI()
        {
            string[] filters = new string[] { "roi"};
            string ROIPath = DLL.QtGUI.getSaveFileName(filters, "Save column ROI and plots state...", "./" + m_mpScene.CM.currentColumn().ROI.m_name + ".roi");

            if (ROIPath.Length == 0) // no path selected
                return false;

            File.WriteAllText(ROIPath, m_mpScene.getCurrentColumnROIAndPlotsStateStr(), Encoding.UTF8);
            string[] parts = ROIPath.Split('.');
            File.WriteAllText(parts[0] + ".sites", m_mpScene.getSitesInROI(), Encoding.UTF8);

            return true;
        }

        /// <summary>
        /// Load a ROI and associated plots states and add it in the current column
        /// </summary>
        /// <returns></returns>
        public bool loadROI()
        {
            string[] filters = new string[] { "roi" };
            string ROIPath = DLL.QtGUI.getOpenFileName(filters, "Select a ROI file...");

            if (ROIPath.Length == 0) // no path selected
                return false;

            string fileText = File.ReadAllText(ROIPath, Encoding.UTF8);

            string[] lines = fileText.Split('\n');
            bool readROI = false, readStates = false;
            List<Vector3> positions = new List<Vector3>();
            List<float> rays = new List<float>();
            string nameROI = "";

            List<string> patientsNames = new List<string>();
            List<List<List<PlotI>>> electrodes = new List<List<List<PlotI>>>();
            int currE = 0;
            int currP = -1;

            for (int ii = 0; ii < lines.Length; ++ii)
            {
                if(lines[ii] == "ROI :")
                {
                    readROI = true;
                    ii++;
                    nameROI = lines[ii];
                    continue;
                }

                if (lines[ii] == "Plots states")
                {
                    readROI = false;
                    readStates = true;
                    continue;
                }

                if (lines[ii].Length == 0)
                    break;

                if(readROI)
                {
                    string[] lineElems = lines[ii].Split(' ');
                    if(lineElems.Length != 5)
                    {
                        Debug.LogError("-ERROR : ROIMenuController::loadROI -> data incorrect at line " + ii);
                        return false;
                    }

                    rays.Add(float.Parse(lineElems[1], CultureInfo.InvariantCulture.NumberFormat));
                    positions.Add(new Vector3(float.Parse(lineElems[2], CultureInfo.InvariantCulture.NumberFormat),
                                float.Parse(lineElems[3], CultureInfo.InvariantCulture.NumberFormat),
                                float.Parse(lineElems[4], CultureInfo.InvariantCulture.NumberFormat)));
                    
                }
                else if (readStates)
                {
                    string[] lineElems = lines[ii].Split(' ');
                    if(lineElems[0] == "n")
                    {
                        if(lineElems.Length != 2)
                        {
                            Debug.LogError("-ERROR : ROIMenuController::loadROI -> data incorrect at line " + ii);
                            return false;
                        }
                        patientsNames.Add(lineElems[1]);
                        electrodes.Add(new List<List<PlotI>>());
                        currP++;
                        continue;
                    }
                    else if(lineElems[0] == "e")
                    {
                        if (lineElems.Length != 2)
                        {
                            Debug.LogError("-ERROR : ROIMenuController::loadROI -> data incorrect at line " + ii);
                            return false;
                        }

                        electrodes[currP].Add(new List<PlotI>());
                        currE = int.Parse(lineElems[1], CultureInfo.InvariantCulture.NumberFormat);
                        continue;
                    }
                    else
                    {
                        if (lineElems.Length != 5)
                        {
                            Debug.LogError("-ERROR : ROIMenuController::loadROI -> data incorrect at line " + ii);
                            return false;
                        }

                        PlotI plot = new PlotI();
                        plot.patientName = patientsNames[patientsNames.Count - 1];
                        plot.name = lineElems[0];
                        plot.idElectrode = currE;
                        plot.exclude = int.Parse(lineElems[1], CultureInfo.InvariantCulture.NumberFormat) == 1;
                        plot.blackList = int.Parse(lineElems[2], CultureInfo.InvariantCulture.NumberFormat) == 1;
                        plot.columnMask = int.Parse(lineElems[3], CultureInfo.InvariantCulture.NumberFormat) == 1;
                        plot.highlight = int.Parse(lineElems[4], CultureInfo.InvariantCulture.NumberFormat) == 1;

                        if (plot.exclude || plot.blackList || plot.columnMask || plot.highlight)
                            electrodes[currP][currE].Add(plot);
                    }
                }
            }

            m_mpScene.updatePlotMasks(m_mpID, electrodes, patientsNames);
            m_mpColumnROI[m_mpID].GetComponent<ColumnElement>().addROI(positions, rays, nameROI);


            m_mpScene.data_.iEEGOutdated = true;
            return true;
        }


        #endregion functions
    }
}