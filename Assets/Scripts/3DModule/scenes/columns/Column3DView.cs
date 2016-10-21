
/**
 * \file    Column3DView.cs
 * \author  Lance Florian
 * \date    21/03/2016
 * \brief   Define Column3DView class
 */


// system
using System.Text;
using System.Collections;
using System.Collections.Generic;

// unity
using UnityEngine;

namespace HBP.VISU3D
{
    /// <summary>
    /// Column 3D view base class
    /// </summary>
    abstract public class Column3DView : MonoBehaviour
    {
        #region members

        public string m_label = ""; /**< name of  the column */
        public string Label
        {
            get { return m_label; }
            set { m_label = value;  }
        }

        // scene
        public bool isIRMF;
        protected bool spScene; /**< is sp scene column ? */
        protected int idColumn; /**< id of the column */
        public string layerColumn; /**< layer of the column */

        // cuts        
        public int nbCuts;  /**< planes cut number */

        // plots
        public int idSelectedPlot = -1;  /**< id of the selected plot of the column */
        public DLL.RawPlotList RawElectrodes = null;  /**< raw format of the plots container dll */
        public GameObject patientPlotsParent = null; /**< parent of the plots */
        public List<List<List<GameObject>>> plotsGO = null; /**< plots GO list with order : patient/electrode/plot */
        public List<Plot> Plots = null; /**< plots list */

        // select plot
        protected SelectRing m_selectRing = null;
        public SelectRing SelectRing { get { return m_selectRing; } }

        // ROI
        protected ROI m_ROI = null;   /**< selected ROI of the column */
        public ROI ROI { get { return m_ROI;} }

        #endregion members

        #region functions


        /// <summary>
        /// Base init class of the column
        /// </summary>
        /// <param name="idColumn"></param>
        /// <param name="nbCuts"></param>
        /// <param name="meshSplitNb"></param>
        /// <param name="plots"></param>
        /// <param name="plotsGO"></param>
        public void init(int idColumn, int nbCuts, int meshSplitNb, DLL.ElectrodesPatientMultiList plots, List<GameObject> PlotsPatientParent)
        {
            // scene
            this.idColumn = idColumn;
            layerColumn = "C" + this.idColumn + "_";
            spScene = transform.parent.name == "SP";
            if (spScene)
                layerColumn += "SP";
            else
                layerColumn += "MP";

            // cuts
            this.nbCuts = nbCuts;

            // select ring
            gameObject.AddComponent<SelectRing>();
            m_selectRing = gameObject.GetComponent<SelectRing>();
            m_selectRing.setLayer(layerColumn);

            // plots
            RawElectrodes = new DLL.RawPlotList();
            plots.extractRawPlotList(RawElectrodes);

            GameObject patientPlotsParent = new GameObject("elecs");
            patientPlotsParent.transform.SetParent(transform);

            plotsGO = new List<List<List<GameObject>>>(PlotsPatientParent.Count);
            Plots = new List<Plot>(plots.getTotalPlotsNumber());
            for (int ii = 0; ii < PlotsPatientParent.Count; ++ii)
            {
                // instantiate patient plots
                GameObject patientPlots = Instantiate(PlotsPatientParent[ii]);
                patientPlots.transform.SetParent(patientPlotsParent.transform);
                patientPlots.name = PlotsPatientParent[ii].name;

                plotsGO.Add(new List<List<GameObject>>(patientPlots.transform.childCount));
                for (int jj = 0; jj < patientPlots.transform.childCount; ++jj)
                {
                    int nbPlots = patientPlots.transform.GetChild(jj).childCount;

                    plotsGO[ii].Add(new List<GameObject>(nbPlots));
                    for (int kk = 0; kk < nbPlots; ++kk)
                    {
                        plotsGO[ii][jj].Add(patientPlots.transform.GetChild(jj).GetChild(kk).gameObject);
                        plotsGO[ii][jj][kk].layer = LayerMask.NameToLayer(layerColumn);
                        Plots.Add(patientPlots.transform.GetChild(jj).GetChild(kk).gameObject.GetComponent<Plot>());

                        int id = Plots.Count - 1;
                        Plots[id].exclude = false;
                        Plots[id].columnROI = !spScene;
                        Plots[id].columnMask = false;
                        Plots[id].blackList = false;
                    }
                }
            }            
        }

        /// <summary>
        ///  Clean all allocated data
        /// </summary>
        public abstract void clean();


