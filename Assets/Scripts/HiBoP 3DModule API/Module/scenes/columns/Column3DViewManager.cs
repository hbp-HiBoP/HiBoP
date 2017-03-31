

/**
 * \file    Column3DViewManager.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define Column3DViewManager class
 */

// system
using System.Linq;
using System.Collections;
using System.Collections.Generic;

// unity
using UnityEngine;

namespace HBP.VISU3D
{
    /// <summary>
    /// A class for managing all the columns data from a specific scene
    /// </summary>
    public class Column3DViewManager : MonoBehaviour
    {
        #region members

        // mani (debug)
        public Color[] colorsPlots = null;

        // columns
        public int idSelectedPatient = 0; /**< id of the selected patient for Multi patient scene */
        public int idSelectedColumn = 0; /**< id of the selected column */
        private List<Column3DViewIEEG> columnsIEEG = null; /**< list of columns iEEG */
        private List<Column3DViewFMRI> columnsFMRI = null; /**< list of columns IRMF */
        private List<GameObject> columnsIeegGo = null; /**< list of columns data GO */
        private List<GameObject> columnsFmriGo = null; /**< list of columns IRMF GO */

        public List<Column3DView> columns = null;
        public List<GameObject> columnsGO = null;

        // data
        public HBP.Data.Patient spPatient = null; /**< sp patient (only if sp columns manager) */
        public List<HBP.Data.Patient> mpPatients = null; /**< mp patients (only if not sp columns manager) */

        // plots
        public DLL.RawSiteList DLLLoadedRawPlotsList = null;
        public DLL.PatientElectrodesList DLLLoadedPatientsElectrodes = null;
        public List<GameObject> SitesList = new List<GameObject>();
        public List<GameObject> PlotsPatientParent = new List<GameObject>(); /**< plots patient parents of the scene */
        public List<List<GameObject>> PlotsElectrodesParent = new List<List<GameObject>>(); /**< plots electrodes parents of the scene */

        // latency        
        public bool latencyFilesDefined = false;
        public bool latencyFileAvailable = false; /**< latency file is available */
        public List<Latencies> latenciesFiles = new List<Latencies>(); /*< list of latency files */

        // timelines 
        public bool globalTimeline = true;  /**< is global timeline enabled */
        public float commonTimelineValue = 0f; /**< commmon value of the timelines */

        // textures
        public List<Vector2[]> uvNull = null;                   /**< null uv vectors */ // // new List<Vector2[]>(); 
        public Color notInBrainColor = Color.black;


        public List<DLL.MRIBrainGenerator> DLLCommonBrainTextureGeneratorList = null; /**< common generators for each brain part  */

        // Common columns cut textures 
        //  textures 2D
        public List<Texture2D> rightGUICutTextures = null;                  /**< list of rotated cut textures| */

        //  generator DLL
        public List<DLL.MRIGeometryCutGenerator> DLLMRIGeometryCutGeneratorList = null; /**< ... */        
        

        // niftii 
        public DLL.NIFTI DLLNii = null;
        // surface 
        public int meshSplitNb = 1;
        public DLL.Surface DLLTriErasingMesh = null; // inused
        public DLL.Surface DLLTriErasingPointMesh = null; // inused
        public DLL.Surface LHemi = null; /**< left hemi mesh */
        public DLL.Surface RHemi = null; /**< right hemi mesh */
        public DLL.Surface BothHemi = null; /**< fustion left/right hemi mesh */
        public DLL.Surface LWhite = null; /**< left white mesh */
        public DLL.Surface RWhite = null; /**< right white mesh */
        public DLL.Surface BothWhite = null; /**< fustion left/right white mesh */

        public List<DLL.Surface> DLLCutsList = null;
        public List<DLL.Surface> DLLSplittedMeshesList = null;
        public List<DLL.Surface> DLLSplittedWhiteMeshesList = null;

        // volume
        public float MRICalMinFactor = 0f;
        public float MRICalMaxFactor = 1f;
        public DLL.Volume DLLVolume = null;
        public List<DLL.Volume> DLLVolumeFMriList = null;

