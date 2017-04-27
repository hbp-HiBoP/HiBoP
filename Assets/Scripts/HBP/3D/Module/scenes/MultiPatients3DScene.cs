
/**
 * \file    MP3DScene.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define MP3DScene class
 */

// system
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
// unity
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;


namespace HBP.Module3D
{
    namespace Events
    {
        /// <summary>
        /// Event for sending a ROI associated to a column id (params : ROI, idColumn)
        /// </summary>
        public class SendColumnROI : UnityEvent<ROI, int> { }

        /// <summary>
        /// Event for creating a new bubble to a column id with a position  (params : position, idColumn)
        /// </summary>
        public class CreateBubble : UnityEvent<Vector3, int> { }

        /// <summary>
        /// Event for selecting a bubble of a column (params : idColumn, idBubble)
        /// </summary>
        public class SelectBubble : UnityEvent<int, int> { }

        /// <summary>
        /// Event for changing the size of a bubble (params : idColumn, idBubble, size)
        /// </summary>
        public class ChangeSizeBubble : UnityEvent<int, int, float> { }

        /// <summary>
        /// Event for removing a bubble (params : idColumn, idBubble)
        /// </summary>
        public class RemoveBubble : UnityEvent<int, int> { }

        /// <summary>
        /// Ask the UI to update set the same cameras to the individual scene
        /// </summary>
        public class ApplySceneCamerasToIndividualScene : UnityEvent { }

        /// <summary>
        /// Invoked whend we load a single patient scene from the mutli patients scene (params : id patient)
        /// </summary>        
        [System.Serializable]
        public class LoadSPSceneFromMP : UnityEvent<int> { }
    }

    /// <summary>
    /// The multi patients scene class, inheritance of Base3DScene.
    /// </summary>
    [AddComponentMenu("Scenes/Multi Patients 3D Scene")]
    public class MultiPatients3DScene : Base3DScene
    {
        #region members
        public override SceneType Type
        {
            get
            {
                return SceneType.MultiPatients;
            }
        }
        Data.Visualisation.MultiPatientsVisualisation m_Visualisation = null;
        public Data.Visualisation.MultiPatientsVisualisation Visualisation
        {
            get
            {
                return m_Visualisation;
            }
        }
        public ReadOnlyCollection<Data.Patient> Patients
        {
            get
            {
                return new ReadOnlyCollection<Data.Patient>(m_Visualisation.Patients);
            }
        }
        public override string Name
        {
            get
            {
                return "MNI";
            }
        }
        public AmbientMode m_ambiantMode = AmbientMode.Flat;
        public float m_ambientIntensity = 1;
        public Color m_ambiantLight = new Color(0.2f, 0.2f, 0.2f, 1);

        // events
        public Events.SendColumnROI SendColumnROIEvent = new Events.SendColumnROI();
        public Events.CreateBubble CreateBubbleEvent = new Events.CreateBubble();
        public Events.SelectBubble SelectBubbleEvent = new Events.SelectBubble();    
        public Events.ChangeSizeBubble ChangeSizeBubbleEvent = new Events.ChangeSizeBubble();
        public Events.RemoveBubble RemoveBubbleEvent = new Events.RemoveBubble();
        public Events.ApplySceneCamerasToIndividualScene ApplySceneCamerasToIndividualScene = new Events.ApplySceneCamerasToIndividualScene();
        public Events.LoadSPSceneFromMP LoadSPSceneFromMP = new Events.LoadSPSceneFromMP();
        
        #endregion members

        #region mono_behaviour

        new void Awake()
        {
            int idScript = TimeExecution.get_ID();
            TimeExecution.start_awake(idScript, TimeExecution.ScriptsId.MP3DScene);

            base.Awake();

            m_MNI = GetComponent<MNIObjects>();
            
            TimeExecution.end_awake(idScript, TimeExecution.ScriptsId.MP3DScene, gameObject);

        }

        #endregion mono_behaviour

        #region Public Methods

