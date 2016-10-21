
/**
 * \file    Column3DViewIEEG.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define Column3DViewIEEG class
 */

// system
using System.Collections;
using System.Collections.Generic;

// unity
using UnityEngine;

namespace HBP.VISU3D
{
    /// <summary>
    /// A 3D column view iEEG, containing all necessary data concerning a data column
    /// </summary>
    public class Column3DViewIEEG : Column3DView
    {
        #region members
        
        // data
        public Data.Visualisation.ColumnData columnData = null; /**< column data formalized by the unity main UI part */

        // textures generators
        public List<DLL.BrainTextureGenerator> DLLBrainTextureGeneratorList = null; /**< generators for each brain part  */
        public List<DLL.CutTextureGenerator> DLLCutTextureGeneratorList = null;     /**< generators for each brain cut */

        // textures 2D
        public List<Texture2D> brainCutWithAmpTextures = null;         /**< list of cut textures */
        public List<Texture2D> brainCutWithAmpRotatedTextures = null;  /**< list of cut rotated textures */

        // iEEG
        public bool sendInfos = true; /**< send info at each plot click ? */
        public bool updateAmplitude; /**< amplitude needs to be updated ? */
        public int columnTimeLineID = 0; /**< timeline column ID */
        public int currentTimeLineID = 0; /**< curent timeline column ID */
        public float sharedMinInf = 0f;
        public float sharedMaxInf = 0f;
        public float minAmp = float.MaxValue; /**< min amplitude value */
        public float maxAmp = float.MinValue; /**< max amplitude value */
        public float gainBubbles = 1f; /**< gain bubbles */
        public float middle = 0f;   /**< middle value */
        public float maxDistanceElec = 15f; /**< amplitude maximum influence of a plot */
        public float spanMin = -50f;
        public float spanMax = 50f;
        public float alphaMin = 0.2f; /**< minimum alpha */
        public float alphaMax = 1f; /**< maximum alpha */
        //  amplitudes
        public int[] dims = new int[3]; /**< amplitudes array dimensions (dims[0] = size| dims[1] = 1 (legacy) | dims[2] = plots number ) */
        public float[] amplitudes = new float[0]; /**< amplitudes 1D array (to be sent to the DLL) */        
        //  plots
        public List<Vector3> electrodesSizeScale = null;  /**< scale of the plots of this column */
        public List<bool> electrodesPositiveColor = null; /**< is positive color ? */

        // latencies
        public bool sourceDefined = false; /**< a source has been defined */
        public bool plotIsASource = false; /**< current selected plot is a source */
        public bool plotLatencyData = false; /**< latency data defined for the current selected plot */
        public int idSourceSelected = -1; /**< id of the selected source */
        public int currentLatencyFile = -1; /**< id of the current latency file */

        #endregion members

        #region functions


        /// <summary>
        /// Init class of the data column
        /// </summary>
        /// <param name="idColumn"></param>
        /// <param name="nbCuts"></param>
        /// <param name="meshSplitNb"></param>
        /// <param name="plots"></param>
        /// <param name="plotsGO"></param>
        public new void init(int idColumn, int nbCuts, int meshSplitNb, DLL.ElectrodesPatientMultiList plots, List<GameObject> PlotsPatientParent)
        {
            // call parent init
            base.init(idColumn, nbCuts, meshSplitNb, plots, PlotsPatientParent);
            isIRMF = false;

            // amplitudes
            updateAmplitude = false;

            // generators dll
            //      brain
            DLLBrainTextureGeneratorList = new List<DLL.BrainTextureGenerator>(meshSplitNb);
            for(int ii = 0; ii < meshSplitNb; ++ii)
                DLLBrainTextureGeneratorList.Add(new DLL.BrainTextureGenerator());            
            //      cuts
            DLLCutTextureGeneratorList = new List<DLL.CutTextureGenerator>();
            for (int ii = 0; ii < nbCuts; ++ii)
            {
                DLLCutTextureGeneratorList.Add(new DLL.CutTextureGenerator());
            }

            // textures 2D
            brainCutWithAmpTextures = new List<Texture2D>();
            brainCutWithAmpRotatedTextures = new List<Texture2D>();
            for (int jj = 0; jj < nbCuts; ++jj)
            {
                brainCutWithAmpTextures.Add(new Texture2D(1, 1));
                brainCutWithAmpRotatedTextures.Add(new Texture2D(1, 1));
            }

            // plots
            electrodesSizeScale = new List<Vector3>(RawElectrodes.getPlotsNumber());
            electrodesPositiveColor = new List<bool>(RawElectrodes.getPlotsNumber());

            // masks
            //maskExcluded = new bool[RawElectrodes.getPlotsNumber()];
            for (int ii = 0; ii < RawElectrodes.getPlotsNumber(); ii++)
            {
                electrodesSizeScale.Add(new Vector3(1, 1, 1));
                electrodesPositiveColor.Add(true);
                //maskExcluded[ii] = false;
            }
        }