        // planes
        public List<Plane> planesCutsCopy = new List<Plane>();  /**< cut planes copied before the cut job */        
        public List<int> idPlanesOrientationList = new List<int>();     /**< id orientation of the cuts planes */
        public List<bool> planesOrientationFlipList = new List<bool>(); /**< flip state of the cuts plantes orientation */

        // UV coordinates
        public List<Vector2[]> UVCoordinatesSplits = null; // uv coordinates for each brain mesh split

        //
        public bool[] commonMask = null;


        // textures
        public int m_idBrainColor = 15;
        public int m_idBrainCutColor = 14;
        public int m_idColormap = 13; // TO move
        public Texture2D brainColorMapTexture = null;
        public Texture2D brainColorTexture = null;


        #endregion members

        #region mono_behaviour

        public void Awake()
        {
            reset(3);
            update_columns_nb(1, 0, 3);
        }

        #endregion mono_behaviour

        #region functions

        /// <summary>
        /// Reset columns and columnsGO with the IEEG and IRMF columns in the good order
        /// </summary>
        private void update_columns_lists()
        {
            columns.Clear();
            columnsGO.Clear();

            for (int ii = 0; ii < columnsIEEG.Count; ++ii)
            {
                columns.Add(columnsIEEG[ii]);
                columnsGO.Add(columnsIeegGo[ii]);
            }
            for (int ii = 0; ii < columnsFMRI.Count; ++ii)
            {
                columns.Add(columnsFMRI[ii]);
                columnsGO.Add(columnsFmriGo[ii]);
            }
        }


        /// <summary>
        /// Reset all data.
        /// </summary>
        public void reset(int cutPlanesNb)
        {
            // init DLL objects
            //      nii loader;
            DLLNii = new DLL.NIFTI();

            // surfaces
            DLLTriErasingMesh = new DLL.Surface(); // inused
            DLLTriErasingPointMesh = new DLL.Surface(); // inused
            LHemi = new DLL.Surface();
            RHemi = new DLL.Surface();
            LWhite = new DLL.Surface();
            RWhite = new DLL.Surface();
            BothHemi = new DLL.Surface();
            BothWhite = new DLL.Surface();
            DLLCutsList = new List<DLL.Surface>();

            // volume
            if (DLLVolume != null)
                DLLVolume.Dispose();
            DLLVolume = new DLL.Volume();

            if (DLLVolumeFMriList != null)
                for (int ii = 0; ii < DLLVolumeFMriList.Count; ++ii)
                    DLLVolumeFMriList[ii].Dispose();
            DLLVolumeFMriList = new List<DLL.Volume>();

            // electrodes
            DLLLoadedPatientsElectrodes = new DLL.PatientElectrodesList();

            // cuts
            //  textures 2D            
            rightGUICutTextures = new List<Texture2D>(cutPlanesNb);

            //  DLL generators
            DLLMRIGeometryCutGeneratorList = new List<DLL.MRIGeometryCutGenerator>(cutPlanesNb);
            for (int ii = 0; ii < cutPlanesNb; ++ii)
            {                
                rightGUICutTextures.Add(new Texture2D(1, 1));                
                DLLMRIGeometryCutGeneratorList.Add(new DLL.MRIGeometryCutGenerator());
            }

            // columns data
            if (columnsIEEG != null)
                for (int ii = 0; ii < columnsIEEG.Count; ++ii)
                {
                    columnsIEEG[ii].clean();
                }

            columnsIEEG = new List<Column3DViewIEEG>();

            // columns IRMF
            if (columnsFMRI != null)
                for (int ii = 0; ii < columnsFMRI.Count; ++ii)
                {
                    columnsFMRI[ii].clean();
                }

            columnsFMRI = new List<Column3DViewFMRI>();

            // columns data game object
            if (columnsIeegGo != null)
                for (int ii = 0; ii < columnsIeegGo.Count; ++ii)
                {
                    Destroy(columnsIeegGo[ii]);
                }

            columnsIeegGo = new List<GameObject>();

            // columns IRMF game object
            if (columnsFmriGo != null)
                for (int ii = 0; ii < columnsFmriGo.Count; ++ii)
                {
                    Destroy(columnsFmriGo[ii]);
                }
            columnsFmriGo = new List<GameObject>();

            // generic columns 
            if (columns != null)
                columns.Clear();
            else
                columns = new List<Column3DView>();

            if (columnsGO != null)
                columnsGO.Clear();
            else
                columnsGO = new List<GameObject>();

            brainColorMapTexture = Texture2Dutility.generate_color_scheme();
            brainColorTexture = Texture2Dutility.generate_color_scheme();

            reset_splits_nb(1);
        }

