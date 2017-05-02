

/**
 * \file    Column3DViewFMRI.cs
 * \author  Lance Florian
 * \date    29/02/2016
 * \brief   Define Column3DViewFMRI class
 */

// system
using System.Collections.Generic;

// unity
using UnityEngine;

namespace HBP.Module3D
{
    /// <summary>
    /// A 3D column view class, containing all necessary data concerning an FMRI column
    /// </summary>
    public class Column3DViewFMRI : Column3DView
    {
        #region members
        public override ColumnType Type
        {
            get
            {
                return ColumnType.FMRI;
            }
        }
        // textures
        public List<Texture2D> brainCutWithFMRITextures = null;        
        public List<Texture2D> guiBrainCutWithFMRITextures = null;

        public List<DLL.Texture> dllBrainCutWithFMRITextures = null;
        public List<DLL.Texture> dllGuiBrainCutWithFMRITextures = null;

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
        /// <param name="plots"></param>
        /// <param name="plotsGO"></param>
        public override void Initialize(int idColumn, int nbCuts,  DLL.PatientElectrodesList plots, List<GameObject> PlotsPatientParent)
        {
            base.Initialize(idColumn, nbCuts, plots, PlotsPatientParent);

            // GO textures            
            brainCutWithFMRITextures = new List<Texture2D>();
            guiBrainCutWithFMRITextures = new List<Texture2D>();
            for (int ii = 0; ii < nbCuts; ++ii)
            {
                brainCutWithFMRITextures.Add(Texture2Dutility.generate_cut(1, 1));
                guiBrainCutWithFMRITextures.Add(Texture2Dutility.generate_GUI(1, 1));
            }
            
            // DLL textures
            dllBrainCutWithFMRITextures = new List<DLL.Texture>(nbCuts);
            dllGuiBrainCutWithFMRITextures = new List<DLL.Texture>(nbCuts);
            for (int jj = 0; jj < nbCuts; ++jj)
            {
                dllBrainCutWithFMRITextures.Add(new DLL.Texture());
                dllGuiBrainCutWithFMRITextures.Add(new DLL.Texture());
            }
        }


        /// <summary>
        /// Update the cut planes number of the 3D column view
        /// </summary>
        /// <param name="newCutsNb"></param>
        public new void update_cuts_planes_nb(int diffCuts)
        {
            base.update_cuts_planes_nb(diffCuts);

            // update number of cuts
            if (diffCuts < 0)
            {
                for (int ii = 0; ii < -diffCuts; ++ii)
                {
                    // GO textures 
                    brainCutWithFMRITextures.Add(Texture2Dutility.generate_cut());
                    guiBrainCutWithFMRITextures.Add(Texture2Dutility.generate_GUI());

                    // DLL textures
                    dllBrainCutWithFMRITextures.Add(new DLL.Texture());
                    dllGuiBrainCutWithFMRITextures.Add(new DLL.Texture());
                }
            }
            else if (diffCuts > 0)
            {                
                for (int ii = 0; ii < diffCuts; ++ii)
                {
                    // GO textures       
                    Destroy(brainCutWithFMRITextures[brainCutWithFMRITextures.Count - 1]);
                    brainCutWithFMRITextures.RemoveAt(brainCutWithFMRITextures.Count - 1);

                    Destroy(guiBrainCutWithFMRITextures[guiBrainCutWithFMRITextures.Count - 1]);
                    guiBrainCutWithFMRITextures.RemoveAt(guiBrainCutWithFMRITextures.Count - 1);

                    // DLL textures
                    dllBrainCutWithFMRITextures.RemoveAt(dllBrainCutWithFMRITextures.Count - 1);
                    dllGuiBrainCutWithFMRITextures.RemoveAt(dllGuiBrainCutWithFMRITextures.Count - 1);
                }
            }
        }

        /// <summary>
        ///  Clean all dll data and unity textures
        /// </summary>
        public override void Clear()
        {
            base.Clear();

            // plots
            m_RawElectrodes.Dispose();

            // textures 2D
            for (int ii = 0; ii < brainCutWithFMRITextures.Count; ++ii)
            {
                Destroy(brainCutWithFMRITextures[ii]);
                Destroy(guiBrainCutWithFMRITextures[ii]);
            }
        }

        public void update_plots_visiblity(SceneStatesInfo data)
        {
            Vector3 normalScale = new Vector3(1, 1, 1);
            MeshRenderer renderer = null;
            SiteType siteType;

            for (int ii = 0; ii < Sites.Count; ++ii)
            {
                bool activity = true;
                bool highlight = Sites[ii].Information.IsHighlighted;
                renderer = Sites[ii].GetComponent<MeshRenderer>();

                if (Sites[ii].Information.IsMasked) // column mask : plot is not visible can't be clicked
                {
                    activity = false;
                    siteType = Sites[ii].Information.IsMarked ? SiteType.Marked : SiteType.Normal;
                }
                else if (Sites[ii].Information.IsInROI) // ROI mask : plot is not visible, can't be clicked
                {
                    activity = false;
                    siteType = Sites[ii].Information.IsMarked ? SiteType.Marked : SiteType.Normal;
                }
                else
                {
                    if (Sites[ii].Information.IsBlackListed) // blacklist mask : plot is barely visible with another color, can be clicked
                    {
                        Sites[ii].transform.localScale = normalScale;
                        siteType = SiteType.BlackListed;
                    }
                    else if(Sites[ii].Information.IsExcluded)
                    {
                        Sites[ii].transform.localScale = normalScale;
                        siteType = SiteType.Excluded;
                    }
                    else // no mask : all plots have the same size and color
                    {
                        Sites[ii].transform.localScale = normalScale;
                        siteType = Sites[ii].Information.IsMarked ? SiteType.Marked : SiteType.Normal;
                    }

                    // select site ring 
                    if (ii == SelectedSiteID)
                        m_selectRing.set_selected_site(Sites[ii], Sites[ii].transform.localScale);

                    renderer.sharedMaterial = SharedMaterials.site_shared_material(highlight, siteType);
                }

                Sites[ii].gameObject.SetActive(activity);
            }
        }

        public void create_GUI_FMRI_texture(int indexCut, string orientation, bool flip, List<Plane> cutPlanes, bool drawLines)
        {
            if (dllBrainCutTextures[indexCut].m_sizeTexture[0] > 0)
            {
                dllGuiBrainCutWithFMRITextures[indexCut].copy_frome_and_rotate(dllBrainCutWithFMRITextures[indexCut], orientation, flip, drawLines, indexCut, cutPlanes, DLLMRITextureCutGeneratorList[indexCut]);
                dllGuiBrainCutWithFMRITextures[indexCut].update_texture_2D(guiBrainCutWithFMRITextures[indexCut], false); // TODO: ;..
            }
        }


        #endregion functions
    }
}