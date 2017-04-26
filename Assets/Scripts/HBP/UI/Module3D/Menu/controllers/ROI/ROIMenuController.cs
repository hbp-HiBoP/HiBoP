
/**
 * \file    ROIMenuController.cs
 * \author  Lance Florian
 * \date    21/04/2016
 * \brief   Define ROIMenuController class
 */


// system
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

// unity
using UnityEngine;
using UnityEngine.Events;

namespace HBP.Module3D
{
    namespace Events
    {
        /// <summary>
        /// Send the path of the saved ROI
        /// </summary>
        [System.Serializable]
        public class ROISavedEvent : UnityEvent<string> { }
    }


    /// <summary>
    /// A class for managing the ROI menu for all the columns of the MNI scene
    /// </summary>
    public class ROIMenuController : MonoBehaviour
    {
        #region members

        // scene
        private MultiPatients3DScene m_scene = null; /**< MP scene */

        // canvas
        public Transform m_middlePanelT = null; /**< middle scene panel transform */

        // events
        public Events.ROISavedEvent ROISavedEvent = new Events.ROISavedEvent();

        // menus
        List<GameObject> m_columnROI = new List<GameObject>();
        ColumnROI m_currentROICol = null;

        #endregion members

        #region mono_behaviour


        #endregion mono_behaviour

        #region functions

        public void init(MultiPatients3DScene scene)
        {
            m_scene = scene;

            // default initlization with one menu per scene
            add_menu();


            m_scene.AskROIUpdateEvent.AddListener((idColumn) =>
            {
                if (idColumn == -1)
                {
                    for (int ii = 0; ii < m_columnROI.Count; ++ii)
                    {
                        m_columnROI[ii].GetComponent<ColumnROI>().set_selected_ROI(0);
                    }
                }
                else
                {
                    m_columnROI[idColumn].GetComponent<ColumnROI>().set_selected_ROI(0);
                }

            });
        }

        /// <summary>
        /// Add a new column menu
        /// </summary>
        public void add_menu()
        {
            int id = m_columnROI.Count;
            //m_displayMenues.Add(false);
            GameObject newROICol = new GameObject("Column " + id);
            m_columnROI.Add(newROICol);
            newROICol.transform.SetParent(transform);
            newROICol.AddComponent<ColumnROI>();
            newROICol.GetComponent<ColumnROI>().init(id, Instantiate(GlobalGOPreloaded.ROILeftMenu), m_scene, m_middlePanelT);

            newROICol.GetComponent<ColumnROI>().ROISynchroEvent.AddListener((idColumn, ROIElemGO) =>
            {
                for (int ii = 0; ii < m_columnROI.Count; ++ii)
                {
                    if (ii != idColumn)
                    {
                        m_columnROI[ii].GetComponent<ColumnROI>().synchro_ROI(ROIElemGO);
                    }
                }
            });

            newROICol.GetComponent<ColumnROI>().SaveROIEvent.AddListener(() =>
            {
                string pathFile = save_ROI();
                if (pathFile.Length == 0)
                {
                    m_scene.display_sceen_message("ERROR during ROI saving !", 2f, 200, 80);
                    return;
                }

                m_scene.display_sceen_message("ROI successfully saved.", 2f, 200, 80);
                ROISavedEvent.Invoke(pathFile);
            });

            newROICol.GetComponent<ColumnROI>().LoadROIEvent.AddListener(() =>
            {
                if (load_ROI())
                    m_scene.display_sceen_message("ROI successfully loaded.", 2f, 200, 80);
                else
                    m_scene.display_sceen_message("ERROR during ROI loading !", 2f, 200, 80);
            });
        }

        /// <summary>
        /// Remove the last column menu
        /// </summary>
        public void remove_last_menu()
        {
            // destroy the column gameobject menu
            int id = m_columnROI.Count - 1;
            m_columnROI[id].GetComponent<ColumnROI>().clean();
            Destroy(m_columnROI[id]);

            // remove the element from the list
            m_columnROI.RemoveAt(id);
        }


        /// <summary>
        /// Update the UI visibility with the current mode
        /// </summary>
        /// <param name="mode"></param>
        public void set_UI_activity(Mode mode)
        {            
            if (m_currentROICol == null)
                return;

            //bool menuDisplayed = m_displayMenu;
            bool menuDisplayed = m_currentROICol.m_isDisplayed;

            // define mode ui specifities
            //switch (mode.m_idMode)
            //{
            //case Mode.ModesId.AmplitudesComputed:
            //    menuDisplayed = menuDisplayed && true;
            //    break;
            //case Mode.ModesId.AllPathDefined:
            //    menuDisplayed = menuDisplayed &&  true;
            //    break;
            //}

            // set the state of the menu
            set_UI_visibility(menuDisplayed);
        }

        /// <summary>
        /// Set the visibility of the UI
        /// </summary>
        /// <param name="visible"></param>
        public void set_UI_visibility(bool visible)
        {
            m_currentROICol.m_isDisplayed = visible;
            update_UI();
        }

        public void update_ROI_mode()
        {
            if (m_currentROICol.m_isDisplayed)
                m_scene.enable_ROI_creation_mode();
            else
                m_scene.disable_ROI_creatino_mode();
        }