        /// <summary>
        /// Reset the number of meshes splits for the brain
        /// </summary>
        /// <param name="nbSplits"></param>
        public void reset_splits_nb(int nbSplits)
        {
            DLLSplittedMeshesList = new List<DLL.Surface>(meshSplitNb);
            DLLSplittedWhiteMeshesList = new List<DLL.Surface>(meshSplitNb);

            // uv coordinates
            UVCoordinatesSplits = new List<Vector2[]>(Enumerable.Repeat(new Vector2[0], meshSplitNb));

            // brain
            //  generators
            DLLCommonBrainTextureGeneratorList = new List<DLL.MRIBrainGenerator>(meshSplitNb);
            for (int ii = 0; ii < meshSplitNb; ++ii)
                DLLCommonBrainTextureGeneratorList.Add(new DLL.MRIBrainGenerator());

            for (int ii = 0; ii < columnsIEEG.Count; ++ii)
                columnsIEEG[ii].reset_splits_nb(nbSplits);

            for (int ii = 0; ii < columnsFMRI.Count; ++ii)
                columnsIEEG[ii].reset_splits_nb(nbSplits);
        }

        /// <summary>
        /// Check if the id corresponds to an IRMF column
        /// </summary>
        /// <param name="idColumn"></param>
        /// <returns></returns>
        public bool is_FMRI_column(int idColumn)
        {
            if(idColumn < columns.Count)
                return columns[idColumn].isFMRI;
            return false;
        }

        /// <summary>
        /// Check if the current selected column is an IRMF one
        /// </summary>
        /// <returns></returns>
        public bool is_current_column_FMRI()
        {
            return is_FMRI_column(idSelectedColumn);
        }

        /// <summary>
        /// Return the current column
        /// </summary>
        /// <returns></returns>
        public Column3DView current_column()
        {
            if (idSelectedColumn < columns.Count)
                return columns[idSelectedColumn];

            return null;
        }

        /// <summary>
        /// Return the column corresponding to the inpud id
        /// </summary>
        /// <param name="idColumn"></param>
        /// <returns></returns>
        public Column3DView col(int idColumn)
        {
            if (idColumn < columns.Count)
                return columns[idColumn];

            return null;
        }

        /// <summary>
        /// Return the IEEG column corresponding to the inpud id
        /// </summary>
        /// <param name="idColumn"></param>
        /// <returns></returns>
        public Column3DViewIEEG IEEG_col(int idColumn)
        {
            return columnsIEEG[idColumn];
        }

        /// <summary>
        /// Return the IRMF column corresponding to the inpud id
        /// </summary>
        /// <param name="idColumn"></param>
        /// <returns></returns>
        public Column3DViewFMRI FMRI_col(int idColumn)
        {
            return columnsFMRI[idColumn];
        }

        /// <summary>
        /// Return the nb of IEEG columns
        /// </summary>
        /// <returns></returns>
        public int IEEG_columns_nb()
        {
            return columnsIEEG.Count;
        }

        /// <summary>
        /// Return the nb of FMRI columns
        /// </summary>
        /// <returns></returns>
        public int FMRI_columns_nb()
        {
            return columnsFMRI.Count;
        }

        /// <summary>
        /// Return the current selected plot of the current selected column
        /// </summary>
        /// <returns></returns>
        public Site curr_site_of_curr_col()
        {
            return current_column().current_selected_site();
        }


