


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
        /// MNI Objects
        /// </summary>
        private MNIObjects m_MNIObjects = null;

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

        private const float LOADING_MESHES_PROGRESS = 0.034f;
        private const float LOADING_COLUMNS_PROGRESS = 0.033f;
        private const float LOADING_ELECTRODES_PROGRESS = 0.033f;
        private const float SETTING_TIMELINE_PROGRESS = 0.4f;
        #endregion
        
        #region Public Methods
        public new void Initialize(Data.Visualization.Visualization visualization)
        {
            int idScript = TimeExecution.ID;
            TimeExecution.StartAwake(idScript, TimeExecution.ScriptsId.MP3DScene);

            base.Initialize(visualization);

            m_MNIObjects = GetComponent<MNIObjects>();

            TimeExecution.EndAwake(idScript, TimeExecution.ScriptsId.MP3DScene, gameObject);
        }
        /// <summary>
        /// Update the meshes cuts
        /// </summary>
        public override void ComputeMeshesCut()
        {
            // choose the mesh
            SceneInformation.MeshToDisplay = new DLL.Surface();
            switch (SceneInformation.MeshTypeToDisplay)
            {
                case SceneStatesInfo.MeshType.Grey:
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

            if (SceneInformation.MeshToDisplay == null) return;

            if (SceneInformation.MeshTypeToDisplay == SceneStatesInfo.MeshType.Inflated)
                m_ColumnManager.DLLSplittedWhiteMeshesList = new List<DLL.Surface>(SceneInformation.MeshToDisplay.SplitToSurfaces(m_ColumnManager.MeshSplitNumber));

            // get the middle
            SceneInformation.MeshCenter = SceneInformation.MeshToDisplay.BoundingBox.Center;

            // cut the mesh
            List<DLL.Surface> cuts;
            if (SceneInformation.MeshTypeToDisplay != SceneStatesInfo.MeshType.Inflated && Cuts.Count > 0)
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
            if (m_ColumnManager.DLLSplittedMeshesList.Count != splits.Count)
                m_ColumnManager.DLLSplittedMeshesList = splits;
            else
            {
                // swap DLL pointer
                for (int ii = 0; ii < splits.Count; ++ii)
                    m_ColumnManager.DLLSplittedMeshesList[ii].SwapDLLHandle(splits[ii]);
            }

            // reset brain texture generator
            for (int ii = 0; ii < m_ColumnManager.DLLSplittedMeshesList.Count; ++ii)
            {
                m_ColumnManager.DLLCommonBrainTextureGeneratorList[ii].Reset(m_ColumnManager.DLLSplittedMeshesList[ii], m_ColumnManager.DLLVolume);
                m_ColumnManager.DLLCommonBrainTextureGeneratorList[ii].ComputeUVMainWithVolume(m_ColumnManager.DLLSplittedMeshesList[ii], m_ColumnManager.DLLVolume, m_ColumnManager.MRICalMinFactor, m_ColumnManager.MRICalMaxFactor);
            }

            // reset tri eraser
            ResetTriangleErasing(false);

            // update cut brain mesh object mesh filter
            for (int ii = 0; ii < m_ColumnManager.DLLSplittedMeshesList.Count; ++ii)
                m_ColumnManager.DLLSplittedMeshesList[ii].UpdateMeshFromDLL(m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);

            // update cuts generators
            for (int ii = 0; ii < Cuts.Count; ++ii)
            {
                if (SceneInformation.MeshTypeToDisplay == SceneStatesInfo.MeshType.Inflated) // if inflated, there is not cuts
                {
                    m_DisplayedObjects.BrainCutMeshes[ii].SetActive(false);
                    continue;
                }

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
            List<Data.Visualization.ColumnData> columnDataList = new List<Data.Visualization.ColumnData>(m_Column3DViewManager.ColumnsIEEG.Count);
            for (int ii = 0; ii < m_Column3DViewManager.ColumnsIEEG.Count; ++ii)
            {
                Data.Visualization.ColumnData columnData = new Data.Visualization.ColumnData();
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
        /// Send additionnal site info to hight level UI
        /// </summary>
        public override void SendAdditionalSiteInfoRequest(Site previousPlot = null) // TODO deporter dans c manager
        {

            switch (m_ColumnManager.SelectedColumn.Type)
            {
                case Column3D.ColumnType.FMRI:
                    return;
                case Column3D.ColumnType.IEEG:
                    Column3DIEEG currIEEGCol = (Column3DIEEG)m_ColumnManager.SelectedColumn;

                    if (currIEEGCol.SelectedSiteID == -1)
                        return;

                    string[] elements = m_ColumnManager.SitesList[currIEEGCol.SelectedSiteID].GetComponent<Site>().Information.FullName.Split('_');

                    if (elements.Length < 3)
                        return;

                    int id = -1;
                    for (int ii = 0; ii < Visualization.Patients.Count; ++ii)
                    {
                        if (Visualization.Patients[ii].Name == elements[2])
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

                    List<List<bool>> masksColumnsData = new List<List<bool>>(m_ColumnManager.ColumnsIEEG.Count);
                    for (int ii = 0; ii < m_ColumnManager.ColumnsIEEG.Count; ++ii)
                    {
                        masksColumnsData.Add(new List<bool>(m_ColumnManager.ColumnsIEEG[ii].Sites.Count));

                        bool isROI = (m_ColumnManager.ColumnsIEEG[ii].SelectedROI.NumberOfBubbles > 0);
                        for (int jj = 0; jj < m_ColumnManager.ColumnsIEEG[ii].Sites.Count; ++jj)
                        {
                            Site p = m_ColumnManager.ColumnsIEEG[ii].Sites[jj];
                            bool keep = (!p.Information.IsBlackListed && !p.Information.IsExcluded && !p.Information.IsMasked);
                            if (isROI)
                                keep = keep && !p.Information.IsOutOfROI;

                            masksColumnsData[ii].Add(keep);
                        }
                    }

                    SiteRequest request = new SiteRequest();
                    request.spScene = false;
                    request.idSite1 = currIEEGCol.SelectedSiteID;
                    request.idSite2 = (previousPlot == null) ? -1 : previousPlot.Information.GlobalID;
                    request.idPatient = Visualization.Patients[id].ID;
                    request.idPatient2 = (previousPlot == null) ? "" : Visualization.Patients[previousPlot.Information.PatientID].ID;
                    request.maskColumn = masksColumnsData;
                    Events.OnRequestSiteInformation.Invoke(request);
                    break;
                default:
                    break;
            }
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
            float progress = 0.5f;

            m_ModesManager.UpdateMode(Mode.FunctionsId.ResetScene);
            
            int sceneID = ApplicationState.Module3D.NumberOfScenesLoadedSinceStart;
            gameObject.name = "MultiPatients Scene (" + sceneID + ")";
            transform.position = new Vector3(HBP3DModule.SPACE_BETWEEN_SCENES_AND_COLUMNS * sceneID, transform.position.y, transform.position.z);

            progress += LOADING_MESHES_PROGRESS;
            onChangeProgress.Invoke(progress, 0.05f, "Loading meshes");
            yield return Ninja.JumpBack;
            // MNI meshes are preloaded
            SceneInformation.VolumeCenter = m_MNIObjects.IRM.Center;
            SceneInformation.MeshesLoaded = true;
            SceneInformation.GreyMeshesAvailables = true;
            SceneInformation.WhiteMeshesAvailables = true;
            SceneInformation.WhiteInflatedMeshesAvailables = true;
            SceneInformation.IsROICreationModeEnabled = false;

            // get the middle
            SceneInformation.MeshCenter = m_MNIObjects.BothHemi.BoundingBox.Center;

            /// TODO
            List<string> ptsFiles = new List<string>(Visualization.Patients.Count), namePatients = new List<string>(Visualization.Patients.Count);
            for (int ii = 0; ii < Visualization.Patients.Count; ++ii)
            {
                ptsFiles.Add(Visualization.Patients[ii].Brain.MNIBasedImplantation);
                namePatients.Add(Visualization.Patients[ii].Place + "_" + Visualization.Patients[ii].Date + "_" + Visualization.Patients[ii].Name);
            }

            yield return Ninja.JumpToUnity;
            progress += LOADING_COLUMNS_PROGRESS;
            onChangeProgress.Invoke(progress, 0.05f, "Loading columns");
            // reset columns
            m_ColumnManager.DLLVolume = null; // this object must no be reseted
            m_ColumnManager.Initialize(Cuts.Count);
            m_ColumnManager.OnSelectColumnManager.AddListener((cm) =>
            {
                IsSelected = true;
            });

            yield return Ninja.JumpBack;
            // retrieve MNI IRM volume
            m_ColumnManager.DLLVolume = m_MNIObjects.IRM;
            SceneInformation.VolumeCenter = m_ColumnManager.DLLVolume.Center;
            SceneInformation.MRILoaded = true;

            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.ResetNIIBrainVolumeFile);
            //##################

            // set references in column manager
            m_ColumnManager.BothHemi = m_MNIObjects.BothHemi;
            m_ColumnManager.BothWhite = m_MNIObjects.BothWhite;
            m_ColumnManager.LHemi = m_MNIObjects.LeftHemi;
            m_ColumnManager.LWhite = m_MNIObjects.LeftWhite;
            m_ColumnManager.RHemi = m_MNIObjects.RightHemi;
            m_ColumnManager.RWhite = m_MNIObjects.RightWhite;
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

            // update scenes cameras
            Events.OnUpdateCameraTarget.Invoke(m_ColumnManager.BothHemi.BoundingBox.Center);
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
        /// <summary>
        /// Define the timeline data with a patient list, a list of column data and the pts paths
        /// </summary>
        /// <param name="patientList"></param>
        /// <param name="columnDataList"></param>
        /// <param name="ptsPathFileList"></param>
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
            m_ColumnManager.SetTimelineData(Visualization.Patients.ToList(), Visualization.Columns);

            // update plots visibility
            m_ColumnManager.UpdateAllColumnsSitesRendering(SceneInformation);
            yield return Ninja.JumpBack;

            // set flag
            SceneInformation.TimelinesLoaded = true;

            Events.OnAskRegionOfInterestUpdate.Invoke(-1);

            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.SetTimelines);
            //##################
        }
        #endregion
    }
}