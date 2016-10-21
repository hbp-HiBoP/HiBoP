
/**
 * \file    MP3DScene.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define MP3DScene class
 */

// system
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

// unity
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Rendering;


namespace HBP.VISU3D
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
        public class LoadSpSceneFromMP : UnityEvent<int> { }
    }

    /// <summary>
    /// The multi patients scene class, inheritance of Base3DScene.
    /// </summary>
    [AddComponentMenu("Scenes/Multi Patients 3D Scene")]
    public class MP3DScene : Base3DScene
    {
        #region members

        HBP.Data.Visualisation.MultiPatientsVisualisationData m_data = null;

        // events
        private Events.SendColumnROI m_sendColumnROIEvent = new Events.SendColumnROI();
        public Events.SendColumnROI SendColumnROIEvent { get { return m_sendColumnROIEvent;}}

        private Events.CreateBubble m_createBubbleEvent = new Events.CreateBubble();
        public Events.CreateBubble CreateBubbleEvent { get { return m_createBubbleEvent; } }

        private Events.SelectBubble m_selectBubbleEvent = new Events.SelectBubble();
        public Events.SelectBubble SelectBubbleEvent { get { return m_selectBubbleEvent; } }

        private Events.ChangeSizeBubble m_changeSizeBubbleEvent = new Events.ChangeSizeBubble();
        public Events.ChangeSizeBubble ChangeSizeBubbleEvent { get { return m_changeSizeBubbleEvent; } }

        private Events.RemoveBubble m_removeBubbleEvent = new Events.RemoveBubble();
        public Events.RemoveBubble RemoveBubbleEvent { get { return m_removeBubbleEvent; } }

        private Events.ApplySceneCamerasToIndividualScene m_ApplySceneCamerasToIndividualScene = new Events.ApplySceneCamerasToIndividualScene();
        public Events.ApplySceneCamerasToIndividualScene ApplySceneCamerasToIndividualScene { get { return m_ApplySceneCamerasToIndividualScene; } }

        private Events.LoadSpSceneFromMP m_loadSpSceneFromMP = new Events.LoadSpSceneFromMP();
        public Events.LoadSpSceneFromMP LoadSpSceneFromMP { get { return m_loadSpSceneFromMP; } }
        
        #endregion members

        #region mono_behaviour

        new void Awake()
        {
            int idScript = TimeExecution.getId();
            TimeExecution.startAwake(idScript, TimeExecution.ScriptsId.MP3DScene);

            base.Awake();

            m_MNI = GetComponent<MNIObjects>();
            
            TimeExecution.endAwake(idScript, TimeExecution.ScriptsId.MP3DScene, gameObject);

        }

        #endregion mono_behaviour

        #region functions




        /// <summary>
        /// Update the meshes cuts
        /// </summary>
        public override void updateMeshesCuts()
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

            // get the middle
            data_.meshCenter = data_.meshToDisplay.boundingBox().center();

            // cut the mesh
            if (data_.meshTypeToDisplay != SceneStatesInfo.MeshType.Inflated && m_CM.planesCutsCopy.Count > 0)
                m_CM.DLLCutsList = new List<DLL.Surface>(data_.meshToDisplay.cutSurface(m_CM.planesCutsCopy.ToArray(), data_.removeFrontPlaneList.ToArray(), !data_.holesEnabled));                
            else
                m_CM.DLLCutsList = new List<DLL.Surface>() { (DLL.Surface)(data_.meshToDisplay.Clone()) };

            // split the cut/inflated mesh
            m_CM.DLLSplittedMeshesList = new List<DLL.Surface>(m_CM.DLLCutsList[0].splitToSurfaces(m_CM.meshSplitNb));
            if (data_.meshTypeToDisplay == SceneStatesInfo.MeshType.Inflated)
            {
                DLL.Surface inflatedMesh = null;
                switch (data_.meshPartToDisplay)
                {
                    case SceneStatesInfo.MeshPart.Both:
                        inflatedMesh = m_MNI.BothWhiteInflated;
                        break;
                    case SceneStatesInfo.MeshPart.Left:
                        inflatedMesh = m_MNI.LeftWhiteInflated;
                        break;
                    case SceneStatesInfo.MeshPart.Right:
                        inflatedMesh = m_MNI.RightWhiteInflated;
                        break;
                }
                m_CM.DLLSplittedWhiteMeshesList = new List<DLL.Surface>(inflatedMesh.splitToSurfaces(m_CM.meshSplitNb));
            }
        }

        public override void endMeshesCuts()
        {
            for (int ii = 0; ii < m_CM.DLLSplittedMeshesList.Count; ++ii)
            {
                // reset brain texture generator
                m_CM.DLLCommonBrainTextureGeneratorList[ii].reset(m_CM.DLLSplittedMeshesList[ii], m_CM.DLLVolume);

                // update cut brain mesh object mesh filter
                m_CM.DLLSplittedMeshesList[ii].udpateMeshWithSurface(go_.MeshesList[ii].GetComponent<MeshFilter>().mesh);
            }

            // create null uv2/uv3 arrays
            m_CM.uvNull = new List<Vector2[]>(m_CM.meshSplitNb);
            for (int ii = 0; ii < m_CM.meshSplitNb; ++ii)
            {
                m_CM.uvNull.Add(new Vector2[go_.MeshesList[ii].GetComponent<MeshFilter>().mesh.vertexCount]);
                Extensions.ArrayExtensions.Fill(m_CM.uvNull[ii], new Vector2(0.01f, 1f));
            }

            // update cuts generators
            for (int ii = 0; ii < m_CM.planesCutsCopy.Count; ++ii)
                if (data_.texturesCutToUpdateMask[ii])
                {
                    if (data_.meshTypeToDisplay == SceneStatesInfo.MeshType.Inflated) // if inflated, there is not cuts
                    {
                        go_.CutsList[ii].SetActive(false);
                        continue;
                    }

                    // reset cut texture generator
                    for (int jj = 0; jj < m_CM.nbIEEGCol(); ++jj)
                        m_CM.DLLCommonCutTextureGeneratorList[ii].reset(m_CM.DLLVolume, m_CM.DLLCutsList[ii + 1], m_CM.planesCutsCopy[ii]);

                    go_.CutsList[ii].SetActive(true); // enable cuts gameobject
                }

            data_.collidersUpdated = false; // colliders are now longer up to date */
            data_.updateCutPlanes = false;   // planes are now longer requested to be updated 
            data_.generatorUpToDate = false; // generator is not up to date anymore

            // update amplitude for all columns
            for (int ii = 0; ii < m_CM.nbIEEGCol(); ++ii)
                m_CM.IEEGCol(ii).updateAmplitude = true;

            // mesh must be updated
            data_.updateGeometry = true;
            data_.updateImageCuts = true;
        }




        /// <summary>
        /// Reset the scene : reload meshes, IRM, plots, and regenerate textures
        /// </summary>
        /// <param name="data"></param>
        public bool reset(Data.Visualisation.MultiPatientsVisualisationData data)
        {
            modes.updateMode(Mode.FunctionsId.resetScene);
            m_data = data;

            // MNI meshes are preloaded
            data_.volumeCenter = m_MNI.IRM.center();
            data_.meshesLoaded = true;
            data_.hemiMeshesAvailables = true;
            data_.whiteMeshesAvailables = true;
            data_.whiteInflatedMeshesAvailables = true;
            data_.ROICreationMode = false;

            // get the middle
            data_.meshCenter = m_MNI.BothHemi.boundingBox().center();

            List<string> ptsFiles = new List<string>(data.GetImplantation().Length), namePatients = new List<string>(data.GetImplantation().Length);
            for (int ii = 0; ii < data.GetImplantation().Length; ++ii)
            {
                ptsFiles.Add(data.GetImplantation()[ii]);
                namePatients.Add(data.Patients[ii].Place + "_" + data.Patients[ii].Date + "_" + data.Patients[ii].Name);
            }

            // reset columns
            m_CM.DLLVolume = null; // this object must no be reseted
            m_CM.reset();

            // retrieve MNI IRM volume
            m_CM.DLLVolume = m_MNI.IRM;
            data_.volumeCenter = m_CM.DLLVolume.center();
            data_.volumeLoaded = true;
            UpdatePlanes.Invoke();


            // send cal values to the UI
            IRMCalValuesUpdate.Invoke(m_CM.DLLVolume.retrieveExtremeValues());


            // update scenes cameras
            UpdateCameraTarget.Invoke(m_MNI.BothHemi.boundingBox().center());


            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.resetNIIBrainVolumeFile);
            //##################

            // reset electrodes
            //if (success)
            bool success = resetElectrodesFile(ptsFiles, namePatients);

            if (success)
            {
                for (int ii = 0; ii < data_.texturesCutToUpdateMask.Count; ++ii)
                    data_.texturesCutToUpdateMask[ii] = true;
                data_.updateCutPlanes = true;
            }

            if (!success)
            {
                Debug.LogError("-ERROR : MP3DScene : reset failed. ");
                data_.reset();
                m_CM.reset();
                resetGameObjets();                
                return false;
            }

            return true;
        }

        /// <summary>
        /// Reset all the plots with a new list of pts files
        /// </summary>
        /// <param name="pathsElectrodesPtsFile"></param>
        /// <returns></returns>
        public bool resetElectrodesFile(List<string> pathsElectrodesPtsFile, List<string> names)
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.resetElectrodesFile))
            {
                Debug.LogError("-ERROR : MultiPatients3DScene::resetElectrodesFile -> no acess for mode : " + modes.currentModeName());
                return false;
            }
            //##################

            // load list of pts files
            data_.electodesLoaded = m_CM.DLLLoadedPatientsElectrodes.loadNamePtsFiles(pathsElectrodesPtsFile, names);

            if (!data_.electodesLoaded)
                return false;

            // destroy previous electrodes gameobject
            for (int ii = 0; ii < m_CM.PlotsList.Count; ++ii)
            {
                // destroy material
                Destroy(m_CM.PlotsList[ii]);
            }
            m_CM.PlotsList.Clear();

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

            if (data_.electodesLoaded)
            {
                int currPlotNb = 0;
                for (int ii = 0; ii < m_CM.DLLLoadedPatientsElectrodes.getPatientNumber(); ++ii)
                {
                    int idPlotPatient = 0;
                    string patientName = m_CM.DLLLoadedPatientsElectrodes.getPatientName(ii);


                    // create plot patient parent
                    m_CM.PlotsPatientParent.Add(new GameObject("P" + ii + " - " + patientName));
                    m_CM.PlotsPatientParent[m_CM.PlotsPatientParent.Count - 1].transform.SetParent(go_.PlotsParent.transform);
                    m_CM.PlotsElectrodesParent.Add(new List<GameObject>(m_CM.DLLLoadedPatientsElectrodes.getElectrodeNumber(ii)));


                    for (int jj = 0; jj < m_CM.DLLLoadedPatientsElectrodes.getElectrodeNumber(ii); ++jj)
                    {
                        // create plot electrode parent
                        m_CM.PlotsElectrodesParent[ii].Add(new GameObject(m_CM.DLLLoadedPatientsElectrodes.getElectrodeName(ii, jj)));
                        m_CM.PlotsElectrodesParent[ii][m_CM.PlotsElectrodesParent[ii].Count - 1].transform.SetParent(m_CM.PlotsPatientParent[ii].transform);


                        for (int kk = 0; kk < m_CM.DLLLoadedPatientsElectrodes.getPlotsElectrodeNumber(ii, jj); ++kk)
                        {
                            GameObject plotGO = Instantiate(BaseGameObjects.Plot);
                            plotGO.name = m_CM.DLLLoadedPatientsElectrodes.getPlotName(ii, jj, kk);
                            //brainElectrode.name = "????? " + cm_.DLLLoadedPatientsElectrodes.getPlotName(ii, jj, kk);
                                     
                            Vector3 posInverted = m_CM.DLLLoadedPatientsElectrodes.getPlotPos(ii, jj, kk);
                            posInverted.x = -posInverted.x;
                            plotGO.transform.position = posInverted;
                            plotGO.transform.SetParent(m_CM.PlotsElectrodesParent[ii][jj].transform);

                            plotGO.GetComponent<MeshFilter>().sharedMesh = SharedMeshes.Plot;

                            plotGO.SetActive(true);
                            plotGO.layer = LayerMask.NameToLayer("Inactive");

                            Plot plot = plotGO.GetComponent<Plot>();
                            plot.idPlotPatient = idPlotPatient++;
                            plot.idPatient = ii;
                            plot.idElectrode = jj;
                            plot.idPlot = kk;
                            plot.idGlobal = currPlotNb++;
                            plot.blackList = false;
                            plot.highlight = false;
                            plot.patientName = patientName;
                            plot.fullName = names[ii] + "_" + plotGO.name;

                            m_CM.PlotsList.Add(plotGO);
                        }
                    }
                }
            }

            // reset selected plot
            for (int ii = 0; ii < m_CM.nbIEEGCol(); ++ii)
            {
                m_CM.IEEGCol(ii).idSelectedPlot = -1;
            }

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.resetElectrodesFile);
            //##################

            return true;
        }

        /// <summary>
        /// Define the timeline data with a patient list, a list of column data and the pts paths
        /// </summary>
        /// <param name="patientList"></param>
        /// <param name="columnDataList"></param>
        /// <param name="ptsPathFileList"></param>
        public void setTimelinesData(List<Data.Patient.Patient> patientList, List<Data.Visualisation.ColumnData> columnDataList, List<string> ptsPathFileList)
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.setTimelines))
            {
                Debug.LogError("-ERROR : Base3DScene::setTimelines -> no acess for mode : " + modes.currentModeName());
                return;
            }
            //##################

            // update columns number
            m_CM.updateColumnsNb(columnDataList.Count, 0, m_CM.planesList.Count);

            // update columns names
            for (int ii = 0; ii < columnDataList.Count; ++ii)
            {
                m_CM.IEEGCol(ii).Label = columnDataList[ii].Label;
            }            

            // set timelines
            m_CM.setMpTimelinesData(patientList, columnDataList, ptsPathFileList);

            // update plots visibility
            m_CM.updateColumnsPlotsRendering(data_);

            // set flag
            data_.timelinesLoaded = true;

            AskROIUpdateEvent.Invoke(-1);

            // send data to UI
            sendIEEGDataToMenu();

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.setTimelines);
            //##################
        }

        /// <summary>
        /// Load a patient in the SP scene
        /// </summary>
        /// <param name="idPatientSelected"></param>
        public void loadPatientInSPScene(int idPatientSelected, int idPlotSelected)
        {
            m_loadSpSceneFromMP.Invoke(idPatientSelected);

            // retrieve patient plots nb
            int nbPlotsSpPatient = m_CM.DLLLoadedPatientsElectrodes.getPlotsPatientNumber(idPatientSelected);

            // retrieve timeline size (time t)
            int size = m_CM.IEEGCol(0).columnData.Values[0].Length;
           
            // compute start value id
            int startId = 0;
            for (int ii = 0; ii < idPatientSelected; ++ii)
            {
                startId += m_CM.DLLLoadedPatientsElectrodes.getPlotsPatientNumber(ii);
            }
            
            // create new column data
            List<Data.Visualisation.ColumnData> columnDataList = new List<Data.Visualisation.ColumnData>(m_CM.nbIEEGCol());
            for (int ii = 0; ii < m_CM.nbIEEGCol(); ++ii)
            {
                Data.Visualisation.ColumnData columnData = new Data.Visualisation.ColumnData();
                columnData.Label = m_CM.IEEGCol(ii).Label;

                // copy iconic scenario reference
                columnData.IconicScenario = m_CM.IEEGCol(ii).columnData.IconicScenario;

                // copy timeline reference
                columnData.TimeLine = m_CM.IEEGCol(ii).columnData.TimeLine;

                // fill new mask
                columnData.MaskPlot = new bool[nbPlotsSpPatient];
                for (int jj = 0; jj < nbPlotsSpPatient; ++jj)
                {
                    columnData.MaskPlot[jj] = m_CM.IEEGCol(ii).columnData.MaskPlot[startId + jj];
                }

                // fill new values
                List<float[]> spColumnValues = new List<float[]>(nbPlotsSpPatient);

                for (int jj = 0; jj < nbPlotsSpPatient; ++jj)
                {
                    float[] valuesT = new float[size];
                    for (int kk = 0; kk < valuesT.Length; ++kk)
                    {
                        valuesT[kk] = m_CM.IEEGCol(ii).columnData.Values[startId + jj][kk];
                    }

                    spColumnValues.Add(valuesT);
                }

                columnData.Values = spColumnValues.ToArray();
                columnDataList.Add(columnData);
            }


            // fill masks bo be send to sp
            List<List<bool>> blackListMask = new List<List<bool>>(m_CM.nbIEEGCol());
            List<List<bool>> excludedMask = new List<List<bool>>(m_CM.nbIEEGCol());
            List<List<bool>> hightLightedMask = new List<List<bool>>(m_CM.nbIEEGCol());
            for (int ii = 0; ii < m_CM.nbIEEGCol(); ++ii)
            {
                blackListMask.Add(new List<bool>(nbPlotsSpPatient));
                excludedMask.Add(new List<bool>(nbPlotsSpPatient));
                hightLightedMask.Add(new List<bool>(nbPlotsSpPatient));
                
                for (int jj = 0; jj < nbPlotsSpPatient; ++jj)
                {
                    blackListMask[ii].Add(m_CM.IEEGCol(ii).Plots[startId + jj].blackList);
                    excludedMask[ii].Add(m_CM.IEEGCol(ii).Plots[startId + jj].exclude);
                    hightLightedMask[ii].Add(m_CM.IEEGCol(ii).Plots[startId + jj].highlight);
                }
            }

            bool success = StaticVisuComponents.HBPMain.setSPData(m_CM.mpPatients[idPatientSelected], columnDataList, idPlotSelected, blackListMask, excludedMask, hightLightedMask);
            if(!success)
            {
                // TODO : reset SP scene
            }

            transform.parent.gameObject.GetComponent<ScenesManager>().setScenesVisibility(true, true);

            // update sp cameras
            m_ApplySceneCamerasToIndividualScene.Invoke();
        }

        /// <summary>
        /// Reset the rendering settings for this scene, called by each MP camera before rendering
        /// </summary>
        /// <param name="cameraRotation"></param>
        public override void resetRenderingSettings(Vector3 cameraRotation)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientIntensity = 1;
            RenderSettings.skybox = null;
            RenderSettings.ambientLight = new Color(0.2f, 0.2f, 0.2f, 1);

            go_.SharedDirLight.GetComponent<Transform>().eulerAngles = cameraRotation;
        }

        /// <summary>
        /// Mouse scroll events managements, call Base3DScene parent function
        /// </summary>
        /// <param name="scrollDelta"></param>
        new public void mouseScrollAction(Vector2 scrollDelta)
        {
            base.mouseScrollAction(scrollDelta);

            int idC = m_CM.idSelectedColumn;
            int idBubble = m_CM.isIRMFColumn(idC) ? m_CM.IRMFCol(idC - m_CM.nbIEEGCol()).ROI.idSelectedBubble : m_CM.IEEGCol(idC).ROI.idSelectedBubble;

            if (data_.ROICreationMode) // ROI : change ROI size
            {
                m_changeSizeBubbleEvent.Invoke(idC, idBubble, (scrollDelta.y < 0  ? 0.9f : 1.1f));
                m_CM.updateColumnsPlotsRendering(data_);
            }
        }

        /// <summary>
        /// Return true if the ROI mode is enabled (allow to switch mouse scroll effect between camera zoom and ROI size changes)
        /// </summary>
        /// <returns></returns>
        public bool ROIModeEnabled()
        {
            return data_.ROICreationMode;
        }

        /// <summary>
        /// Keyboard events management, call Base3DScene parent function
        /// </summary>
        /// <param name="keyCode"></param>
        new public void keyboardAction(KeyCode keyCode)
        {
            base.keyboardAction(keyCode);

            switch (keyCode)
            {
                // choose active plane
                case KeyCode.Delete:

                    if (data_.ROICreationMode)
                    {
                        int idC = m_CM.idSelectedColumn;

                        Column3DView col =  m_CM.currentColumn();                        
                        int idBubble = col.ROI.idSelectedBubble;
                        if (idBubble != -1)
                            m_removeBubbleEvent.Invoke(idC, idBubble);

                        m_CM.updateColumnsPlotsRendering(data_);
                    }

                    break;
            }
        }

        /// <summary>
        /// Manage the clicks event in the scene
        /// </summary>
        /// <param name="ray"></param>
        public void clickOnScene(Ray ray)
        {
            // scene not loaded
            if (!data_.volumeLoaded)
                return;

            // update colliders if necessary
            if (!data_.collidersUpdated)
                updateMeshesColliders();

            // retrieve layer
            int layerMask = 0;
            layerMask |= 1 << LayerMask.NameToLayer(m_CM.currentColumn().layerColumn);
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
                if(hits[ii].transform.GetComponent<Plot>() != null) // plot hit
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

                string namePatientClickedPlot = hits[idClosestPlotHit].collider.gameObject.GetComponent<Plot>().patientName;

                for (int ii = 0; ii < m_CM.mpPatients.Count; ++ii)
                {
                    string currentPatient = m_CM.mpPatients[ii].Place + "_" + m_CM.mpPatients[ii].Date + "_" + m_CM.mpPatients[ii].Name;
                    if (currentPatient == namePatientClickedPlot)
                    {
                        idPatientSelected = ii;
                        break;
                    }
                }

                m_CM.idSelectedPatient = idPatientSelected;

                int idPlotGlobal = hits[idClosestPlotHit].collider.gameObject.GetComponent<Plot>().idGlobal;
                m_CM.currentColumn().idSelectedPlot = idPlotGlobal;

                m_CM.updateColumnsPlotsRendering(data_);
                ClickPlot.Invoke(-1); // test

                return;
            }
            else
            {
                if (data_.ROICreationMode)
                {
                    if (ROIHit) // ROI collision -> select ROI
                    {
                        if (m_CM.currentColumn().ROI.checkCollision(ray))
                        {
                            int idClickedBubble = m_CM.currentColumn().ROI.collidedClosestSphereId(ray);
                            m_selectBubbleEvent.Invoke(m_CM.idSelectedColumn, idClickedBubble);
                        }

                        return;
                    }
                    else if (meshHit) // mesh collision -> create newROI
                    {
                        m_createBubbleEvent.Invoke(hits[idClosestNonPlot].point, m_CM.idSelectedColumn);

                        m_CM.updateColumnsPlotsRendering(data_);
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
        public void moveMouseOnScene(Ray ray, Vector3 mousePosition, int idColumn)
        {
            // scene not loaded
            if (!data_.volumeLoaded)
                return;

            if (m_CM.idSelectedColumn != idColumn) // not the selected column
                return;

            // retrieve layer
            int layerMask = 0;
            layerMask |= 1 << LayerMask.NameToLayer(m_CM.currentColumn().layerColumn);
            layerMask |= 1 << LayerMask.NameToLayer("Meshes_MP");

            // raycasts
            RaycastHit[] hits = Physics.RaycastAll(ray.origin, ray.direction, Mathf.Infinity, layerMask);
            if (hits.Length == 0)
            {
                UpdateDisplayedPlotsInfo.Invoke(new plotInfo(false, mousePosition, false));
                return;
            }

            // check the hits
            int idHitToKeep = -1;
            float minDistance = float.MaxValue;
            for(int ii = 0; ii < hits.Length; ++ii)
            {
                if (hits[ii].collider.GetComponent<Plot>() == null) // not a plot hit
                    continue;

                if (minDistance > hits[ii].distance)
                {
                    minDistance = hits[ii].distance;
                    idHitToKeep = ii;
                }
            }

            if (idHitToKeep == -1) // not plot hit
            {
                UpdateDisplayedPlotsInfo.Invoke(new plotInfo(false, mousePosition, false));
                return;
            }

            if (hits[idHitToKeep].collider.transform.parent.name == "cuts" || hits[idHitToKeep].collider.transform.parent.name == "meshes") // meshes hit
            {
                UpdateDisplayedPlotsInfo.Invoke(new plotInfo(false, mousePosition, false));
                return;
            }

            // IRMF
            if (m_CM.isIRMFCurrentColumn())
            {
                UpdateDisplayedPlotsInfo.Invoke(new plotInfo(true, mousePosition, true, false, hits[idHitToKeep].collider.gameObject.name));
                return;
            }

            // IEEG
            Column3DViewIEEG currIEEGCol = (Column3DViewIEEG)m_CM.currentColumn();

            // retrieve current plot amp
            float amp = 0;
            if (currIEEGCol.columnData.Values.Length > 0)
            {
                amp = currIEEGCol.columnData.Values[hits[idHitToKeep].collider.gameObject.GetComponent<Plot>().idGlobal][currIEEGCol.currentTimeLineID];
            }

            UpdateDisplayedPlotsInfo.Invoke(new plotInfo(true, mousePosition, m_CM.isIRMFCurrentColumn(), false, hits[idHitToKeep].collider.GetComponent<Plot>().fullName, "" + amp));
        }

        public void disablePlotDisplayWindows(int idColumn)
        {
            UpdateDisplayedPlotsInfo.Invoke(new plotInfo(false, new Vector3(0, 0, 0), false));
        }

        /// <summary>
        /// Enter in the ROI creation mode
        /// </summary>
        public void enableROICreationMode()
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.enableROICreationMode))
            {
                Debug.LogError("-ERROR : Base3DScene::enableROICreationMode -> no acess for mode : " + modes.currentModeName());
                return;
            }
            //##################

            data_.ROICreationMode = true;
            //cm_.updateROIVisibility(data_.ROICreationMode);            

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.enableROICreationMode);
            //##################
        }

        /// <summary>
        /// Exit the ROI creation mode
        /// </summary>
        public void disableROICreationMode()
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.disableROICreationMode))
            {
                Debug.LogError("-ERROR : Base3DScene::disableROICreationMode -> no acess for mode : " + modes.currentModeName());
                return;
            }
            //##################

            data_.ROICreationMode = false;
            //cm_.updateROIVisibility(data_.ROICreationMode);

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.disableROICreationMode);
            //##################
        }

        /// <summary>
        /// Send additionnal plot info to hight level UI
        /// </summary>
        public override void sendAdditionnalPlotInfoRequest(Plot previousPlot = null) // TODO deporter dans c manager
        {
            if (m_CM.currentColumn().isIRMF)
                return;


            Debug.Log("sendAdditionnalPlotInfoRequest MP ! ");
            // IEEG
            Column3DViewIEEG currIEEGCol = (Column3DViewIEEG)m_CM.currentColumn();
            Debug.Log(currIEEGCol.name + " " + m_CM.currentColumn().idSelectedPlot);

            if (currIEEGCol.idSelectedPlot == -1)
                return;

            Debug.Log("elements MP ! ");

            string[] elements = m_CM.PlotsList[currIEEGCol.idSelectedPlot].GetComponent<Plot>().fullName.Split('_');

            Debug.Log("elements MP ! " + elements.Length + " "+ m_CM.PlotsList[currIEEGCol.idSelectedPlot].GetComponent<Plot>().fullName);
            if (elements.Length < 3)
                return;

            int id = -1;
            for(int ii = 0; ii < m_data.Patients.Count; ++ii)
            {
                if(m_data.Patients[ii].Name == elements[2])
                {
                    id = ii;
                    break;
                }
            }

            if(id == -1)
            {
                // ERROR
                return;
            }

            Debug.Log("masksColumnsData");

            List<List<bool>> masksColumnsData = new List<List<bool>>(m_CM.nbIEEGCol());
            for (int ii = 0; ii < m_CM.nbIEEGCol(); ++ii)
            {
                masksColumnsData.Add(new List<bool>(m_CM.IEEGCol(ii).Plots.Count));

                bool isROI = (m_CM.IEEGCol(ii).ROI.getNbSpheres() > 0);
                for(int jj = 0; jj < m_CM.IEEGCol(ii).Plots.Count; ++jj)
                {
                    Plot p = m_CM.IEEGCol(ii).Plots[jj];
                    bool keep = (!p.blackList && !p.exclude && !p.columnMask);
                    if (isROI)
                        keep = keep && !p.columnROI;

                    masksColumnsData[ii].Add(keep);
                }
            }

            Debug.Log("plotRequest");

            plotRequest request = new plotRequest();            
            request.spScene = false;
            request.idPlot = currIEEGCol.idSelectedPlot;
            request.idPlot2 = (previousPlot == null) ? -1 : previousPlot.idGlobal;
            request.idPatient = m_data.Patients[id].ID;
            request.idPatient2 = (previousPlot == null) ? "" : m_data.Patients[previousPlot.idPatient].ID;
            request.maskColumn = masksColumnsData;
            PlotInfoRequest.Invoke(request);
        }


        public void updateCurrentROI(int idColumn)
        {
            bool[] maskROI = new bool[m_CM.PlotsList.Count];

            // update mask ROI
            for (int ii = 0; ii < maskROI.Length; ++ii)
                maskROI[ii] = m_CM.col(idColumn).Plots[ii].columnROI;

            m_CM.col(idColumn).ROI.updateMask(m_CM.col(idColumn).RawElectrodes, maskROI);
            for (int ii = 0; ii < m_CM.col(idColumn).Plots.Count; ++ii)
                m_CM.col(idColumn).Plots[ii].columnROI = maskROI[ii];

            m_CM.updateColumnsPlotsRendering(data_);
        }

        /// <summary>
        /// Update the ROI of a column from the interface
        /// </summary>
        /// <param name="idColumn"></param>
        /// <param name="roi"></param>
        public void updateROI(int idColumn, ROI roi)
        {
            m_CM.col(idColumn).updateROI(roi);
            updateCurrentROI(idColumn);
        }

        /// <summary>
        /// Return the string information of the current column ROI and plots states
        /// </summary>
        /// <returns></returns>
        public string getCurrentColumnROIAndPlotsStateStr()
        {
            Column3DView currentCol = m_CM.currentColumn();
            return "ROI :\n" +  currentCol.ROI.getROIStr() + currentCol.getPlotsStateStr();
        }

        public string getSitesInROI()
        {
            Column3DView currentCol = m_CM.currentColumn();
            return "Sites in ROI:\n" + currentCol.ROI.getROIStr() + currentCol.getOnlyPlotsInROIStr();
        }


        public void updatePlotMasks(int idColumn, List<List<List<PlotI>>> plots, List<string> patientsName)
        {
            // reset previous masks
            for (int ii = 0; ii < CM.col(idColumn).Plots.Count; ++ii)
            {
                CM.col(idColumn).Plots[ii].exclude = false;
                CM.col(idColumn).Plots[ii].blackList = false;
                CM.col(idColumn).Plots[ii].highlight = false;
                CM.col(idColumn).Plots[ii].columnMask = false;
            }

            // update masks
            for (int ii = 0; ii < CM.col(idColumn).Plots.Count; ii++)
            {
                for (int jj = 0; jj < plots.Count; ++jj) // patient
                {
                    if (patientsName[jj] != CM.col(idColumn).Plots[ii].patientName)
                        continue;                    

                    for (int kk = 0; kk < plots[jj].Count; kk++) // electrode
                    {
                        for(int ll = 0; ll < plots[jj][kk].Count; ll++) // plot
                        {
                            string namePlot = plots[jj][kk][ll].patientName + "_" + plots[jj][kk][ll].name;
                            if (namePlot != CM.col(idColumn).Plots[ii].fullName)
                                continue;

                            CM.col(idColumn).Plots[ii].exclude = plots[jj][kk][ll].exclude;
                            CM.col(idColumn).Plots[ii].blackList = plots[jj][kk][ll].blackList;
                            CM.col(idColumn).Plots[ii].highlight = plots[jj][kk][ll].highlight;
                            CM.col(idColumn).Plots[ii].columnMask = plots[jj][kk][ll].columnMask;
                        }
                    }

                }
            }
        }


        #endregion functions
    }
}