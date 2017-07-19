
/**
 * \file    SP3DScene.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define SP3DScene class
 */

using System;
using System.Collections.Generic;

// unity
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Events;
using System.Collections;
using CielaSpike;

namespace HBP.Module3D
{
    /// <summary>
    /// The single patient scene class, inheritance of Base3DScene.
    /// </summary>
    [AddComponentMenu("Scenes/Single Patient 3D Scene")]
    public class SinglePatient3DScene : Base3DScene
    {
        #region Properties
        /// <summary>
        /// Type of the scene
        /// </summary>
        public override SceneType Type
        {
            get
            {
                return SceneType.SinglePatient;
            }
        }
        /// <summary>
        /// Patient of this scene
        /// </summary>
        public Data.Patient Patient
        {
            get
            {
                return Visualization.Patients[0];
            }
        }
        /// <summary>
        /// CCEP Labels
        /// </summary>
        private List<string> CCEPLabels = null;

        /// <summary>
        /// Event for asking the UI to update the latencies display on the plot menu (params : labels)
        /// </summary>
        public GenericEvent<List<string>> OnUpdateLatencies = new GenericEvent<List<string>>();

        private const float LOADING_TRANSFORMATION_PROGRESS = 0.05f;
        private const float LOADING_MESHES_PROGRESS = 0.2f;
        private const float LOADING_VOLUME_PROGRESS = 0.15f;
        private const float LOADING_ELECTRODES_PROGRESS = 0.05f;
        private const float SETTING_TIMELINE_PROGRESS = 0.1f;
        #endregion

