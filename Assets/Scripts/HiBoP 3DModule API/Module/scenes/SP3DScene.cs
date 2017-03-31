
/**
 * \file    SP3DScene.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define SP3DScene class
 */

// system
using System;
using System.Collections.Generic;

// unity
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Events;

namespace HBP.VISU3D
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
    public class SP3DScene : Base3DScene
    {
        #region members

        public AmbientMode m_ambiantMode = AmbientMode.Flat;
        public float m_ambientIntensity = 1;
        public Color m_ambiantLight = new Color(0.2f, 0.2f, 0.2f, 1);


        HBP.Data.Patient m_patient = null;

        // events
        private Events.UpdateLatencies m_updateLatencies = new Events.UpdateLatencies();
        public Events.UpdateLatencies UpdateLatencies { get { return m_updateLatencies; } }

        private List<string> CCEPLabels = null;

        #endregion members

        #region functions

        /// <summary>
        /// Update the meshes cuts
        /// </summary>
        public override void compute_meshes_cuts()
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
                            data_.meshToDisplay = (DLL.Surface)m_CM.BothHemi;
                            break;
                        case SceneStatesInfo.MeshPart.Left:
                            data_.meshToDisplay = (DLL.Surface)m_CM.LHemi;
                            break;
                        case SceneStatesInfo.MeshPart.Right:
                            data_.meshToDisplay = (DLL.Surface)m_CM.RHemi;
                            break;
                    }
                    break;
                case SceneStatesInfo.MeshType.White:
                    switch (data_.meshPartToDisplay)
                    {
                        case SceneStatesInfo.MeshPart.Both:
                            data_.meshToDisplay = (DLL.Surface)m_CM.BothWhite;
                            break;
                        case SceneStatesInfo.MeshPart.Left:
                            data_.meshToDisplay = (DLL.Surface)m_CM.LWhite;
                            break;
                        case SceneStatesInfo.MeshPart.Right:
                            data_.meshToDisplay = (DLL.Surface)m_CM.RWhite;
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
            if (planesList.Count > 0)
                cuts = new List<DLL.Surface>(data_.meshToDisplay.cut(m_CM.planesCutsCopy.ToArray(), data_.removeFrontPlaneList.ToArray(), !data_.holesEnabled));
            else
                cuts = new List<DLL.Surface>() { (DLL.Surface)data_.meshToDisplay.Clone() };

            if (m_CM.DLLCutsList.Count != cuts.Count)
                m_CM.DLLCutsList = cuts;
            else
            {
                // swap DLL pointer
                for (int ii = 0; ii < cuts.Count; ++ii)
                    m_CM.DLLCutsList[ii].swap_DLL_handle(cuts[ii]);
            }

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-SP3DScene-Update compute_meshes_cuts 1 splitToSurfaces"); // 2%

            // split the cut mesh         
            m_CM.DLLSplittedMeshesList = new List<DLL.Surface>(m_CM.DLLCutsList[0].split_to_surfaces(m_CM.meshSplitNb));

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-SP3DScene-Update compute_meshes_cuts 2 reset brain texture generator"); // 11%

            // reset brain texture generator
            for (int ii = 0; ii < m_CM.meshSplitNb; ++ii)
            {
                m_CM.DLLCommonBrainTextureGeneratorList[ii].reset(m_CM.DLLSplittedMeshesList[ii], m_CM.DLLVolume);
                m_CM.DLLCommonBrainTextureGeneratorList[ii].compute_UVMain_with_volume(m_CM.DLLSplittedMeshesList[ii], m_CM.DLLVolume, m_CM.MRICalMinFactor, m_CM.MRICalMaxFactor);
            }

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-SP3DScene-Update compute_meshes_cuts 3 update cut brain mesh object mesh filter"); // 6%

            reset_tri_erasing(false);

            // update brain mesh object mesh filter
            for (int ii = 0; ii < m_CM.meshSplitNb; ++ii)
                m_CM.DLLSplittedMeshesList[ii].update_mesh_from_dll(go_.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-SP3DScene-Update compute_meshes_cuts 4 update cuts generators"); // 17%
            

            // update cuts generators
            for (int ii = 0; ii < m_CM.planesCutsCopy.Count; ++ii)
            {
                for (int jj = 0; jj < m_CM.IEEG_columns_nb(); ++jj)
                    m_CM.DLLMRIGeometryCutGeneratorList[ii].reset(m_CM.DLLVolume, m_CM.planesCutsCopy[ii]);                        

                m_CM.DLLMRIGeometryCutGeneratorList[ii].update_cut_mesh_UV(CM.DLLCutsList[ii + 1]);
                m_CM.DLLCutsList[ii + 1].update_mesh_from_dll(go_.brainCutMeshes[ii].GetComponent<MeshFilter>().mesh);
            }

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-SP3DScene-Update compute_meshes_cuts 5 create null uv2/uv3 arrays"); // 0%
            
            // create null uv2/uv3 arrays
            m_CM.uvNull = new List<Vector2[]>(m_CM.meshSplitNb);
            for (int ii = 0; ii < m_CM.meshSplitNb; ++ii)
            {
                m_CM.uvNull.Add(new Vector2[go_.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh.vertexCount]);
                Extensions.ArrayExtensions.Fill(m_CM.uvNull[ii], new Vector2(0.01f, 1f));
            }

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-SP3DScene-Update compute_meshes_cuts 6 end"); // 0%
            

            // enable cuts gameobject
            for (int ii = 0; ii < m_CM.planesCutsCopy.Count; ++ii)
                    go_.brainCutMeshes[ii].SetActive(true); 

            data_.collidersUpdated = false; // colliders are now longer up to date
            data_.updateCutMeshGeometry = false;   // planes are now longer requested to be updated 
            data_.generatorUpToDate = false; // generator is not up to date anymore

            // update amplitude for all columns
            for (int ii = 0; ii < m_CM.IEEG_columns_nb(); ++ii)
                m_CM.IEEG_col(ii).updateIEEG = true;

            UnityEngine.Profiling.Profiler.EndSample();
        }


        /// <summary>
        /// Reset the scene : reload meshes, IRM, plots, and regenerate textures
        /// </summary>
        /// <param name="patient"></param>
        public bool reset(Data.Patient patient, bool postIRM)
        {
            modes.updateMode(Mode.FunctionsId.resetScene);

            m_patient = patient;

            List<string> ptsFiles = new List<string>(), namePatients = new List<string>();
            ptsFiles.Add(m_patient.Brain.PatientReferenceFrameImplantation); //.PatientBasedImplantation);
            namePatients.Add(m_patient.Place + "_" + m_patient.Date + "_" + m_patient.Name);

            List<string> meshesFiles = new List<string>();
            meshesFiles.Add(m_patient.Brain.LeftCerebralHemisphereMesh);
            meshesFiles.Add(m_patient.Brain.RightCerebralHemisphereMesh);

            // reset columns
            m_CM.reset(planesList.Count);

            DLL.Transformation meshTransformation = new DLL.Transformation();
            meshTransformation.load(m_patient.Brain.PreOperationReferenceFrameToScannerReferenceFrameTransformation);
            if (postIRM)
            {
                // ...
            }


            // load meshes
            bool success = reset_brain_surface(meshesFiles, m_patient.Brain.PreOperationReferenceFrameToScannerReferenceFrameTransformation);

            // load volume
            if (success)
                success = reset_NII_brain_volume(m_patient.Brain.PreOperationMRI);

            // load electrodes
            if (success)
                success = reset_electrodes(ptsFiles, namePatients);

            if (success)
                data_.updateCutMeshGeometry = true;

            if (!success)
            {
                Debug.LogError("-ERROR : SP3DScene : reset failed. ");
                data_.reset();
                m_CM.reset(planesList.Count);
                reset_scene_GO();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Reset the meshes of the scene with GII files
        /// </summary>
        /// <param name="pathGIIBrainFiles"></param>
        /// <param name="pathTransformFile"></param>
        /// <returns></returns>
        public bool reset_brain_surface(List<string> pathGIIBrainFiles, string pathTransformFile)
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.resetGIIBrainSurfaceFile))
            {
                Debug.LogError("-ERROR : Base3DScene::resetGIIBrainSurfaceFile -> no acess for mode : " + modes.currentModeName());
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
            bool leftMeshLoaded = m_CM.LHemi.load_GII_file(pathGIIBrainFiles[0], true, pathTransformFile);
            bool leftWhiteLoaded = false, rightWhiteLoaded = false;
            bool leftParcelsLoaded = false, rightParcelsLoaded = false;
            if (leftMeshLoaded)
            {
                m_CM.LHemi.flip_triangles();  // the transformation inverses one axis, so faces of the surface must be flipped (TODO : case with no transformation or no inverse axi transfo)
                m_CM.LHemi.compute_normals();

                string leftWhitePath = pathGIIBrainFiles[0].Replace("_Lhemi", "_Lwhite");
                leftWhiteLoaded = m_CM.LWhite.load_GII_file(leftWhitePath, true, pathTransformFile);
                if (leftWhiteLoaded)
                {
                    m_CM.LWhite.flip_triangles();
                    m_CM.LWhite.compute_normals();

                    string[] split = leftWhitePath.Split('/');
                    string parcelPath = leftWhitePath.Substring(0, leftWhitePath.LastIndexOf('/')) + "/surface_analysis/" + split[split.Length - 1].Replace(".gii", "") + "_parcels_marsAtlas.gii";
                    leftParcelsLoaded = m_CM.LWhite.seach_mars_parcel_file_and_update_colors(GlobalGOPreloaded.MarsAtlasIndex, parcelPath);
                }                
            }


            // load right hemi
            bool rightMeshLoaded = m_CM.RHemi.load_GII_file(pathGIIBrainFiles[1], true, pathTransformFile);
            if (rightMeshLoaded)
            {
                m_CM.RHemi.flip_triangles();  // the transformation inverses one axis, so faces of the surface must be flipped (TODO : case with no transformation or no inverse axi transfo)
                m_CM.RHemi.compute_normals();

                string rightWhitePath = pathGIIBrainFiles[1].Replace("_Rhemi", "_Rwhite");
                rightWhiteLoaded = m_CM.RWhite.load_GII_file(rightWhitePath, true, pathTransformFile);
                if (rightWhiteLoaded)
                {
                    m_CM.RWhite.flip_triangles();
                    m_CM.RWhite.compute_normals();

                    string[] split = rightWhitePath.Split('/');
                    string parcelPath = rightWhitePath.Substring(0, rightWhitePath.LastIndexOf('/')) + "/surface_analysis/" + split[split.Length - 1].Replace(".gii", "") + "_parcels_marsAtlas.gii";
                    rightParcelsLoaded = m_CM.RWhite.seach_mars_parcel_file_and_update_colors(GlobalGOPreloaded.MarsAtlasIndex, parcelPath);
                }
            }

            // fusion
            if (leftMeshLoaded && rightMeshLoaded)
            {
                // copy left
                m_CM.BothHemi = (DLL.Surface)m_CM.LHemi.Clone();

                // add right
                m_CM.BothHemi.add(m_CM.RHemi);
                data_.meshesLoaded = true;

                // get the middle
                data_.meshCenter = m_CM.BothHemi.bounding_box().center();

                if(rightWhiteLoaded && leftWhiteLoaded)
                {
                    m_CM.BothWhite = (DLL.Surface)m_CM.LWhite.Clone();
                    // add right
                    m_CM.BothWhite.add(m_CM.RWhite);
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

            int maxVerticesNb = m_CM.BothHemi.vertices_nb();
            if (leftWhiteLoaded && rightWhiteLoaded)
                maxVerticesNb = Math.Max(maxVerticesNb, m_CM.BothWhite.vertices_nb());
            int nbSplits = (maxVerticesNb / 65000) + (int)(((maxVerticesNb % 60000) != 0) ? 3 : 2);
            reset_splits_nb(nbSplits);
            
            // update scenes cameras
            UpdateCameraTarget.Invoke(m_CM.BothHemi.bounding_box().center());
           
            // set the transform as the mesh center
            data_.hemiMeshesAvailables = true;

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.resetGIIBrainSurfaceFile);
            //##################

            return data_.meshesLoaded;
        }


        /// <summary>
        /// Reset all the sites with a new list of pts files
        /// </summary>
        /// <param name="pathsElectrodesPtsFile"></param>
        /// <returns></returns>
        public bool reset_electrodes(List<string> pathsElectrodesPtsFile, List<string> namePatients)
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.resetElectrodesFile))
            {
                Debug.LogError("-ERROR : SinglePatient3DScene::resetElectrodesFile -> no acess for mode : " + modes.currentModeName());
                return false;
            }
            //##################

            // load list of pts files
            data_.sitesLoaded = m_CM.DLLLoadedPatientsElectrodes.load_pts_files(pathsElectrodesPtsFile, namePatients, GlobalGOPreloaded.MarsAtlasIndex);

            // destroy previous electrodes gameobjects
            for (int ii = 0; ii < m_CM.SitesList.Count; ++ii)
            {
                //Destroy(go_.PlotsList[ii].GetComponent<MeshFilter>().mesh); // now it's a shared mesh
                Destroy(m_CM.SitesList[ii]);
            }
            m_CM.SitesList.Clear();


            // destroy plots elecs/patients parents
            for (int ii = 0; ii < m_CM.PlotsPatientParent.Count; ++ii)
            {
                Destroy(m_CM.PlotsPatientParent[ii]);
                for (int jj = 0; jj < m_CM.PlotsElectrodesParent[ii].Count; ++jj)
                {
                    Destroy(m_CM.PlotsElectrodesParent[ii][jj]);
                }

            }
            m_CM.PlotsPatientParent.Clear();
            m_CM.PlotsElectrodesParent.Clear();


            if (data_.sitesLoaded)
            {
                int currPlotNb = 0;
                for (int ii = 0; ii < m_CM.DLLLoadedPatientsElectrodes.patients_nb(); ++ii)
                {
                    int idPlotPatient = 0;
                    string patientName = m_CM.DLLLoadedPatientsElectrodes.patient_name(ii);

                    // create plot patient parent
                    m_CM.PlotsPatientParent.Add(new GameObject("P" + ii + " - " + patientName));
                    m_CM.PlotsPatientParent[m_CM.PlotsPatientParent.Count - 1].transform.SetParent(go_.sitesMeshesParent.transform);
                    m_CM.PlotsElectrodesParent.Add(new List<GameObject>(m_CM.DLLLoadedPatientsElectrodes.electrodes_nb(ii)));

                    for (int jj = 0; jj < m_CM.DLLLoadedPatientsElectrodes.electrodes_nb(ii); ++jj)
                    {
                        // create plot electrode parent
                        m_CM.PlotsElectrodesParent[ii].Add(new GameObject(m_CM.DLLLoadedPatientsElectrodes.electrode_name(ii, jj)));
                        m_CM.PlotsElectrodesParent[ii][m_CM.PlotsElectrodesParent[ii].Count - 1].transform.SetParent(m_CM.PlotsPatientParent[ii].transform);

                        for (int kk = 0; kk < m_CM.DLLLoadedPatientsElectrodes.electrode_sites_nb(ii, jj); ++kk)
                        {
                            Vector3 positionInverted = m_CM.DLLLoadedPatientsElectrodes.site_pos(ii, jj, kk);
                            positionInverted.x = -positionInverted.x;


                            GameObject siteGO = Instantiate(GlobalGOPreloaded.Plot);
                            siteGO.name = m_CM.DLLLoadedPatientsElectrodes.site_name(ii, jj, kk);

                            siteGO.transform.position = positionInverted;// + go_.PlotsParent.transform.position; // TODO : ?
                            siteGO.transform.SetParent(m_CM.PlotsElectrodesParent[ii][jj].transform);
                            siteGO.GetComponent<MeshFilter>().sharedMesh = SharedMeshes.Site;

                            siteGO.SetActive(true);
                            siteGO.layer = LayerMask.NameToLayer("Inactive");

                            Site site = siteGO.GetComponent<Site>();
                            site.idSitePatient = idPlotPatient++;
                            site.idPatient = ii;
                            site.idElectrode = jj;
                            site.idSite = kk;
                            site.idGlobal = currPlotNb++;
                            site.blackList = false;
                            site.highlight = false;
                            site.patientName = patientName;
                            site.fullName = namePatients[ii] + "_" + siteGO.name;

                            // mars atlas
                            site.labelMarsAtlas =  m_CM.DLLLoadedPatientsElectrodes.site_mars_atlas_label(ii,jj,kk);//
                            site.greyWhiteMatter = 0;//        

                            m_CM.SitesList.Add(siteGO);
                        }
                    }
                }
            }

            // reset selected plot
            for (int ii = 0; ii < m_CM.columns.Count; ++ii)
            {
                m_CM.col(ii).id_selected_site = -1;
            }

            // update latencies
            m_CM.DLLLoadedRawPlotsList = new DLL.RawSiteList();
            m_CM.DLLLoadedPatientsElectrodes.extract_raw_site_list(m_CM.DLLLoadedRawPlotsList);


            // reset latencies
            m_CM.latenciesFiles = new List<Latencies>();
            CCEPLabels = new List<string>();
            //for (int ii = 0; ii < m_patient.Brain.Connectivities.Count; ++ii)
            {
                Latencies latencies = null;

                
                if(m_patient.Brain.PlotsConnectivity == "dummyPath")
                //if (m_patient.Brain.Connectivities[ii].Path == "dummyPath")
                {
                    // generate dummy latencies
                    latencies = m_CM.DLLLoadedRawPlotsList.generate_dummy_latencies();
                }
                else
                {
                    // load latency file
                    latencies = m_CM.DLLLoadedRawPlotsList.update_latencies_with_file(m_patient.Brain.PlotsConnectivity);// Connectivities[ii].Path);
                }

                //latencies = m_CM.DLLLoadedRawPlotsList.updateLatenciesWithFile("C:/Users/Florian/Desktop/amplitudes_latencies/amplitudes_latencies/SIEJO_amplitudes_latencies.txt");
                latencies.name = m_patient.Brain.PlotsConnectivity; //Connectivities[ii].Label;
                m_CM.latenciesFiles.Add(latencies);
                CCEPLabels.Add(latencies.name);
            }

            m_CM.latencyFilesDefined = true; //(m_patient.Brain.Connectivities.Count > 0);
            m_updateLatencies.Invoke(CCEPLabels);


            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.resetElectrodesFile);
            //##################

            return data_.sitesLoaded;
        }


        /// <summary>
        /// Define the timeline data with a patient and a list of column data
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="columnDataList"></param>
        public void set_timeline_data(HBP.Data.Patient patient, List<HBP.Data.Visualisation.ColumnData> columnDataList)
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.setTimelines))
            {
                Debug.LogError("-ERROR : Base3DScene::setTimelines -> no acess for mode : " + modes.currentModeName());
                return;
            }
            //##################

            // update columns number
            m_CM.update_columns_nb(columnDataList.Count, 0, planesList.Count);

            // update columns names
            for (int ii = 0; ii < columnDataList.Count; ++ii)
            {
                m_CM.IEEG_col(ii).Label = columnDataList[ii].Label;                
            }            

            // set timelines
            m_CM.set_SP_timeline_data(patient, columnDataList);

            // update plots visibility
            m_CM.update_all_columns_sites_rendering(data_);

            // set flag
            data_.timelinesLoaded = true;

            // send data to UI
            send_IEEG_parameters_to_menu();
            
            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.setTimelines);
            //##################
        }

        /// <summary>
        /// Set the current plot to be selected in all the columns
        /// </summary>
        /// <param name="idSelectedPlot"></param>
        public void define_selected_site(int idSelectedPlot)
        {
            m_updateLatencies.Invoke(CCEPLabels);

            for (int ii = 0; ii < m_CM.IEEG_columns_nb(); ++ii)
            {
                m_CM.IEEG_col(ii).id_selected_site = idSelectedPlot;
                ClickSite.Invoke(ii);
            }
        }

        /// <summary>
        /// Update the columns masks of the scene
        /// </summary>
        /// <param name="blacklistMasks"></param>
        /// <param name="excludedMasks"></param>
        public void set_columns_site_mask(List<List<bool>> blacklistMasks, List<List<bool>> excludedMasks, List<List<bool>> hightLightedMasks)
        {
            for(int ii = 0; ii < m_CM.IEEG_columns_nb(); ++ii)
            {
                for (int jj = 0; jj < m_CM.SitesList.Count; ++jj)
                {
                    m_CM.IEEG_col(ii).Sites[jj].blackList = blacklistMasks[ii][jj];
                    m_CM.IEEG_col(ii).Sites[jj].exclude = excludedMasks[ii][jj];
                    m_CM.IEEG_col(ii).Sites[jj].highlight = hightLightedMasks[ii][jj];
                }
            }

            m_CM.update_all_columns_sites_rendering(data_);
        }

        /// <summary>
        /// Reset the rendering settings for this scene, called by each MP camera before rendering
        /// </summary>
        /// <param name="cameraRotation"></param>
        public override void reset_rendering_settings(Vector3 cameraRotation)
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
        public void click_on_scene(Ray ray)
        {
            // scene not loaded
            if (!data_.mriLoaded)
                return;

            // update colliders if necessary (SLOW)
            if (!data_.collidersUpdated)
                update_meshes_colliders();

            // retrieve layer
            int layerMask = 0;
            layerMask |= 1 << LayerMask.NameToLayer(m_CM.current_column().layerColumn);
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

                    for (int ii = 0; ii < m_CM.DLLSplittedMeshesList.Count; ++ii)
                    {
                        //if (go_.brainSurfaceMeshes[ii].name == hit.collider.gameObject.name)
                        {
                            m_CM.DLLSplittedMeshesList[ii].update_mesh_from_dll(go_.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);
                            //break;
                        }
                    }
                }

                return;
            }

            // plot hit            
            int clickedPlotID = hit.collider.gameObject.GetComponent<Site>().idGlobal;
            Column3DView currColumn = m_CM.current_column();
            currColumn.id_selected_site = clickedPlotID;

            if (!m_CM.is_current_column_FMRI())
            {
                Column3DViewIEEG dataC = (Column3DViewIEEG)currColumn;

                m_CM.latencyFileAvailable = (dataC.currentLatencyFile != -1);
                dataC.sourceDefined = false;
                dataC.siteIsSource = false;
                dataC.siteLatencyData = false;

                if (m_CM.latencyFileAvailable)
                {
                    dataC.sourceDefined = (dataC.idSourceSelected != -1);
                    if (m_CM.latenciesFiles[dataC.currentLatencyFile].is_size_a_source(dataC.id_selected_site))
                    {
                        dataC.siteIsSource = true;
                    }

                    if (dataC.sourceDefined)
                    {
                        dataC.siteLatencyData = m_CM.latenciesFiles[dataC.currentLatencyFile].is_site_responsive_for_source(dataC.id_selected_site, dataC.idSourceSelected);
                    }
                }
            }

            ClickSite.Invoke(-1);            
            m_CM.update_all_columns_sites_rendering(data_);            
        }

        /// <summary>
        /// anage the mouse movments event in the scene
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="mousePosition"></param>
        /// /// <param name="idColumn"></param>
        public new void move_mouse_on_scene(Ray ray, Vector3 mousePosition, int idColumn)
        {
            base.move_mouse_on_scene(ray, mousePosition, idColumn);

            // scene not loaded
            if (!data_.mriLoaded)
                return;

            // current column is different : we display only for the focused column
            if (m_CM.idSelectedColumn != idColumn)
                return;

            // retrieve layer
            int layerMask = 0;
            layerMask |= 1 << LayerMask.NameToLayer(m_CM.current_column().layerColumn);
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

            // fMRI
            if (m_CM.is_FMRI_column(m_CM.idSelectedColumn))
            {
                UpdateDisplayedSitesInfo.Invoke(new SiteInfo(site, true, mousePosition, true, false, hit.collider.GetComponent<Site>().fullName));
                return;
            }

            // IEEG
            Column3DViewIEEG currIEEGCol = (Column3DViewIEEG)m_CM.current_column();
            int idPlot = site.idSitePatient;

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
                Latencies latencyFile = m_CM.latenciesFiles[currIEEGCol.currentLatencyFile];

                if (currIEEGCol.idSourceSelected == -1) // no source selected
                {
                    latency = "...";
                    height = "no source selected";
                }
                else if(currIEEGCol.idSourceSelected == idPlot) // plot is the source
                {
                    latency = "0";
                    height = "source";
                }
                else
                {
                    if(latencyFile.is_site_responsive_for_source(idPlot, currIEEGCol.idSourceSelected)) // data available
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

            UpdateDisplayedSitesInfo.Invoke(new SiteInfo(site, true, mousePosition, m_CM.is_current_column_FMRI(), data_.displayCcepMode,
                hit.collider.GetComponent<Site>().fullName, "" + amp, height, latency));
        }

        public void disable_plot_display_window(int idColumn)
        {
            UpdateDisplayedSitesInfo.Invoke(new SiteInfo(null, false, new Vector3(0, 0, 0), false));
        }

        /// <summary>
        /// Update the display mode of the scene
        /// </summary>
        /// <param name="isCeepMode"></param>
        public void set_CCEP_display_mode(bool isCeepMode)
        {
            display_sceen_message(isCeepMode ? "CCEP mode enabled" : "iEEG mode enabled", 1.5f, 150, 30);

            data_.displayCcepMode = isCeepMode;
            //UpdateCutsInUI.Invoke(m_CM.getBrainCutTextureList(m_CM.idSelectedColumn, true, data_.generatorUpToDate, data_.displayLatenciesMode), m_CM.idSelectedColumn, m_CM.planesList.Count);

            m_CM.update_all_columns_sites_rendering(data_);

            // force mode to update UI
            modes.set_current_mode_specifications(true);
        }

        /// <summary>
        /// Define the current plot as the source
        /// </summary>
        public void set_current_site_as_source()
        {
            if (m_CM.current_column().isFMRI)
                return;

            Column3DViewIEEG currCol = (Column3DViewIEEG)m_CM.current_column();
            currCol.idSourceSelected = currCol.id_selected_site;
            m_CM.update_all_columns_sites_rendering(data_);
        }

        /// <summary>
        /// Undefine the current plot as the source
        /// </summary>
        public void undefine_current_source()
        {
            if (m_CM.current_column().isFMRI)
                return;

            Column3DViewIEEG currCol = (Column3DViewIEEG)m_CM.current_column();
            currCol.idSourceSelected = -1;
            m_CM.update_all_columns_sites_rendering(data_);
        }

        /// <summary>
        /// Send additionnal plot info to hight level UI
        /// </summary>
        public override void send_additionnal_site_info_request(Site previousPlot = null) // TODO : deporter dans c manager
        {
            if (m_CM.current_column().isFMRI)
                return;

            if (m_CM.current_column().id_selected_site != -1)
            {                
                List<List<bool>> masksColumnsData = new List<List<bool>>(m_CM.IEEG_columns_nb());                
                for (int ii = 0; ii < m_CM.IEEG_columns_nb(); ++ii)
                {
                    masksColumnsData.Add(new List<bool>(m_CM.IEEG_col(ii).Sites.Count));

                    for (int jj = 0; jj < m_CM.IEEG_col(ii).Sites.Count; ++jj)
                    {
                        Site p = m_CM.IEEG_col(ii).Sites[jj];
                        bool keep = (!p.blackList && !p.exclude && !p.columnMask);
                        masksColumnsData[ii].Add(keep);
                    }
                }
               
                SiteRequest request = new SiteRequest();
                request.spScene = true;
                request.idSite1 = m_CM.current_column().id_selected_site;
                request.idSite2 = (previousPlot == null) ? - 1 : previousPlot.idSitePatient;
                request.idPatient = m_patient.ID;
                request.idPatient2 = m_patient.ID;
                request.maskColumn = masksColumnsData;

                PlotInfoRequest.Invoke(request);
            }
        }

        /// <summary>
        /// Update the id of the latency file
        /// </summary>
        /// <param name="id"></param>
        public void update_current_latency_file(int id)
        {
            if (m_CM.current_column().isFMRI)
                return;

            Column3DViewIEEG currCol = (Column3DViewIEEG)m_CM.current_column();
            currCol.currentLatencyFile = id;
            undefine_current_source();
        }


        #endregion functions
    }
}