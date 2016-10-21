

/**
 * \file    Column3DViewIRMF.cs
 * \author  Lance Florian
 * \date    29/02/2016
 * \brief   Define Column3DViewIRMF class
 */

// system
using System.Collections;
using System.Collections.Generic;

// unity
using UnityEngine;

namespace HBP.VISU3D
{
    /// <summary>
    /// A 3D column view class, containing all necessary data concerning an IRMF column
    /// </summary>
    public class Column3DViewIRMF : Column3DView
    {
        #region members

        // textures 2D
        public List<Texture2D> brainCutIRMFTextures = null;         /**< list of cut textures */
        public List<Texture2D> brainCutIRMFRotatedTextures = null;  /**< list of cut rotated textures */

        public float calMin = 0.4f;
        public float calMax = 0.6f;

        public float alpha = 0.5f;

        #endregion members

        #region functions

        /// <summary>
        /// Init the IRMF column
        /// </summary>
        /// <param name="idColumn"></param>
        /// <param name="nbCuts"></param>
        /// <param name="meshSplitNb"></param>
        /// <param name="plots"></param>
        /// <param name="plotsGO"></param>
        public new void init(int idColumn, int nbCuts, int meshSplitNb, DLL.ElectrodesPatientMultiList plots, List<GameObject> PlotsPatientParent)
        {
            base.init(idColumn, nbCuts, meshSplitNb, plots, PlotsPatientParent);
            isIRMF = true;

            // textures 2D
            brainCutIRMFTextures = new List<Texture2D>();
            brainCutIRMFRotatedTextures = new List<Texture2D>();
            for (int jj = 0; jj < nbCuts; ++jj)
            {
                brainCutIRMFTextures.Add(new Texture2D(1, 1));
                brainCutIRMFRotatedTextures.Add(new Texture2D(1, 1));
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
                for (int ii = 0; ii < -diffCuts; ++ii)
                {
                    brainCutIRMFTextures.Add(new Texture2D(1, 1));
                    int id = brainCutIRMFTextures.Count - 1;
                    brainCutIRMFTextures[id].filterMode = FilterMode.Trilinear; // TODO : test performances with this parameter
                    brainCutIRMFTextures[id].wrapMode = TextureWrapMode.Clamp;
                    brainCutIRMFTextures[id].anisoLevel = 9; // TODO : test performances with this parameter
                    brainCutIRMFTextures[id].mipMapBias = -2; // never superior to -1 (colorscheme 8 texture glitch)

                    brainCutIRMFRotatedTextures.Add(new Texture2D(1, 1));
                    brainCutIRMFRotatedTextures[id].filterMode = FilterMode.Trilinear; // TODO : test performances with this parameter
                    brainCutIRMFRotatedTextures[id].wrapMode = TextureWrapMode.Clamp;
                    brainCutIRMFRotatedTextures[id].anisoLevel = 9; // TODO : test performances with this parameter
                    brainCutIRMFRotatedTextures[id].mipMapBias = -2; // never superior to -1 (colorscheme 8 texture glitch)
                }
            }
            else if (diffCuts > 0)
            {
                // textures 2D
                for (int ii = 0; ii < diffCuts; ++ii)
                {
                    Destroy(brainCutIRMFTextures[brainCutIRMFTextures.Count - 1]);
                    Destroy(brainCutIRMFRotatedTextures[brainCutIRMFRotatedTextures.Count - 1]);

                    brainCutIRMFTextures.RemoveAt(brainCutIRMFTextures.Count - 1);
                    brainCutIRMFRotatedTextures.RemoveAt(brainCutIRMFRotatedTextures.Count - 1);
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

            // textures 2D
            for (int ii = 0; ii < brainCutIRMFTextures.Count; ++ii)
            {
                Destroy(brainCutIRMFTextures[ii]);
                Destroy(brainCutIRMFRotatedTextures[ii]);
            }
        }

        public void updatePlotsVisibility(SceneStatesInfo data)
        {
            Vector3 normalScale = new Vector3(1, 1, 1);
            MeshRenderer renderer = null;
            PlotType plotType;

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
                    else if(Plots[ii].exclude)
                    {
                        Plots[ii].transform.localScale = normalScale;
                        plotType = PlotType.Excluded;
                    }
                    else // no mask : all plots have the same size and color
                    {
                        Plots[ii].transform.localScale = normalScale;
                        plotType = PlotType.Normal;
                    }

                    //renderer.sharedMaterial = SharedMaterials.plotMat(highlight, plotType);

                    // select plot ring 
                    if (ii == idSelectedPlot)
                    {
                        //m_selectRing.setMaterial(SharedMaterials.plotMat(true, plotType));
                        //highlight = true;
                        m_selectRing.setSelectedPlot(Plots[ii], Plots[ii].transform.localScale);
                    }

                    //PlotShaderInfo plotShaderInfo = Plot.instanceShaderInfo(highlight, plotType);
                    //props.SetColor("_Color", plotShaderInfo.color);
                    //props.SetFloat("_Glossiness", plotShaderInfo.smoothness);
                    //renderer.SetPropertyBlock(props);

                    Material plotMaterial = SharedMaterials.plotMat(highlight, plotType);
                    renderer.sharedMaterial = plotMaterial;

                }

                Plots[ii].gameObject.SetActive(activity);
            }
        }


        #endregion functions
    }
}