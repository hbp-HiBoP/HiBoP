
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
        /// Visualization corresponding to this scenes
        /// </summary>
        public new Data.Visualization.SinglePatientVisualization Visualization
        {
            get
            {
                return m_Visualization as Data.Visualization.SinglePatientVisualization;
            }
            set
            {
                m_Visualization = value;
            }
        }
        /// <summary>
        /// Patient of this scene
        /// </summary>
        public Data.Patient Patient
        {
            get
            {
                return Visualization.Patient;
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
                case SceneStatesInfo.MeshType.Hemi:
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
                cuts = new List<DLL.Surface>(SceneInformation.MeshToDisplay.Cut(m_Cuts.ToArray(), !SceneInformation.CutHolesEnabled));
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
            for (int ii = 0; ii < m_Cuts.Count; ++ii)
            {
                for (int jj = 0; jj < m_ColumnManager.ColumnsIEEG.Count; ++jj)
                    m_ColumnManager.DLLMRIGeometryCutGeneratorList[ii].Reset(m_ColumnManager.DLLVolume, m_Cuts[ii]);                        

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
            for (int ii = 0; ii < m_Cuts.Count; ++ii)
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
        /// Reset the scene : reload meshes, IRM, plots, and regenerate textures
        /// </summary>
        /// <param name="patient"></param>
        public bool Initialize(Data.Visualization.SinglePatientVisualization visualization, bool postIRM)
        {
            m_ModesManager.UpdateMode(Mode.FunctionsId.ResetScene);

            Visualization = visualization;
            gameObject.name = "SinglePatient Scene (" + GetComponentInParent<ScenesManager>().NumberOfScenesLoadedSinceStart + ")";
            transform.position = new Vector3(SPACE_BETWEEN_SCENES * GetComponentInParent<ScenesManager>().NumberOfScenesLoadedSinceStart, transform.position.y, transform.position.z);

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
                Debug.Log("OnSelectColumnManager");
                IsSelected = cm.IsSelected;
            });

            DLL.Transformation meshTransformation = new DLL.Transformation();
            meshTransformation.Load(Patient.Brain.PreoperativeBasedToScannerBasedTransformation);
            if (postIRM)
            {
                // ...
            }


            // load meshes
            bool success = LoadBrainSurface(meshesFiles, Patient.Brain.PreoperativeBasedToScannerBasedTransformation);

            // load volume
            if (success)
                success = LoadNiftiBrainVolume(Patient.Brain.PreoperativeMRI);

            // load electrodes
            if (success)
                success = LoadSites(ptsFiles, namePatients);

            if (success)
                SceneInformation.CutMeshGeometryNeedsUpdate = true;

            if (!success)
            {
                Debug.LogError("-ERROR : SP3DScene : reset failed. ");
                SceneInformation.Reset();
                m_ColumnManager.Initialize(Cuts.Count);
                ResetSceneGameObjects();
                return false;
            }

            SetTimelineData();
            SelectSite(-1);

            // update scenes cameras
            OnUpdateCameraTarget.Invoke(m_ColumnManager.BothHemi.BoundingBox.Center);

            DisplayScreenMessage("Single Patient Scene loaded : " + visualization.Patient.Place + "_" + visualization.Patient.Name + "_" + visualization.Patient.Date, 2.0f, 400, 80);
            return true;
        }
        /// <summary>
        /// Reset the meshes of the scene with GII files
        /// </summary>
        /// <param name="pathGIIBrainFiles"></param>
        /// <param name="pathTransformFile"></param>
        /// <returns></returns>
        public bool LoadBrainSurface(List<string> pathGIIBrainFiles, string pathTransformFile)
        {
            //####### CHECK ACESS
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.ResetGIIBrainSurfaceFile))
            {
                Debug.LogError("-ERROR : Base3DScene::resetGIIBrainSurfaceFile -> no acess for mode : " + m_ModesManager.CurrentModeName);
                return false;
            }
            //##################

            SceneInformation.MeshesLoaded = false;

            // checks parameters
            foreach (string elem in pathGIIBrainFiles)
                if (elem.Length == 0)
                {
                    Debug.LogError("-ERROR : Base3DScene::resetGIIBrainSurfaceFile -> path gii file is empty. ");
                    return (SceneInformation.MeshesLoaded = false);
                }
            if (pathTransformFile.Length == 0)
            {
                Debug.LogError("-ERROR : Base3DScene::resetGIIBrainSurfaceFile -> path transform file is empty. ");
                return (SceneInformation.MeshesLoaded = false);
            }


            DLL.Transformation transfo = new DLL.Transformation(); // ############################ TEST
            transfo.Load(pathTransformFile);

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
                    leftParcelsLoaded = m_ColumnManager.LWhite.SearchMarsParcelFileAndUpdateColors(GlobalGOPreloaded.MarsAtlasIndex, parcelPath);
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
                    rightParcelsLoaded = m_ColumnManager.RWhite.SearchMarsParcelFileAndUpdateColors(GlobalGOPreloaded.MarsAtlasIndex, parcelPath);
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

                if(rightWhiteLoaded && leftWhiteLoaded)
                {
                    m_ColumnManager.BothWhite = (DLL.Surface)m_ColumnManager.LWhite.Clone();
                    // add right
                    m_ColumnManager.BothWhite.Add(m_ColumnManager.RWhite);
                    SceneInformation.WhiteMeshesAvailables = true;

                    SceneInformation.MarsAtlasParcelsLoaed = leftParcelsLoaded && rightParcelsLoaded;
                }
            }
            else
            {
                SceneInformation.MeshesLoaded = false;
                Debug.LogError("-ERROR : Base3DScene::resetGIIBrainSurfaceFile -> load GII file failed, left : " + leftMeshLoaded + " right : " + rightMeshLoaded);
                return false;
            }

            int maxVerticesNb = m_ColumnManager.BothHemi.NumberOfVertices;
            if (leftWhiteLoaded && rightWhiteLoaded)
                maxVerticesNb = Math.Max(maxVerticesNb, m_ColumnManager.BothWhite.NumberOfVertices);
            int nbSplits = (maxVerticesNb / 65000) + (int)(((maxVerticesNb % 60000) != 0) ? 3 : 2);
            ResetSplitsNumber(nbSplits);
           
            // set the transform as the mesh center
            SceneInformation.HemiMeshesAvailables = true;

            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.ResetGIIBrainSurfaceFile);
            //##################

            return SceneInformation.MeshesLoaded;
        }
        /// <summary>
        /// Reset all the sites with a new list of pts files
        /// </summary>
        /// <param name="pathsElectrodesPtsFile"></param>
        /// <returns></returns>
        public bool LoadSites(List<string> pathsElectrodesPtsFile, List<string> namePatients)
        {
            //####### CHECK ACESS
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.ResetElectrodesFile))
            {
                Debug.LogError("-ERROR : SinglePatient3DScene::resetElectrodesFile -> no acess for mode : " + m_ModesManager.CurrentModeName);
                return false;
            }
            //##################

            // load list of pts files
            SceneInformation.SitesLoaded = m_ColumnManager.DLLLoadedPatientsElectrodes.LoadPTSFiles(pathsElectrodesPtsFile, namePatients, GlobalGOPreloaded.MarsAtlasIndex); // TODO (maybe) : replace with values from visualization

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
                    m_ColumnManager.SitesElectrodesParent.Add(new List<GameObject>(m_ColumnManager.DLLLoadedPatientsElectrodes.NumberOfElectrodesInPatient(ii)));

                    for (int jj = 0; jj < m_ColumnManager.DLLLoadedPatientsElectrodes.NumberOfElectrodesInPatient(ii); ++jj)
                    {
                        // create plot electrode parent
                        m_ColumnManager.SitesElectrodesParent[ii].Add(new GameObject(m_ColumnManager.DLLLoadedPatientsElectrodes.ElectrodeName(ii, jj)));
                        m_ColumnManager.SitesElectrodesParent[ii][m_ColumnManager.SitesElectrodesParent[ii].Count - 1].transform.SetParent(m_ColumnManager.SitesPatientParent[ii].transform);

                        for (int kk = 0; kk < m_ColumnManager.DLLLoadedPatientsElectrodes.NumberOfSitesInElectrode(ii, jj); ++kk)
                        {
                            Vector3 positionInverted = m_ColumnManager.DLLLoadedPatientsElectrodes.SitePosition(ii, jj, kk);
                            positionInverted.x = -positionInverted.x;


                            GameObject siteGO = Instantiate(GlobalGOPreloaded.Plot);
                            siteGO.name = m_ColumnManager.DLLLoadedPatientsElectrodes.SiteName(ii, jj, kk);

                            siteGO.transform.position = positionInverted;// + go_.PlotsParent.transform.position; // TODO : ?
                            siteGO.transform.SetParent(m_ColumnManager.SitesElectrodesParent[ii][jj].transform);
                            siteGO.GetComponent<MeshFilter>().sharedMesh = SharedMeshes.Site;

                            siteGO.SetActive(true);
                            siteGO.layer = LayerMask.NameToLayer("Inactive");

                            Site site = siteGO.GetComponent<Site>();
                            site.Information.SitePatientID = idPlotPatient++;
                            site.Information.PatientID = ii;
                            site.Information.ElectrodeID = jj;
                            site.Information.SiteID = kk;
                            site.Information.GlobalID = currPlotNb++;
                            site.Information.IsBlackListed = false;
                            site.Information.IsHighlighted = false;
                            site.Information.PatientName = patientName;
                            site.Information.FullName = namePatients[ii] + "_" + siteGO.name;

                            // mars atlas
                            site.Information.MarsAtlasIndex =  m_ColumnManager.DLLLoadedPatientsElectrodes.MarsAtlasLabelOfSite(ii,jj,kk);//

                            m_ColumnManager.SitesList.Add(siteGO);
                        }
                    }
                }
            }

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

                
                if(Patient.Brain.SitesConnectivities == "dummyPath" || Patient.Brain.SitesConnectivities == string.Empty)
                {
                    // generate dummy latencies
                    latencies = m_ColumnManager.DLLLoadedRawSitesList.GenerateDummyLatencies();
                }
                else
                {
                    // load latency file
                    latencies = m_ColumnManager.DLLLoadedRawSitesList.UpdateLatenciesWithFile(Patient.Brain.SitesConnectivities);// Connectivities[ii].Path);
                }

                if(latencies != null)
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

            return SceneInformation.SitesLoaded;
        }
        /// <summary>
        /// Define the timeline data with a patient and a list of column data
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="columnDataList"></param>
        public void SetTimelineData()
        {
            //####### CHECK ACESS
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.SetTimelines))
            {
                Debug.LogError("-ERROR : Base3DScene::setTimelines -> no acess for mode : " + m_ModesManager.CurrentModeName);
                return;
            }
            //##################
            
            // update columns number
            m_ColumnManager.UpdateColumnsNumber(Visualization.Columns.Count, 0, Cuts.Count);

            // update columns names
            for (int ii = 0; ii < Visualization.Columns.Count; ++ii)
            {
                m_ColumnManager.ColumnsIEEG[ii].Label = Visualization.Columns[ii].DataLabel;                
            }

            //FIXME
            return;

            // set timelines
            m_ColumnManager.SetTimelineData(Patient, Visualization.Columns);

            // update plots visibility
            m_ColumnManager.UpdateAllColumnsSitesRendering(SceneInformation);

            // set flag
            SceneInformation.TimelinesLoaded = true;

            // send data to UI
            SendIEEGParametersToMenu();
            
            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.SetTimelines);
            //##################
        }
        /// <summary>
        /// Set the current plot to be selected in all the columns
        /// </summary>
        /// <param name="selectedSiteID"></param>
        public void SelectSite(int selectedSiteID)
        {
            OnUpdateLatencies.Invoke(CCEPLabels);

            for (int ii = 0; ii < m_ColumnManager.ColumnsIEEG.Count; ++ii)
            {
                m_ColumnManager.ColumnsIEEG[ii].SelectedSiteID = selectedSiteID;
                OnClickSite.Invoke(ii);
            }
        }
        /// <summary>
        /// Update the columns masks of the scene
        /// </summary>
        /// <param name="blacklistMasks"></param>
        /// <param name="excludedMasks"></param>
        public void SetColumnSiteMask(List<List<bool>> blacklistMasks, List<List<bool>> excludedMasks, List<List<bool>> hightLightedMasks)
        {
            for(int ii = 0; ii < m_ColumnManager.ColumnsIEEG.Count; ++ii)
            {
                for (int jj = 0; jj < m_ColumnManager.SitesList.Count; ++jj)
                {
                    m_ColumnManager.ColumnsIEEG[ii].Sites[jj].Information.IsBlackListed = blacklistMasks[ii][jj];
                    m_ColumnManager.ColumnsIEEG[ii].Sites[jj].Information.IsExcluded = excludedMasks[ii][jj];
                    m_ColumnManager.ColumnsIEEG[ii].Sites[jj].Information.IsHighlighted = hightLightedMasks[ii][jj];
                }
            }

            m_ColumnManager.UpdateAllColumnsSitesRendering(SceneInformation);
        }
        /// <summary>
        /// Manage the clicks event in the scene
        /// </summary>
        /// <param name="ray"></param>
        public override void ClickOnScene(Ray ray)
        {
            // scene not loaded
            if (!SceneInformation.MRILoaded)
                return;

            // update colliders if necessary (SLOW)
            if (!SceneInformation.CollidersUpdated)
                UpdateMeshesColliders();

            // retrieve layer
            int layerMask = 0;
            layerMask |= 1 << LayerMask.NameToLayer(m_ColumnManager.SelectedColumn.Layer);
            layerMask |= 1 << LayerMask.NameToLayer("Meshes_SP");

            // collision with all colliders
            RaycastHit hit;
            bool isCollision = Physics.Raycast(ray.origin, ray.direction, out hit, Mathf.Infinity, layerMask);
            if (!isCollision) // no hit
                return;

            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Meshes_SP")) // mesh hit
            {
                if (hit.collider.gameObject.name.StartsWith("cut")) // cut hit
                    return;

                if (m_TriEraser.IsEnabled && m_TriEraser.IsClickAvailable)
                {
                    //Debug.DrawRay(ray.origin, hit.point, Color.red, 2f, false);
                    m_TriEraser.EraseTriangles(ray.direction, hit.point);

                    for (int ii = 0; ii < m_ColumnManager.DLLSplittedMeshesList.Count; ++ii)
                    {
                        //if (go_.brainSurfaceMeshes[ii].name == hit.collider.gameObject.name)
                        {
                            m_ColumnManager.DLLSplittedMeshesList[ii].UpdateMeshFromDLL(m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);
                            //break;
                        }
                    }
                }

                return;
            }

            // plot hit            
            int clickedPlotID = hit.collider.gameObject.GetComponent<Site>().Information.GlobalID;
            Column3D currColumn = m_ColumnManager.SelectedColumn;
            currColumn.SelectedSiteID = clickedPlotID;
            switch (m_ColumnManager.SelectedColumn.Type)
            {
                case Column3D.ColumnType.FMRI:
                    break;
                case Column3D.ColumnType.IEEG:
                    Column3DIEEG dataC = (Column3DIEEG)currColumn;

                    m_ColumnManager.LatencyFileAvailable = (dataC.CurrentLatencyFile != -1);
                    dataC.SourceDefined = false;
                    dataC.IsSiteASource = false;
                    dataC.SiteLatencyData = false;

                    if (m_ColumnManager.LatencyFileAvailable)
                    {
                        dataC.SourceDefined = (dataC.SourceSelectedID != -1);
                        if (m_ColumnManager.LatenciesFiles[dataC.CurrentLatencyFile].IsSiteASource(dataC.SelectedSiteID))
                        {
                            dataC.IsSiteASource = true;
                        }

                        if (dataC.SourceDefined)
                        {
                            dataC.SiteLatencyData = m_ColumnManager.LatenciesFiles[dataC.CurrentLatencyFile].IsSiteResponsiveForSource(dataC.SelectedSiteID, dataC.SourceSelectedID);
                        }
                    }
                    break;
                default:
                    break;
            }

            OnClickSite.Invoke(-1);            
            m_ColumnManager.UpdateAllColumnsSitesRendering(SceneInformation);            
        }
        /// <summary>
        /// Manage the mouse movments event in the scene
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="mousePosition"></param>
        /// /// <param name="idColumn"></param>
        public override void MoveMouseOnScene(Ray ray, Vector3 mousePosition, int idColumn)
        {
            // scene not loaded
            if (!SceneInformation.MRILoaded)
                return;

            // current column is different : we display only for the selected column
            if (m_ColumnManager.SelectedColumnID != idColumn)
                return;

            // retrieve layer
            int layerMask = 0;
            layerMask |= 1 << LayerMask.NameToLayer(m_ColumnManager.SelectedColumn.Layer);
            layerMask |= 1 << LayerMask.NameToLayer("Meshes_SP");

            RaycastHit hit;
            bool isCollision = Physics.Raycast(ray.origin, ray.direction, out hit, Mathf.Infinity, layerMask);
            if (!isCollision)
            {
                OnUpdateDisplayedSitesInfo.Invoke(new SiteInfo(null, false, mousePosition, false));
                return;
            }
            if (hit.collider.transform.parent.name == "Cuts" || hit.collider.transform.parent.name == "Brains")
            {
                OnUpdateDisplayedSitesInfo.Invoke(new SiteInfo(null, false, mousePosition, false));
                return;
            }

            Site site = hit.collider.GetComponent<Site>();

            switch (m_ColumnManager.SelectedColumn.Type)
            {
                case Column3D.ColumnType.FMRI:
                    OnUpdateDisplayedSitesInfo.Invoke(new SiteInfo(site, true, mousePosition, true, false, hit.collider.GetComponent<Site>().Information.FullName));
                    return;
                case Column3D.ColumnType.IEEG:
                    Column3DIEEG currIEEGCol = (Column3DIEEG)m_ColumnManager.SelectedColumn;
                    int idPlot = site.Information.SitePatientID;

                    // retrieve currant plot amp
                    float amp = 0;
                    if (currIEEGCol.IEEGValuesBySiteID.Length > 0)
                    {
                        amp = currIEEGCol.IEEGValuesBySiteID[idPlot][currIEEGCol.CurrentTimeLineID];
                    }

                    // retrieve current lateny/height
                    string latency = "none", height = "none";
                    if (currIEEGCol.CurrentLatencyFile != -1)
                    {
                        Latencies latencyFile = m_ColumnManager.LatenciesFiles[currIEEGCol.CurrentLatencyFile];

                        if (currIEEGCol.SourceSelectedID == -1) // no source selected
                        {
                            latency = "...";
                            height = "no source selected";
                        }
                        else if (currIEEGCol.SourceSelectedID == idPlot) // plot is the source
                        {
                            latency = "0";
                            height = "source";
                        }
                        else
                        {
                            if (latencyFile.IsSiteResponsiveForSource(idPlot, currIEEGCol.SourceSelectedID)) // data available
                            {
                                latency = "" + latencyFile.LatenciesValues[currIEEGCol.SourceSelectedID][idPlot];
                                height = "" + latencyFile.Heights[currIEEGCol.SourceSelectedID][idPlot];
                            }
                            else
                            {
                                latency = "No data";
                                height = "No data";
                            }
                        }
                    }

                    OnUpdateDisplayedSitesInfo.Invoke(new SiteInfo(site, true, mousePosition, m_ColumnManager.SelectedColumn.Type == Column3D.ColumnType.FMRI, SceneInformation.DisplayCCEPMode,
                        hit.collider.GetComponent<Site>().Information.FullName, "" + amp, height, latency));
                    break;
                default:
                    break;
            }

            
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idColumn"></param>
        public override void DisableSiteDisplayWindow(int idColumn)
        {
            OnUpdateDisplayedSitesInfo.Invoke(new SiteInfo(null, false, new Vector3(0, 0, 0), false));
        }
        /// <summary>
        /// Update the display mode of the scene
        /// </summary>
        /// <param name="isCeepMode"></param>
        public void SetCCEPDisplayMode(bool isCeepMode)
        {
            DisplayScreenMessage(isCeepMode ? "CCEP mode enabled" : "iEEG mode enabled", 1.5f, 150, 30);

            SceneInformation.DisplayCCEPMode = isCeepMode;
            //UpdateCutsInUI.Invoke(m_CM.getBrainCutTextureList(m_CM.idSelectedColumn, true, data_.generatorUpToDate, data_.displayLatenciesMode), m_CM.idSelectedColumn, m_CM.planesList.Count);

            m_ColumnManager.UpdateAllColumnsSitesRendering(SceneInformation);

            // force mode to update UI
            m_ModesManager.SetCurrentModeSpecifications(true);
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

                        OnRequestSiteInformation.Invoke(request);
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
    }
}