        /// <summary>
        /// Update the plot mask of the dll with all the masks
        /// </summary>
        public void updateDLLPlotMask()
        {
            bool noROI = spScene ? false : (m_ROI.getNbSpheres() == 0);
            for (int ii = 0; ii < Plots.Count; ++ii)
            {
                RawElectrodes.updateMask(ii, (Plots[ii].columnMask || Plots[ii].blackList || Plots[ii].exclude || (Plots[ii].columnROI && !noROI)));
            }
        }


        /// <summary>
        /// Return the amplitudes dimension array
        /// </summary>
        /// <returns></returns>
        public int[] dimensions() { return dims; }

        /// <summary>
        /// Return the amplitudes array
        /// </summary>
        /// <returns></returns>
        public float[] getAmplitudes()
        {
            return amplitudes;
        }

        /// <summary>
        /// Return the length of the timeline 
        /// </summary>
        /// <returns></returns>
        public int timelineLength()
        {
            return dims[0];
        }

        /// <summary>
        /// Specify a new columnData to be associated with the columnd3DView
        /// </summary>
        /// <param name="columnData"></param>
        public void setColumnData(Data.Visualisation.ColumnData columnData)
        {
            this.columnData = columnData;

            // update amplitudes sizes and values
            dims = new int[3];
            dims[0] = columnData.TimeLine.Size;
            dims[1] = 1;
            dims[2] = columnData.MaskPlot.Length;

            minAmp = float.MaxValue;
            maxAmp = float.MinValue;

            amplitudes = new float[dims[0] * dims[1] * dims[2]];
            for (int ii = 0; ii < dims[0]; ++ii)
            {
                for (int jj = 0; jj < dims[2]; ++jj)
                {
                    amplitudes[ii * dims[2] + jj] = columnData.Values[jj][ii];

                    // update min/max values
                    if (columnData.Values[jj][ii] > maxAmp)
                        maxAmp = columnData.Values[jj][ii];

                    if (columnData.Values[jj][ii] < minAmp)
                        minAmp = columnData.Values[jj][ii];
                }
            }

            middle = (minAmp + maxAmp) / 2;
            spanMin = minAmp;
            spanMax = maxAmp;

            //maskColumn = columnData.MaskPlot; // copy ref array
            for (int ii = 0; ii < Plots.Count; ++ii)
            {
                Plots[ii].columnMask = columnData.MaskPlot[ii];
            }
        }

        /// <summary>
        /// Update plots sizes and colors arrays for iEEG (to be called before the rendering update)
        /// </summary>
        public void updatePlotsSizeAndColorsArraysForIEEG()
        {
            float diffMin = spanMin - middle;
            float diffMax = spanMax - middle;

            for (int ii = 0; ii < Plots.Count; ++ii)
            {
                float value = columnData.Values[ii][currentTimeLineID];
                if (value < spanMin)
                    value = spanMin;
                if (value > spanMax)
                    value = spanMax;

                value -= middle;

                if (value < 0)
                {
                    electrodesPositiveColor[ii] = false;
                    value = 0.5f + 2 * (value / diffMin);                    
                }
                else
                {
                    electrodesPositiveColor[ii] = true;
                    value = 0.5f + 2 * (value / diffMax);
                }

                electrodesSizeScale[ii] = new Vector3(value, value, value);
            }
        }