        /// <summary>
        /// Set the plots visibility state
        /// </summary>
        /// <param name="isVisible"></param>
        public void setVisiblePlots(bool isVisible)
        {
            string layer;
            if (isVisible)
                layer = layerColumn;
            else
                layer = "Inactive";

            for(int ii = 0; ii < Plots.Count; ++ii)
            {
                Plots[ii].gameObject.layer = LayerMask.NameToLayer(layer);
            }
        }


        /// <summary>
        /// Return the current selected plot of the column
        /// </summary>
        /// <returns></returns>
        public Plot currentSelectedPlot()
        {
            if (idSelectedPlot >= 0 && idSelectedPlot < Plots.Count)
                return Plots[idSelectedPlot];

            return null;
        }

        /// <summary>
        /// Update the ROI
        /// </summary>
        /// <param name="ROI"></param>
        public void updateROI(ROI ROI)
        {
            if(m_ROI != null)
            {
                m_ROI.setVisible(false);
            }

            m_ROI = ROI;
            m_ROI.setVisible(true);
        }

        /// <summary>
        /// Retrieve a string containing all the plots states
        /// </summary>
        /// <returns></returns>
        public string getPlotsStateStr()
        {
            string text = "Plots states\n";
            int id = 0;
            for(int ii = 0; ii < plotsGO.Count; ++ii) // patients
            {
                text += "n " + Plots[id].patientName + "\n";
                for (int jj = 0; jj < plotsGO[ii].Count; ++jj) // electrodes
                {
                    text += "e " + jj + "\n";
                    for (int kk = 0; kk < plotsGO[ii][jj].Count; ++kk, id++) // plots
                    {
                        string plotGOName = plotsGO[ii][jj][kk].name;
                        string[] split = plotGOName.Split('_');
                        text += split[split.Length - 1] + " " + (Plots[id].exclude ? 1 : 0) + " " + (Plots[id].blackList ? 1 : 0 ) + " " + (Plots[id].columnMask ? 1 : 0) + " " + (Plots[id].highlight? 1 : 0) + "\n";
                    }
                }
            }
            
            return text;
        }

        public string getOnlyPlotsInROIStr()
        {
            List<bool> sitesInROIPerPatient = new List<bool>(plotsGO.Count);
            List<List<bool>> sitesInROIPerElectrode = new List<List<bool>>(plotsGO.Count);
            List<List<List<bool>>> sitesInROIPerPlot = new List<List<List<bool>>>(plotsGO.Count);
            int id = 0;
            for (int ii = 0; ii < plotsGO.Count; ++ii)
            {                
                bool isInROIP = false;
                sitesInROIPerPlot.Add(new List<List<bool>>(plotsGO[ii].Count));
                sitesInROIPerElectrode.Add(new List<bool>(plotsGO[ii].Count));
                for (int jj = 0; jj < plotsGO[ii].Count; ++jj)
                {
                    bool isInROIElec = false;
                    sitesInROIPerPlot[ii].Add(new List<bool>(plotsGO[ii][jj].Count));
                    for (int kk = 0; kk < plotsGO[ii][jj].Count; ++kk, ++id)
                    {
                        bool inROI = !Plots[id].columnROI;
                        bool blackList = Plots[id].blackList;

                        bool keep = inROI && !blackList;

                        if (!isInROIElec)
                            isInROIElec = keep;

                        if (!isInROIP)
                        {
                            isInROIP = keep;
                        }

                        sitesInROIPerPlot[ii][jj].Add(keep);
                    }
                    sitesInROIPerElectrode[ii].Add(isInROIElec);
                }
                sitesInROIPerPatient.Add(isInROIP);
            }

            string text = "";
            id = 0;
            for (int ii = 0; ii < plotsGO.Count; ++ii) // patients
            {
                if (sitesInROIPerPatient[ii])
                    text += "n " + Plots[id].patientName + "\n";

                for (int jj = 0; jj < plotsGO[ii].Count; ++jj) // electrodes
                {
                    if (sitesInROIPerElectrode[ii][jj])
                        text += "e " + jj + "\n";

                    for (int kk = 0; kk < plotsGO[ii][jj].Count; ++kk, id++) // plots
                    {
                        if (sitesInROIPerPlot[ii][jj][kk])
                        {
                            string plotGOName = plotsGO[ii][jj][kk].name;
                            string[] split = plotGOName.Split('_');
                            text += split[split.Length - 1] + " " + (Plots[id].exclude ? 1 : 0) + " " + (Plots[id].blackList ? 1 : 0) + " " + (Plots[id].columnMask ? 1 : 0) + " " + (Plots[id].highlight ? 1 : 0) + "\n";
                        }
                    }
                }
            }

            return text;
        }


        abstract public void updateCutPlanesNumber(int nbCuts);

        #endregion members
    }
}