        /// <summary>
        /// Update the meshes cuts
        /// </summary>
        public override void compute_meshes_cuts()
        {
            // choose the mesh
            data_.meshToDisplay = null;
            switch (data_.meshTypeToDisplay)
            {
                case SceneStatesInfo.MeshType.Hemi:
                    switch (data_.meshPartToDisplay)
                    {
                        case SceneStatesInfo.MeshPart.Both:
                            data_.meshToDisplay = m_MNI.BothHemi;
                            break;
                        case SceneStatesInfo.MeshPart.Left:
                            data_.meshToDisplay = m_MNI.LeftHemi;
                            break;
                        case SceneStatesInfo.MeshPart.Right:
                            data_.meshToDisplay = m_MNI.RightHemi;
                            break;
                    }
                    break;
                case SceneStatesInfo.MeshType.White:
                    switch (data_.meshPartToDisplay)
                    {
                        case SceneStatesInfo.MeshPart.Both:
                            data_.meshToDisplay = m_MNI.BothWhite;
                            break;
                        case SceneStatesInfo.MeshPart.Left:
                            data_.meshToDisplay = m_MNI.LeftWhite;
                            break;
                        case SceneStatesInfo.MeshPart.Right:
                            data_.meshToDisplay = m_MNI.RightWhite;
                            break;
                    }
                    break;
                case SceneStatesInfo.MeshType.Inflated:
                    switch (data_.meshPartToDisplay)
                    {
                        case SceneStatesInfo.MeshPart.Both:
                            data_.meshToDisplay = m_MNI.BothWhiteInflated;
                            break;
                        case SceneStatesInfo.MeshPart.Left:
                            data_.meshToDisplay = m_MNI.LeftWhiteInflated;
                            break;
                        case SceneStatesInfo.MeshPart.Right:
                            data_.meshToDisplay = m_MNI.RightWhiteInflated;
                            break;
                    }
                    break;
            }

            if (data_.meshTypeToDisplay == SceneStatesInfo.MeshType.Inflated)
                m_Column3DViewManager.DLLSplittedWhiteMeshesList = new List<DLL.Surface>(data_.meshToDisplay.split_to_surfaces(m_Column3DViewManager.meshSplitNb));

            // get the middle
            data_.meshCenter = data_.meshToDisplay.bounding_box().center();

            // cut the mesh
            List<DLL.Surface> cuts;
            if (data_.meshTypeToDisplay != SceneStatesInfo.MeshType.Inflated && m_Column3DViewManager.planesCutsCopy.Count > 0)
                cuts = new List<DLL.Surface>(data_.meshToDisplay.cut(m_Column3DViewManager.planesCutsCopy.ToArray(), data_.removeFrontPlaneList.ToArray(), !data_.holesEnabled));
            else
                cuts = new List<DLL.Surface>() { (DLL.Surface)(data_.meshToDisplay.Clone())};

            if(m_Column3DViewManager.DLLCutsList.Count != cuts.Count)
                m_Column3DViewManager.DLLCutsList = cuts;
            else
            {
                // swap DLL pointer
                for (int ii = 0; ii < cuts.Count; ++ii)
                    m_Column3DViewManager.DLLCutsList[ii].swap_DLL_handle(cuts[ii]);
            }

            // split the mesh
            List<DLL.Surface> splits = new List<DLL.Surface>(m_Column3DViewManager.DLLCutsList[0].split_to_surfaces(m_Column3DViewManager.meshSplitNb));
            if (m_Column3DViewManager.DLLSplittedMeshesList.Count != splits.Count)
                m_Column3DViewManager.DLLSplittedMeshesList = splits;
            else
            {
                // swap DLL pointer
                for (int ii = 0; ii < splits.Count; ++ii)
                    m_Column3DViewManager.DLLSplittedMeshesList[ii].swap_DLL_handle(splits[ii]);
            }

            // reset brain texture generator
            for (int ii = 0; ii < m_Column3DViewManager.DLLSplittedMeshesList.Count; ++ii)
            {
                m_Column3DViewManager.DLLCommonBrainTextureGeneratorList[ii].reset(m_Column3DViewManager.DLLSplittedMeshesList[ii], m_Column3DViewManager.DLLVolume);
                m_Column3DViewManager.DLLCommonBrainTextureGeneratorList[ii].compute_UVMain_with_volume(m_Column3DViewManager.DLLSplittedMeshesList[ii], m_Column3DViewManager.DLLVolume, m_Column3DViewManager.MRICalMinFactor, m_Column3DViewManager.MRICalMaxFactor);
            }

            // reset tri eraser
            reset_tri_erasing(false);

            // update cut brain mesh object mesh filter
            for (int ii = 0; ii < m_Column3DViewManager.DLLSplittedMeshesList.Count; ++ii)
                m_Column3DViewManager.DLLSplittedMeshesList[ii].update_mesh_from_dll(go_.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);

            // update cuts generators
            for (int ii = 0; ii < m_Column3DViewManager.planesCutsCopy.Count; ++ii)
            {
                if (data_.meshTypeToDisplay == SceneStatesInfo.MeshType.Inflated) // if inflated, there is not cuts
                {
                    go_.brainCutMeshes[ii].SetActive(false);
                    continue;
                }

                for (int jj = 0; jj < m_Column3DViewManager.ColumnsIEEG.Count; ++jj)
                    m_Column3DViewManager.DLLMRIGeometryCutGeneratorList[ii].reset(m_Column3DViewManager.DLLVolume, m_Column3DViewManager.planesCutsCopy[ii]);

                m_Column3DViewManager.DLLMRIGeometryCutGeneratorList[ii].update_cut_mesh_UV(Column3DViewManager.DLLCutsList[ii + 1]);
                m_Column3DViewManager.DLLCutsList[ii + 1].update_mesh_from_dll(go_.brainCutMeshes[ii].GetComponent<MeshFilter>().mesh);
                go_.brainCutMeshes[ii].SetActive(true); // enable cuts gameobject
            }

            // create null uv2/uv3 arrays
            m_Column3DViewManager.uvNull = new List<Vector2[]>(m_Column3DViewManager.meshSplitNb);
            for (int ii = 0; ii < m_Column3DViewManager.meshSplitNb; ++ii)
            {
                m_Column3DViewManager.uvNull.Add(new Vector2[go_.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh.vertexCount]);
                Extensions.ArrayExtensions.Fill(m_Column3DViewManager.uvNull[ii], new Vector2(0.01f, 1f));
            }

            data_.collidersUpdated = false; // colliders are now longer up to date */
            data_.updateCutMeshGeometry = false;   // planes are now longer requested to be updated 
            data_.generatorUpToDate = false; // generator is not up to date anymore

            // update amplitude for all columns
            for (int ii = 0; ii < m_Column3DViewManager.ColumnsIEEG.Count; ++ii)
                m_Column3DViewManager.ColumnsIEEG[ii].updateIEEG = true;
        }