        public void update_colormap(int idColormap)
        {
            m_idColormap = idColormap;
            DLL.Texture tex = DLL.Texture.generate_1D_color_texture(m_idColormap);
            tex.update_texture_2D(brainColorMapTexture);
        }

        public void update_brain_cut_color(int idBrainCutColor)
        {
            m_idBrainCutColor = idBrainCutColor;
        }

        public void reset_colors()
        {
            for (int ii = 0; ii < columns.Count; ++ii)
                col(ii).reset_color_schemes(m_idColormap, m_idBrainCutColor);
        }

        /// <summary>
        /// Update the number of cut planes for every column
        /// </summary>
        /// <param name="nbCuts"></param>
        public void update_cuts_nb(int nbCuts)
        {
            // update common
            int diffCuts = DLLMRIGeometryCutGeneratorList.Count - nbCuts;
            if (diffCuts < 0)
            {
                // textures 2D
                for (int ii = 0; ii < -diffCuts; ++ii)
                {
                    // GO textures        
                    rightGUICutTextures.Add(new Texture2D(1, 1));
                    int id = rightGUICutTextures.Count - 1;
                    rightGUICutTextures[id].filterMode = FilterMode.Trilinear; // TODO : test performances with this parameter
                    rightGUICutTextures[id].wrapMode = TextureWrapMode.Clamp;
                    rightGUICutTextures[id].anisoLevel = 9; // TODO : test performances with this parameter
                    rightGUICutTextures[id].mipMapBias = -2; // never superior to -1 (colorscheme 8 texture glitch)    

                    // DLL generators       
                    DLLMRIGeometryCutGeneratorList.Add(new DLL.MRIGeometryCutGenerator());
                }
            }
            else if (diffCuts > 0)
            {                
                for (int ii = 0; ii < diffCuts; ++ii)
                {
                    // GO textures
                    Destroy(rightGUICutTextures[rightGUICutTextures.Count - 1]);
                    rightGUICutTextures.RemoveAt(rightGUICutTextures.Count - 1);

                    // DLL generators
                    DLLMRIGeometryCutGeneratorList[DLLMRIGeometryCutGeneratorList.Count - 1].Dispose();
                    DLLMRIGeometryCutGeneratorList.RemoveAt(DLLMRIGeometryCutGeneratorList.Count - 1);
                }
            }

            // update columns
            for (int ii = 0; ii < IEEG_columns_nb(); ++ii)
                IEEG_col(ii).update_cuts_planes_nb(nbCuts);

            for (int ii = 0; ii < FMRI_columns_nb(); ++ii)
                FMRI_col(ii).update_cuts_planes_nb(nbCuts);
        }

