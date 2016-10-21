

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
        private List<Column3DViewIRMF> columnsIRMF = null; /**< list of columns IRMF */
        private List<GameObject> columnsIEEGGO = null; /**< list of columns data GO */
        private List<GameObject> columnsIRMFGO = null; /**< list of columns IRMF GO */

        public List<Column3DView> columns = null;
        public List<GameObject> columnsGO = null;

        // data
        public HBP.Data.Patient.Patient spPatient = null; /**< sp patient (only if sp columns manager) */
        public List<HBP.Data.Patient.Patient> mpPatients = null; /**< mp patients (only if not sp columns manager) */

        // plots
        public DLL.RawPlotList DLLLoadedRawPlotsList = null;
        public DLL.ElectrodesPatientMultiList DLLLoadedPatientsElectrodes = null;
        public List<GameObject> PlotsList = new List<GameObject>();
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
        public List<Vector2[]> uvNull = new List<Vector2[]>(); /**< null uv vectors */
        public Color notInBrainColor = Color.black;
        public DLL.Texture DLLBrainMainColor = null;            /**< main color dll texture of the brain mesh */
        public DLL.Texture DLLBrainColorScheme = null;          /**< brain colorscheme dll texture */
        public DLL.Texture DLLCutColorScheme = null;            /**< cut colorscheme dll texture */
        public DLL.Texture DLLCutIRMFColorScheme = null;        /**< cut colorscheme dll texture */
        public Texture2D brainMainColorTexture = null;          /**< main color unity 2D texture of the brain mesh */
        public Texture2D brainColorSchemeTexture = null;        /**< brain colorscheme unity 2D texture  */
        public List<Texture2D> brainCutTextures = null;         /**< list of cut textures */
        public List<Texture2D> brainCutRotatedTextures = null;  /**< list of rotated cut textures| */
        public List<DLL.BrainTextureGenerator> DLLCommonBrainTextureGeneratorList = null; /**< common generators for each brain part  */
        public List<DLL.CutTextureGenerator> DLLCommonCutTextureGeneratorList = null;     /**< common generators for each cut part  */

        // niftii 
        public DLL.NIFTI DLLNii = null;
        // surface 
        public int meshSplitNb = 4;
        public DLL.Surface DLLTriErasingMesh = null; // inused
        public DLL.Surface DLLTriErasingPointMesh = null; // inused
        public DLL.Surface LHemi = null; /**< left hemi mesh */
        public DLL.Surface RHemi = null; /**< right hemi mesh */
        public DLL.Surface BothHemi = null; /**< fustion left/right hemi mesh */
        public List<DLL.Surface> DLLCutsList = null;
        public List<DLL.Surface> DLLSplittedMeshesList = null;
        public List<DLL.Surface> DLLSplittedWhiteMeshesList = null;

        // volume
        public float IRMCalMinFactor = 0f;
        public float IRMCalMaxFactor = 1f;
        public DLL.Volume DLLVolume = null;
        public List<DLL.Volume> DLLVolumeIRMFList = null;

        // planes
        public List<Plane> planesCutsCopy = new List<Plane>();  /**< cut planes copied before the cut job */
        public List<Plane> planesList = new List<Plane>();              /**< cut planes list */
        public List<int> idPlanesOrientationList = new List<int>();     /**< id orientation of the cuts planes */
        public List<bool> planesOrientationFlipList = new List<bool>(); /**< flip state of the cuts plantes orientation */

        // UV coordinates
        public List<Vector2[]> UVCoordinatesSplits = null; // uv coordinates for each brain mesh split

        //
        public bool[] commonMask = null;

        #endregion members

        #region mono_behaviour

        public void Awake()
        {
            reset();
            updateColumnsNb(1, 0, 3);
        }

        #endregion mono_behaviour

        #region functions

        /// <summary>
        /// Reset columns and columnsGO with the IEEG and IRMF columns in the good order
        /// </summary>
        private void updateColumnsLists()
        {
            columns.Clear();
            columnsGO.Clear();

            for (int ii = 0; ii < columnsIEEG.Count; ++ii)
            {
                columns.Add(columnsIEEG[ii]);
                columnsGO.Add(columnsIEEGGO[ii]);
            }
            for (int ii = 0; ii < columnsIRMF.Count; ++ii)
            {
                columns.Add(columnsIRMF[ii]);
                columnsGO.Add(columnsIRMFGO[ii]);
            }
        }


        /// <summary>
        /// Reset all data.
        /// </summary>
        public void reset()
        {
            // init DLL objects
            //      nii loader;
            DLLNii = new DLL.NIFTI();

            // surfaces
            DLLTriErasingMesh = new DLL.Surface(); // inused
            DLLTriErasingPointMesh = new DLL.Surface(); // inused
            LHemi = new DLL.Surface();
            RHemi = new DLL.Surface();
            BothHemi = new DLL.Surface();
            DLLCutsList = new List<DLL.Surface>();
            DLLSplittedMeshesList = new List<DLL.Surface>(meshSplitNb);
            DLLSplittedWhiteMeshesList = new List<DLL.Surface>(meshSplitNb);

            // volume
            if (DLLVolume != null)
                DLLVolume.Dispose();
            DLLVolume = new DLL.Volume();

            if (DLLVolumeIRMFList != null)
                for (int ii = 0; ii < DLLVolumeIRMFList.Count; ++ii)
                    DLLVolumeIRMFList[ii].Dispose();
            DLLVolumeIRMFList = new List<DLL.Volume>();

            // electrodes
            DLLLoadedPatientsElectrodes = new DLL.ElectrodesPatientMultiList();

            // uv coordinates
            UVCoordinatesSplits = new List<Vector2[]> (Enumerable.Repeat(new Vector2[0], meshSplitNb));

            // textures dll
            DLLCutColorScheme = new DLL.Texture();
            DLLCutIRMFColorScheme = new DLL.Texture();
            DLLBrainMainColor = new DLL.Texture();
            DLLBrainColorScheme = new DLL.Texture();

            // textures 2D
            brainMainColorTexture = new Texture2D(1, 1);
            brainColorSchemeTexture = new Texture2D(1, 1);

            // textures 2D
            brainCutTextures = new List<Texture2D>(Enumerable.Repeat(new Texture2D(1, 1), planesList.Count));
            brainCutRotatedTextures = new List<Texture2D>(Enumerable.Repeat(new Texture2D(1, 1), planesList.Count));

            // generators dll
            //      brain
            DLLCommonBrainTextureGeneratorList = new List<DLL.BrainTextureGenerator>(meshSplitNb);
            for(int ii = 0; ii < meshSplitNb; ++ii)
                DLLCommonBrainTextureGeneratorList.Add(new DLL.BrainTextureGenerator());            
            //      cuts
            DLLCommonCutTextureGeneratorList = new List<DLL.CutTextureGenerator>(planesList.Count);
            for (int ii = 0; ii < planesList.Count; ++ii) 
                DLLCommonCutTextureGeneratorList.Add(new DLL.CutTextureGenerator());

            // columns data
            if (columnsIEEG != null)
                for (int ii = 0; ii < columnsIEEG.Count; ++ii)
                {
                    columnsIEEG[ii].clean();
                }

            columnsIEEG = new List<Column3DViewIEEG>();

            // columns IRMF
            if (columnsIRMF != null)
                for (int ii = 0; ii < columnsIRMF.Count; ++ii)
                {
                    columnsIRMF[ii].clean();
                }

            columnsIRMF = new List<Column3DViewIRMF>();

            // columns data game object
            if (columnsIEEGGO != null)
                for (int ii = 0; ii < columnsIEEGGO.Count; ++ii)
                {
                    Destroy(columnsIEEGGO[ii]);
                }

            columnsIEEGGO = new List<GameObject>();

            // columns IRMF game object
            if (columnsIRMFGO != null)
                for (int ii = 0; ii < columnsIRMFGO.Count; ++ii)
                {
                    Destroy(columnsIRMFGO[ii]);
                }
            columnsIRMFGO = new List<GameObject>();

            // generic columns 
            if (columns != null)
                columns.Clear();
            else
                columns = new List<Column3DView>();

            if (columnsGO != null)
                columnsGO.Clear();
            else
                columnsGO = new List<GameObject>();
        }

        /// <summary>
        /// Check if the id corresponds to an IRMF column
        /// </summary>
        /// <param name="idColumn"></param>
        /// <returns></returns>
        public bool isIRMFColumn(int idColumn)
        {
            if(idColumn < columns.Count)
                return columns[idColumn].isIRMF;
            return false;
        }

        /// <summary>
        /// Check if the current selected column is an IRMF one
        /// </summary>
        /// <returns></returns>
        public bool isIRMFCurrentColumn()
        {
            return isIRMFColumn(idSelectedColumn);
        }

        /// <summary>
        /// Return the current column
        /// </summary>
        /// <returns></returns>
        public Column3DView currentColumn()
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
        public Column3DViewIEEG IEEGCol(int idColumn)
        {
            return columnsIEEG[idColumn];
        }

        /// <summary>
        /// Return the IRMF column corresponding to the inpud id
        /// </summary>
        /// <param name="idColumn"></param>
        /// <returns></returns>
        public Column3DViewIRMF IRMFCol(int idColumn)
        {
            return columnsIRMF[idColumn];
        }

        /// <summary>
        /// Return the nb of IEEG columns
        /// </summary>
        /// <returns></returns>
        public int nbIEEGCol()
        {
            return columnsIEEG.Count;
        }

        /// <summary>
        /// Return the nb of IRMF columns
        /// </summary>
        /// <returns></returns>
        public int nbIRMFCol()
        {
            return columnsIRMF.Count;
        }

        /// <summary>
        /// Return the current selected plot of the current selected column
        /// </summary>
        /// <returns></returns>
        public Plot currPlotOfCurrCol()
        {
            return currentColumn().currentSelectedPlot();
        }

        /// <summary>
        /// Update the number of cut planes for every column
        /// </summary>
        /// <param name="nbCuts"></param>
        public void updateCutsNb(int nbCuts)
        {
            // update common
            int diffCuts = DLLCommonCutTextureGeneratorList.Count - nbCuts;
            if (diffCuts < 0)
            {
                // generators
                for (int ii = 0; ii < -diffCuts; ++ii)
                    DLLCommonCutTextureGeneratorList.Add(new DLL.CutTextureGenerator());

                // textures 2D
                for (int ii = 0; ii < -diffCuts; ++ii)
                {
                    brainCutTextures.Add(new Texture2D(1, 1));
                    int id = brainCutTextures.Count - 1;
                    brainCutTextures[id].filterMode = FilterMode.Trilinear; // TODO : test performances with this parameter
                    brainCutTextures[id].wrapMode = TextureWrapMode.Clamp;
                    brainCutTextures[id].anisoLevel = 9; // TODO : test performances with this parameter
                    brainCutTextures[id].mipMapBias = -2; // never superior to -1 (colorscheme 8 texture glitch)

                    brainCutRotatedTextures.Add(new Texture2D(1, 1));
                    brainCutRotatedTextures[id].filterMode = FilterMode.Trilinear; // TODO : test performances with this parameter
                    brainCutRotatedTextures[id].wrapMode = TextureWrapMode.Clamp;
                    brainCutRotatedTextures[id].anisoLevel = 9; // TODO : test performances with this parameter
                    brainCutRotatedTextures[id].mipMapBias = -2; // never superior to -1 (colorscheme 8 texture glitch)
                }
            }
            else if (diffCuts > 0)
            {
                // generators
                for (int ii = 0; ii < diffCuts; ++ii)
                {
                    DLLCommonCutTextureGeneratorList[DLLCommonCutTextureGeneratorList.Count - 1].Dispose();
                    DLLCommonCutTextureGeneratorList.RemoveAt(DLLCommonCutTextureGeneratorList.Count - 1);
                }

                // textures 2D
                for (int ii = 0; ii < diffCuts; ++ii)
                {
                    Destroy(brainCutTextures[brainCutTextures.Count - 1]);
                    Destroy(brainCutRotatedTextures[brainCutRotatedTextures.Count - 1]);

                    brainCutTextures.RemoveAt(brainCutTextures.Count - 1);
                    brainCutRotatedTextures.RemoveAt(brainCutRotatedTextures.Count - 1);
                }
            }

            // update data columns
            for (int ii = 0; ii < columns.Count; ++ii)
            {
                columns[ii].updateCutPlanesNumber(nbCuts);
            }
        }

        /// <summary>
        /// Reset the colorsheme textures used with the generators
        /// </summary>
        private void resetColorSchemesTextures()
        {
            // id colorScheme to use for cuts meshes
            int cutSchemesToUse = 15; // 13
            //int cutSchemesToUse = 0;

            // id colorScheme to use for brain meshes
            int brainSchemesToUse = 13;
            //int brainSchemesToUse = 0;

            // influences
            List<Color> infColors = new List<Color>(), infIRMFColors = new List<Color>();
            infColors.Add(TextureColors.blue());
            infColors.Add(TextureColors.coolBlue());
            infColors.Add(TextureColors.green());
            infColors.Add(TextureColors.yellow());
            infColors.Add(TextureColors.red());
            float[] infFactors = new float[3] { 0.4f, 0.5f, 0.6f };
            infIRMFColors.Add(TextureColors.red());
            infIRMFColors.Add(TextureColors.red());
            infIRMFColors.Add(TextureColors.red());
            infIRMFColors.Add(TextureColors.red());
            infIRMFColors.Add(TextureColors.red());


            DLLCutColorScheme = DLL.Texture.generateColorScheme(cutSchemesToUse, infColors, infFactors);
            DLLCutIRMFColorScheme = DLL.Texture.generateColorScheme(0, infIRMFColors, infFactors);
            DLLBrainColorScheme = DLL.Texture.generateColorScheme(cutSchemesToUse, infColors, infFactors);
            DLLBrainMainColor = DLL.Texture.generate1DColorScheme(brainSchemesToUse);

            Destroy(brainMainColorTexture);
            brainMainColorTexture = DLLBrainMainColor.getTexture2D();
            brainMainColorTexture.filterMode = FilterMode.Trilinear; // TODO : test performances with this parameter
            brainMainColorTexture.wrapMode = TextureWrapMode.Clamp;
            brainMainColorTexture.anisoLevel = 9; // TODO : test performances with this parameter
            brainMainColorTexture.mipMapBias = -2; // never superior to -1 (colorscheme 8 texture glitch)

            Destroy(brainColorSchemeTexture); // TODO : inused
            brainColorSchemeTexture = DLLBrainColorScheme.getTexture2D();
            brainColorSchemeTexture.filterMode = FilterMode.Trilinear; // TODO : test performances with this parameter
            brainColorSchemeTexture.wrapMode = TextureWrapMode.Clamp;
            brainColorSchemeTexture.anisoLevel = 9; // TODO : test performances with this parameter
            brainColorSchemeTexture.mipMapBias = -2; // never superior to -1 (colorscheme 8 texture glitch)     
        }


        /// <summary>
        /// Update the number of columns and set the number of cut planes for each ones
        /// </summary>
        /// <param name="nbIEEGColumns"></param>
        /// /// <param name="nbIRMFColumns"></param>
        /// <param name="nbCuts"></param>
        public void updateColumnsNb(int nbIEEGColumns, int nbIRMFColumns, int nbCuts)
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
            if (nbIRMFColumns != columnsIRMF.Count)
            {
                for (int ii = 0; ii < columnsIRMF.Count; ++ii)
                {
                    columnsIRMF[ii].clean();
                }
            }

            // resize the data GO list            
            int diffIEEGColumns = columnsIEEG.Count - nbIEEGColumns;
            if (diffIEEGColumns < 0)
            {
                for (int ii = 0; ii < -diffIEEGColumns; ++ii)
                {
                    // add column
                    columnsIEEGGO.Add(new GameObject("column IEEG " + columnsIEEGGO.Count + 1));
                    columnsIEEGGO[columnsIEEGGO.Count - 1].AddComponent<Column3DViewIEEG>();
                    columnsIEEGGO[columnsIEEGGO.Count - 1].transform.SetParent(this.transform);
                }
            }
            else if (diffIEEGColumns > 0)
            {
                for (int ii = 0; ii < diffIEEGColumns; ++ii)
                {
                    // destroy column
                    int idColumn = columnsIEEGGO.Count - 1;
                    Destroy(columnsIEEGGO[idColumn]);
                    columnsIEEGGO.RemoveAt(idColumn);
                }
            }

            // resize the IRMF GO list      
            int diffIRMFColumns = columnsIRMF.Count - nbIRMFColumns;
            if (diffIRMFColumns < 0)
            {
                for (int ii = 0; ii < -diffIRMFColumns; ++ii)
                {
                    // add column
                    DLLVolumeIRMFList.Add(new DLL.Volume());

                    columnsIRMFGO.Add(new GameObject("column IRMF " + columnsIRMFGO.Count + 1));
                    columnsIRMFGO[columnsIRMFGO.Count - 1].AddComponent<Column3DViewIRMF>();
                    columnsIRMFGO[columnsIRMFGO.Count - 1].transform.SetParent(this.transform);
                }
            }
            else if (diffIRMFColumns > 0)
            {
                for (int ii = 0; ii < diffIRMFColumns; ++ii)
                {
                    // destroy column
                    int idColumn = columnsIRMFGO.Count - 1;
                    Destroy(columnsIRMFGO[idColumn]);
                    columnsIRMFGO.RemoveAt(idColumn);

                    DLLVolumeIRMFList[idColumn].Dispose();
                    DLLVolumeIRMFList.RemoveAt(idColumn);
                }
            }

            // init new columns IEEG            
            if (nbIEEGColumns != columnsIEEG.Count)
            {
                columnsIEEG = new List<Column3DViewIEEG>();
                for (int ii = 0; ii < nbIEEGColumns; ++ii)
                {
                    columnsIEEG.Add(columnsIEEGGO[ii].GetComponent<Column3DViewIEEG>());
                    columnsIEEG[ii].init(ii, nbCuts, meshSplitNb, DLLLoadedPatientsElectrodes, PlotsPatientParent);

                    if (latencyFilesDefined)
                        columnsIEEG[ii].currentLatencyFile = 0;
                }
            }

            // init new columns IRMF
            if (nbIRMFColumns != columnsIRMF.Count)
            {
                // update IRMF columns mask
                bool[] maskColumnsOR = new bool[DLLLoadedPatientsElectrodes.getTotalPlotsNumber()];
                for (int ii = 0; ii < PlotsList.Count; ++ii)
                {
                    bool mask = false;

                    for (int jj = 0; jj < columnsIEEG.Count; ++jj)
                    {
                        mask = mask || columnsIEEG[jj].Plots[ii].columnMask;
                    }

                    maskColumnsOR[ii] = mask;
                }

                for (int ii = 0; ii < columnsIRMF.Count; ++ii)
                {
                    for (int jj = 0; jj < PlotsList.Count; ++jj)
                    {
                        columnsIRMF[ii].Plots[jj].columnMask = maskColumnsOR[jj];
                    }
                }

                columnsIRMF = new List<Column3DViewIRMF>();
                for (int ii = 0; ii < nbIRMFColumns; ++ii)
                {
                    columnsIRMF.Add(columnsIRMFGO[ii].GetComponent<Column3DViewIRMF>());
                    columnsIRMF[columnsIRMF.Count - 1].init(columnsIEEG.Count + ii, nbCuts, meshSplitNb, DLLLoadedPatientsElectrodes, PlotsPatientParent);

                    for (int jj = 0; jj < PlotsList.Count; ++jj)
                    {
                        columnsIRMF[columnsIRMF.Count - 1].Plots[jj].columnMask = maskColumnsOR[jj];
                    }
                }
            }

            // rest columns/columnsGO
            updateColumnsLists();

            commonMask = new bool[DLLLoadedPatientsElectrodes.getTotalPlotsNumber()];

            // reset textures
            resetColorSchemesTextures();

            if (idSelectedColumn >= columns.Count)
                idSelectedColumn = columns.Count - 1;
        }

        /// <summary>
        /// Define the single patient and associated data
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="columnDataList"></param>
        public void setSpTimelinesData(HBP.Data.Patient.Patient patient, List<HBP.Data.Visualisation.ColumnData> columnDataList)
        {
            spPatient = patient;
            for (int ii = 0; ii < columnsIEEG.Count; ++ii)
            {
                columnsIEEG[ii].setColumnData(columnDataList[ii]);
            }
        }


        /// <summary>
        /// Define the mp patients list and associated data
        /// </summary>
        /// <param name="patientList"></param>
        /// <param name="columnDataList"></param>
        /// <param name="ptsPathFileList"></param>
        public void setMpTimelinesData(List<Data.Patient.Patient> patientList, List<Data.Visualisation.ColumnData> columnDataList, List<string> ptsPathFileList)
        {
            mpPatients = patientList;
            for (int ii = 0; ii < columnsIEEG.Count; ++ii)
            {
                columnsIEEG[ii].setColumnData(columnDataList[ii]);
            }
        }

        /// <summary>
        /// Return the brain cut texture list corresponding to the situation
        /// </summary>
        /// <param name="indexColumn"></param>
        /// <param name="rotatedTexture"></param>
        /// <param name="generatorUpToDate"></param>
        /// <param name="displayLatenciesMode"></param>
        /// <returns></returns>
        public List<Texture2D> getBrainCutTextureList(int indexColumn, bool rotatedTexture, bool generatorUpToDate, bool displayLatenciesMode)
        {
            if (isIRMFColumn(indexColumn))
            {
                if (!rotatedTexture)
                    return columnsIRMF[indexColumn - columnsIEEG.Count].brainCutIRMFTextures;

                return columnsIRMF[indexColumn - columnsIEEG.Count].brainCutIRMFRotatedTextures;
            }
            else
            {
                if (generatorUpToDate && !displayLatenciesMode)
                {
                    if (!rotatedTexture)
                        return columnsIEEG[indexColumn].brainCutWithAmpTextures;

                    return columnsIEEG[indexColumn].brainCutWithAmpRotatedTextures;
                }

                if (!rotatedTexture)
                    return brainCutTextures;

                return brainCutRotatedTextures;
            }
        }

        /// <summary>
        /// Return the good cut texture corresponding to the situation
        /// </summary>
        /// <param name="indexColumn"></param>
        /// <param name="indexCut"></param>
        /// <param name="rotatedTexture"></param>
        /// <param name="generatorUpToDate"></param>
        /// <returns></returns>
        public Texture2D getBrainCutTexture(int indexColumn, int indexCut, bool rotatedTexture, bool generatorUpToDate, bool displayLatenciesMode)
        {
            return getBrainCutTextureList(indexColumn, rotatedTexture, generatorUpToDate, displayLatenciesMode)[indexCut];
        }

        /// <summary>
        /// Create the mesh texture dll and texture2D, and update the mesh UV
        /// </summary>
        /// <param name="indexCut"></param>
        public void createTextureAndUpdateMeshUV(int indexCut)
        {
            Profiler.BeginSample("createTextureAndUpdateMeshUV 0");
            DLLCommonCutTextureGeneratorList[indexCut].createTextureAndUpdateMeshUV(DLLVolume, planesList[indexCut], DLLCutColorScheme, DLLCutsList[indexCut + 1], 
                IRMCalMinFactor, IRMCalMaxFactor, notInBrainColor);
            Profiler.EndSample();

            Profiler.BeginSample("createTextureAndUpdateMeshUV 1");

            // destroy previous textures
            Destroy(brainCutTextures[indexCut]);
            Destroy(brainCutRotatedTextures[indexCut]);

            Profiler.EndSample();
            Profiler.BeginSample("createTextureAndUpdateMeshUV 2");

            // retrieve orientation to apply
            string orientation = "custom";
            if (idPlanesOrientationList[indexCut] == 0)
            {
                orientation = "Axial";
            }
            else if (idPlanesOrientationList[indexCut] == 1)
            {
                orientation = "Coronal";
            }
            else if (idPlanesOrientationList[indexCut] == 2)
            {
                orientation = "Sagital";
            }

            bool flip = planesOrientationFlipList[indexCut];

            DLL.Texture textureCut = DLLCommonCutTextureGeneratorList[indexCut].getTexture();

            Profiler.EndSample();
            Profiler.BeginSample("createTextureAndUpdateMeshUV 3");

            // blur texture
            //if (!singlePatient)
            //    textureCut.applyBlur();

            brainCutTextures[indexCut] = textureCut.getTexture2D();

            Profiler.EndSample();
            Profiler.BeginSample("createTextureAndUpdateMeshUV 4");

            // rotate texture
            if (textureCut.m_sizeTexture[0] > 0)
            {
                DLL.Texture rotatedTextureCut = DLL.Texture.applyCorrespondingRotationToCut(textureCut, orientation, flip);
                brainCutRotatedTextures[indexCut] = rotatedTextureCut.getTexture2D();
            }
            else
            {
                brainCutRotatedTextures[indexCut] = new Texture2D(1, 1);
            }

            Profiler.EndSample();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexColumn"></param>
        /// <param name="indexCut"></param>
        /// <param name="thresholdInfluence"></param>
        public void colorTextureWithIRMF(int idIRMF, int indexCut)
        {
            DLLCommonCutTextureGeneratorList[indexCut].colorTextureWithIRMF(DLLVolumeIRMFList[idIRMF], DLLCutIRMFColorScheme, 
                columnsIRMF[idIRMF].calMin, columnsIRMF[idIRMF].calMax, columnsIRMF[idIRMF].alpha);

            // destroy previous textures
            Destroy(columnsIRMF[idIRMF].brainCutIRMFTextures[indexCut]);
            Destroy(columnsIRMF[idIRMF].brainCutIRMFRotatedTextures[indexCut]);

            // retrieve orientation to apply
            string orientation = "custom";
            if (idPlanesOrientationList[indexCut] == 0)
            {
                orientation = "Axial";
            }
            else if (idPlanesOrientationList[indexCut] == 1)
            {
                orientation = "Coronal";
            }
            else if (idPlanesOrientationList[indexCut] == 2)
            {
                orientation = "Sagital";
            }

            bool flip = planesOrientationFlipList[indexCut];

            DLL.Texture textureCut = DLLCommonCutTextureGeneratorList[indexCut].getTextureWithIRMF();

            // retrieve normal texture
            columnsIRMF[idIRMF].brainCutIRMFTextures[indexCut] = textureCut.getTexture2D();

            // retrieve rotated texture
            if (textureCut.m_sizeTexture[0] > 0)
            {
                DLL.Texture rotatedTextureCut = DLL.Texture.applyCorrespondingRotationToCut(textureCut, orientation, flip);
                columnsIRMF[idIRMF].brainCutIRMFRotatedTextures[indexCut] = rotatedTextureCut.getTexture2D();
            }
            else
            {
                columnsIRMF[idIRMF].brainCutIRMFRotatedTextures[indexCut] = new Texture2D(1, 1);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexColumn"></param>
        /// <param name="indexCut"></param>
        /// <param name="thresholdInfluence"></param>
        public void colorTextureWithAmplitudes(int indexColumn, int indexCut)
        {
            Profiler.BeginSample("colorTextureWithAmplitudes 0");
            


            // ...
            columnsIEEG[indexColumn].DLLCutTextureGeneratorList[indexCut].colorTextureWithAmplitudes(columnsIEEG[indexColumn], DLLCutColorScheme, notInBrainColor);

            Profiler.EndSample();
            Profiler.BeginSample("colorTextureWithAmplitudes 1");

            // destroy previous textures
            Destroy(columnsIEEG[indexColumn].brainCutWithAmpTextures[indexCut]);
            Destroy(columnsIEEG[indexColumn].brainCutWithAmpRotatedTextures[indexCut]);

            // retrieve orientation to apply
            string orientation = "custom";
            if (idPlanesOrientationList[indexCut] == 0)
            {
                orientation = "Axial";
            }
            else if (idPlanesOrientationList[indexCut] == 1)
            {
                orientation = "Coronal";
            }
            else if (idPlanesOrientationList[indexCut] == 2)
            {
                orientation = "Sagital";
            }

            bool flip = planesOrientationFlipList[indexCut];


            DLL.Texture textureCut = columnsIEEG[indexColumn].DLLCutTextureGeneratorList[indexCut].getTextureWithAmplitudes();

            Profiler.EndSample();
            Profiler.BeginSample("colorTextureWithAmplitudes 2");

            // blur texture
            //if (!singlePatient)
            //    textureCut.applyBlur();

            columnsIEEG[indexColumn].brainCutWithAmpTextures[indexCut] = textureCut.getTexture2D();

            Color32 a;

            Profiler.EndSample();


            Profiler.BeginSample("colorTextureWithAmplitudes 3");

            // rotate texture
            if (textureCut.m_sizeTexture[0] > 0)
            {
                DLL.Texture rotatedTextureCut = DLL.Texture.applyCorrespondingRotationToCut(textureCut, orientation, flip);
                columnsIEEG[indexColumn].brainCutWithAmpRotatedTextures[indexCut] = rotatedTextureCut.getTexture2D();
            }
            else
            {
                columnsIEEG[indexColumn].brainCutWithAmpRotatedTextures[indexCut] = new Texture2D(1, 1);
            }
            Profiler.EndSample();

        }

        /// <summary>
        /// Compute the amplitudes textures coordinates for the brain mesh
        /// </summary>
        /// <param name="whiteInflatedMeshes"></param>
        /// <param name="indexColumn"></param>
        /// <param name="thresholdInfluence"></param>
        /// <param name="alphaMin"></param>
        /// <param name="alphaMax"></param>
        public void computeSurfaceTextCoordAmplitudes(bool whiteInflatedMeshes, int indexColumn)
        {
            //System.IntPtr ptr = DLL.BrainTextureGenerator.create_BrainSurfaceTextureGeneratorsContainer();
            //for (int ii = 0; ii < meshSplitNb; ++ii)
            //    DLL.BrainTextureGenerator.add_BrainSurfaceTextureGeneratorsContainer(ptr, columnsIEEG[indexColumn].DLLBrainTextureGeneratorList[ii].getHandle());
            //DLL.BrainTextureGenerator.display_BrainSurfaceTextureGeneratorsContainer(ptr);
            
            for (int ii = 0; ii < meshSplitNb; ++ii)
            {
                columnsIEEG[indexColumn].DLLBrainTextureGeneratorList[ii].computeSurfaceTextCoordAmplitudes(whiteInflatedMeshes ?
                    DLLSplittedWhiteMeshesList[ii] : DLLSplittedMeshesList[ii], columnsIEEG[indexColumn]);
            }
        }

        /// <summary>
        /// Update the plot rendering parameters for all columns
        /// </summary>
        public void updateColumnsPlotsRendering(SceneStatesInfo data)
        {
            for (int ii = 0; ii < columnsIEEG.Count; ++ii)
            {
                Latencies latencyFile = null;

                if (columnsIEEG[ii].currentLatencyFile != -1)
                {
                    latencyFile = latenciesFiles[columnsIEEG[ii].currentLatencyFile];
                }

                columnsIEEG[ii].updatePlotsRendering(data, latencyFile);
            }
            for (int ii = 0; ii < columnsIRMF.Count; ++ii)
            {
                columnsIRMF[ii].updatePlotsVisibility(data);
            }
        }

        /// <summary>
        /// Update the visiblity of the ROI for all columns
        /// </summary>
        /// <param name="visible"></param>
        public void updateROIVisibility(bool visible)
        {
            for (int ii = 0; ii < columnsIEEG.Count; ++ii)
            {
                if (columnsIEEG[ii].ROI != null)
                    columnsIEEG[ii].ROI.setRenderingState(visible);
            }

            for (int ii = 0; ii < columnsIRMF.Count; ++ii)
            {
                if (columnsIRMF[ii].ROI != null)
                    columnsIRMF[ii].ROI.setRenderingState(visible);
            }
        }

        /// <summary>
        /// Update the visiblity of the plots for all columns
        /// </summary>
        /// <param name="visible"></param>
        public void updatePlotsVisibility(bool visible)
        {
            for (int ii = 0; ii < columnsIEEG.Count; ++ii)
            {
                columnsIEEG[ii].setVisiblePlots(visible);
            }

            for (int ii = 0; ii < columnsIRMF.Count; ++ii)
            {
                columnsIRMF[ii].setVisiblePlots(visible);

            }
        }

        #endregion functions
    }
}