        /// <summary>
        /// Reset the scene : reload MRI, sites, and regenerate textures
        /// </summary>
        /// <param name="data"></param>
        public bool Initialize(Data.Visualisation.MultiPatientsVisualisation visualisation)
        {
            m_ModesManager.updateMode(Mode.FunctionsId.resetScene);
            m_Visualisation = visualisation;

            // MNI meshes are preloaded
            data_.volumeCenter = m_MNI.IRM.center();
            data_.meshesLoaded = true;
            data_.hemiMeshesAvailables = true;
            data_.whiteMeshesAvailables = true;
            data_.whiteInflatedMeshesAvailables = true;
            data_.ROICreationMode = false;

            // get the middle
            data_.meshCenter = m_MNI.BothHemi.bounding_box().center();

            List<string> ptsFiles = new List<string>(m_Visualisation.GetImplantation().Length), namePatients = new List<string>(m_Visualisation.GetImplantation().Length);
            for (int ii = 0; ii < m_Visualisation.GetImplantation().Length; ++ii)
            {
                ptsFiles.Add(m_Visualisation.GetImplantation()[ii]);
                namePatients.Add(m_Visualisation.Patients[ii].Place + "_" + m_Visualisation.Patients[ii].Date + "_" + m_Visualisation.Patients[ii].Name);
            }

            // reset columns
            m_Column3DViewManager.DLLVolume = null; // this object must no be reseted
            m_Column3DViewManager.Initialize(PlanesList.Count);

            // retrieve MNI IRM volume
            m_Column3DViewManager.DLLVolume = m_MNI.IRM;
            data_.volumeCenter = m_Column3DViewManager.DLLVolume.center();
            data_.mriLoaded = true;
            UpdatePlanes.Invoke();


            // send cal values to the UI
            IRMCalValuesUpdate.Invoke(m_Column3DViewManager.DLLVolume.retrieve_extreme_values());


            // update scenes cameras
            UpdateCameraTarget.Invoke(m_MNI.BothHemi.bounding_box().center());


            //####### UDPATE MODE
            m_ModesManager.updateMode(Mode.FunctionsId.resetNIIBrainVolumeFile);
            //##################

            // reset electrodes
            bool success = reset_electrodes_files(ptsFiles, namePatients);

            // define meshes splits nb
            reset_splits_nb(3);

            if (success)
                data_.updateCutMeshGeometry = true;

            if (!success)
            {
                Debug.LogError("-ERROR : MP3DScene : reset failed. ");
                data_.reset();
                m_Column3DViewManager.Initialize(PlanesList.Count);
                reset_scene_GO();                
                return false;
            }

            SetTimelineData();
            update_selected_column(0);

            display_sceen_message("Multi Patients Scene loaded", 2.0f, 400, 80);
            return true;
        }