        /// <summary>
        /// Update the number of columns and set the number of cut planes for each ones
        /// </summary>
        /// <param name="nbIEEGColumns"></param>
        /// /// <param name="nbIRMFColumns"></param>
        /// <param name="nbCuts"></param>
        public void update_columns_nb(int nbIEEGColumns, int nbIRMFColumns, int nbCuts)
        {            
            // clean data columns if changes in data columns nb
            if (nbIEEGColumns != columnsIEEG.Count)
            {
                for (int ii = 0; ii < columnsIEEG.Count; ++ii)
                {
                    columnsIEEG[ii].clean();
                }
            }

            // clean IRMF columns if changes in IRMF columns nb
            if (nbIRMFColumns != columnsFMRI.Count)
            {
                for (int ii = 0; ii < columnsFMRI.Count; ++ii)
                {
                    columnsFMRI[ii].clean();
                }
            }

            // resize the data GO list            
            int diffIEEGColumns = columnsIEEG.Count - nbIEEGColumns;
            if (diffIEEGColumns < 0)
            {
                for (int ii = 0; ii < -diffIEEGColumns; ++ii)
                {
                    // add column
                    columnsIeegGo.Add(new GameObject("column IEEG " + columnsIeegGo.Count + 1));
                    columnsIeegGo[columnsIeegGo.Count - 1].AddComponent<Column3DViewIEEG>();
                    columnsIeegGo[columnsIeegGo.Count - 1].transform.SetParent(this.transform);
                }
            }
            else if (diffIEEGColumns > 0)
            {
                for (int ii = 0; ii < diffIEEGColumns; ++ii)
                {
                    // destroy column
                    int idColumn = columnsIeegGo.Count - 1;
                    Destroy(columnsIeegGo[idColumn]);
                    columnsIeegGo.RemoveAt(idColumn);
                }
            }

            // resize the IRMF GO list      
            int diffIRMFColumns = columnsFMRI.Count - nbIRMFColumns;
            if (diffIRMFColumns < 0)
            {
                for (int ii = 0; ii < -diffIRMFColumns; ++ii)
                {
                    // add column
                    DLLVolumeFMriList.Add(new DLL.Volume());

                    columnsFmriGo.Add(new GameObject("column FMRI " + columnsFmriGo.Count + 1));
                    columnsFmriGo[columnsFmriGo.Count - 1].AddComponent<Column3DViewFMRI>();
                    columnsFmriGo[columnsFmriGo.Count - 1].transform.SetParent(this.transform);
                }
            }
            else if (diffIRMFColumns > 0)
            {
                for (int ii = 0; ii < diffIRMFColumns; ++ii)
                {
                    // destroy column
                    int idColumn = columnsFmriGo.Count - 1;
                    Destroy(columnsFmriGo[idColumn]);
                    columnsFmriGo.RemoveAt(idColumn);

                    DLLVolumeFMriList[idColumn].Dispose();
                    DLLVolumeFMriList.RemoveAt(idColumn);
                }
            }

            // init new columns IEEG            
            if (nbIEEGColumns != columnsIEEG.Count)
            {
                columnsIEEG = new List<Column3DViewIEEG>();
                for (int ii = 0; ii < nbIEEGColumns; ++ii)
                {
                    columnsIEEG.Add(columnsIeegGo[ii].GetComponent<Column3DViewIEEG>());
                    columnsIEEG[ii].init(ii, nbCuts, DLLLoadedPatientsElectrodes, PlotsPatientParent);
                    columnsIEEG[ii].reset_splits_nb(meshSplitNb);

                    if (latencyFilesDefined)
                        columnsIEEG[ii].currentLatencyFile = 0;
                }
            }

            // init new columns IRMF
            if (nbIRMFColumns != columnsFMRI.Count)
            {
                // update IRMF columns mask
                bool[] maskColumnsOR = new bool[DLLLoadedPatientsElectrodes.total_sites_nb()];
                for (int ii = 0; ii < SitesList.Count; ++ii)
                {
                    bool mask = false;

                    for (int jj = 0; jj < columnsIEEG.Count; ++jj)
                    {
                        mask = mask || columnsIEEG[jj].Sites[ii].columnMask;
                    }

                    maskColumnsOR[ii] = mask;
                }

                for (int ii = 0; ii < columnsFMRI.Count; ++ii)
                {
                    for (int jj = 0; jj < SitesList.Count; ++jj)
                    {
                        columnsFMRI[ii].Sites[jj].columnMask = maskColumnsOR[jj];
                    }
                }

                columnsFMRI = new List<Column3DViewFMRI>();
                for (int ii = 0; ii < nbIRMFColumns; ++ii)
                {
                    columnsFMRI.Add(columnsFmriGo[ii].GetComponent<Column3DViewFMRI>());
                    columnsFMRI[columnsFMRI.Count - 1].init(columnsIEEG.Count + ii, nbCuts, DLLLoadedPatientsElectrodes, PlotsPatientParent);
                    columnsFMRI[columnsFMRI.Count - 1].reset_splits_nb(meshSplitNb);

                    for (int jj = 0; jj < SitesList.Count; ++jj)
                    {
                        columnsFMRI[columnsFMRI.Count - 1].Sites[jj].columnMask = maskColumnsOR[jj];
                    }
                }
            }

            // rest columns/columnsGO
            update_columns_lists();

            commonMask = new bool[DLLLoadedPatientsElectrodes.total_sites_nb()];

            if (idSelectedColumn >= columns.Count)
                idSelectedColumn = columns.Count - 1;


            for (int ii = 0; ii < columns.Count; ++ii)
                col(ii).reset_color_schemes(m_idColormap, m_idBrainCutColor);
        }

