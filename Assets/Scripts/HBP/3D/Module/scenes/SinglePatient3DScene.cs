
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
    namespace Events
    {
        /// <summary>
        /// Event for asking the UI to update the latencies display on the plot menu (params : labels)
        /// </summary>
        public class UpdateLatencies : UnityEvent<List<string>> { }
    }



    /// <summary>
    /// The single patient scene class, inheritance of Base3DScene.
    /// </summary>
    [AddComponentMenu("Scenes/Single Patient 3D Scene")]
    public class SinglePatient3DScene : Base3DScene
    {
        #region Properties
        public override SceneType Type
        {
            get
            {
                return SceneType.SinglePatient;
            }
        }
        public AmbientMode m_ambiantMode = AmbientMode.Flat;
        public float m_ambientIntensity = 1;
        public Color m_ambiantLight = new Color(0.2f, 0.2f, 0.2f, 1);

        
        public new Data.Visualisation.SinglePatientVisualisation Visualisation
        {
            get
            {
                return Visualisation as Data.Visualisation.SinglePatientVisualisation;
            }
            set
            {
                Visualisation = value;
            }
        }
        public Data.Patient Patient
        {
            get
            {
                return Visualisation.Patient;
            }
        }
        // events
        private Events.UpdateLatencies m_updateLatencies = new Events.UpdateLatencies();
        public Events.UpdateLatencies UpdateLatencies { get { return m_updateLatencies; } }

        private List<string> CCEPLabels = null;

        #endregion members

        #region Public Methods
        /// <summary>
        /// Update the meshes cuts
        /// </summary>
        public override void ComputeMeshesCut()
        {
            UnityEngine.Profiling.Profiler.BeginSample("TEST-SP3DScene-Update compute_meshes_cuts 0 cutSurface"); // 40%

            // choose the mesh
            data_.meshToDisplay = null;
            switch (data_.meshTypeToDisplay)
            {
                case SceneStatesInfo.MeshType.Hemi:
                    switch (data_.meshPartToDisplay)
                    {
                        case SceneStatesInfo.MeshPart.Both:
                            data_.meshToDisplay = (DLL.Surface)m_Column3DViewManager.BothHemi;
                            break;
                        case SceneStatesInfo.MeshPart.Left:
                            data_.meshToDisplay = (DLL.Surface)m_Column3DViewManager.LHemi;
                            break;
                        case SceneStatesInfo.MeshPart.Right:
                            data_.meshToDisplay = (DLL.Surface)m_Column3DViewManager.RHemi;
                            break;
                    }
                    break;
                case SceneStatesInfo.MeshType.White:
                    switch (data_.meshPartToDisplay)
                    {
                        case SceneStatesInfo.MeshPart.Both:
                            data_.meshToDisplay = (DLL.Surface)m_Column3DViewManager.BothWhite;
                            break;
                        case SceneStatesInfo.MeshPart.Left:
                            data_.meshToDisplay = (DLL.Surface)m_Column3DViewManager.LWhite;
                            break;
                        case SceneStatesInfo.MeshPart.Right:
                            data_.meshToDisplay = (DLL.Surface)m_Column3DViewManager.RWhite;
                            break;
                    }
                    break;
                case SceneStatesInfo.MeshType.Inflated:
                    // ...
                    break;
            }

            // get the middle
            data_.meshCenter = data_.meshToDisplay.bounding_box().center();

            // cut the mesh
            List<DLL.Surface> cuts;
            if (PlanesList.Count > 0)
                cuts = new List<DLL.Surface>(data_.meshToDisplay.cut(m_Column3DViewManager.planesCutsCopy.ToArray(), data_.removeFrontPlaneList.ToArray(), !data_.holesEnabled));
            else
                cuts = new List<DLL.Surface>() { (DLL.Surface)data_.meshToDisplay.Clone() };

            if (m_Column3DViewManager.DLLCutsList.Count != cuts.Count)
                m_Column3DViewManager.DLLCutsList = cuts;
            else
            {
                // swap DLL pointer
                for (int ii = 0; ii < cuts.Count; ++ii)
                    m_Column3DViewManager.DLLCutsList[ii].swap_DLL_handle(cuts[ii]);
            }

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-SP3DScene-Update compute_meshes_cuts 1 splitToSurfaces"); // 2%

            // split the cut mesh         
            m_Column3DViewManager.DLLSplittedMeshesList = new List<DLL.Surface>(m_Column3DViewManager.DLLCutsList[0].split_to_surfaces(m_Column3DViewManager.meshSplitNb));

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-SP3DScene-Update compute_meshes_cuts 2 reset brain texture generator"); // 11%

            // reset brain texture generator
            for (int ii = 0; ii < m_Column3DViewManager.meshSplitNb; ++ii)
            {
                m_Column3DViewManager.DLLCommonBrainTextureGeneratorList[ii].reset(m_Column3DViewManager.DLLSplittedMeshesList[ii], m_Column3DViewManager.DLLVolume);
                m_Column3DViewManager.DLLCommonBrainTextureGeneratorList[ii].compute_UVMain_with_volume(m_Column3DViewManager.DLLSplittedMeshesList[ii], m_Column3DViewManager.DLLVolume, m_Column3DViewManager.MRICalMinFactor, m_Column3DViewManager.MRICalMaxFactor);
            }

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-SP3DScene-Update compute_meshes_cuts 3 update cut brain mesh object mesh filter"); // 6%

            ResetTriangleErasing(false);

            // update brain mesh object mesh filter
            for (int ii = 0; ii < m_Column3DViewManager.meshSplitNb; ++ii)
                m_Column3DViewManager.DLLSplittedMeshesList[ii].update_mesh_from_dll(go_.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-SP3DScene-Update compute_meshes_cuts 4 update cuts generators"); // 17%
            

            // update cuts generators
            for (int ii = 0; ii < m_Column3DViewManager.planesCutsCopy.Count; ++ii)
            {
                for (int jj = 0; jj < m_Column3DViewManager.ColumnsIEEG.Count; ++jj)
                    m_Column3DViewManager.DLLMRIGeometryCutGeneratorList[ii].reset(m_Column3DViewManager.DLLVolume, m_Column3DViewManager.planesCutsCopy[ii]);                        

                m_Column3DViewManager.DLLMRIGeometryCutGeneratorList[ii].update_cut_mesh_UV(Column3DViewManager.DLLCutsList[ii + 1]);
                m_Column3DViewManager.DLLCutsList[ii + 1].update_mesh_from_dll(go_.brainCutMeshes[ii].GetComponent<MeshFilter>().mesh);
            }

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-SP3DScene-Update compute_meshes_cuts 5 create null uv2/uv3 arrays"); // 0%
            
            // create null uv2/uv3 arrays
            m_Column3DViewManager.uvNull = new List<Vector2[]>(m_Column3DViewManager.meshSplitNb);
            for (int ii = 0; ii < m_Column3DViewManager.meshSplitNb; ++ii)
            {
                m_Column3DViewManager.uvNull.Add(new Vector2[go_.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh.vertexCount]);
                Extensions.ArrayExtensions.Fill(m_Column3DViewManager.uvNull[ii], new Vector2(0.01f, 1f));
            }

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-SP3DScene-Update compute_meshes_cuts 6 end"); // 0%
            

            // enable cuts gameobject
            for (int ii = 0; ii < m_Column3DViewManager.planesCutsCopy.Count; ++ii)
                    go_.brainCutMeshes[ii].SetActive(true); 

            data_.collidersUpdated = false; // colliders are now longer up to date
            data_.updateCutMeshGeometry = false;   // planes are now longer requested to be updated 
            data_.generatorUpToDate = false; // generator is not up to date anymore

            // update amplitude for all columns
            for (int ii = 0; ii < m_Column3DViewManager.ColumnsIEEG.Count; ++ii)
                m_Column3DViewManager.ColumnsIEEG[ii].updateIEEG = true;

            UnityEngine.Profiling.Profiler.EndSample();
        }
        /// <summary>
        /// Reset the scene : reload meshes, IRM, plots, and regenerate textures
        /// </summary>
        /// <param name="patient"></param>
        public bool Initialize(Data.Visualisation.SinglePatientVisualisation visualisation, bool postIRM)
        {
            m_ModesManager.updateMode(Mode.FunctionsId.resetScene);

            Visualisation = visualisation;

            List<string> ptsFiles = new List<string>(), namePatients = new List<string>();
            ptsFiles.Add(Patient.Brain.PatientReferenceFrameImplantation); //.PatientBasedImplantation);
            namePatients.Add(Patient.Place + "_" + Patient.Date + "_" + Patient.Name);

            List<string> meshesFiles = new List<string>();
            meshesFiles.Add(Patient.Brain.LeftCerebralHemisphereMesh);
            meshesFiles.Add(Patient.Brain.RightCerebralHemisphereMesh);

            // reset columns
            m_Column3DViewManager.Initialize(PlanesList.Count);

            DLL.Transformation meshTransformation = new DLL.Transformation();
            meshTransformation.load(Patient.Brain.PreOperationReferenceFrameToScannerReferenceFrameTransformation);
            if (postIRM)
            {
                // ...
            }


            // load meshes
            bool success = ResetBrainSurface(meshesFiles, Patient.Brain.PreOperationReferenceFrameToScannerReferenceFrameTransformation);

            // load volume
            if (success)
                success = ResetNiftiBrainVolume(Patient.Brain.PreOperationMRI);

            // load electrodes
            if (success)
                success = ResetElectrodes(ptsFiles, namePatients);

            if (success)
                data_.updateCutMeshGeometry = true;

            if (!success)
            {
                Debug.LogError("-ERROR : SP3DScene : reset failed. ");
                data_.reset();
                m_Column3DViewManager.Initialize(PlanesList.Count);
                ResetSceneGameObjects();
                return false;
            }
            
            SetTimelineData();
            SelectSite(-1);
            UpdateSelectedColumn(0);

            DisplayScreenMessage("Single Patient Scene loaded : " + visualisation.Patient.Place + "_" + visualisation.Patient.Name + "_" + visualisation.Patient.Date, 2.0f, 400, 80);
            return true;
        }
        /// <summary>
        /// Reset the meshes of the scene with GII files
        /// </summary>
        /// <param name="pathGIIBrainFiles"></param>
        /// <param name="pathTransformFile"></param>
        /// <returns></returns>
        public bool ResetBrainSurface(List<string> pathGIIBrainFiles, string pathTransformFile)
        {
            //####### CHECK ACESS
            if (!m_ModesManager.functionAccess(Mode.FunctionsId.resetGIIBrainSurfaceFile))
            {
                Debug.LogError("-ERROR : Base3DScene::resetGIIBrainSurfaceFile -> no acess for mode : " + m_ModesManager.currentModeName());
                return false;
            }
            //##################

            data_.meshesLoaded = false;

            // checks parameters
            foreach (string elem in pathGIIBrainFiles)
                if (elem.Length == 0)
                {
                    Debug.LogError("-ERROR : Base3DScene::resetGIIBrainSurfaceFile -> path gii file is empty. ");
                    return (data_.meshesLoaded = false);
                }
            if (pathTransformFile.Length == 0)
            {
                Debug.LogError("-ERROR : Base3DScene::resetGIIBrainSurfaceFile -> path transform file is empty. ");
                return (data_.meshesLoaded = false);
            }


            DLL.Transformation transfo = new DLL.Transformation(); // ############################ TEST
            transfo.load(pathTransformFile);

            // load left hemi
            bool leftMeshLoaded = m_Column3DViewManager.LHemi.load_GII_file(pathGIIBrainFiles[0], true, pathTransformFile);
            bool leftWhiteLoaded = false, rightWhiteLoaded = false;
            bool leftParcelsLoaded = false, rightParcelsLoaded = false;
            if (leftMeshLoaded)
            {
                m_Column3DViewManager.LHemi.flip_triangles();  // the transformation inverses one axis, so faces of the surface must be flipped (TODO : case with no transformation or no inverse axi transfo)
                m_Column3DViewManager.LHemi.compute_normals();

                string leftWhitePath = pathGIIBrainFiles[0].Replace("_Lhemi", "_Lwhite");
                leftWhiteLoaded = m_Column3DViewManager.LWhite.load_GII_file(leftWhitePath, true, pathTransformFile);
                if (leftWhiteLoaded)
                {
                    m_Column3DViewManager.LWhite.flip_triangles();
                    m_Column3DViewManager.LWhite.compute_normals();

                    string[] split = leftWhitePath.Split('\\');
                    string parcelPath = leftWhitePath.Substring(0, leftWhitePath.LastIndexOf('\\')) + "\\surface_analysis\\" + split[split.Length - 1].Replace(".gii", "") + "_parcels_marsAtlas.gii";
                    leftParcelsLoaded = m_Column3DViewManager.LWhite.seach_mars_parcel_file_and_update_colors(GlobalGOPreloaded.MarsAtlasIndex, parcelPath);
                }                
            }


            // load right hemi
            bool rightMeshLoaded = m_Column3DViewManager.RHemi.load_GII_file(pathGIIBrainFiles[1], true, pathTransformFile);
            if (rightMeshLoaded)
            {
                m_Column3DViewManager.RHemi.flip_triangles();  // the transformation inverses one axis, so faces of the surface must be flipped (TODO : case with no transformation or no inverse axi transfo)
                m_Column3DViewManager.RHemi.compute_normals();

                string rightWhitePath = pathGIIBrainFiles[1].Replace("_Rhemi", "_Rwhite");
                rightWhiteLoaded = m_Column3DViewManager.RWhite.load_GII_file(rightWhitePath, true, pathTransformFile);
                if (rightWhiteLoaded)
                {
                    m_Column3DViewManager.RWhite.flip_triangles();
                    m_Column3DViewManager.RWhite.compute_normals();

                    string[] split = rightWhitePath.Split('\\');
                    string parcelPath = rightWhitePath.Substring(0, rightWhitePath.LastIndexOf('\\')) + "\\surface_analysis\\" + split[split.Length - 1].Replace(".gii", "") + "_parcels_marsAtlas.gii";
                    rightParcelsLoaded = m_Column3DViewManager.RWhite.seach_mars_parcel_file_and_update_colors(GlobalGOPreloaded.MarsAtlasIndex, parcelPath);
                }
            }

            // fusion
            if (leftMeshLoaded && rightMeshLoaded)
            {
                // copy left
                m_Column3DViewManager.BothHemi = (DLL.Surface)m_Column3DViewManager.LHemi.Clone();

                // add right
                m_Column3DViewManager.BothHemi.add(m_Column3DViewManager.RHemi);
                data_.meshesLoaded = true;

                // get the middle
                data_.meshCenter = m_Column3DViewManager.BothHemi.bounding_box().center();

                if(rightWhiteLoaded && leftWhiteLoaded)
                {
                    m_Column3DViewManager.BothWhite = (DLL.Surface)m_Column3DViewManager.LWhite.Clone();
                    // add right
                    m_Column3DViewManager.BothWhite.add(m_Column3DViewManager.RWhite);
                    data_.whiteMeshesAvailables = true;

                    data_.marsAtlasParcelsLoaed = leftParcelsLoaded && rightParcelsLoaded;
                }
            }
            else
            {
                data_.meshesLoaded = false;
                Debug.LogError("-ERROR : Base3DScene::resetGIIBrainSurfaceFile -> load GII file failed, left : " + leftMeshLoaded + " right : " + rightMeshLoaded);
                return false;
            }

            int maxVerticesNb = m_Column3DViewManager.BothHemi.vertices_nb();
            if (leftWhiteLoaded && rightWhiteLoaded)
                maxVerticesNb = Math.Max(maxVerticesNb, m_Column3DViewManager.BothWhite.vertices_nb());
            int nbSplits = (maxVerticesNb / 65000) + (int)(((maxVerticesNb % 60000) != 0) ? 3 : 2);
            ResetSplitsNumber(nbSplits);
            
            // update scenes cameras
            UpdateCameraTarget.Invoke(m_Column3DViewManager.BothHemi.bounding_box().center());
           
            // set the transform as the mesh center
            data_.hemiMeshesAvailables = true;

            //####### UDPATE MODE
            m_ModesManager.updateMode(Mode.FunctionsId.resetGIIBrainSurfaceFile);
            //##################

            return data_.meshesLoaded;
        }
        /// <summary>
        /// Reset all the sites with a new list of pts files
        /// </summary>
        /// <param name="pathsElectrodesPtsFile"></param>
        /// <returns></returns>
        public bool ResetElectrodes(List<string> pathsElectrodesPtsFile, List<string> namePatients)
        {
            //####### CHECK ACESS
            if (!m_ModesManager.functionAccess(Mode.FunctionsId.resetElectrodesFile))
            {
                Debug.LogError("-ERROR : SinglePatient3DScene::resetElectrodesFile -> no acess for mode : " + m_ModesManager.currentModeName());
                return false;
            }
            //##################

            // load list of pts files
            data_.sitesLoaded = m_Column3DViewManager.DLLLoadedPatientsElectrodes.load_pts_files(pathsElectrodesPtsFile, namePatients, GlobalGOPreloaded.MarsAtlasIndex); // TODO (maybe) : replace with values from visualisation

            // destroy previous electrodes gameobjects
            for (int ii = 0; ii < m_Column3DViewManager.SitesList.Count; ++ii)
            {
                //Destroy(go_.PlotsList[ii].GetComponent<MeshFilter>().mesh); // now it's a shared mesh
                Destroy(m_Column3DViewManager.SitesList[ii]);
            }
            m_Column3DViewManager.SitesList.Clear();


            // destroy plots elecs/patients parents
            for (int ii = 0; ii < m_Column3DViewManager.PlotsPatientParent.Count; ++ii)
            {
                Destroy(m_Column3DViewManager.PlotsPatientParent[ii]);
                for (int jj = 0; jj < m_Column3DViewManager.PlotsElectrodesParent[ii].Count; ++jj)
                {
                    Destroy(m_Column3DViewManager.PlotsElectrodesParent[ii][jj]);
                }

            }
            m_Column3DViewManager.PlotsPatientParent.Clear();
            m_Column3DViewManager.PlotsElectrodesParent.Clear();


            if (data_.sitesLoaded)
            {
                int currPlotNb = 0;
                for (int ii = 0; ii < m_Column3DViewManager.DLLLoadedPatientsElectrodes.patients_nb(); ++ii)
                {
                    int idPlotPatient = 0;
                    string patientName = m_Column3DViewManager.DLLLoadedPatientsElectrodes.patient_name(ii);

                    // create plot patient parent
                    m_Column3DViewManager.PlotsPatientParent.Add(new GameObject("P" + ii + " - " + patientName));
                    m_Column3DViewManager.PlotsPatientParent[m_Column3DViewManager.PlotsPatientParent.Count - 1].transform.SetParent(go_.sitesMeshesParent.transform);
                    m_Column3DViewManager.PlotsElectrodesParent.Add(new List<GameObject>(m_Column3DViewManager.DLLLoadedPatientsElectrodes.electrodes_nb(ii)));

                    for (int jj = 0; jj < m_Column3DViewManager.DLLLoadedPatientsElectrodes.electrodes_nb(ii); ++jj)
                    {
                        // create plot electrode parent
                        m_Column3DViewManager.PlotsElectrodesParent[ii].Add(new GameObject(m_Column3DViewManager.DLLLoadedPatientsElectrodes.electrode_name(ii, jj)));
                        m_Column3DViewManager.PlotsElectrodesParent[ii][m_Column3DViewManager.PlotsElectrodesParent[ii].Count - 1].transform.SetParent(m_Column3DViewManager.PlotsPatientParent[ii].transform);

                        for (int kk = 0; kk < m_Column3DViewManager.DLLLoadedPatientsElectrodes.electrode_sites_nb(ii, jj); ++kk)
                        {
                            Vector3 positionInverted = m_Column3DViewManager.DLLLoadedPatientsElectrodes.site_pos(ii, jj, kk);
                            positionInverted.x = -positionInverted.x;


                            GameObject siteGO = Instantiate(GlobalGOPreloaded.Plot);
                            siteGO.name = m_Column3DViewManager.DLLLoadedPatientsElectrodes.site_name(ii, jj, kk);

                            siteGO.transform.position = positionInverted;// + go_.PlotsParent.transform.position; // TODO : ?
                            siteGO.transform.SetParent(m_Column3DViewManager.PlotsElectrodesParent[ii][jj].transform);
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
                            site.Information.MarsAtlasIndex =  m_Column3DViewManager.DLLLoadedPatientsElectrodes.site_mars_atlas_label(ii,jj,kk);//

                            m_Column3DViewManager.SitesList.Add(siteGO);
                        }
                    }
                }
            }

            // reset selected plot
            for (int ii = 0; ii < m_Column3DViewManager.Columns.Count; ++ii)
            {
                m_Column3DViewManager.Columns[ii].SelectedSiteID = -1;
            }

            // update latencies
            m_Column3DViewManager.DLLLoadedRawPlotsList = new DLL.RawSiteList();
            m_Column3DViewManager.DLLLoadedPatientsElectrodes.extract_raw_site_list(m_Column3DViewManager.DLLLoadedRawPlotsList);


            ///////////////////////// TO DOOOOO : FixMe /////////////////////////////////////
            // reset latencies
            m_Column3DViewManager.latenciesFiles = new List<Latencies>();
            CCEPLabels = new List<string>();
            //for (int ii = 0; ii < Patient.Brain.Connectivities.Count; ++ii)
            {
                Latencies latencies = null;

                
                if(Patient.Brain.PlotsConnectivity == "dummyPath" || Patient.Brain.PlotsConnectivity == string.Empty)
                {
                    // generate dummy latencies
                    latencies = m_Column3DViewManager.DLLLoadedRawPlotsList.generate_dummy_latencies();
                }
                else
                {
                    // load latency file
                    latencies = m_Column3DViewManager.DLLLoadedRawPlotsList.update_latencies_with_file(Patient.Brain.PlotsConnectivity);// Connectivities[ii].Path);
                }

                if(latencies != null)
                {
                    latencies.name = Patient.Brain.PlotsConnectivity; //Connectivities[ii].Label;
                    m_Column3DViewManager.latenciesFiles.Add(latencies);
                    CCEPLabels.Add(latencies.name);
                }

                //latencies = m_CM.DLLLoadedRawPlotsList.updateLatenciesWithFile("C:/Users/Florian/Desktop/amplitudes_latencies/amplitudes_latencies/SIEJO_amplitudes_latencies.txt");

            }

            m_Column3DViewManager.latencyFilesDefined = true; //(Patient.Brain.Connectivities.Count > 0);
            m_updateLatencies.Invoke(CCEPLabels);

            //////////////////////////////////////////////////////////////////////////////////////////////

            //####### UDPATE MODE
            m_ModesManager.updateMode(Mode.FunctionsId.resetElectrodesFile);
            //##################

            return data_.sitesLoaded;
        }
        /// <summary>
        /// Define the timeline data with a patient and a list of column data
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="columnDataList"></param>
        public void SetTimelineData()
        {
            //####### CHECK ACESS
            if (!m_ModesManager.functionAccess(Mode.FunctionsId.setTimelines))
            {
                Debug.LogError("-ERROR : Base3DScene::setTimelines -> no acess for mode : " + m_ModesManager.currentModeName());
                return;
            }
            //##################
            
            // update columns number
            m_Column3DViewManager.update_columns_nb(Visualisation.Columns.Count, 0, PlanesList.Count);

            // update columns names
            for (int ii = 0; ii < Visualisation.Columns.Count; ++ii)
            {
                m_Column3DViewManager.ColumnsIEEG[ii].Label = Visualisation.Columns[ii].Label;                
            }

            // set timelines
            m_Column3DViewManager.SetTimelineData(Patient, Visualisation.Columns);

            // update plots visibility
            m_Column3DViewManager.update_all_columns_sites_rendering(data_);

            // set flag
            data_.timelinesLoaded = true;

            // send data to UI
            SendIEEGParametersToMenu();
            
            //####### UDPATE MODE
            m_ModesManager.updateMode(Mode.FunctionsId.setTimelines);
            //##################
        }
        /// <summary>
        /// Set the current plot to be selected in all the columns
        /// </summary>
        /// <param name="idSelectedPlot"></param>
        public void SelectSite(int idSelectedPlot)
        {
            m_updateLatencies.Invoke(CCEPLabels);

            for (int ii = 0; ii < m_Column3DViewManager.ColumnsIEEG.Count; ++ii)
            {
                m_Column3DViewManager.ColumnsIEEG[ii].SelectedSiteID = idSelectedPlot;
                ClickSite.Invoke(ii);
            }
        }
        /// <summary>
        /// Update the columns masks of the scene
        /// </summary>
        /// <param name="blacklistMasks"></param>
        /// <param name="excludedMasks"></param>
        public void SetColumnSiteMask(List<List<bool>> blacklistMasks, List<List<bool>> excludedMasks, List<List<bool>> hightLightedMasks)
        {
            for(int ii = 0; ii < m_Column3DViewManager.ColumnsIEEG.Count; ++ii)
            {
                for (int jj = 0; jj < m_Column3DViewManager.SitesList.Count; ++jj)
                {
                    m_Column3DViewManager.ColumnsIEEG[ii].Sites[jj].Information.IsBlackListed = blacklistMasks[ii][jj];
                    m_Column3DViewManager.ColumnsIEEG[ii].Sites[jj].Information.IsExcluded = excludedMasks[ii][jj];
                    m_Column3DViewManager.ColumnsIEEG[ii].Sites[jj].Information.IsHighlighted = hightLightedMasks[ii][jj];
                }
            }

            m_Column3DViewManager.update_all_columns_sites_rendering(data_);
        }
        /// <summary>
        /// Reset the rendering settings for this scene, called by each MP camera before rendering
        /// </summary>
        /// <param name="cameraRotation"></param>
        public override void ResetRenderingSettings(Vector3 cameraRotation)
        {
            RenderSettings.ambientMode = m_ambiantMode;
            RenderSettings.ambientIntensity = m_ambientIntensity;
            RenderSettings.skybox = null;
            RenderSettings.ambientLight = m_ambiantLight;
            go_.sharedDirLight.GetComponent<Transform>().eulerAngles = cameraRotation;
        }
        /// <summary>
        /// Manage the clicks event in the scene
        /// </summary>
        /// <param name="ray"></param>
        public override void ClickOnScene(Ray ray)
        {
            // scene not loaded
            if (!data_.mriLoaded)
                return;

            // update colliders if necessary (SLOW)
            if (!data_.collidersUpdated)
                UpdateMeshesColliders();

            // retrieve layer
            int layerMask = 0;
            layerMask |= 1 << LayerMask.NameToLayer(m_Column3DViewManager.SelectedColumn.Layer);
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

                if (m_triEraser.is_enabled() && m_triEraser.is_click_available())
                {
                    //Debug.DrawRay(ray.origin, hit.point, Color.red, 2f, false);
                    m_triEraser.erase_triangles(ray.direction, hit.point);

                    for (int ii = 0; ii < m_Column3DViewManager.DLLSplittedMeshesList.Count; ++ii)
                    {
                        //if (go_.brainSurfaceMeshes[ii].name == hit.collider.gameObject.name)
                        {
                            m_Column3DViewManager.DLLSplittedMeshesList[ii].update_mesh_from_dll(go_.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);
                            //break;
                        }
                    }
                }

                return;
            }

            // plot hit            
            int clickedPlotID = hit.collider.gameObject.GetComponent<Site>().Information.GlobalID;
            Column3DView currColumn = m_Column3DViewManager.SelectedColumn;
            currColumn.SelectedSiteID = clickedPlotID;
            switch (m_Column3DViewManager.SelectedColumn.Type)
            {
                case Column3DView.ColumnType.FMRI:
                    break;
                case Column3DView.ColumnType.IEEG:
                    Column3DViewIEEG dataC = (Column3DViewIEEG)currColumn;

                    m_Column3DViewManager.latencyFileAvailable = (dataC.currentLatencyFile != -1);
                    dataC.sourceDefined = false;
                    dataC.siteIsSource = false;
                    dataC.siteLatencyData = false;

                    if (m_Column3DViewManager.latencyFileAvailable)
                    {
                        dataC.sourceDefined = (dataC.idSourceSelected != -1);
                        if (m_Column3DViewManager.latenciesFiles[dataC.currentLatencyFile].is_size_a_source(dataC.SelectedSiteID))
                        {
                            dataC.siteIsSource = true;
                        }

                        if (dataC.sourceDefined)
                        {
                            dataC.siteLatencyData = m_Column3DViewManager.latenciesFiles[dataC.currentLatencyFile].is_site_responsive_for_source(dataC.SelectedSiteID, dataC.idSourceSelected);
                        }
                    }
                    break;
                default:
                    break;
            }

            ClickSite.Invoke(-1);            
            m_Column3DViewManager.update_all_columns_sites_rendering(data_);            
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
            if (!data_.mriLoaded)
                return;

            // current column is different : we display only for the focused column
            if (m_Column3DViewManager.SelectedColumnID != idColumn)
                return;

            // retrieve layer
            int layerMask = 0;
            layerMask |= 1 << LayerMask.NameToLayer(m_Column3DViewManager.SelectedColumn.Layer);
            layerMask |= 1 << LayerMask.NameToLayer("Meshes_SP");

            RaycastHit hit;
            bool isCollision = Physics.Raycast(ray.origin, ray.direction, out hit, Mathf.Infinity, layerMask);
            if (!isCollision)
            {
                UpdateDisplayedSitesInfo.Invoke(new SiteInfo(null, false, mousePosition, false));
                return;
            }
            if (hit.collider.transform.parent.name == "cuts" || hit.collider.transform.parent.name == "brains")
            {
                UpdateDisplayedSitesInfo.Invoke(new SiteInfo(null, false, mousePosition, false));
                return;
            }

            Site site = hit.collider.GetComponent<Site>();

            switch (m_Column3DViewManager.SelectedColumn.Type)
            {
                case Column3DView.ColumnType.FMRI:
                    UpdateDisplayedSitesInfo.Invoke(new SiteInfo(site, true, mousePosition, true, false, hit.collider.GetComponent<Site>().Information.FullName));
                    return;
                case Column3DView.ColumnType.IEEG:
                    Column3DViewIEEG currIEEGCol = (Column3DViewIEEG)m_Column3DViewManager.SelectedColumn;
                    int idPlot = site.Information.SitePatientID;

                    // retrieve currant plot amp
                    float amp = 0;
                    if (currIEEGCol.columnData.Values.Length > 0)
                    {
                        amp = currIEEGCol.columnData.Values[idPlot][currIEEGCol.currentTimeLineID];
                    }

                    // retrieve current lateny/height
                    string latency = "none", height = "none";
                    if (currIEEGCol.currentLatencyFile != -1)
                    {
                        Latencies latencyFile = m_Column3DViewManager.latenciesFiles[currIEEGCol.currentLatencyFile];

                        if (currIEEGCol.idSourceSelected == -1) // no source selected
                        {
                            latency = "...";
                            height = "no source selected";
                        }
                        else if (currIEEGCol.idSourceSelected == idPlot) // plot is the source
                        {
                            latency = "0";
                            height = "source";
                        }
                        else
                        {
                            if (latencyFile.is_site_responsive_for_source(idPlot, currIEEGCol.idSourceSelected)) // data available
                            {
                                latency = "" + latencyFile.latencies[currIEEGCol.idSourceSelected][idPlot];
                                height = "" + latencyFile.heights[currIEEGCol.idSourceSelected][idPlot];
                            }
                            else
                            {
                                latency = "No data";
                                height = "No data";
                            }
                        }
                    }

                    UpdateDisplayedSitesInfo.Invoke(new SiteInfo(site, true, mousePosition, m_Column3DViewManager.SelectedColumn.Type == Column3DView.ColumnType.FMRI, data_.displayCcepMode,
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
            UpdateDisplayedSitesInfo.Invoke(new SiteInfo(null, false, new Vector3(0, 0, 0), false));
        }
        /// <summary>
        /// Update the display mode of the scene
        /// </summary>
        /// <param name="isCeepMode"></param>
        public void SetCCEPDisplayMode(bool isCeepMode)
        {
            DisplayScreenMessage(isCeepMode ? "CCEP mode enabled" : "iEEG mode enabled", 1.5f, 150, 30);

            data_.displayCcepMode = isCeepMode;
            //UpdateCutsInUI.Invoke(m_CM.getBrainCutTextureList(m_CM.idSelectedColumn, true, data_.generatorUpToDate, data_.displayLatenciesMode), m_CM.idSelectedColumn, m_CM.planesList.Count);

            m_Column3DViewManager.update_all_columns_sites_rendering(data_);

            // force mode to update UI
            m_ModesManager.set_current_mode_specifications(true);
        }
        /// <summary>
        /// Define the current plot as the source
        /// </summary>
        public void SetCurrentSiteAsSource()
        {
            switch (m_Column3DViewManager.SelectedColumn.Type)
            {
                case Column3DView.ColumnType.FMRI:
                    return;
                case Column3DView.ColumnType.IEEG:
                    Column3DViewIEEG currCol = (Column3DViewIEEG)m_Column3DViewManager.SelectedColumn;
                    currCol.idSourceSelected = currCol.SelectedSiteID;
                    m_Column3DViewManager.update_all_columns_sites_rendering(data_);
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
            switch (m_Column3DViewManager.SelectedColumn.Type)
            {
                case Column3DView.ColumnType.FMRI:
                    return;
                case Column3DView.ColumnType.IEEG:
                    Column3DViewIEEG currCol = (Column3DViewIEEG)m_Column3DViewManager.SelectedColumn;
                    currCol.idSourceSelected = -1;
                    m_Column3DViewManager.update_all_columns_sites_rendering(data_);
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
            switch (m_Column3DViewManager.SelectedColumn.Type)
            {
                case Column3DView.ColumnType.FMRI:
                    return;
                case Column3DView.ColumnType.IEEG:
                    if (m_Column3DViewManager.SelectedColumn.SelectedSiteID != -1)
                    {
                        List<List<bool>> masksColumnsData = new List<List<bool>>(m_Column3DViewManager.ColumnsIEEG.Count);
                        for (int ii = 0; ii < m_Column3DViewManager.ColumnsIEEG.Count; ++ii)
                        {
                            masksColumnsData.Add(new List<bool>(m_Column3DViewManager.ColumnsIEEG[ii].Sites.Count));

                            for (int jj = 0; jj < m_Column3DViewManager.ColumnsIEEG[ii].Sites.Count; ++jj)
                            {
                                Site p = m_Column3DViewManager.ColumnsIEEG[ii].Sites[jj];
                                bool keep = (!p.Information.IsBlackListed && !p.Information.IsExcluded && !p.Information.IsMasked);
                                masksColumnsData[ii].Add(keep);
                            }
                        }

                        SiteRequest request = new SiteRequest();
                        request.spScene = true;
                        request.idSite1 = m_Column3DViewManager.SelectedColumn.SelectedSiteID;
                        request.idSite2 = (previousPlot == null) ? -1 : previousPlot.Information.SitePatientID;
                        request.idPatient = Patient.ID;
                        request.idPatient2 = Patient.ID;
                        request.maskColumn = masksColumnsData;

                        SiteInfoRequest.Invoke(request);
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
            switch (m_Column3DViewManager.SelectedColumn.Type)
            {
                case Column3DView.ColumnType.FMRI:
                    return;
                case Column3DView.ColumnType.IEEG:
                    Column3DViewIEEG currCol = (Column3DViewIEEG)m_Column3DViewManager.SelectedColumn;
                    currCol.currentLatencyFile = id;
                    UndefineCurrentSource();
                    break;
                default:
                    break;
            }
        }
        #endregion
    }
}