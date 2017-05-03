
/**
 * \file    MP3DScene.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define MP3DScene class
 */

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
        #region Properties
        public override SceneType Type
        {
            get
            {
                return SceneType.MultiPatients;
            }
        }

        public new Data.Visualisation.MultiPatientsVisualisation Visualisation
        {
            get
            {
                return Visualisation as Data.Visualisation.MultiPatientsVisualisation;
            }
            set
            {
                Visualisation = value;
            }
        }
        public ReadOnlyCollection<Data.Patient> Patients
        {
            get
            {
                return new ReadOnlyCollection<Data.Patient>(Visualisation.Patients);
            }
        }
        public AmbientMode AmbiantMode = AmbientMode.Flat;
        public float AmbientIntensity = 1;
        public Color AmbiantLight = new Color(0.2f, 0.2f, 0.2f, 1);

        // events
        public Events.SendColumnROI SendColumnROIEvent = new Events.SendColumnROI();
        public Events.CreateBubble CreateBubbleEvent = new Events.CreateBubble();
        public Events.SelectBubble SelectBubbleEvent = new Events.SelectBubble();    
        public Events.ChangeSizeBubble ChangeSizeBubbleEvent = new Events.ChangeSizeBubble();
        public Events.RemoveBubble RemoveBubbleEvent = new Events.RemoveBubble();
        public Events.ApplySceneCamerasToIndividualScene ApplySceneCamerasToIndividualScene = new Events.ApplySceneCamerasToIndividualScene();
        public Events.LoadSPSceneFromMP LoadSPSceneFromMP = new Events.LoadSPSceneFromMP();
        #endregion

        #region Private Methods
        new void Awake()
        {
            int idScript = TimeExecution.get_ID();
            TimeExecution.start_awake(idScript, TimeExecution.ScriptsId.MP3DScene);

            base.Awake();

            m_MNIObjects = GetComponent<MNIObjects>();
            
            TimeExecution.end_awake(idScript, TimeExecution.ScriptsId.MP3DScene, gameObject);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Update the meshes cuts
        /// </summary>
        public override void ComputeMeshesCut()
        {
            // choose the mesh
            SceneInformation.MeshToDisplay = null;
            switch (SceneInformation.MeshTypeToDisplay)
            {
                case SceneStatesInfo.MeshType.Hemi:
                    switch (SceneInformation.MeshPartToDisplay)
                    {
                        case SceneStatesInfo.MeshPart.Both:
                            SceneInformation.MeshToDisplay = m_MNIObjects.BothHemi;
                            break;
                        case SceneStatesInfo.MeshPart.Left:
                            SceneInformation.MeshToDisplay = m_MNIObjects.LeftHemi;
                            break;
                        case SceneStatesInfo.MeshPart.Right:
                            SceneInformation.MeshToDisplay = m_MNIObjects.RightHemi;
                            break;
                    }
                    break;
                case SceneStatesInfo.MeshType.White:
                    switch (SceneInformation.MeshPartToDisplay)
                    {
                        case SceneStatesInfo.MeshPart.Both:
                            SceneInformation.MeshToDisplay = m_MNIObjects.BothWhite;
                            break;
                        case SceneStatesInfo.MeshPart.Left:
                            SceneInformation.MeshToDisplay = m_MNIObjects.LeftWhite;
                            break;
                        case SceneStatesInfo.MeshPart.Right:
                            SceneInformation.MeshToDisplay = m_MNIObjects.RightWhite;
                            break;
                    }
                    break;
                case SceneStatesInfo.MeshType.Inflated:
                    switch (SceneInformation.MeshPartToDisplay)
                    {
                        case SceneStatesInfo.MeshPart.Both:
                            SceneInformation.MeshToDisplay = m_MNIObjects.BothWhiteInflated;
                            break;
                        case SceneStatesInfo.MeshPart.Left:
                            SceneInformation.MeshToDisplay = m_MNIObjects.LeftWhiteInflated;
                            break;
                        case SceneStatesInfo.MeshPart.Right:
                            SceneInformation.MeshToDisplay = m_MNIObjects.RightWhiteInflated;
                            break;
                    }
                    break;
            }

            if (SceneInformation.MeshTypeToDisplay == SceneStatesInfo.MeshType.Inflated)
                m_Column3DViewManager.DLLSplittedWhiteMeshesList = new List<DLL.Surface>(SceneInformation.MeshToDisplay.SplitToSurfaces(m_Column3DViewManager.MeshSplitNumber));

            // get the middle
            SceneInformation.MeshCenter = SceneInformation.MeshToDisplay.BoundingBox().Center();

            // cut the mesh
            List<DLL.Surface> cuts;
            if (SceneInformation.MeshTypeToDisplay != SceneStatesInfo.MeshType.Inflated && m_Column3DViewManager.PlanesCutsCopy.Count > 0)
                cuts = new List<DLL.Surface>(SceneInformation.MeshToDisplay.Cut(m_Column3DViewManager.PlanesCutsCopy.ToArray(), SceneInformation.RemoveFrontPlaneList.ToArray(), !SceneInformation.CutHolesEnabled));
            else
                cuts = new List<DLL.Surface>() { (DLL.Surface)(SceneInformation.MeshToDisplay.Clone())};

            if(m_Column3DViewManager.DLLCutsList.Count != cuts.Count)
                m_Column3DViewManager.DLLCutsList = cuts;
            else
            {
                // swap DLL pointer
                for (int ii = 0; ii < cuts.Count; ++ii)
                    m_Column3DViewManager.DLLCutsList[ii].SwapDLLHandle(cuts[ii]);
            }

            // split the mesh
            List<DLL.Surface> splits = new List<DLL.Surface>(m_Column3DViewManager.DLLCutsList[0].SplitToSurfaces(m_Column3DViewManager.MeshSplitNumber));
            if (m_Column3DViewManager.DLLSplittedMeshesList.Count != splits.Count)
                m_Column3DViewManager.DLLSplittedMeshesList = splits;
            else
            {
                // swap DLL pointer
                for (int ii = 0; ii < splits.Count; ++ii)
                    m_Column3DViewManager.DLLSplittedMeshesList[ii].SwapDLLHandle(splits[ii]);
            }

            // reset brain texture generator
            for (int ii = 0; ii < m_Column3DViewManager.DLLSplittedMeshesList.Count; ++ii)
            {
                m_Column3DViewManager.DLLCommonBrainTextureGeneratorList[ii].Reset(m_Column3DViewManager.DLLSplittedMeshesList[ii], m_Column3DViewManager.DLLVolume);
                m_Column3DViewManager.DLLCommonBrainTextureGeneratorList[ii].ComputeUVMainWithVolume(m_Column3DViewManager.DLLSplittedMeshesList[ii], m_Column3DViewManager.DLLVolume, m_Column3DViewManager.MRICalMinFactor, m_Column3DViewManager.MRICalMaxFactor);
            }

            // reset tri eraser
            ResetTriangleErasing(false);

            // update cut brain mesh object mesh filter
            for (int ii = 0; ii < m_Column3DViewManager.DLLSplittedMeshesList.Count; ++ii)
                m_Column3DViewManager.DLLSplittedMeshesList[ii].UpdateMeshFromDLL(m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);

            // update cuts generators
            for (int ii = 0; ii < m_Column3DViewManager.PlanesCutsCopy.Count; ++ii)
            {
                if (SceneInformation.MeshTypeToDisplay == SceneStatesInfo.MeshType.Inflated) // if inflated, there is not cuts
                {
                    m_DisplayedObjects.BrainCutMeshes[ii].SetActive(false);
                    continue;
                }

                for (int jj = 0; jj < m_Column3DViewManager.ColumnsIEEG.Count; ++jj)
                    m_Column3DViewManager.DLLMRIGeometryCutGeneratorList[ii].Reset(m_Column3DViewManager.DLLVolume, m_Column3DViewManager.PlanesCutsCopy[ii]);

                m_Column3DViewManager.DLLMRIGeometryCutGeneratorList[ii].UpdateCutMeshUV(Column3DViewManager.DLLCutsList[ii + 1]);
                m_Column3DViewManager.DLLCutsList[ii + 1].UpdateMeshFromDLL(m_DisplayedObjects.BrainCutMeshes[ii].GetComponent<MeshFilter>().mesh);
                m_DisplayedObjects.BrainCutMeshes[ii].SetActive(true); // enable cuts gameobject
            }

            // create null uv2/uv3 arrays
            m_Column3DViewManager.UVNull = new List<Vector2[]>(m_Column3DViewManager.MeshSplitNumber);
            for (int ii = 0; ii < m_Column3DViewManager.MeshSplitNumber; ++ii)
            {
                m_Column3DViewManager.UVNull.Add(new Vector2[m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh.vertexCount]);
                Extensions.ArrayExtensions.Fill(m_Column3DViewManager.UVNull[ii], new Vector2(0.01f, 1f));
            }

            SceneInformation.CollidersUpdated = false; // colliders are now longer up to date */
            SceneInformation.CutMeshGeometryNeedsUpdate = false;   // planes are now longer requested to be updated 
            SceneInformation.IsGeneratorUpToDate = false; // generator is not up to date anymore

            // update amplitude for all columns
            for (int ii = 0; ii < m_Column3DViewManager.ColumnsIEEG.Count; ++ii)
                m_Column3DViewManager.ColumnsIEEG[ii].UpdateIEEG = true;
        }
        /// <summary>
        /// Reset the scene : reload MRI, sites, and regenerate textures
        /// </summary>
        /// <param name="data"></param>
        public bool Initialize(Data.Visualisation.MultiPatientsVisualisation visualisation)
        {
            m_ModesManager.UpdateMode(Mode.FunctionsId.ResetScene);
            Visualisation = visualisation;

            // MNI meshes are preloaded
            SceneInformation.VolumeCenter = m_MNIObjects.IRM.Center();
            SceneInformation.MeshesLoaded = true;
            SceneInformation.HemiMeshesAvailables = true;
            SceneInformation.WhiteMeshesAvailables = true;
            SceneInformation.WhiteInflatedMeshesAvailables = true;
            SceneInformation.IsROICreationModeEnabled = false;

            // get the middle
            SceneInformation.MeshCenter = m_MNIObjects.BothHemi.BoundingBox().Center();

            List<string> ptsFiles = new List<string>(Visualisation.GetImplantation().Length), namePatients = new List<string>(Visualisation.GetImplantation().Length);
            for (int ii = 0; ii < Visualisation.GetImplantation().Length; ++ii)
            {
                ptsFiles.Add(Visualisation.GetImplantation()[ii]);
                namePatients.Add(Visualisation.Patients[ii].Place + "_" + Visualisation.Patients[ii].Date + "_" + Visualisation.Patients[ii].Name);
            }

            // reset columns
            m_Column3DViewManager.DLLVolume = null; // this object must no be reseted
            m_Column3DViewManager.Initialize(PlanesList.Count);

            // retrieve MNI IRM volume
            m_Column3DViewManager.DLLVolume = m_MNIObjects.IRM;
            SceneInformation.VolumeCenter = m_Column3DViewManager.DLLVolume.Center();
            SceneInformation.MRILoaded = true;
            UpdatePlanes.Invoke();


            // send cal values to the UI
            IRMCalValuesUpdate.Invoke(m_Column3DViewManager.DLLVolume.RetrieveExtremeValues());


            // update scenes cameras
            UpdateCameraTarget.Invoke(m_MNIObjects.BothHemi.BoundingBox().Center());


            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.ResetNIIBrainVolumeFile);
            //##################

            // reset electrodes
            bool success = ResetElectrodesFiles(ptsFiles, namePatients);

            // define meshes splits nb
            ResetSplitsNumber(3);

            if (success)
                SceneInformation.CutMeshGeometryNeedsUpdate = true;

            if (!success)
            {
                Debug.LogError("-ERROR : MP3DScene : reset failed. ");
                SceneInformation.Reset();
                m_Column3DViewManager.Initialize(PlanesList.Count);
                ResetSceneGameObjects();                
                return false;
            }

            SetTimelineData();
            UpdateSelectedColumn(0);

            DisplayScreenMessage("Multi Patients Scene loaded", 2.0f, 400, 80);
            return true;
        }
        /// <summary>
        /// Reset all the sites with a new list of pts files
        /// </summary>
        /// <param name="pathsElectrodesPtsFile"></param>
        /// <returns></returns>
        public bool ResetElectrodesFiles(List<string> pathsElectrodesPtsFile, List<string> names)
        {
            //####### CHECK ACESS
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.ResetElectrodesFile))
            {
                Debug.LogError("-ERROR : MultiPatients3DScene::resetElectrodesFile -> no acess for mode : " + m_ModesManager.CurrentModeName);
                return false;
            }
            //##################

            // load list of pts files
            SceneInformation.SitesLoaded = m_Column3DViewManager.DLLLoadedPatientsElectrodes.LoadPTSFiles(pathsElectrodesPtsFile, names, GlobalGOPreloaded.MarsAtlasIndex);

            if (!SceneInformation.SitesLoaded)
                return false;

            // destroy previous electrodes gameobject
            for (int ii = 0; ii < m_Column3DViewManager.SitesList.Count; ++ii)
            {
                // destroy material
                Destroy(m_Column3DViewManager.SitesList[ii]);
            }
            m_Column3DViewManager.SitesList.Clear();

            // destroy plots elecs/patients parents
            for (int ii = 0; ii < m_Column3DViewManager.SitesPatientParent.Count; ++ii)
            {
                Destroy(m_Column3DViewManager.SitesPatientParent[ii]);
                for (int jj = 0; jj < m_Column3DViewManager.SitesElectrodesParent[ii].Count; ++jj)
                {
                    Destroy(m_Column3DViewManager.SitesElectrodesParent[ii][jj]);
                }

            }
            m_Column3DViewManager.SitesPatientParent.Clear();
            m_Column3DViewManager.SitesElectrodesParent.Clear();

            if (SceneInformation.SitesLoaded)
            {
                int currPlotNb = 0;
                for (int ii = 0; ii < m_Column3DViewManager.DLLLoadedPatientsElectrodes.NumberOfPatients(); ++ii)
                {
                    int idPlotPatient = 0;
                    string patientName = m_Column3DViewManager.DLLLoadedPatientsElectrodes.PatientName(ii);


                    // create plot patient parent
                    m_Column3DViewManager.SitesPatientParent.Add(new GameObject("P" + ii + " - " + patientName));
                    m_Column3DViewManager.SitesPatientParent[m_Column3DViewManager.SitesPatientParent.Count - 1].transform.SetParent(m_DisplayedObjects.SitesMeshesParent.transform);
                    m_Column3DViewManager.SitesElectrodesParent.Add(new List<GameObject>(m_Column3DViewManager.DLLLoadedPatientsElectrodes.NumberOfElectrodesInPatient(ii)));


                    for (int jj = 0; jj < m_Column3DViewManager.DLLLoadedPatientsElectrodes.NumberOfElectrodesInPatient(ii); ++jj)
                    {
                        // create plot electrode parent
                        m_Column3DViewManager.SitesElectrodesParent[ii].Add(new GameObject(m_Column3DViewManager.DLLLoadedPatientsElectrodes.ElectrodeName(ii, jj)));
                        m_Column3DViewManager.SitesElectrodesParent[ii][m_Column3DViewManager.SitesElectrodesParent[ii].Count - 1].transform.SetParent(m_Column3DViewManager.SitesPatientParent[ii].transform);


                        for (int kk = 0; kk < m_Column3DViewManager.DLLLoadedPatientsElectrodes.NumberOfSitesInElectrode(ii, jj); ++kk)
                        {
                            GameObject siteGO = Instantiate(GlobalGOPreloaded.Plot);
                            siteGO.name = m_Column3DViewManager.DLLLoadedPatientsElectrodes.SiteName(ii, jj, kk);
                            //brainElectrode.name = "????? " + cm_.DLLLoadedPatientsElectrodes.getPlotName(ii, jj, kk);
                                     
                            Vector3 posInverted = m_Column3DViewManager.DLLLoadedPatientsElectrodes.SitePosition(ii, jj, kk);
                            posInverted.x = -posInverted.x;
                            siteGO.transform.position = posInverted;
                            siteGO.transform.SetParent(m_Column3DViewManager.SitesElectrodesParent[ii][jj].transform);

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
                            site.Information.MarsAtlasIndex = m_Column3DViewManager.DLLLoadedPatientsElectrodes.MarsAtlasLabelOfSite(ii, jj, kk);//
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
            m_ModesManager.UpdateMode(Mode.FunctionsId.ResetElectrodesFile);
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
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.SetTimelines))
            {
                Debug.LogError("-ERROR : Base3DScene::setTimelines -> no acess for mode : " + m_ModesManager.CurrentModeName);
                return;
            }
            //##################

            // update columns number
            m_Column3DViewManager.UpdateColumnsNumber(Visualisation.Columns.Count, 0, PlanesList.Count);

            // update columns names
            for (int ii = 0; ii < Visualisation.Columns.Count; ++ii)
            {
                m_Column3DViewManager.ColumnsIEEG[ii].Label = Visualisation.Columns[ii].Label;
            }            

            // set timelines
            m_Column3DViewManager.SetTimelineData(Visualisation.Patients.ToList(), Visualisation.Columns);

            // update plots visibility
            m_Column3DViewManager.UpdateAllColumnsSitesRendering(SceneInformation);

            // set flag
            SceneInformation.TimelinesLoaded = true;

            AskROIUpdateEvent.Invoke(-1);

            // send data to UI
            SendIEEGParametersToMenu();

            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.SetTimelines);
            //##################
        }
        /// <summary>
        /// Load a patient in the SP scene
        /// </summary>
        /// <param name="idPatientSelected"></param>
        public void LoadPatientInSinglePatientScene(int idPatientSelected, int idPlotSelected)
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
        public override void ResetRenderingSettings(Vector3 cameraRotation)
        {
            RenderSettings.ambientMode = AmbiantMode;
            RenderSettings.ambientIntensity = AmbientIntensity;
            RenderSettings.skybox = null;
            RenderSettings.ambientLight = AmbiantLight;
            m_DisplayedObjects.SharedDirectionalLight.GetComponent<Transform>().eulerAngles = cameraRotation;
        }
        /// <summary>
        /// Mouse scroll events managements, call Base3DScene parent function
        /// </summary>
        /// <param name="scrollDelta"></param>
        public new void MouseScrollAction(Vector2 scrollDelta)
        {
            base.MouseScrollAction(scrollDelta);
            ChangeRegionOfInterestSize(scrollDelta.y);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="coeff"></param>
        private void ChangeRegionOfInterestSize(float coeff)
        {
            int idC = m_Column3DViewManager.SelectedColumnID;
            if (SceneInformation.IsROICreationModeEnabled) // ROI : change ROI size
            {
                ROI ROI = m_Column3DViewManager.Columns[idC].SelectedROI;
                if (ROI != null)
                {
                    ChangeSizeBubbleEvent.Invoke(idC, ROI.SelectedBubbleID, (coeff < 0 ? 0.9f : 1.1f));
                    m_Column3DViewManager.UpdateAllColumnsSitesRendering(SceneInformation);
                }
            }
        }
        /// <summary>
        /// Return true if the ROI mode is enabled (allow to switch mouse scroll effect between camera zoom and ROI size changes)
        /// </summary>
        /// <returns></returns>
        public bool IsRegionOfInterestModeEnabled()
        {
            return SceneInformation.IsROICreationModeEnabled;
        }
        /// <summary>
        /// Keyboard events management, call Base3DScene parent function
        /// </summary>
        /// <param name="keyCode"></param>
        public new void KeyboardAction(KeyCode keyCode)
        {
            base.KeyboardAction(keyCode);
            switch (keyCode)
            {
                // choose active plane
                case KeyCode.Delete:                
                    if (SceneInformation.IsROICreationModeEnabled)
                    {
                        int idC = m_Column3DViewManager.SelectedColumnID;

                        Column3DView col =  m_Column3DViewManager.SelectedColumn;                        
                        int idBubble = col.SelectedROI.SelectedBubbleID;
                        if (idBubble != -1)
                            RemoveBubbleEvent.Invoke(idC, idBubble);

                        m_Column3DViewManager.UpdateAllColumnsSitesRendering(SceneInformation);
                    }
                    break;
                case KeyCode.KeypadPlus:
                    ChangeRegionOfInterestSize(0.2f);
                    break;
                case KeyCode.KeypadMinus:
                    ChangeRegionOfInterestSize(-0.2f);
                    break;
            }
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

            // update colliders if necessary
            if (!SceneInformation.CollidersUpdated)
                UpdateMeshesColliders();

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

                m_Column3DViewManager.SelectedPatientID = idPatientSelected;

                int idPlotGlobal = hits[idClosestPlotHit].collider.gameObject.GetComponent<Site>().Information.GlobalID;
                m_Column3DViewManager.SelectedColumn.SelectedSiteID = idPlotGlobal;

                m_Column3DViewManager.UpdateAllColumnsSitesRendering(SceneInformation);
                ClickSite.Invoke(-1); // test

                return;
            }
            else
            {
                if (SceneInformation.IsROICreationModeEnabled)
                {
                    if (ROIHit) // ROI collision -> select ROI
                    {
                        if (m_Column3DViewManager.SelectedColumn.SelectedROI.CheckCollision(ray))
                        {
                            int idClickedBubble = m_Column3DViewManager.SelectedColumn.SelectedROI.CollidedClosestBubbleID(ray);
                            SelectBubbleEvent.Invoke(m_Column3DViewManager.SelectedColumnID, idClickedBubble);
                        }

                        return;
                    }
                    else if (meshHit) // mesh collision -> create newROI
                    {   
                        CreateBubbleEvent.Invoke(hits[idClosestNonPlot].point, m_Column3DViewManager.SelectedColumnID);

                        m_Column3DViewManager.UpdateAllColumnsSitesRendering(SceneInformation);
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

                        if (m_TriEraser.IsEnabled && m_TriEraser.IsClickAvailable())
                        {
                            m_TriEraser.EraseTriangles(ray.direction, hit.point);
                            for (int ii = 0; ii < m_Column3DViewManager.DLLSplittedMeshesList.Count; ++ii)
                                m_Column3DViewManager.DLLSplittedMeshesList[ii].UpdateMeshFromDLL(m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);             
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
        public override void MoveMouseOnScene(Ray ray, Vector3 mousePosition, int idColumn)
        {
            // scene not loaded
            if (!SceneInformation.MRILoaded)
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

            switch (m_Column3DViewManager.SelectedColumn.Type)
            {
                case Column3DView.ColumnType.FMRI:
                    UpdateDisplayedSitesInfo.Invoke(new SiteInfo(site, true, mousePosition, true, false, hits[idHitToKeep].collider.GetComponent<Site>().Information.FullName));
                    break;
                case Column3DView.ColumnType.IEEG:
                    Column3DViewIEEG currIEEGCol = (Column3DViewIEEG)m_Column3DViewManager.SelectedColumn;

                    // retrieve current plot amp
                    float amp = 0;
                    if (currIEEGCol.Column.Values.Length > 0)
                        amp = currIEEGCol.Column.Values[site.Information.GlobalID][currIEEGCol.CurrentTimeLineID];

                    UpdateDisplayedSitesInfo.Invoke(new SiteInfo(site, true, mousePosition, m_Column3DViewManager.SelectedColumn.Type == Column3DView.ColumnType.FMRI, false, hits[idHitToKeep].collider.GetComponent<Site>().Information.FullName, "" + amp));
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
        /// 
        /// </summary>
        public void EnableRegionOfInterestCreationMode()
        {
            SceneInformation.IsROICreationModeEnabled = true;
            Column3DViewManager.UpdateROIVisibility(true);
        }
        /// <summary>
        /// 
        /// </summary>
        public void DisableRegionOfInterestCreationMode()
        {
            SceneInformation.IsROICreationModeEnabled = false;
            Column3DViewManager.UpdateROIVisibility(false);
        }
        /// <summary>
        /// Send additionnal site info to hight level UI
        /// </summary>
        public override void SendAdditionalSiteInfoRequest(Site previousPlot = null) // TODO deporter dans c manager
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
                    for (int ii = 0; ii < Visualisation.Patients.Count; ++ii)
                    {
                        if (Visualisation.Patients[ii].Name == elements[2])
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

                        bool isROI = (m_Column3DViewManager.ColumnsIEEG[ii].SelectedROI.NumberOfBubbles() > 0);
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
                    request.idPatient = Visualisation.Patients[id].ID;
                    request.idPatient2 = (previousPlot == null) ? "" : Visualisation.Patients[previousPlot.Information.PatientID].ID;
                    request.maskColumn = masksColumnsData;
                    SiteInfoRequest.Invoke(request);
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idColumn"></param>
        public void UpdateCurrentRegionOfInterest(int idColumn)
        {
            bool[] maskROI = new bool[m_Column3DViewManager.SitesList.Count];

            // update mask ROI
            for (int ii = 0; ii < maskROI.Length; ++ii)
                maskROI[ii] = m_Column3DViewManager.Columns[idColumn].Sites[ii].Information.IsInROI;

            m_Column3DViewManager.Columns[idColumn].SelectedROI.UpdateMask(m_Column3DViewManager.Columns[idColumn].RawElectrodes, maskROI);
            for (int ii = 0; ii < m_Column3DViewManager.Columns[idColumn].Sites.Count; ++ii)
                m_Column3DViewManager.Columns[idColumn].Sites[ii].Information.IsInROI = maskROI[ii];

            m_Column3DViewManager.UpdateAllColumnsSitesRendering(SceneInformation);
        }
        /// <summary>
        /// Update the ROI of a column from the interface
        /// </summary>
        /// <param name="idColumn"></param>
        /// <param name="roi"></param>
        public void UpdateRegionOfInterest(int idColumn, ROI roi)
        {
            m_Column3DViewManager.Columns[idColumn].UpdateROI(roi);
            UpdateCurrentRegionOfInterest(idColumn);
        }
        /// <summary>
        /// Return the string information of the current column ROI and plots states
        /// </summary>
        /// <returns></returns>
        public string GetCurrentColumnRegionOfInterestAndSitesStatesIntoString()
        {
            Column3DView currentCol = m_Column3DViewManager.SelectedColumn;
            return "ROI :\n" +  currentCol.SelectedROI.BubblesInformationIntoString() + currentCol.SiteStatesIntoString();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetSitesInRegionOfInterestIntoString()
        {
            Column3DView currentCol = m_Column3DViewManager.SelectedColumn;
            return "Sites in ROI:\n" + currentCol.SelectedROI.BubblesInformationIntoString() + currentCol.OnlySitesInROIIntoString();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idColumn"></param>
        /// <param name="plots"></param>
        /// <param name="patientsName"></param>
        public void UpdateSitesMasks(int idColumn, List<List<List<SiteInformation>>> plots, List<string> patientsName)
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
        #endregion
    }
}