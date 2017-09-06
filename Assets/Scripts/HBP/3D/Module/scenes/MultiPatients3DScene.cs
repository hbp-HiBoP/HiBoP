


using CielaSpike;
/**
* \file    MP3DScene.cs
* \author  Lance Florian
* \date    2015
* \brief   Define MP3DScene class
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
// unity
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;


namespace HBP.Module3D
{
    /// <summary>
    /// The multi patients scene class, inheritance of Base3DScene.
    /// </summary>
    [AddComponentMenu("Scenes/Multi Patients 3D Scene")]
    public class MultiPatients3DScene : Base3DScene
    {
        #region Properties
        /// <summary>
        /// Type of the scene
        /// </summary>
        public override SceneType Type
        {
            get
            {
                return SceneType.MultiPatients;
            }
        }


        /// <summary>
        /// Event for sending a ROI associated to a column id (params : ROI, idColumn)
        /// </summary>
        public GenericEvent<ROI, int> SendColumnROIEvent = new GenericEvent<ROI, int>();
        /// <summary>
        /// Event for creating a new bubble to a column id with a position  (params : position, idColumn)
        /// </summary>
        public GenericEvent<Vector3, int> CreateBubbleEvent = new GenericEvent<Vector3, int>();
        /// <summary>
        /// Event for selecting a bubble of a column (params : idColumn, idBubble)
        /// </summary>
        public GenericEvent<int, int> SelectBubbleEvent = new GenericEvent<int, int>();
        /// <summary>
        /// Event for changing the size of a bubble (params : idColumn, idBubble, size)
        /// </summary>
        public GenericEvent<int, int, float> ChangeSizeBubbleEvent = new GenericEvent<int, int, float>();
        /// <summary>
        /// Event for removing a bubble (params : idColumn, idBubble)
        /// </summary>
        public GenericEvent<int, int> RemoveBubbleEvent = new GenericEvent<int, int>();
        /// <summary>
        /// Ask the UI to update set the same cameras to the individual scene
        /// </summary>
        public UnityEvent ApplySceneCamerasToIndividualScene = new UnityEvent();
        /// <summary>
        /// Invoked whend we load a single patient scene from the mutli patients scene (params : id patient)
        /// </summary>        
        public GenericEvent<Data.Visualization.Visualization, Data.Patient> OnLoadSinglePatientSceneFromMultiPatientsScene = new GenericEvent<Data.Visualization.Visualization, Data.Patient>();

        private const float LOADING_MESHES_PROGRESS = 0.8f;
        private const float LOADING_COLUMNS_PROGRESS = 0.03f;
        private const float LOADING_ELECTRODES_PROGRESS = 0.03f;
        private const float SETTING_TIMELINE_PROGRESS = 0.14f;
        #endregion
        
        #region Public Methods
        public new void Initialize(Data.Visualization.Visualization visualization)
        {
            int idScript = TimeExecution.ID;
            TimeExecution.StartAwake(idScript, TimeExecution.ScriptsId.MP3DScene);

            base.Initialize(visualization);

            TimeExecution.EndAwake(idScript, TimeExecution.ScriptsId.MP3DScene, gameObject);
        }
        /// <summary>
        /// Update the meshes cuts
        /// </summary>
        public override void ComputeMeshesCut()
        {
            // choose the mesh
            SceneInformation.MeshToDisplay = new DLL.Surface();
            if (m_ColumnManager.SelectedMesh is LeftRightMesh3D)
            {
                LeftRightMesh3D selectedMesh = (LeftRightMesh3D)m_ColumnManager.SelectedMesh;
                switch (SceneInformation.MeshPartToDisplay)
                {
                    case SceneStatesInfo.MeshPart.Left:
                        SceneInformation.MeshToDisplay = selectedMesh.Left;
                        break;
                    case SceneStatesInfo.MeshPart.Right:
                        SceneInformation.MeshToDisplay = selectedMesh.Right;
                        break;
                    case SceneStatesInfo.MeshPart.Both:
                        SceneInformation.MeshToDisplay = selectedMesh.Both;
                        break;
                    default:
                        SceneInformation.MeshToDisplay = selectedMesh.Both;
                        break;
                }
            }
            else
            {
                SceneInformation.MeshToDisplay = m_ColumnManager.SelectedMesh.Both;
            }

            if (SceneInformation.MeshToDisplay == null) return;
            
            m_ColumnManager.SelectedMesh.SplittedMeshes = new List<DLL.Surface>(SceneInformation.MeshToDisplay.SplitToSurfaces(m_ColumnManager.MeshSplitNumber));

            // get the middle
            SceneInformation.MeshCenter = SceneInformation.MeshToDisplay.BoundingBox.Center;

            // cut the mesh
            List<DLL.Surface> cuts;
            if (Cuts.Count > 0)
                cuts = new List<DLL.Surface>(SceneInformation.MeshToDisplay.Cut(Cuts.ToArray(), !SceneInformation.CutHolesEnabled));
            else
                cuts = new List<DLL.Surface>() { (DLL.Surface)(SceneInformation.MeshToDisplay.Clone())};

            if(m_ColumnManager.DLLCutsList.Count != cuts.Count)
                m_ColumnManager.DLLCutsList = cuts;
            else
            {
                // swap DLL pointer
                for (int ii = 0; ii < cuts.Count; ++ii)
                    m_ColumnManager.DLLCutsList[ii].SwapDLLHandle(cuts[ii]);
            }

            // split the mesh
            List<DLL.Surface> splits = new List<DLL.Surface>(m_ColumnManager.DLLCutsList[0].SplitToSurfaces(m_ColumnManager.MeshSplitNumber));
            if (m_ColumnManager.SelectedMesh.SplittedMeshes.Count != splits.Count)
                m_ColumnManager.SelectedMesh.SplittedMeshes = splits;
            else
            {
                // swap DLL pointer
                for (int ii = 0; ii < splits.Count; ++ii)
                    m_ColumnManager.SelectedMesh.SplittedMeshes[ii].SwapDLLHandle(splits[ii]);
            }

            // reset brain texture generator
            for (int ii = 0; ii < m_ColumnManager.SelectedMesh.SplittedMeshes.Count; ++ii)
            {
                m_ColumnManager.DLLCommonBrainTextureGeneratorList[ii].Reset(m_ColumnManager.SelectedMesh.SplittedMeshes[ii], m_ColumnManager.DLLVolume);
                m_ColumnManager.DLLCommonBrainTextureGeneratorList[ii].ComputeUVMainWithVolume(m_ColumnManager.SelectedMesh.SplittedMeshes[ii], m_ColumnManager.DLLVolume, m_ColumnManager.MRICalMinFactor, m_ColumnManager.MRICalMaxFactor);
            }

            // reset tri eraser
            ResetTriangleErasing(false);

            // update cut brain mesh object mesh filter
            UpdateMeshesFromDLL();

            // update cuts generators
            for (int ii = 0; ii < Cuts.Count; ++ii)
            {
                for (int jj = 0; jj < m_ColumnManager.ColumnsIEEG.Count; ++jj)
                    m_ColumnManager.DLLMRIGeometryCutGeneratorList[ii].Reset(m_ColumnManager.DLLVolume, Cuts[ii]);

                m_ColumnManager.DLLMRIGeometryCutGeneratorList[ii].UpdateCutMeshUV(ColumnManager.DLLCutsList[ii + 1]);
                m_ColumnManager.DLLCutsList[ii + 1].UpdateMeshFromDLL(m_DisplayedObjects.BrainCutMeshes[ii].GetComponent<MeshFilter>().mesh);
                m_DisplayedObjects.BrainCutMeshes[ii].SetActive(true); // enable cuts gameobject
            }

            // create null uv2/uv3 arrays
            m_ColumnManager.UVNull = new List<Vector2[]>(m_ColumnManager.MeshSplitNumber);
            for (int ii = 0; ii < m_ColumnManager.MeshSplitNumber; ++ii)
            {
                m_ColumnManager.UVNull.Add(new Vector2[m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh.vertexCount]);
                Extensions.ArrayExtensions.Fill(m_ColumnManager.UVNull[ii], new Vector2(0.01f, 1f));
            }

            SceneInformation.CollidersUpdated = false; // colliders are now longer up to date */
            SceneInformation.CutMeshGeometryNeedsUpdate = false;   // planes are now longer requested to be updated 
            SceneInformation.IsGeneratorUpToDate = false; // generator is not up to date anymore

            // update amplitude for all columns
            for (int ii = 0; ii < m_ColumnManager.ColumnsIEEG.Count; ++ii)
                m_ColumnManager.ColumnsIEEG[ii].UpdateIEEG = true;
        }
        /// <summary>
        /// Load a patient in the SP scene
        /// </summary>
        /// <param name="idPatientSelected"></param>
        public void LoadPatientInSinglePatientScene(Data.Visualization.Visualization visualization, Data.Patient patient, int idPlotSelected)
        {
            OnLoadSinglePatientSceneFromMultiPatientsScene.Invoke(visualization, patient);
            ApplySceneCamerasToIndividualScene.Invoke();
        }
        /// <summary>
        /// Update the ROI of a column from the interface
        /// </summary>
        /// <param name="idColumn"></param>
        /// <param name="roi"></param>
        public void UpdateRegionOfInterest(Column3D column, ROI roi)
        {
            column.SelectedROI = roi;
            UpdateCurrentRegionOfInterest(column);
        }
        /// <summary>
        /// Return the string information of the current column ROI and plots states
        /// </summary>
        /// <returns></returns>
        public string GetCurrentColumnRegionOfInterestAndSitesStatesIntoString()
        {
            Column3D currentCol = m_ColumnManager.SelectedColumn;
            return "ROI :\n" +  currentCol.SelectedROI.BubblesInformationIntoString() + currentCol.SiteStatesIntoString();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetSitesInRegionOfInterestIntoString()
        {
            Column3D currentCol = m_ColumnManager.SelectedColumn;
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
            for (int ii = 0; ii < ColumnManager.Columns[idColumn].Sites.Count; ++ii)
            {
                ColumnManager.Columns[idColumn].Sites[ii].Information.IsExcluded = false;
                ColumnManager.Columns[idColumn].Sites[ii].Information.IsBlackListed = false;
                ColumnManager.Columns[idColumn].Sites[ii].Information.IsHighlighted = false;
                ColumnManager.Columns[idColumn].Sites[ii].Information.IsMasked = false;
            }

            // update masks
            for (int ii = 0; ii < ColumnManager.Columns[idColumn].Sites.Count; ii++)
            {
                for (int jj = 0; jj < plots.Count; ++jj) // patient
                {
                    if (patientsName[jj] != ColumnManager.Columns[idColumn].Sites[ii].Information.PatientName)
                        continue;                    

                    for (int kk = 0; kk < plots[jj].Count; kk++) // electrode
                    {
                        for(int ll = 0; ll < plots[jj][kk].Count; ll++) // plot
                        {
                            string namePlot = plots[jj][kk][ll].PatientName + "_" + plots[jj][kk][ll].FullName;
                            if (namePlot != ColumnManager.Columns[idColumn].Sites[ii].Information.FullName)
                                continue;

                            ColumnManager.Columns[idColumn].Sites[ii].Information.IsExcluded = plots[jj][kk][ll].IsExcluded;
                            ColumnManager.Columns[idColumn].Sites[ii].Information.IsBlackListed = plots[jj][kk][ll].IsBlackListed;
                            ColumnManager.Columns[idColumn].Sites[ii].Information.IsHighlighted = plots[jj][kk][ll].IsHighlighted;
                            ColumnManager.Columns[idColumn].Sites[ii].Information.IsMasked = plots[jj][kk][ll].IsMasked;
                        }
                    }

                }
            }
        }
        #endregion

        #region Coroutines
        /// <summary>
        /// Reset the scene : reload MRI, sites, and regenerate textures
        /// </summary>
        /// <param name="data"></param>
        public IEnumerator c_Initialize(Data.Visualization.Visualization visualization, GenericEvent<float, float, string> onChangeProgress)
        {
            yield return Ninja.JumpToUnity;
            float progress = 1.0f;
            onChangeProgress.Invoke(progress, 0.0f, "");

            m_ModesManager.UpdateMode(Mode.FunctionsId.ResetScene);
            
            int sceneID = ApplicationState.Module3D.NumberOfScenesLoadedSinceStart;
            gameObject.name = "MultiPatients Scene (" + sceneID + ")";
            transform.position = new Vector3(HBP3DModule.SPACE_BETWEEN_SCENES_AND_COLUMNS * sceneID, transform.position.y, transform.position.z);

            progress += LOADING_MESHES_PROGRESS;
            onChangeProgress.Invoke(progress, 4.0f, "Loading meshes");
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadMNIObjects());
            yield return Ninja.JumpBack;

            // MNI meshes are preloaded
            SceneInformation.VolumeCenter = m_MNIObjects.IRM.Center;
            SceneInformation.MeshesLoaded = true;
            SceneInformation.IsROICreationModeEnabled = false;

            /// TODO
            List<string> ptsFiles = new List<string>(Visualization.Patients.Count), namePatients = new List<string>(Visualization.Patients.Count);
            for (int ii = 0; ii < Visualization.Patients.Count; ++ii)
            {
                //ptsFiles.Add(Visualization.Patients[ii].Brain.MNIBasedImplantation);
                //TOCHECK
                ptsFiles.Add(Visualization.Patients[ii].Brain.Implantations.Find((i) => i.Name == "MNI").Path);
                namePatients.Add(Visualization.Patients[ii].Place + "_" + Visualization.Patients[ii].Date + "_" + Visualization.Patients[ii].Name);
            }

            yield return Ninja.JumpToUnity;
            progress += LOADING_COLUMNS_PROGRESS;
            onChangeProgress.Invoke(progress, 0.05f, "Loading columns");
            // reset columns
            m_ColumnManager.DLLVolume = null; // this object must no be reseted
            m_ColumnManager.Initialize(Cuts.Count);

            yield return Ninja.JumpBack;
            // retrieve MNI IRM volume
            m_ColumnManager.DLLVolume = m_MNIObjects.IRM;
            SceneInformation.VolumeCenter = m_ColumnManager.DLLVolume.Center;
            SceneInformation.MRILoaded = true;

            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.ResetNIIBrainVolumeFile);
            //##################

            // FIXME
            // set references in column manager
            m_ColumnManager.DLLNii = m_MNIObjects.NII;

            // reset electrodes
            yield return Ninja.JumpToUnity;
            progress += LOADING_ELECTRODES_PROGRESS;
            onChangeProgress.Invoke(progress, 0.05f, "Loading electrodes");
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadSites(ptsFiles, namePatients));

            // define meshes splits nb
            ResetSplitsNumber(3);
            
            SceneInformation.CutMeshGeometryNeedsUpdate = true;

            progress += SETTING_TIMELINE_PROGRESS;
            onChangeProgress.Invoke(progress, 0.5f, "Setting timeline");
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_SetTimelineData());

            m_ColumnManager.InitializeColumnsMeshes(m_DisplayedObjects.BrainSurfaceMeshesParent);
            // update scenes cameras
            Events.OnUpdateCameraTarget.Invoke(m_ColumnManager.SelectedMesh.Both.BoundingBox.Center);
            DisplayScreenMessage("Multi Patients Scene loaded", 2.0f, 400, 80);
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
            SceneInformation.SitesLoaded = m_ColumnManager.DLLLoadedPatientsElectrodes.LoadPTSFiles(pathsElectrodesPtsFile, patientNames, ApplicationState.Module3D.MarsAtlasIndex);

            yield return Ninja.JumpToUnity;
            // destroy previous electrodes gameobject
            for (int ii = 0; ii < m_ColumnManager.SitesList.Count; ++ii)
            {
                // destroy material
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

            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.ResetElectrodesFile);
            //##################
        }
        #endregion
    }
}