        /// <summary>
        /// Define the single patient and associated data
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="columnDataList"></param>
        public void set_SP_timeline_data(HBP.Data.Patient patient, List<HBP.Data.Visualisation.ColumnData> columnDataList)
        {
            spPatient = patient;
            for (int ii = 0; ii < columnsIEEG.Count; ++ii)
            {
                columnsIEEG[ii].set_column_data(columnDataList[ii]);
            }
        }


        /// <summary>
        /// Define the mp patients list and associated data
        /// </summary>
        /// <param name="patientList"></param>
        /// <param name="columnDataList"></param>
        /// <param name="ptsPathFileList"></param>
        public void set_MP_timeline_data(List<Data.Patient> patientList, List<Data.Visualisation.ColumnData> columnDataList, List<string> ptsPathFileList)
        {
            mpPatients = patientList;
            for (int ii = 0; ii < columnsIEEG.Count; ++ii)
            {
                columnsIEEG[ii].set_column_data(columnDataList[ii]);
            }
        }


        /// <summary>
        /// Create the cut mesh texture dll and texture2D
        /// </summary>
        /// <param name="indexCut"></param>
        public void create_MRI_texture(int indexCut, int indexColumn)
        {
            UnityEngine.Profiling.Profiler.BeginSample("create_MRI_texture");
                col(indexColumn).create_MRI_texture(DLLMRIGeometryCutGeneratorList[indexCut], DLLVolume, indexCut, MRICalMinFactor, MRICalMaxFactor);
            UnityEngine.Profiling.Profiler.EndSample();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexCut"></param>
        /// <param name="indexColumn"></param>
        public void create_GUI_MRI_texture(int indexCut, int indexColumn)
        {
            // retrieve orientation to apply
            string orientation = "custom";
            if (idPlanesOrientationList[indexCut] == 0)
                orientation = "Axial";
            else if (idPlanesOrientationList[indexCut] == 1)
                orientation = "Coronal";
            else if (idPlanesOrientationList[indexCut] == 2)
                orientation = "Sagital";

            col(indexColumn).create_GUI_MRI_texture(indexCut, orientation, planesOrientationFlipList[indexCut], planesCutsCopy, orientation != "custom");            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexCut"></param>
        /// <param name="indexColumn"></param>
        public void create_GUI_IEEG_texture(int indexCut, int indexColumn)
        {
            // retrieve orientation to apply
            string orientation = "custom";
            if (idPlanesOrientationList[indexCut] == 0)
                orientation = "Axial";
            else if (idPlanesOrientationList[indexCut] == 1)
                orientation = "Coronal";
            else if (idPlanesOrientationList[indexCut] == 2)
                orientation = "Sagital";

            ((Column3DViewIEEG)col(indexColumn)).create_GUI_IEEG_texture(indexCut, orientation, planesOrientationFlipList[indexCut], planesCutsCopy, orientation != "custom");
        }


        public void create_GUI_FMRI_texture(int indexCut, int indexColumn)
        {
            // retrieve orientation to apply
            string orientation = "custom";
            if (idPlanesOrientationList[indexCut] == 0)
                orientation = "Axial";
            else if (idPlanesOrientationList[indexCut] == 1)
                orientation = "Coronal";
            else if (idPlanesOrientationList[indexCut] == 2)
                orientation = "Sagital";

            ((Column3DViewFMRI)col(indexColumn)).create_GUI_FMRI_texture(indexCut, orientation, planesOrientationFlipList[indexCut], planesCutsCopy, orientation != "custom");
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexColumn"></param>
        /// <param name="indexCut"></param>
        /// <param name="thresholdInfluence"></param>
        public void color_cuts_textures_with_IEEG(int indexColumn, int indexCut)
        {
            Column3DViewIEEG column = columnsIEEG[indexColumn];            
            DLL.MRITextureCutGenerator generator = column.DLLMRITextureCutGeneratorList[indexCut];        
            generator.fill_texture_with_IEEG(column, column.DLLCutColorScheme, notInBrainColor);

            DLL.Texture cutTexture = column.dllBrainCutWithIEEGTextures[indexCut];
            generator.update_texture_with_IEEG(cutTexture);
            cutTexture.update_texture_2D(column.brainCutWithIEEGTextures[indexCut]); // update mesh cut 2D texture
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexColumn"></param>
        /// <param name="indexCut"></param>
        public void color_cuts_textures_with_FMRI(int indexColumn, int indexCut)
        {
            Column3DViewFMRI column = columnsFMRI[indexColumn];
            DLL.MRITextureCutGenerator generator = column.DLLMRITextureCutGeneratorList[indexCut];
            generator.fill_texture_with_FMRI(column, DLLVolumeFMriList[indexColumn]);

            DLL.Texture cutTexture = column.dllBrainCutWithFMRITextures[indexCut];
            generator.update_texture_with_FMRI(cutTexture);
            cutTexture.update_texture_2D(column.brainCutWithFMRITextures[indexCut]); // update mesh cut 2D texture
        }

        /// <summary>
        /// Compute the amplitudes textures coordinates for the brain mesh
        /// When to call ? changes in IEEGColumn.currentTimeLineID, IEEGColumn.alphaMin, IEEGColumn.alphaMax
        /// </summary>
        /// <param name="whiteInflatedMeshes"></param>
        /// <param name="indexColumn"></param>
        /// <param name="thresholdInfluence"></param>
        /// <param name="alphaMin"></param>
        /// <param name="alphaMax"></param>
        public bool compute_surface_brain_UV_with_IEEG(bool whiteInflatedMeshes, int indexColumn)
        {
            for (int ii = 0; ii < meshSplitNb; ++ii)
                if(!columnsIEEG[indexColumn].DLLBrainTextureGeneratorList[ii].compute_surface_UV_IEEG(whiteInflatedMeshes ? DLLSplittedWhiteMeshesList[ii] : DLLSplittedMeshesList[ii], columnsIEEG[indexColumn]))
                    return false;

            return true;
        }

        /// <summary>
        /// Update the plot rendering parameters for all columns
        /// </summary>
        public void update_all_columns_sites_rendering(SceneStatesInfo data)
        {
            for (int ii = 0; ii < columnsIEEG.Count; ++ii)
            {
                Latencies latencyFile = null;
                if (columnsIEEG[ii].currentLatencyFile != -1)
                    latencyFile = latenciesFiles[columnsIEEG[ii].currentLatencyFile];

                columnsIEEG[ii].update_sites_size_and_color_arrays_for_IEEG(); // TEST
                columnsIEEG[ii].update_sites_rendering(data, latencyFile);
            }

            for (int ii = 0; ii < columnsFMRI.Count; ++ii)
                columnsFMRI[ii].update_plots_visiblity(data);
        }

        /// <summary>
        /// Update the visiblity of the ROI for all columns
        /// </summary>
        /// <param name="visible"></param>
        public void update_ROI_visibility(bool visible)
        {
            // disable all ROI render
            for(int ii = 0; ii < columns.Count; ++ii)
                if (columns[ii].ROI != null)
                    columns[ii].ROI.set_rendering_state(false);

            if(current_column() != null)
                if(current_column().ROI != null)
                current_column().ROI.set_rendering_state(visible);
        }

        /// <summary>
        /// Update the visiblity of the plots for all columns
        /// </summary>
        /// <param name="visible"></param>
        public void update_sites_visibiliy(bool visible)
        {
            for (int ii = 0; ii < columnsIEEG.Count; ++ii)
            {
                columnsIEEG[ii].set_visible_sites(visible);
            }

            for (int ii = 0; ii < columnsFMRI.Count; ++ii)
            {
                columnsFMRI[ii].set_visible_sites(visible);

            }
        }

        #endregion functions
    }
}