        /// <summary>
        /// Update the cut planes number of the 3D column view
        /// </summary>
        /// <param name="nbCuts"></param>
        public override void updateCutPlanesNumber(int nbCuts)
        {
            int diffCuts = this.nbCuts - nbCuts;
            this.nbCuts = nbCuts;

            // update number of cuts
            if (diffCuts < 0)
            {
                // generators
                for (int ii = 0; ii < -diffCuts; ++ii)
                    DLLCutTextureGeneratorList.Add(new DLL.CutTextureGenerator());

                for (int ii = 0; ii < -diffCuts; ++ii)
                {
                    brainCutWithAmpTextures.Add(new Texture2D(1, 1));
                    int id = brainCutWithAmpTextures.Count - 1;
                    brainCutWithAmpTextures[id].filterMode = FilterMode.Trilinear; // TODO : test performances with this parameter
                    brainCutWithAmpTextures[id].wrapMode = TextureWrapMode.Clamp;
                    brainCutWithAmpTextures[id].anisoLevel = 9; // TODO : test performances with this parameter
                    brainCutWithAmpTextures[id].mipMapBias = -2; // never superior to -1 (colorscheme 8 texture glitch)

                    brainCutWithAmpRotatedTextures.Add(new Texture2D(1, 1));
                    brainCutWithAmpRotatedTextures[id].filterMode = FilterMode.Trilinear; // TODO : test performances with this parameter
                    brainCutWithAmpRotatedTextures[id].wrapMode = TextureWrapMode.Clamp;
                    brainCutWithAmpRotatedTextures[id].anisoLevel = 9; // TODO : test performances with this parameter
                    brainCutWithAmpRotatedTextures[id].mipMapBias = -2; // never superior to -1 (colorscheme 8 texture glitch)
                }
            }
            else if (diffCuts > 0)
            {
                // generators
                for (int ii = 0; ii < diffCuts; ++ii)
                {
                    DLLCutTextureGeneratorList[DLLCutTextureGeneratorList.Count - 1].Dispose();
                    DLLCutTextureGeneratorList.RemoveAt(DLLCutTextureGeneratorList.Count - 1);
                }
                // textures 2D
                for (int ii = 0; ii < diffCuts; ++ii)
                {
                    Destroy(brainCutWithAmpTextures[brainCutWithAmpTextures.Count - 1]);
                    Destroy(brainCutWithAmpRotatedTextures[brainCutWithAmpRotatedTextures.Count - 1]);

                    brainCutWithAmpTextures.RemoveAt(brainCutWithAmpTextures.Count - 1);
                    brainCutWithAmpRotatedTextures.RemoveAt(brainCutWithAmpRotatedTextures.Count - 1);
                }
            }
        }

        /// <summary>
        ///  Clean all dll data and unity textures
        /// </summary>
        public override void clean()
        {
            // plots
            RawElectrodes.Dispose();

            // DLL
            // generators
            for (int ii = 0; ii < DLLBrainTextureGeneratorList.Count; ++ii)
            {
                DLLBrainTextureGeneratorList[ii].Dispose();
            }

            for (int ii = 0; ii < DLLCutTextureGeneratorList.Count; ++ii)
            {
                DLLCutTextureGeneratorList[ii].Dispose();
            }

            // textures 2D
            for (int ii = 0; ii < brainCutWithAmpTextures.Count; ++ii)
            {
                Destroy(brainCutWithAmpTextures[ii]);
                Destroy(brainCutWithAmpRotatedTextures[ii]);
            }
        }