        /// <summary>
        /// Reset all the sites with a new list of pts files
        /// </summary>
        /// <param name="pathsElectrodesPtsFile"></param>
        /// <returns></returns>
        public bool reset_electrodes_files(List<string> pathsElectrodesPtsFile, List<string> names)
        {
            //####### CHECK ACESS
            if (!m_ModesManager.functionAccess(Mode.FunctionsId.resetElectrodesFile))
            {
                Debug.LogError("-ERROR : MultiPatients3DScene::resetElectrodesFile -> no acess for mode : " + m_ModesManager.currentModeName());
                return false;
            }
            //##################

            // load list of pts files
            data_.sitesLoaded = m_Column3DViewManager.DLLLoadedPatientsElectrodes.load_pts_files(pathsElectrodesPtsFile, names, GlobalGOPreloaded.MarsAtlasIndex);

            if (!data_.sitesLoaded)
                return false;

            // destroy previous electrodes gameobject
            for (int ii = 0; ii < m_Column3DViewManager.SitesList.Count; ++ii)
            {
                // destroy material
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
                            GameObject siteGO = Instantiate(GlobalGOPreloaded.Plot);
                            siteGO.name = m_Column3DViewManager.DLLLoadedPatientsElectrodes.site_name(ii, jj, kk);
                            //brainElectrode.name = "????? " + cm_.DLLLoadedPatientsElectrodes.getPlotName(ii, jj, kk);
                                     
                            Vector3 posInverted = m_Column3DViewManager.DLLLoadedPatientsElectrodes.site_pos(ii, jj, kk);
                            posInverted.x = -posInverted.x;
                            siteGO.transform.position = posInverted;
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
                            site.Information.FullName = names[ii] + "_" + siteGO.name;
                            site.IsActive = true;

                            // mars atlas
                            //Debug.Log("sp-> " + ii + " " + jj + " " + kk);
                            site.Information.MarsAtlasIndex = m_Column3DViewManager.DLLLoadedPatientsElectrodes.site_mars_atlas_label(ii, jj, kk);//
                            m_Column3DViewManager.SitesList.Add(siteGO);
                        }
                    }
                }
            }

            // reset selected plot
            for (int ii = 0; ii < m_Column3DViewManager.ColumnsIEEG.Count; ++ii)
            {
                m_Column3DViewManager.ColumnsIEEG[ii].SelectedSiteID = -1;
            }

            //####### UDPATE MODE
            m_ModesManager.updateMode(Mode.FunctionsId.resetElectrodesFile);
            //##################

            return true;
        }

        /// <summary>
        /// Define the timeline data with a patient list, a list of column data and the pts paths
        /// </summary>
        /// <param name="patientList"></param>
        /// <param name="columnDataList"></param>
        /// <param name="ptsPathFileList"></param>
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
            m_Column3DViewManager.SetTimelineData(Visualisation.Patients.ToList(), Visualisation.Columns);

            // update plots visibility
            m_Column3DViewManager.update_all_columns_sites_rendering(data_);

            // set flag
            data_.timelinesLoaded = true;

            AskROIUpdateEvent.Invoke(-1);

            // send data to UI
            send_IEEG_parameters_to_menu();

            //####### UDPATE MODE
            m_ModesManager.updateMode(Mode.FunctionsId.setTimelines);
            //##################
        }

        /// <summary>
        /// Load a patient in the SP scene
        /// </summary>
        /// <param name="idPatientSelected"></param>
        public void load_patient_in_SP_scene(int idPatientSelected, int idPlotSelected)
        {
            LoadSPSceneFromMP.Invoke(idPatientSelected);
            /*
            // retrieve patient plots nb
            int nbPlotsSpPatient = m_Column3DViewManager.DLLLoadedPatientsElectrodes.patient_sites_nb(idPatientSelected);

            // retrieve timeline size (time t)
            int size = m_Column3DViewManager.ColumnsIEEG[0].columnData.Values[0].Length;
           
            // compute start value id
            int startId = 0;
            for (int ii = 0; ii < idPatientSelected; ++ii)
                startId += m_Column3DViewManager.DLLLoadedPatientsElectrodes.patient_sites_nb(ii);

            // create new column data
            List<Data.Visualisation.ColumnData> columnDataList = new List<Data.Visualisation.ColumnData>(m_Column3DViewManager.ColumnsIEEG.Count);
            for (int ii = 0; ii < m_Column3DViewManager.ColumnsIEEG.Count; ++ii)
            {
                Data.Visualisation.ColumnData columnData = new Data.Visualisation.ColumnData();
                columnData.Label = m_Column3DViewManager.ColumnsIEEG[ii].Label;

                // copy iconic scenario reference
                columnData.IconicScenario = m_Column3DViewManager.ColumnsIEEG[ii].columnData.IconicScenario;

                // copy timeline reference
                columnData.TimeLine = m_Column3DViewManager.ColumnsIEEG[ii].columnData.TimeLine;

                // fill new mask
                columnData.PlotMask = new bool[nbPlotsSpPatient];
                for (int jj = 0; jj < nbPlotsSpPatient; ++jj)
                {
                    columnData.PlotMask[jj] = m_Column3DViewManager.ColumnsIEEG[ii].columnData.SiteMask[startId + jj];
                }

                // fill new values
                List<float[]> spColumnValues = new List<float[]>(nbPlotsSpPatient);

                for (int jj = 0; jj < nbPlotsSpPatient; ++jj)
                {
                    float[] valuesT = new float[size];
                    for (int kk = 0; kk < valuesT.Length; ++kk)
                    {
                        valuesT[kk] = m_Column3DViewManager.ColumnsIEEG[ii].columnData.Values[startId + jj][kk];
                    }

                    spColumnValues.Add(valuesT);
                }

                columnData.Values = spColumnValues.ToArray();
                columnDataList.Add(columnData);
            }


            // fill masks bo be send to sp
            List<List<bool>> blackListMask = new List<List<bool>>(m_Column3DViewManager.ColumnsIEEG.Count);
            List<List<bool>> excludedMask = new List<List<bool>>(m_Column3DViewManager.ColumnsIEEG.Count);
            List<List<bool>> hightLightedMask = new List<List<bool>>(m_Column3DViewManager.ColumnsIEEG.Count);
            for (int ii = 0; ii < m_Column3DViewManager.ColumnsIEEG.Count; ++ii)
            {
                blackListMask.Add(new List<bool>(nbPlotsSpPatient));
                excludedMask.Add(new List<bool>(nbPlotsSpPatient));
                hightLightedMask.Add(new List<bool>(nbPlotsSpPatient));
                
                for (int jj = 0; jj < nbPlotsSpPatient; ++jj)
                {
                    blackListMask[ii].Add(m_Column3DViewManager.ColumnsIEEG[ii].Sites[startId + jj].Information.IsBlackListed);
                    excludedMask[ii].Add(m_Column3DViewManager.ColumnsIEEG[ii].Sites[startId + jj].Information.IsExcluded);
                    hightLightedMask[ii].Add(m_Column3DViewManager.ColumnsIEEG[ii].Sites[startId + jj].Information.IsHighlighted);
                }
            }

            bool success = StaticComponents.HBPMain.SetSinglePatientSceneData(Patients[idPatientSelected], columnDataList, idPlotSelected, blackListMask, excludedMask, hightLightedMask);
            if(!success)
            {
                // TODO : reset SP scene
            }

            transform.parent.gameObject.GetComponent<ScenesManager>().setScenesVisibility(true, true);

            // update sp cameras
            ApplySceneCamerasToIndividualScene.Invoke();
            */
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
        /// Mouse scroll events managements, call Base3DScene parent function
        /// </summary>
        /// <param name="scrollDelta"></param>
        new public void mouse_scroll_action(Vector2 scrollDelta)
        {
            base.mouse_scroll_action(scrollDelta);
            change_size_ROI(scrollDelta.y);
        }

        private void change_size_ROI(float coeff)
        {
            int idC = m_Column3DViewManager.SelectedColumnID;
            if (data_.ROICreationMode) // ROI : change ROI size
            {
                ROI ROI = m_Column3DViewManager.Columns[idC].SelectedROI;
                if (ROI != null)
                {
                    ChangeSizeBubbleEvent.Invoke(idC, ROI.idSelectedBubble, (coeff < 0 ? 0.9f : 1.1f));
                    m_Column3DViewManager.update_all_columns_sites_rendering(data_);
                }
            }
        }

        /// <summary>
        /// Return true if the ROI mode is enabled (allow to switch mouse scroll effect between camera zoom and ROI size changes)
        /// </summary>
        /// <returns></returns>
        public bool is_ROI_mode_enabled()
        {
            return data_.ROICreationMode;
        }

        /// <summary>
        /// Keyboard events management, call Base3DScene parent function
        /// </summary>
        /// <param name="keyCode"></param>
        new public void keyboard_action(KeyCode keyCode)
        {
            base.keyboard_action(keyCode);
            switch (keyCode)
            {
                // choose active plane
                case KeyCode.Delete:                
                    if (data_.ROICreationMode)
                    {
                        int idC = m_Column3DViewManager.SelectedColumnID;

                        Column3DView col =  m_Column3DViewManager.SelectedColumn;                        
                        int idBubble = col.SelectedROI.idSelectedBubble;
                        if (idBubble != -1)
                            RemoveBubbleEvent.Invoke(idC, idBubble);

                        m_Column3DViewManager.update_all_columns_sites_rendering(data_);
                    }
                    break;
                case KeyCode.KeypadPlus:
                    change_size_ROI(0.2f);
                    break;
                case KeyCode.KeypadMinus:
                    change_size_ROI(-0.2f);
                    break;
            }
        }

        /// <summary>
        /// Manage the clicks event in the scene
        /// </summary>
        /// <param name="ray"></param>
        public override void click_on_scene(Ray ray)
        {
            // scene not loaded
            if (!data_.mriLoaded)
                return;

            // update colliders if necessary
            if (!data_.collidersUpdated)
                update_meshes_colliders();

            // retrieve layer
            int layerMask = 0;
            layerMask |= 1 << LayerMask.NameToLayer(m_Column3DViewManager.SelectedColumn.Layer);
            layerMask |= 1 << LayerMask.NameToLayer("Meshes_MP");

            // raycasts with current column plots/ROI and scene meshes
            RaycastHit[] hits = Physics.RaycastAll(ray.origin, ray.direction, Mathf.Infinity, layerMask);
            if (hits.Length == 0)
            {
                return; // no hits
            }

            // check plots hits
            bool meshHit = false, plotHit = false, ROIHit = false;
            int idClosestPlotHit = -1;
            float minDistance = float.MaxValue;
            for (int ii = 0; ii < hits.Length; ++ii)
            {
                if(hits[ii].transform.GetComponent<Site>() != null) // plot hit
                {
                    
                    if (minDistance > hits[ii].distance)
                    {
                        minDistance = hits[ii].distance;
                        idClosestPlotHit = ii;
                    }
                }
            }
            if (idClosestPlotHit != -1)
                plotHit = true;


            int idClosestNonPlot = -1;
            if (!plotHit) // no plot hit
            {
                minDistance = float.MaxValue;
                for (int ii = 0; ii < hits.Length; ++ii)
                {
                    if (minDistance > hits[ii].distance)
                    {
                        minDistance = hits[ii].distance;
                        idClosestNonPlot = ii;
                    }
                }

                if (idClosestNonPlot != -1) // mesh hit or ROI hit
                {                    
                    if (hits[idClosestNonPlot].transform.GetComponent<Bubble>() != null)
                    {
                        ROIHit = true;
                    }
                    else
                    {
                        ROIHit = false;
                    }

                    meshHit = !ROIHit;
                }
            }
            
            if(plotHit) // plot collision -> select PLOT
            {
                int idPatientSelected = 0;

                string namePatientClickedPlot = hits[idClosestPlotHit].collider.gameObject.GetComponent<Site>().Information.PatientName;

                for (int ii = 0; ii < Patients.Count; ++ii)
                {
                    string currentPatient = Patients[ii].Place + "_" + Patients[ii].Date + "_" + Patients[ii].Name;
                    if (currentPatient == namePatientClickedPlot)
                    {
                        idPatientSelected = ii;
                        break;
                    }
                }

                m_Column3DViewManager.idSelectedPatient = idPatientSelected;

                int idPlotGlobal = hits[idClosestPlotHit].collider.gameObject.GetComponent<Site>().Information.GlobalID;
                m_Column3DViewManager.SelectedColumn.SelectedSiteID = idPlotGlobal;

                m_Column3DViewManager.update_all_columns_sites_rendering(data_);
                ClickSite.Invoke(-1); // test

                return;
            }
            else
            {
                if (data_.ROICreationMode)
                {
                    if (ROIHit) // ROI collision -> select ROI
                    {
                        if (m_Column3DViewManager.SelectedColumn.SelectedROI.check_collision(ray))
                        {
                            int idClickedBubble = m_Column3DViewManager.SelectedColumn.SelectedROI.collided_closest_bubble_id(ray);
                            SelectBubbleEvent.Invoke(m_Column3DViewManager.SelectedColumnID, idClickedBubble);
                        }

                        return;
                    }
                    else if (meshHit) // mesh collision -> create newROI
                    {   
                        CreateBubbleEvent.Invoke(hits[idClosestNonPlot].point, m_Column3DViewManager.SelectedColumnID);

                        m_Column3DViewManager.update_all_columns_sites_rendering(data_);
                        return;
                    }
                }
                else
                {
                    RaycastHit hit;
                    bool isCollision = Physics.Raycast(ray.origin, ray.direction, out hit, Mathf.Infinity, layerMask);
                    if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Meshes_MP") && isCollision) // mesh hit
                    {
                        if (hit.collider.gameObject.name.StartsWith("cut")) // cut hit
                            return;

                        if (m_triEraser.is_enabled() && m_triEraser.is_click_available())
                        {
                            m_triEraser.erase_triangles(ray.direction, hit.point);
                            for (int ii = 0; ii < m_Column3DViewManager.DLLSplittedMeshesList.Count; ++ii)
                                m_Column3DViewManager.DLLSplittedMeshesList[ii].update_mesh_from_dll(go_.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);             
                        }

                        return;
                    }                    
                }
            }         
        }        

        /// <summary>
        /// Manage the mouse movement events in the scene
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

            if (m_Column3DViewManager.SelectedColumnID != idColumn) // not the selected column
                return;

            // retrieve layer
            int layerMask = 0;
            layerMask |= 1 << LayerMask.NameToLayer(m_Column3DViewManager.SelectedColumn.Layer);
            layerMask |= 1 << LayerMask.NameToLayer("Meshes_MP");

            // raycasts
            RaycastHit[] hits = Physics.RaycastAll(ray.origin, ray.direction, Mathf.Infinity, layerMask);
            if (hits.Length == 0)
            {
                UpdateDisplayedSitesInfo.Invoke(new SiteInfo(null, false, mousePosition, false));
                return;
            }

            // check the hits
            int idHitToKeep = -1;
            float minDistance = float.MaxValue;
            for(int ii = 0; ii < hits.Length; ++ii)
            {
                if (hits[ii].collider.GetComponent<Site>() == null) // not a plot hit
                    continue;

                if (minDistance > hits[ii].distance)
                {
                    minDistance = hits[ii].distance;
                    idHitToKeep = ii;
                }
            }

            if (idHitToKeep == -1) // not plot hit
            {
                UpdateDisplayedSitesInfo.Invoke(new SiteInfo(null, false, mousePosition, false));
                return;
            }

            if (hits[idHitToKeep].collider.transform.parent.name == "cuts" || hits[idHitToKeep].collider.transform.parent.name == "brains") // meshes hit
            {
                UpdateDisplayedSitesInfo.Invoke(new SiteInfo(null, false, mousePosition, false));
                return;
            }

            Site site = hits[idHitToKeep].collider.GetComponent<Site>();

            // fMRI
            switch (m_Column3DViewManager.SelectedColumn.Type)
            {
                case Column3DView.ColumnType.FMRI:
                    UpdateDisplayedSitesInfo.Invoke(new SiteInfo(site, true, mousePosition, true, false, hits[idHitToKeep].collider.GetComponent<Site>().Information.FullName));
                    break;
                case Column3DView.ColumnType.IEEG:
                    Column3DViewIEEG currIEEGCol = (Column3DViewIEEG)m_Column3DViewManager.SelectedColumn;

                    // retrieve current plot amp
                    float amp = 0;
                    if (currIEEGCol.columnData.Values.Length > 0)
                        amp = currIEEGCol.columnData.Values[site.Information.GlobalID][currIEEGCol.currentTimeLineID];

                    UpdateDisplayedSitesInfo.Invoke(new SiteInfo(site, true, mousePosition, m_Column3DViewManager.SelectedColumn.Type == Column3DView.ColumnType.FMRI, false, hits[idHitToKeep].collider.GetComponent<Site>().Information.FullName, "" + amp));
                    break;
                default:
                    break;
            }
        }

        public override void disable_site_display_window(int idColumn)
        {
            UpdateDisplayedSitesInfo.Invoke(new SiteInfo(null, false, new Vector3(0, 0, 0), false));
        }

        public void enable_ROI_creation_mode()
        {
            data_.ROICreationMode = true;
            Column3DViewManager.update_ROI_visibility(true);
        }

        public void disable_ROI_creatino_mode()
        {
            data_.ROICreationMode = false;
            Column3DViewManager.update_ROI_visibility(false);
        }

        /// <summary>
        /// Send additionnal site info to hight level UI
        /// </summary>
        public override void send_additionnal_site_info_request(Site previousPlot = null) // TODO deporter dans c manager
        {

            switch (m_Column3DViewManager.SelectedColumn.Type)
            {
                case Column3DView.ColumnType.FMRI:
                    return;
                case Column3DView.ColumnType.IEEG:
                    Column3DViewIEEG currIEEGCol = (Column3DViewIEEG)m_Column3DViewManager.SelectedColumn;

                    if (currIEEGCol.SelectedSiteID == -1)
                        return;

                    string[] elements = m_Column3DViewManager.SitesList[currIEEGCol.SelectedSiteID].GetComponent<Site>().Information.FullName.Split('_');

                    if (elements.Length < 3)
                        return;

                    int id = -1;
                    for (int ii = 0; ii < m_Visualisation.Patients.Count; ++ii)
                    {
                        if (m_Visualisation.Patients[ii].Name == elements[2])
                        {
                            id = ii;
                            break;
                        }
                    }

                    if (id == -1)
                    {
                        // ERROR
                        return;
                    }

                    List<List<bool>> masksColumnsData = new List<List<bool>>(m_Column3DViewManager.ColumnsIEEG.Count);
                    for (int ii = 0; ii < m_Column3DViewManager.ColumnsIEEG.Count; ++ii)
                    {
                        masksColumnsData.Add(new List<bool>(m_Column3DViewManager.ColumnsIEEG[ii].Sites.Count));

                        bool isROI = (m_Column3DViewManager.ColumnsIEEG[ii].SelectedROI.bubbles_nb() > 0);
                        for (int jj = 0; jj < m_Column3DViewManager.ColumnsIEEG[ii].Sites.Count; ++jj)
                        {
                            Site p = m_Column3DViewManager.ColumnsIEEG[ii].Sites[jj];
                            bool keep = (!p.Information.IsBlackListed && !p.Information.IsExcluded && !p.Information.IsMasked);
                            if (isROI)
                                keep = keep && !p.Information.IsInROI;

                            masksColumnsData[ii].Add(keep);
                        }
                    }

                    SiteRequest request = new SiteRequest();
                    request.spScene = false;
                    request.idSite1 = currIEEGCol.SelectedSiteID;
                    request.idSite2 = (previousPlot == null) ? -1 : previousPlot.Information.GlobalID;
                    request.idPatient = m_Visualisation.Patients[id].ID;
                    request.idPatient2 = (previousPlot == null) ? "" : m_Visualisation.Patients[previousPlot.Information.PatientID].ID;
                    request.maskColumn = masksColumnsData;
                    SiteInfoRequest.Invoke(request);
                    break;
                default:
                    break;
            }
        }

        public void update_current_ROI(int idColumn)
        {
            bool[] maskROI = new bool[m_Column3DViewManager.SitesList.Count];

            // update mask ROI
            for (int ii = 0; ii < maskROI.Length; ++ii)
                maskROI[ii] = m_Column3DViewManager.Columns[idColumn].Sites[ii].Information.IsInROI;

            m_Column3DViewManager.Columns[idColumn].SelectedROI.update_mask(m_Column3DViewManager.Columns[idColumn].RawElectrodes, maskROI);
            for (int ii = 0; ii < m_Column3DViewManager.Columns[idColumn].Sites.Count; ++ii)
                m_Column3DViewManager.Columns[idColumn].Sites[ii].Information.IsInROI = maskROI[ii];

            m_Column3DViewManager.update_all_columns_sites_rendering(data_);
        }

        /// <summary>
        /// Update the ROI of a column from the interface
        /// </summary>
        /// <param name="idColumn"></param>
        /// <param name="roi"></param>
        public void update_ROI(int idColumn, ROI roi)
        {
            m_Column3DViewManager.Columns[idColumn].update_ROI(roi);
            update_current_ROI(idColumn);
        }

        /// <summary>
        /// Return the string information of the current column ROI and plots states
        /// </summary>
        /// <returns></returns>
        public string get_current_column_ROI_and_sites_state_str()
        {
            Column3DView currentCol = m_Column3DViewManager.SelectedColumn;
            return "ROI :\n" +  currentCol.SelectedROI.ROIbubbulesInfos() + currentCol.site_state_str();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string get_sites_in_ROI()
        {
            Column3DView currentCol = m_Column3DViewManager.SelectedColumn;
            return "Sites in ROI:\n" + currentCol.SelectedROI.ROIbubbulesInfos() + currentCol.only_sites_in_ROI_str();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="idColumn"></param>
        /// <param name="plots"></param>
        /// <param name="patientsName"></param>
        public void update_sites_mask(int idColumn, List<List<List<SiteInformation>>> plots, List<string> patientsName)
        {
            // reset previous masks
            for (int ii = 0; ii < Column3DViewManager.Columns[idColumn].Sites.Count; ++ii)
            {
                Column3DViewManager.Columns[idColumn].Sites[ii].Information.IsExcluded = false;
                Column3DViewManager.Columns[idColumn].Sites[ii].Information.IsBlackListed = false;
                Column3DViewManager.Columns[idColumn].Sites[ii].Information.IsHighlighted = false;
                Column3DViewManager.Columns[idColumn].Sites[ii].Information.IsMasked = false;
            }

            // update masks
            for (int ii = 0; ii < Column3DViewManager.Columns[idColumn].Sites.Count; ii++)
            {
                for (int jj = 0; jj < plots.Count; ++jj) // patient
                {
                    if (patientsName[jj] != Column3DViewManager.Columns[idColumn].Sites[ii].Information.PatientName)
                        continue;                    

                    for (int kk = 0; kk < plots[jj].Count; kk++) // electrode
                    {
                        for(int ll = 0; ll < plots[jj][kk].Count; ll++) // plot
                        {
                            string namePlot = plots[jj][kk][ll].PatientName + "_" + plots[jj][kk][ll].FullName;
                            if (namePlot != Column3DViewManager.Columns[idColumn].Sites[ii].Information.FullName)
                                continue;

                            Column3DViewManager.Columns[idColumn].Sites[ii].Information.IsExcluded = plots[jj][kk][ll].IsExcluded;
                            Column3DViewManager.Columns[idColumn].Sites[ii].Information.IsBlackListed = plots[jj][kk][ll].IsBlackListed;
                            Column3DViewManager.Columns[idColumn].Sites[ii].Information.IsHighlighted = plots[jj][kk][ll].IsHighlighted;
                            Column3DViewManager.Columns[idColumn].Sites[ii].Information.IsMasked = plots[jj][kk][ll].IsMasked;
                        }
                    }

                }
            }
        }


        #endregion functions
    }
}