        #region Public Methods
        /// <summary>
        /// Update the meshes cuts
        /// </summary>
        public override void ComputeMeshesCut()
        {
            UnityEngine.Profiling.Profiler.BeginSample("TEST-SP3DScene-Update compute_meshes_cuts 0 cutSurface"); // 40%

            // choose the mesh
            SceneInformation.MeshToDisplay = new DLL.Surface();
            switch (SceneInformation.MeshTypeToDisplay)
            {
                case SceneStatesInfo.MeshType.Grey:
                    switch (SceneInformation.MeshPartToDisplay)
                    {
                        case SceneStatesInfo.MeshPart.Both:
                            SceneInformation.MeshToDisplay = m_ColumnManager.BothHemi;
                            break;
                        case SceneStatesInfo.MeshPart.Left:
                            SceneInformation.MeshToDisplay = m_ColumnManager.LHemi;
                            break;
                        case SceneStatesInfo.MeshPart.Right:
                            SceneInformation.MeshToDisplay = m_ColumnManager.RHemi;
                            break;
                    }
                    break;
                case SceneStatesInfo.MeshType.White:
                    switch (SceneInformation.MeshPartToDisplay)
                    {
                        case SceneStatesInfo.MeshPart.Both:
                            SceneInformation.MeshToDisplay = m_ColumnManager.BothWhite;
                            break;
                        case SceneStatesInfo.MeshPart.Left:
                            SceneInformation.MeshToDisplay = m_ColumnManager.LWhite;
                            break;
                        case SceneStatesInfo.MeshPart.Right:
                            SceneInformation.MeshToDisplay = m_ColumnManager.RWhite;
                            break;
                    }
                    break;
                case SceneStatesInfo.MeshType.Inflated:
                    // ...
                    break;
            }

            if (SceneInformation.MeshToDisplay == null) return;

            // get the middle
            SceneInformation.MeshCenter = SceneInformation.MeshToDisplay.BoundingBox.Center;

            // cut the mesh
            List<DLL.Surface> cuts;
            if (Cuts.Count > 0)
                cuts = new List<DLL.Surface>(SceneInformation.MeshToDisplay.Cut(Cuts.ToArray(), !SceneInformation.CutHolesEnabled));
            else
                cuts = new List<DLL.Surface>() { (DLL.Surface)SceneInformation.MeshToDisplay.Clone() };

            if (m_ColumnManager.DLLCutsList.Count != cuts.Count)
                m_ColumnManager.DLLCutsList = cuts;
            else
            {
                // swap DLL pointer
                for (int ii = 0; ii < cuts.Count; ++ii)
                    m_ColumnManager.DLLCutsList[ii].SwapDLLHandle(cuts[ii]);
            }

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-SP3DScene-Update compute_meshes_cuts 1 splitToSurfaces"); // 2%

            // split the cut mesh         
            m_ColumnManager.DLLSplittedMeshesList = new List<DLL.Surface>(m_ColumnManager.DLLCutsList[0].SplitToSurfaces(m_ColumnManager.MeshSplitNumber));

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-SP3DScene-Update compute_meshes_cuts 2 reset brain texture generator"); // 11%

            // reset brain texture generator
            for (int ii = 0; ii < m_ColumnManager.MeshSplitNumber; ++ii)
            {
                m_ColumnManager.DLLCommonBrainTextureGeneratorList[ii].Reset(m_ColumnManager.DLLSplittedMeshesList[ii], m_ColumnManager.DLLVolume);
                m_ColumnManager.DLLCommonBrainTextureGeneratorList[ii].ComputeUVMainWithVolume(m_ColumnManager.DLLSplittedMeshesList[ii], m_ColumnManager.DLLVolume, m_ColumnManager.MRICalMinFactor, m_ColumnManager.MRICalMaxFactor);
            }

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-SP3DScene-Update compute_meshes_cuts 3 update cut brain mesh object mesh filter"); // 6%

            ResetTriangleErasing(false);

            // update brain mesh object mesh filter
            for (int ii = 0; ii < m_ColumnManager.MeshSplitNumber; ++ii)
                m_ColumnManager.DLLSplittedMeshesList[ii].UpdateMeshFromDLL(m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-SP3DScene-Update compute_meshes_cuts 4 update cuts generators"); // 17%
            

            // update cuts generators
            for (int ii = 0; ii < Cuts.Count; ++ii)
            {
                for (int jj = 0; jj < m_ColumnManager.ColumnsIEEG.Count; ++jj)
                    m_ColumnManager.DLLMRIGeometryCutGeneratorList[ii].Reset(m_ColumnManager.DLLVolume, Cuts[ii]);                        

                m_ColumnManager.DLLMRIGeometryCutGeneratorList[ii].UpdateCutMeshUV(ColumnManager.DLLCutsList[ii + 1]);
                m_ColumnManager.DLLCutsList[ii + 1].UpdateMeshFromDLL(m_DisplayedObjects.BrainCutMeshes[ii].GetComponent<MeshFilter>().mesh);
            }

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-SP3DScene-Update compute_meshes_cuts 5 create null uv2/uv3 arrays"); // 0%
            
            // create null uv2/uv3 arrays
            m_ColumnManager.UVNull = new List<Vector2[]>(m_ColumnManager.MeshSplitNumber);
            for (int ii = 0; ii < m_ColumnManager.MeshSplitNumber; ++ii)
            {
                m_ColumnManager.UVNull.Add(new Vector2[m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh.vertexCount]);
                Extensions.ArrayExtensions.Fill(m_ColumnManager.UVNull[ii], new Vector2(0.01f, 1f));
            }

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-SP3DScene-Update compute_meshes_cuts 6 end"); // 0%
            

            // enable cuts gameobject
            for (int ii = 0; ii < Cuts.Count; ++ii)
                    m_DisplayedObjects.BrainCutMeshes[ii].SetActive(true); 

            SceneInformation.CollidersUpdated = false; // colliders are now longer up to date
            SceneInformation.CutMeshGeometryNeedsUpdate = false;   // planes are now longer requested to be updated 
            SceneInformation.IsGeneratorUpToDate = false; // generator is not up to date anymore

            // update amplitude for all columns
            for (int ii = 0; ii < m_ColumnManager.ColumnsIEEG.Count; ++ii)
                m_ColumnManager.ColumnsIEEG[ii].UpdateIEEG = true;

            UnityEngine.Profiling.Profiler.EndSample();
        }
        /// <summary>
        /// Define the current plot as the source
        /// </summary>
        public void SetCurrentSiteAsSource()
        {
            switch (m_ColumnManager.SelectedColumn.Type)
            {
                case Column3D.ColumnType.FMRI:
                    return;
                case Column3D.ColumnType.IEEG:
                    Column3DIEEG currCol = (Column3DIEEG)m_ColumnManager.SelectedColumn;
                    currCol.SourceSelectedID = currCol.SelectedSiteID;
                    m_ColumnManager.UpdateAllColumnsSitesRendering(SceneInformation);
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// Undefine the current plot as the source
        /// </summary>
        public void UndefineCurrentSource()
        {
            switch (m_ColumnManager.SelectedColumn.Type)
            {
                case Column3D.ColumnType.FMRI:
                    return;
                case Column3D.ColumnType.IEEG:
                    Column3DIEEG currCol = (Column3DIEEG)m_ColumnManager.SelectedColumn;
                    currCol.SourceSelectedID = -1;
                    m_ColumnManager.UpdateAllColumnsSitesRendering(SceneInformation);
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// Send additionnal plot info to hight level UI
        /// </summary>
        public override void SendAdditionalSiteInfoRequest(Site previousPlot = null) // TODO : deporter dans c manager
        {
            switch (m_ColumnManager.SelectedColumn.Type)
            {
                case Column3D.ColumnType.FMRI:
                    return;
                case Column3D.ColumnType.IEEG:
                    if (m_ColumnManager.SelectedColumn.SelectedSiteID != -1)
                    {
                        List<List<bool>> masksColumnsData = new List<List<bool>>(m_ColumnManager.ColumnsIEEG.Count);
                        for (int ii = 0; ii < m_ColumnManager.ColumnsIEEG.Count; ++ii)
                        {
                            masksColumnsData.Add(new List<bool>(m_ColumnManager.ColumnsIEEG[ii].Sites.Count));

                            for (int jj = 0; jj < m_ColumnManager.ColumnsIEEG[ii].Sites.Count; ++jj)
                            {
                                Site p = m_ColumnManager.ColumnsIEEG[ii].Sites[jj];
                                bool keep = (!p.Information.IsBlackListed && !p.Information.IsExcluded && !p.Information.IsMasked);
                                masksColumnsData[ii].Add(keep);
                            }
                        }

                        SiteRequest request = new SiteRequest();
                        request.spScene = true;
                        request.idSite1 = m_ColumnManager.SelectedColumn.SelectedSiteID;
                        request.idSite2 = (previousPlot == null) ? -1 : previousPlot.Information.SitePatientID;
                        request.idPatient = Patient.ID;
                        request.idPatient2 = Patient.ID;
                        request.maskColumn = masksColumnsData;

                        Events.OnRequestSiteInformation.Invoke(request);
                    }
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// Update the id of the latency file
        /// </summary>
        /// <param name="id"></param>
        public void UpdateCurrentLatencyFile(int id)
        {
            switch (m_ColumnManager.SelectedColumn.Type)
            {
                case Column3D.ColumnType.FMRI:
                    return;
                case Column3D.ColumnType.IEEG:
                    Column3DIEEG currCol = (Column3DIEEG)m_ColumnManager.SelectedColumn;
                    currCol.CurrentLatencyFile = id;
                    UndefineCurrentSource();
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region Coroutines
        /// <summary>
        /// Reset the scene : reload meshes, IRM, plots, and regenerate textures
        /// </summary>
        /// <param name="patient"></param>
        public IEnumerator c_Initialize(Data.Visualization.Visualization visualization, GenericEvent<float, float, string> onChangeProgress, bool postIRM)
        {
            yield return Ninja.JumpToUnity;
            float progress = 0.5f;

            m_ModesManager.UpdateMode(Mode.FunctionsId.ResetScene);
            
            int sceneID = ApplicationState.Module3D.NumberOfScenesLoadedSinceStart;
            gameObject.name = "SinglePatient Scene (" + sceneID + ")";
            transform.position = new Vector3(HBP3DModule.SPACE_BETWEEN_SCENES_AND_COLUMNS * sceneID, transform.position.y, transform.position.z);

            List<string> ptsFiles = new List<string>(), namePatients = new List<string>();
            ptsFiles.Add(Patient.Brain.PatientBasedImplantation);
            namePatients.Add(Patient.Place + "_" + Patient.Date + "_" + Patient.Name);

            List<string> meshesFiles = new List<string>();
            meshesFiles.Add(Patient.Brain.LeftCerebralHemisphereMesh);
            meshesFiles.Add(Patient.Brain.RightCerebralHemisphereMesh);
            
            // reset columns
            m_ColumnManager.Initialize(m_Cuts.Count);
            m_ColumnManager.OnSelectColumnManager.AddListener((cm) =>
            {
                IsSelected = true;
            });
            
            progress += LOADING_TRANSFORMATION_PROGRESS;
            onChangeProgress.Invoke(progress, 0.05f, "Loading Transformation");
            DLL.Transformation meshTransformation = new DLL.Transformation();
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(meshTransformation.c_Load(Patient.Brain.PreoperativeBasedToScannerBasedTransformation));
            if (postIRM) { /*TODO*/ }
            
            progress += LOADING_MESHES_PROGRESS;
            onChangeProgress.Invoke(progress, 2.0f, "Loading meshes");
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadBrainSurface(meshesFiles, Patient.Brain.PreoperativeBasedToScannerBasedTransformation));
            
            progress += LOADING_VOLUME_PROGRESS;
            onChangeProgress.Invoke(progress, 1.5f, "Loading volume");
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadNiftiBrainVolume(Patient.Brain.PreoperativeMRI));

            progress += LOADING_ELECTRODES_PROGRESS;
            onChangeProgress.Invoke(progress, 0.05f, "Loading electrodes");
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadSites(ptsFiles, namePatients));
            SceneInformation.CutMeshGeometryNeedsUpdate = true;
            
            progress += SETTING_TIMELINE_PROGRESS;
            onChangeProgress.Invoke(progress, 0.5f, "Setting timeline");
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_SetTimelineData());
            
            UpdateMeshesColliders();
            Events.OnUpdateCameraTarget.Invoke(m_ColumnManager.BothHemi.BoundingBox.Center);
        }
        /// <summary>
        /// Reset the meshes of the scene with GII files
        /// </summary>
        /// <param name="pathGIIBrainFiles"></param>
        /// <param name="pathTransformFile"></param>
        /// <returns></returns>
        private IEnumerator c_LoadBrainSurface(List<string> pathGIIBrainFiles, string pathTransformFile)
        {
            //####### CHECK ACESS
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.ResetGIIBrainSurfaceFile))
            {
                throw new ModeAccessException(m_ModesManager.CurrentModeName);
            }
            //##################

            SceneInformation.MeshesLoaded = false;

            // checks parameters
            foreach (string elem in pathGIIBrainFiles)
                if (elem.Length == 0)
                {
                    throw new EmptyFilePathException("GII");
                }
            if (pathTransformFile.Length == 0)
            {
                throw new EmptyFilePathException("Transform");
            }


            DLL.Transformation transfo = new DLL.Transformation(); // ############################ TEST
            yield return transfo.c_Load(pathTransformFile);

            // load left hemi
            bool leftMeshLoaded = m_ColumnManager.LHemi.LoadGIIFile(pathGIIBrainFiles[0], true, pathTransformFile);
            bool leftWhiteLoaded = false, rightWhiteLoaded = false;
            bool leftParcelsLoaded = false, rightParcelsLoaded = false;
            if (leftMeshLoaded)
            {
                m_ColumnManager.LHemi.FlipTriangles();  // the transformation inverses one axis, so faces of the surface must be flipped (TODO : case with no transformation or no inverse axi transfo)
                m_ColumnManager.LHemi.ComputeNormals();

                string leftWhitePath = pathGIIBrainFiles[0].Replace("_Lhemi", "_Lwhite");
                leftWhiteLoaded = m_ColumnManager.LWhite.LoadGIIFile(leftWhitePath, true, pathTransformFile);
                if (leftWhiteLoaded)
                {
                    m_ColumnManager.LWhite.FlipTriangles();
                    m_ColumnManager.LWhite.ComputeNormals();

                    string[] split = leftWhitePath.Split('\\');
                    string parcelPath = leftWhitePath.Substring(0, leftWhitePath.LastIndexOf('\\')) + "\\surface_analysis\\" + split[split.Length - 1].Replace(".gii", "") + "_parcels_marsAtlas.gii";
                    leftParcelsLoaded = m_ColumnManager.LWhite.SearchMarsParcelFileAndUpdateColors(ApplicationState.Module3D.MarsAtlasIndex, parcelPath);
                }
            }


            // load right hemi
            bool rightMeshLoaded = m_ColumnManager.RHemi.LoadGIIFile(pathGIIBrainFiles[1], true, pathTransformFile);
            if (rightMeshLoaded)
            {
                m_ColumnManager.RHemi.FlipTriangles();  // the transformation inverses one axis, so faces of the surface must be flipped (TODO : case with no transformation or no inverse axi transfo)
                m_ColumnManager.RHemi.ComputeNormals();

                string rightWhitePath = pathGIIBrainFiles[1].Replace("_Rhemi", "_Rwhite");
                rightWhiteLoaded = m_ColumnManager.RWhite.LoadGIIFile(rightWhitePath, true, pathTransformFile);
                if (rightWhiteLoaded)
                {
                    m_ColumnManager.RWhite.FlipTriangles();
                    m_ColumnManager.RWhite.ComputeNormals();

                    string[] split = rightWhitePath.Split('\\');
                    string parcelPath = rightWhitePath.Substring(0, rightWhitePath.LastIndexOf('\\')) + "\\surface_analysis\\" + split[split.Length - 1].Replace(".gii", "") + "_parcels_marsAtlas.gii";
                    rightParcelsLoaded = m_ColumnManager.RWhite.SearchMarsParcelFileAndUpdateColors(ApplicationState.Module3D.MarsAtlasIndex, parcelPath);
                }
            }

            // fusion
            if (leftMeshLoaded && rightMeshLoaded)
            {
                // copy left
                m_ColumnManager.BothHemi = (DLL.Surface)m_ColumnManager.LHemi.Clone();

                // add right
                m_ColumnManager.BothHemi.Add(m_ColumnManager.RHemi);
                SceneInformation.MeshesLoaded = true;

                // get the middle
                SceneInformation.MeshCenter = m_ColumnManager.BothHemi.BoundingBox.Center;

                if (rightWhiteLoaded && leftWhiteLoaded)
                {
                    m_ColumnManager.BothWhite = (DLL.Surface)m_ColumnManager.LWhite.Clone();
                    // add right
                    m_ColumnManager.BothWhite.Add(m_ColumnManager.RWhite);
                    SceneInformation.WhiteMeshesAvailables = true;

                    SceneInformation.MarsAtlasParcelsLoaded = leftParcelsLoaded && rightParcelsLoaded;
                }
            }
            else
            {
                SceneInformation.MeshesLoaded = false;
                throw new CanNotLoadGIIFile(leftMeshLoaded, rightMeshLoaded);
            }

            int maxVerticesNb = m_ColumnManager.BothHemi.NumberOfVertices;
            if (leftWhiteLoaded && rightWhiteLoaded)
                maxVerticesNb = Math.Max(maxVerticesNb, m_ColumnManager.BothWhite.NumberOfVertices);
            int nbSplits = (maxVerticesNb / 65000) + (((maxVerticesNb % 60000) != 0) ? 3 : 2);
            yield return Ninja.JumpToUnity;
            ResetSplitsNumber(nbSplits);
            yield return Ninja.JumpBack;

            // set the transform as the mesh center
            SceneInformation.GreyMeshesAvailables = true;

            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.ResetGIIBrainSurfaceFile);
            //##################

            yield return SceneInformation.MeshesLoaded;
        }
        /// <summary>
        /// Reset all the sites with a new list of pts files
        /// </summary>
        /// <param name="pathsElectrodesPtsFile"></param>
        /// <returns></returns>
        private IEnumerator c_LoadSites(List<string> pathsElectrodesPtsFile, List<string> patientNames)
        {
            //####### CHECK ACESS
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.ResetElectrodesFile))
            {
                throw new ModeAccessException(m_ModesManager.CurrentModeName);
            }
            //##################

            // load list of pts files
            SceneInformation.SitesLoaded = m_ColumnManager.DLLLoadedPatientsElectrodes.LoadPTSFiles(pathsElectrodesPtsFile, patientNames, ApplicationState.Module3D.MarsAtlasIndex); // TODO (maybe) : replace with values from visualization

            yield return Ninja.JumpToUnity;
            // destroy previous electrodes gameobjects
            for (int ii = 0; ii < m_ColumnManager.SitesList.Count; ++ii)
            {
                //Destroy(go_.PlotsList[ii].GetComponent<MeshFilter>().mesh); // now it's a shared mesh
                Destroy(m_ColumnManager.SitesList[ii]);
            }
            m_ColumnManager.SitesList.Clear();


            // destroy plots elecs/patients parents
            for (int ii = 0; ii < m_ColumnManager.SitesPatientParent.Count; ++ii)
            {
                Destroy(m_ColumnManager.SitesPatientParent[ii]);
                for (int jj = 0; jj < m_ColumnManager.SitesElectrodesParent[ii].Count; ++jj)
                {
                    Destroy(m_ColumnManager.SitesElectrodesParent[ii][jj]);
                }

            }
            m_ColumnManager.SitesPatientParent.Clear();
            m_ColumnManager.SitesElectrodesParent.Clear();

            // TODO : Do this with the visualization data because everything is already read
            if (SceneInformation.SitesLoaded)
            {
                int currPlotNb = 0;
                for (int ii = 0; ii < m_ColumnManager.DLLLoadedPatientsElectrodes.NumberOfPatients; ++ii)
                {
                    int idPlotPatient = 0;
                    string patientName = m_ColumnManager.DLLLoadedPatientsElectrodes.PatientName(ii);
                    
                    // create plot patient parent
                    m_ColumnManager.SitesPatientParent.Add(new GameObject("P" + ii + " - " + patientName));
                    m_ColumnManager.SitesPatientParent[m_ColumnManager.SitesPatientParent.Count - 1].transform.SetParent(m_DisplayedObjects.SitesMeshesParent.transform);
                    m_ColumnManager.SitesPatientParent[m_ColumnManager.SitesPatientParent.Count - 1].transform.localPosition = Vector3.zero;
                    m_ColumnManager.SitesElectrodesParent.Add(new List<GameObject>(m_ColumnManager.DLLLoadedPatientsElectrodes.NumberOfElectrodesInPatient(ii)));

                    for (int jj = 0; jj < m_ColumnManager.DLLLoadedPatientsElectrodes.NumberOfElectrodesInPatient(ii); ++jj)
                    {
                        // create plot electrode parent
                        m_ColumnManager.SitesElectrodesParent[ii].Add(new GameObject(m_ColumnManager.DLLLoadedPatientsElectrodes.ElectrodeName(ii, jj)));
                        m_ColumnManager.SitesElectrodesParent[ii][m_ColumnManager.SitesElectrodesParent[ii].Count - 1].transform.SetParent(m_ColumnManager.SitesPatientParent[ii].transform);
                        m_ColumnManager.SitesElectrodesParent[ii][m_ColumnManager.SitesElectrodesParent[ii].Count - 1].transform.localPosition = Vector3.zero;

                        for (int kk = 0; kk < m_ColumnManager.DLLLoadedPatientsElectrodes.NumberOfSitesInElectrode(ii, jj); ++kk)
                        {
                            Vector3 invertedPosition = m_ColumnManager.DLLLoadedPatientsElectrodes.SitePosition(ii, jj, kk);
                            invertedPosition.x = -invertedPosition.x;

                            GameObject siteGameObject = Instantiate(m_SitePrefab);
                            siteGameObject.name = m_ColumnManager.DLLLoadedPatientsElectrodes.SiteName(ii, jj, kk);

                            siteGameObject.transform.SetParent(m_ColumnManager.SitesElectrodesParent[ii][jj].transform);
                            siteGameObject.transform.localPosition = invertedPosition;
                            siteGameObject.GetComponent<MeshFilter>().sharedMesh = SharedMeshes.Site;

                            siteGameObject.SetActive(true);
                            siteGameObject.layer = LayerMask.NameToLayer("Inactive");

                            Site site = siteGameObject.GetComponent<Site>();
                            site.Information.SitePatientID = idPlotPatient++;
                            site.Information.PatientID = ii;
                            site.Information.ElectrodeID = jj;
                            site.Information.SiteID = kk;
                            site.Information.GlobalID = currPlotNb++;
                            site.Information.IsBlackListed = false;
                            site.Information.IsHighlighted = false;
                            site.Information.IsExcluded = false;
                            site.Information.IsOutOfROI = false;
                            site.Information.IsMarked = false;
                            site.Information.IsMasked = false;
                            site.Information.PatientName = patientName;
                            site.Information.FullName = patientNames[ii] + "_" + siteGameObject.name;
                            site.Information.MarsAtlasIndex = m_ColumnManager.DLLLoadedPatientsElectrodes.MarsAtlasLabelOfSite(ii, jj, kk);
                            site.IsActive = true;

                            m_ColumnManager.SitesList.Add(siteGameObject);
                        }
                    }
                }
            }

            yield return Ninja.JumpBack;
            // reset selected plot
            for (int ii = 0; ii < m_ColumnManager.Columns.Count; ++ii)
            {
                m_ColumnManager.Columns[ii].SelectedSiteID = -1;
            }

            // update latencies
            m_ColumnManager.DLLLoadedRawSitesList = new DLL.RawSiteList();
            m_ColumnManager.DLLLoadedPatientsElectrodes.ExtractRawSiteList(m_ColumnManager.DLLLoadedRawSitesList);


            // FIXME : Something has been commented. See one of the first commits for more information
            // reset latencies
            m_ColumnManager.LatenciesFiles = new List<Latencies>();
            CCEPLabels = new List<string>();
            //for (int ii = 0; ii < Patient.Brain.Connectivities.Count; ++ii)
            {
                Latencies latencies = null;


                if (Patient.Brain.SitesConnectivities == "dummyPath" || Patient.Brain.SitesConnectivities == string.Empty)
                {
                    // generate dummy latencies
                    latencies = m_ColumnManager.DLLLoadedRawSitesList.GenerateDummyLatencies();
                }
                else
                {
                    // load latency file
                    latencies = m_ColumnManager.DLLLoadedRawSitesList.UpdateLatenciesWithFile(Patient.Brain.SitesConnectivities);// Connectivities[ii].Path);
                }

                if (latencies != null)
                {
                    latencies.Name = Patient.Brain.SitesConnectivities; //Connectivities[ii].Label;
                    m_ColumnManager.LatenciesFiles.Add(latencies);
                    CCEPLabels.Add(latencies.Name);
                }

                //latencies = m_CM.DLLLoadedRawPlotsList.updateLatenciesWithFile("C:/Users/Florian/Desktop/amplitudes_latencies/amplitudes_latencies/SIEJO_amplitudes_latencies.txt");

            }

            m_ColumnManager.LatencyFilesDefined = true; //(Patient.Brain.Connectivities.Count > 0);
            OnUpdateLatencies.Invoke(CCEPLabels);

            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.ResetElectrodesFile);
            //##################
        }
        /// <summary>
        /// Define the timeline data with a patient and a list of column data
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="columnDataList"></param>
        private IEnumerator c_SetTimelineData()
        {
            //####### CHECK ACESS
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.SetTimelines))
            {
                throw new ModeAccessException(m_ModesManager.CurrentModeName);
            }
            //##################

            yield return Ninja.JumpToUnity;
            // update columns number
            m_ColumnManager.UpdateColumnsNumber(Visualization.Columns.Count, 0, Cuts.Count);
            yield return Ninja.JumpBack;

            // update columns names
            for (int ii = 0; ii < Visualization.Columns.Count; ++ii)
            {
                m_ColumnManager.ColumnsIEEG[ii].Label = Visualization.Columns[ii].DisplayLabel;
            }

            yield return Ninja.JumpToUnity;
            // set timelines
            m_ColumnManager.SetTimelineData(Patient, Visualization.Columns);

            // update plots visibility
            m_ColumnManager.UpdateAllColumnsSitesRendering(SceneInformation);
            yield return Ninja.JumpBack;

            // set flag
            SceneInformation.TimelinesLoaded = true;

            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.SetTimelines);
            //##################
        }
        #endregion
    }
}