        /// <summary>
        /// Update the plots rendering (iEEG or latencies)
        /// </summary>
        public void updatePlotsRendering(SceneStatesInfo data, Latencies latenciesFile)
        {
            Vector3 noScale = new Vector3(0, 0, 0);
            Vector3 normalScale = new Vector3(1, 1, 1);
            MeshRenderer renderer = null;
            PlotType plotType;

            if (data.displayLatenciesMode) // latencies
            {
                for (int ii = 0; ii < Plots.Count; ++ii)
                {
                    //MaterialPropertyBlock props = new MaterialPropertyBlock();

                    bool activity = true;
                    bool highlight = Plots[ii].highlight;
                    float customAlpha = -1f;
                    renderer = Plots[ii].GetComponent<MeshRenderer>();

                    if (Plots[ii].blackList) // blacklisted plot
                    {
                        Plots[ii].transform.localScale = noScale;
                        plotType = PlotType.BlackListed;
                    }
                    else if (Plots[ii].exclude) // excluded plot
                    {
                        Plots[ii].transform.localScale = normalScale;
                        plotType = PlotType.Excluded;
                    }
                    else if (latenciesFile != null) // latency file available
                    {
                        if (idSourceSelected == -1) // no source selected
                        {
                            Plots[ii].transform.localScale = normalScale;
                            plotType = latenciesFile.isPlotASource(ii) ? PlotType.Source : PlotType.NotASource;
                        }
                        else // source selected
                        {
                            if (ii == idSourceSelected)
                            {
                                Plots[ii].transform.localScale = normalScale;
                                plotType = PlotType.Source;                                
                            }
                            else if (latenciesFile.isPlotResponsiveForSource(ii, idSourceSelected)) // data available
                            {
                                // set color
                                plotType = (latenciesFile.positiveHeight[idSourceSelected][ii]) ? PlotType.NonePos : PlotType.NoneNeg;
     
                                // set transparency
                                customAlpha = latenciesFile.transparencies[idSourceSelected][ii] - 0.25f;

                                if (Plots[ii].highlight)
                                    customAlpha = 1;

                                // set size
                                float size = latenciesFile.sizes[idSourceSelected][ii];
                                Plots[ii].transform.localScale = new Vector3(size, size, size);
                            }
                            else // no data available
                            {
                                Plots[ii].transform.localScale = normalScale;
                                plotType = PlotType.NoLatencyData;
                            }
                        }
                    }
                    else // no mask and no latency file available : all plots have the same size and color
                    {
                        Plots[ii].transform.localScale = normalScale;
                        plotType = PlotType.Normal;
                    }

                    // select plot ring 
                    if (ii == idSelectedPlot)
                    {
                        //highlight = true;
                        m_selectRing.setSelectedPlot(Plots[ii], Plots[ii].transform.localScale);
                    }

                    Material plotMaterial = SharedMaterials.plotMat(highlight, plotType);
                    //PlotShaderInfo plotShaderInfo = Plot.instanceShaderInfo(highlight, plotType);
                    if (customAlpha > 0f)
                    {
                        //plotShaderInfo.color.a = customAlpha;
                        Color col = plotMaterial.color;
                        col.a = customAlpha;
                        plotMaterial.color = col;
                    }

                    //props.SetColor("_Color", plotShaderInfo.color);
                    //props.SetFloat("_Glossiness", plotShaderInfo.smoothness);
                    //renderer.SetPropertyBlock(props);

                    renderer.sharedMaterial = plotMaterial;

                    Plots[ii].gameObject.SetActive(activity);
                }          
            }
            else // amplitudes
            {
                for (int ii = 0; ii < Plots.Count; ++ii)
                {

                    //MaterialPropertyBlock props = new MaterialPropertyBlock();

                    bool activity = true;
                    bool highlight = Plots[ii].highlight;
                    renderer = Plots[ii].GetComponent<MeshRenderer>();

                    if (Plots[ii].columnMask) // column mask : plot is not visible can't be clicked
                    {
                        activity = false;
                        plotType = PlotType.Normal;
                    }
                    else if (Plots[ii].columnROI) // ROI mask : plot is not visible, can't be clicked
                    {
                        activity = false;
                        plotType = PlotType.Normal;
                    }
                    else
                    {
                        if (Plots[ii].blackList) // blacklist mask : plot is barely visible with another color, can be clicked
                        {
                            Plots[ii].transform.localScale = normalScale;
                            plotType = PlotType.BlackListed;
                        }
                        else if (Plots[ii].exclude) // excluded mask : plot is a little visible with another color, can be clicked
                        {
                            Plots[ii].transform.localScale = normalScale;
                            plotType = PlotType.Excluded;
                        }
                        else if (data.generatorUpToDate) // no mask, and amplitudes are computed : plot is totally visible, parameters depend on the scene, can be clicker
                        {
                            Plots[ii].transform.localScale = electrodesSizeScale[ii] * gainBubbles;
                            //  plot size (collider and shape) and color are updated with the current timeline amplitude   
                            plotType = electrodesPositiveColor[ii] ? PlotType.Positive : PlotType.Negative;
                        }
                        else // no mask and no amplitude computed : all plots have the same size and color
                        {
                            Plots[ii].transform.localScale = normalScale;
                            plotType = PlotType.Normal;
                        }
                    }

                    // select plot ring 
                    if (ii == idSelectedPlot)
                    {
                        //highlight = true;
                        m_selectRing.setSelectedPlot(Plots[ii], Plots[ii].transform.localScale);
                    }

                    //PlotShaderInfo plotShaderInfo = Plot.instanceShaderInfo(highlight, plotType);
                    //props.SetColor("_Color", plotShaderInfo.color);
                    //props.SetFloat("_Glossiness", plotShaderInfo.smoothness);
                    //renderer.SetPropertyBlock(props);

                    Material plotMaterial = SharedMaterials.plotMat(highlight, plotType);
                    renderer.sharedMaterial = plotMaterial;



                    Plots[ii].gameObject.SetActive(activity);
                }
            }

            if (idSelectedPlot == -1)
            {
                m_selectRing.setSelectedPlot(null, new Vector3(0,0,0));
            }

        }



        #endregion functions
    }

}