        /// <summary>
        /// Define the current column selected
        /// </summary>
        /// <param name="columnId"></param>
        public void define_current_column(int columnId = -1)
        {
            if (columnId == -1 || columnId >= m_columnROI.Count)
                return;

            m_currentROICol = m_columnROI[columnId].GetComponent<ColumnROI>();

            update_UI();            
        }

        /// <summary>
        /// Update the menu UI activity with the current parameters
        /// </summary>
        private void update_UI()
        {
            // hide all the menus
            for (int ii = 0; ii < m_columnROI.Count; ++ii)
                m_columnROI[ii].GetComponent<ColumnROI>().set_menu_active(false);

            m_currentROICol.set_menu_active(m_currentROICol.m_isDisplayed);
            update_ROI_mode();
        }

        /// <summary>
        /// Adapt the number of menues
        /// </summary>
        /// <param name="nbColumns"></param>
        public void define_columns_number(int nbColumns)
        {
            int currColNb = m_columnROI.Count;
            for (int ii = 0; ii < currColNb; ++ii)
            {
                remove_last_menu();
            }

            for (int ii = 0; ii < nbColumns; ++ii)
            {
                add_menu();
            }
        }

        /// <summary>
        /// Save the current column ROI and qirzq states
        /// </summary>
        /// <returns></returns>
        public string save_ROI()
        {
            string[] filters = new string[] { "roi"};
            string ROIPath = Module3D.DLL.QtGUI.get_saved_file_name(filters, "Save column ROI and plots state...", "./" + m_scene.Column3DViewManager.SelectedColumn.SelectedROI.m_ROIname + ".roi");

            if (ROIPath.Length == 0) // no path selected
                return "";

            File.WriteAllText(ROIPath, m_scene.get_current_column_ROI_and_sites_state_str(), Encoding.UTF8);
            string[] parts = ROIPath.Split('.');
            File.WriteAllText(parts[0] + ".sites", m_scene.get_sites_in_ROI(), Encoding.UTF8);

            return ROIPath;
        }

        /// <summary>
        /// Load a ROI and associated plots states and add it in the current column
        /// </summary>
        /// <returns></returns>
        public bool load_ROI()
        {
            string[] filters = new string[] { "roi" };
            string ROIPath = Module3D.DLL.QtGUI.GetExistingFileName(filters, "Select a ROI file...");

            if (ROIPath.Length == 0) // no path selected
                return false;

            string fileText = File.ReadAllText(ROIPath, Encoding.UTF8);

            string[] lines = fileText.Split('\n');
            bool readROI = false, readStates = false;
            List<Vector3> positions = new List<Vector3>();
            List<float> rays = new List<float>();
            string nameROI = "";

            List<string> patientsNames = new List<string>();
            List<List<List<SiteInformation>>> electrodes = new List<List<List<SiteInformation>>>();
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
                        Debug.LogError("-ERROR : ROIMenuController::load_ROI -> data incorrect at line " + ii);
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
                            Debug.LogError("-ERROR : ROIMenuController::load_ROI -> data incorrect at line " + ii);
                            return false;
                        }
                        patientsNames.Add(lineElems[1]);
                        electrodes.Add(new List<List<SiteInformation>>());
                        currP++;
                        continue;
                    }
                    else if(lineElems[0] == "e")
                    {
                        if (lineElems.Length != 2)
                        {
                            Debug.LogError("-ERROR : ROIMenuController::load_ROI -> data incorrect at line " + ii);
                            return false;
                        }

                        electrodes[currP].Add(new List<SiteInformation>());
                        currE = int.Parse(lineElems[1], CultureInfo.InvariantCulture.NumberFormat);
                        continue;
                    }
                    else
                    {
                        if (lineElems.Length != 5)
                        {
                            Debug.LogError("-ERROR : ROIMenuController::load_ROI -> data incorrect at line " + ii);
                            return false;
                        }

                        SiteInformation plot = new SiteInformation();
                        plot.PatientName = patientsNames[patientsNames.Count - 1];
                        plot.FullName = lineElems[0];
                        plot.ElectrodeID = currE;
                        plot.IsExcluded = int.Parse(lineElems[1], CultureInfo.InvariantCulture.NumberFormat) == 1;
                        plot.IsBlackListed = int.Parse(lineElems[2], CultureInfo.InvariantCulture.NumberFormat) == 1;
                        plot.IsMasked = int.Parse(lineElems[3], CultureInfo.InvariantCulture.NumberFormat) == 1;
                        plot.IsHighlighted = int.Parse(lineElems[4], CultureInfo.InvariantCulture.NumberFormat) == 1;

                        if (plot.IsExcluded || plot.IsBlackListed || plot.IsMasked || plot.IsHighlighted)
                            electrodes[currP][currE].Add(plot);
                    }
                }
            }

            m_scene.update_sites_mask(m_currentROICol.m_idColumn, electrodes, patientsNames);
            m_columnROI[m_currentROICol.m_idColumn].GetComponent<ColumnROI>().add_ROI(positions, rays, nameROI);

            m_scene.data_.iEEGOutdated = true;
            return true;
        }


        #endregion functions
    }
}