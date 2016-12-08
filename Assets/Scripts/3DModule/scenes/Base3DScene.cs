
/**
 * \file    Base3DScene.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define Base3DScene and ComputeGeneratorsJob classes
 */

// system
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

// unity
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace HBP.VISU3D
{
    /// <summary>
    /// Plot request to be send to the outside UI
    /// </summary>
    public struct plotRequest
    {
        public bool spScene; /**< is a single patient scene ? */
        public int idPlot; /**< id of the first plot */
        public int idPlot2;  /**< id of second plot */
        public string idPatient; /**< id of the patient corresponding to the first plot */
        public string idPatient2;  /**< id of the patient corresponding to the second plot*/
        public List<List<bool>> maskColumn; /**< masks of the plots :  dim[0] = columnds data nb, dim[1] = plots nb.  if true : the plot is not excluded/blacklisted/column masked and is in a ROI (if there is at least one ROI, if not ROI defined, all the plots are considered inside a ROI) */

        public void display()
        {
            Debug.Log("plotRequest : " + spScene + " " + idPlot + " " + idPlot2 + " " + idPatient + " " + idPatient2 + " " + maskColumn.Count);
            if (maskColumn.Count > 0)
            {
                Debug.Log("size1 : " + maskColumn[0].Count);
                string mask = "";
                for (int ii = 0; ii < maskColumn[0].Count; ++ii)
                    mask += maskColumn[0][ii] + " ";
                Debug.Log("-> mask : " + mask);
            }
        }
    }

    /// <summary>
    /// Plot info to be send to the UI
    /// </summary>
    public class plotInfo
    {
        public plotInfo(bool enabled, Vector3 position, bool isIRMF, bool displayLatencies = false, string name = "", string amplitude = "", string height = "", string latency = "")
        {
            this.enabled = enabled;
            this.position = position;
            this.isIRMF = isIRMF;
            this.displayLatencies = displayLatencies;
            this.name = name;
            this.amplitude = amplitude;
            this.height = height;
            this.latency = latency;
        }

        public bool isIRMF;
        public bool enabled;
        public bool displayLatencies;

        public Vector3 position;

        public string name;
        public string amplitude;
        public string height;
        public string latency;
    }

    /// <summary>
    /// IRM cal values
    /// </summary>
    public struct IRMCalValues
    {
        public float min;
        public float max;
        public float loadedCalMin;
        public float loadedCalMax;
        public float computedCalMin;
        public float computedCalMax;
    }

    /// <summary>
    /// IEEG sites parameters
    /// </summary>
    public struct iEEGSitesParameters
    {
        public int columnId;
        public float gain;
        public float maxDistance;
    }

    /// <summary>
    /// IEEG alpha parameters 
    /// </summary>
    public struct iEEGAlphaParameters
    {
        public int columnId;
        public float alphaMin;
        public float alphaMax;
    }

    /// <summary>
    /// IEEG threhsolds parameters
    /// </summary>
    public struct iEEGThresholdParameters
    {
        public int columnId;
        public float minSpan;
        public float middle;
        public float maxSpan;
    }


    /// <summary>
    /// IEEG data to be send to the UI
    /// </summary>
    public struct iEEGDataParameters
    {
        public int columnId;

        public float maxDistance;
        public float gain;

        public float minAmp;
        public float maxAmp;

        public float alphaMin;
        public float alphaMax;

        public float spanMin;
        public float middle;
        public float spanMax;        
    }

    /// <summary>
    /// IRMF data to be send to the UI
    /// </summary>
    public struct IRMFDataParameters
    {
        public bool singlePatient;
        public int columnId;
        public float alpha;
        public float calMin; 
        public float calMax;
        public IRMCalValues calValues;
    }

    namespace Events
    {
        /// <summary>
        /// UI event for sending a plot info request to the outside UI (params : plotRequest)
        /// </summary>
        public class InfoPlotRequest : UnityEvent<plotRequest> { }

        /// <summary>
        /// Event for sending info in order to display a message in a scene screen (params : message, duration, width, height)
        /// </summary>
        public class DisplaySceneMessage : UnityEvent<string, float, int, int> { }

        /// <summary>
        /// Event for sending info in order to display a loadingbar in a scene screen (params : duration, width, height, value)
        /// </summary>
        public class DisplaySceneProgressBar : UnityEvent<float, int, int, float> { }

        /// <summary>
        /// Event of sending the current selected scene and column (params : spScene, id column)
        /// </summary>
        public class UpdateSceneAndColSelection : UnityEvent<bool, int> { }


        /// <summary>
        /// Event for colormap values associated to a column id (params : minValue, middle, maxValue, id)
        /// </summary>
        public class SendColormapEvent : UnityEvent<float, float, float, int> { }

        /// <summary>
        /// Event for sending IEEG data parameters to UI (params : IEEGDataParameters)
        /// </summary>
        public class SendIEEGParameters : UnityEvent<iEEGDataParameters> { }

        /// <summary>
        /// Event for sending IRMF data parameters to UI (params : IRMFDataParameters)
        /// </summary>
        public class SendIRMFParameters : UnityEvent<IRMFDataParameters> { }

        /// <summary>
        /// Event for updating cuts planes 
        /// </summary>
        public class UpdatePlanes : UnityEvent { }

        /// <summary>
        /// Event for updating the planes cuts display in the cameras
        /// </summary>
        public class ModifyPlanesCuts : UnityEvent { }

        /// <summary>
        /// Event for updating the cuts images in the UI (params : textures, columnId, planeNb)
        /// </summary>
        public class UpdateCutsInUI : UnityEvent<List<Texture2D>, int, int> { }

        /// <summary>
        /// Send the new selected id column to the UI (params : idColumn)
        /// </summary>
        public class DefineSelectedColumn : UnityEvent<int> { }

        /// <summary>
        /// Event for updating time in the UI
        /// </summary>
        public class UpdateTimeInUI : UnityEvent { }

        /// <summary>
        /// 
        /// </summary>
        public class UpdateDisplayedPlotsInfo : UnityEvent<plotInfo> { }

        /// <summary>
        /// Ask the UI to udpate the ROI of all the scene columns
        /// </summary>
        public class AskROIUpdate : UnityEvent<int> { }

        /// <summary>
        /// Ask the camera manager to update the target for this scene
        /// </summary>
        public class UpdateCameraTarget : UnityEvent<Vector3> { }

        /// <summary>
        /// Occurs when a plot is clicked in the scene (params : id of the column, if = -1 use the current selected column id)
        /// </summary>
        public class ClickPlot : UnityEvent<int> { }

        /// <summary>
        /// Event for updating the IRM cal values in the UI
        /// </summary>
        public class IRMCalValuesUpdate : UnityEvent<IRMCalValues> { }

    }

    /// <summary>
    /// Class containing all the DLL data and the displayable Gameobjects of a 3D scene.
    /// </summary>
    [AddComponentMenu("Scenes/Base 3D Scene")]
    abstract public class Base3DScene : MonoBehaviour
    {
        #region members          

        // scene        
        public UIOverlayManager m_uiOverlayManager; /**< UI overlay manager of the scenes */

        // data
        public bool singlePatient; /**< is a sp scene*/
        public SceneStatesInfo data_ = null; /**<  data of the scene */
        protected ModesManager modes = null; /**< modes of the scene */
        protected DisplayedObjects3DView go_ = null; /**< displayable objects of the scene */

        protected MNIObjects m_MNI = null;

        protected Column3DViewManager m_CM = null; /**< column data manager */
        public Column3DViewManager CM { get { return m_CM; } }

        // threads / jobs
        protected CutMeshJob m_cutMeshJob = null; /**< cut mesh job */
        protected ComputeGeneratorsJob m_computeGeneratorsJob = null; /**< generator computing job */

        // events
        public Events.UpdatePlanes UpdatePlanes = new Events.UpdatePlanes(); 
        public Events.ModifyPlanesCuts ModifyPlanesCuts = new Events.ModifyPlanesCuts();
        public Events.SendIEEGParameters SendIEEGParameters = new Events.SendIEEGParameters();
        public Events.SendIRMFParameters SendIRMFParameters = new Events.SendIRMFParameters();
        public Events.SendColormapEvent SendColorMapValues = new Events.SendColormapEvent();
        public Events.SendModeSpecifications SendModeSpecifications = new Events.SendModeSpecifications(); 
        public Events.DisplaySceneMessage DisplayScreenMessage = new Events.DisplaySceneMessage();
        public Events.DisplaySceneProgressBar DisplaySceneProgressBar = new Events.DisplaySceneProgressBar();
        public Events.InfoPlotRequest PlotInfoRequest = new Events.InfoPlotRequest();
        public Events.UpdateCutsInUI UpdateCutsInUI = new Events.UpdateCutsInUI();
        public Events.DefineSelectedColumn DefineSelectedColumn = new Events.DefineSelectedColumn();
        public Events.UpdateTimeInUI UpdateTimeInUI = new Events.UpdateTimeInUI();
        public Events.UpdateDisplayedPlotsInfo UpdateDisplayedPlotsInfo = new Events.UpdateDisplayedPlotsInfo();
        public Events.AskROIUpdate AskROIUpdateEvent = new Events.AskROIUpdate();
        public Events.UpdateCameraTarget UpdateCameraTarget = new Events.UpdateCameraTarget();
        public Events.ClickPlot ClickPlot = new Events.ClickPlot();
        public Events.IRMCalValuesUpdate IRMCalValuesUpdate = new Events.IRMCalValuesUpdate();
        

        #endregion members

        #region mono_behaviour

        /// <summary>
        /// This function is always called before any Start functions and also just after a prefab is instantiated. (If a GameObject is inactive during start up Awake is not called until it is made active.)
        /// </summary>
        protected void Awake()
        {
            int idScript = TimeExecution.getId();
            TimeExecution.startAwake(idScript, TimeExecution.ScriptsId.Base3DScene);

            go_ = new DisplayedObjects3DView();
            data_ = new SceneStatesInfo();
            m_CM = GetComponent<Column3DViewManager>();
            singlePatient = (name == "SP");


            // set meshes layer
            data_.MeshesLayerName = singlePatient ? "Meshes_SP" : "Meshes_MP";

            // init modes            
            modes = transform.Find("modes").gameObject.GetComponent<ModesManager>();
            modes.init(this);
            modes.SendModeSpecifications.AddListener((specs) =>
            {
                SendModeSpecifications.Invoke(specs);

                // update scene visibility
                updateVisibility(specs.itemMaskDisplay[0], specs.itemMaskDisplay[1], specs.itemMaskDisplay[2]);
            });

            // init GO
            initGameObjects();


            TimeExecution.endAwake(idScript, TimeExecution.ScriptsId.Base3DScene, gameObject);
        }


        /// <summary>
        /// Update is called once per frame. It is the main workhorse function for frame updates.
        /// </summary>
        protected void Update()
        {
            setCurrentModeSpecifications();

            if (modes.currentIdMode() == Mode.ModesId.NoPathDefined)
                return;

            // TEMP : useless
            for (int ii = 0; ii < data_.removeFrontPlaneList.Count; ++ii)
                data_.removeFrontPlaneList[ii] = false;


            // check if we must perform new cuts of the brain            
            if (data_.updateCutPlanes)
            {
                CM.planesCutsCopy = new List<Plane>();
                for (int ii = 0; ii < CM.planesList.Count; ++ii)
                    CM.planesCutsCopy.Add(new Plane(CM.planesList[ii].point, CM.planesList[ii].normal));

                updateMeshesCuts();
                endMeshesCuts();
                //if (m_cutMeshJob == null)
                //{
                //    // copy planes before the job
                //    CM.planesCutsCopy = new List<Plane>();
                //    for(int ii = 0; ii < CM.planesList.Count; ++ii)
                //        CM.planesCutsCopy.Add(new Plane(CM.planesList[ii].point, CM.planesList[ii].normal));

                //    m_cutMeshJob = new CutMeshJob();
                //    m_cutMeshJob.scene_ = this;
                //    m_cutMeshJob.Start();
                //}
            }

            // check job state
            //if (m_cutMeshJob != null)
            //{
            //    if (m_cutMeshJob.Update())
            //    {
            //        endMeshesCuts();
            //        m_cutMeshJob = null;
            //    }
            //}

            // check job state
            if (m_computeGeneratorsJob != null)
            {
                float currState;
                data_.rwl.AcquireReaderLock(1000);
                currState = data_.currentComputingState;
                data_.rwl.ReleaseReaderLock();

                displayScreenMessage("Computing...", 50f, 250, 40);
                displayProgressBar(currState, 50f, 250, 40);


                if (m_computeGeneratorsJob.Update())
                    endJob();
            }


            Profiler.BeginSample("TEST-MAIN-update");



            Profiler.EndSample();

            // display scene volume bbox (gizmos)
            //Geometry.displayBBoxDebug(m_.volumeBBox, transform.position);
            //Geometry.displayBBoxDebug(m_meshesBBox, transform.position);

            //for (int ii = 0; ii < go_.MeshesList.Count; ++ii)
            //    Geometry.displayNormalsDebug(go_.MeshesList[ii]);

            // display scene planes (gizmos)
            //for (int ii = 0; ii < m_CM.planesList.Count; ++ii)
            //    Geometry.displayBBoxPlaneIntersec(m_CM.DLLVolume.dllboundingBox(), m_CM.planesList[ii], transform.position);
        }

        #endregion mono_behaviour   

        #region functions

        public abstract void updateMeshesCuts();
        public abstract void endMeshesCuts();
        public abstract void sendAdditionnalPlotInfoRequest(Plot previousPlot = null);

        private void endJob()
        {
            // computing ended
            m_computeGeneratorsJob = null;
            data_.generatorIsComputing = false;

            // generators are now up to date
            data_.generatorUpToDate = true;
            data_.iEEGOutdated = false;

            // send inf values to overlays
            for (int ii = 0; ii < m_CM.nbIEEGCol(); ++ii)
            {
                float maxValue = Math.Max(Math.Abs(m_CM.IEEGCol(ii).sharedMinInf), Math.Abs(m_CM.IEEGCol(ii).sharedMaxInf));
                float minValue = -maxValue;
                minValue += m_CM.IEEGCol(ii).middle;
                maxValue += m_CM.IEEGCol(ii).middle;
                SendColorMapValues.Invoke(minValue, m_CM.IEEGCol(ii).middle, maxValue, ii);
            }

            // we need to update the textures with the generators
            for (int ii = 0; ii < data_.texturesCutToUpdateMask.Count; ++ii)
            {
                data_.texturesCutToUpdateMask[ii] = true;
            }

            // amplitudes are not displayed yet
            for (int ii = 0; ii < m_CM.nbIEEGCol(); ++ii)
                m_CM.IEEGCol(ii).updateAmplitude = true;

            //####### CHECK ACCESS
            if (!modes.functionAccess(Mode.FunctionsId.post_updateGenerators))
            {
                Debug.LogError("-ERROR : Base3DScene::post_updateGenerators -> no acess for mode : " + modes.currentModeName());
            }
            //##################

            displayScreenMessage("Computing finished !", 1f, 250, 40);
            displayProgressBar(1f, 1f, 250, 40);

            // update plots visibility
            m_CM.updateColumnsPlotsRendering(data_);

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.post_updateGenerators);
            //##################
        }

        /// <summary>
        /// Init gameobjects of the scene
        /// </summary>
        protected void initGameObjects()
        {
            // init parents 
            go_.PlotsParent = transform.Find("elecs").gameObject;
            go_.MeshesParent = transform.Find("meshes").gameObject;
            go_.CutsParent = transform.Find("cuts").gameObject;

            // init lights
            go_.SharedDirLight = transform.parent.Find("directionnal light").gameObject;

            // init default planes
            m_CM.planesList = new List<Plane>();
            m_CM.idPlanesOrientationList = new List<int>();
            m_CM.planesOrientationFlipList = new List<bool>();
            data_.removeFrontPlaneList = new List<bool>();
            data_.numberOfCutsPerPlane = new List<int>();
            data_.texturesCutToUpdateMask = new List<bool>();
            go_.CutsList = new List<GameObject>();

            // initialize lists with defaults gameobjects
            go_.MeshesList = new List<GameObject>(m_CM.meshSplitNb);
            for (int ii = 0; ii < m_CM.meshSplitNb; ++ii)
            { 
                go_.MeshesList.Add(Instantiate(BaseGameObjects.Brain));
                go_.MeshesList[ii].name = "brain_" + ii;
                go_.MeshesList[ii].transform.parent = go_.MeshesParent.transform;
                go_.MeshesList[ii].AddComponent<MeshCollider>();
                go_.MeshesList[ii].SetActive(true);
                go_.MeshesList[ii].layer = LayerMask.NameToLayer(data_.MeshesLayerName);
            }

            // TEST tri erasing
            go_.MeshesListTriErasing = new List<GameObject>(m_CM.meshSplitNb);
            for (int ii = 0; ii < m_CM.meshSplitNb; ++ii)
            {
                go_.MeshesListTriErasing.Add(Instantiate(BaseGameObjects.Brain));
                go_.MeshesListTriErasing[ii].name = "tri_erasing_brain_" + ii;
                go_.MeshesListTriErasing[ii].transform.parent = go_.MeshesParent.transform;
                go_.MeshesListTriErasing[ii].AddComponent<MeshCollider>();
                go_.MeshesListTriErasing[ii].SetActive(true);
                go_.MeshesListTriErasing[ii].layer = LayerMask.NameToLayer("Inactive");
            }
        }

        /// <summary>
        /// Reset the gameobjects of the scene
        /// </summary>
        public void resetGameObjets()
        {
            // destroy meshes
            for (int ii = 0; ii < go_.MeshesList.Count; ++ii)
            {
                Destroy(go_.MeshesList[ii]);
            }

            for (int ii = 0; ii < go_.MeshesListTriErasing.Count; ++ii)
            {
                Destroy(go_.MeshesListTriErasing[ii]);
            }

            for (int ii = 0; ii < go_.CutsList.Count; ++ii)
            {
                go_.CutsList[ii].SetActive(false);
            }

            // reset meshes
            go_.MeshesList = new List<GameObject>(m_CM.meshSplitNb);
            for (int ii = 0; ii < m_CM.meshSplitNb; ++ii)
            {
                go_.MeshesList.Add(Instantiate(BaseGameObjects.Brain));
                go_.MeshesList[ii].name = "brain_" + ii;
                go_.MeshesList[ii].transform.parent = go_.MeshesParent.transform;
                go_.MeshesList[ii].AddComponent<MeshCollider>();
                go_.MeshesList[ii].SetActive(true);
            }

            go_.MeshesListTriErasing = new List<GameObject>(m_CM.meshSplitNb);
            for (int ii = 0; ii < m_CM.meshSplitNb; ++ii)
            {
                go_.MeshesListTriErasing.Add(Instantiate(BaseGameObjects.Brain));
                go_.MeshesListTriErasing[ii].name = "tri_erasing_brain_" + ii;
                go_.MeshesListTriErasing[ii].transform.parent = go_.MeshesParent.transform;
                go_.MeshesListTriErasing[ii].AddComponent<MeshCollider>();
                go_.MeshesListTriErasing[ii].SetActive(true);
            }
        }

        /// <summary>
        /// Set UI screen space/overlays layers mask settings corresponding to the current mode of the scene
        /// </summary>
        public void setCurrentModeSpecifications(bool force = false)
        {
            modes.setCurrentModeSpecifications(force);
        }

        /// <summary>
        /// Update the plot masks
        /// </summary>
        /// <param name="allColumn"></param>
        /// <param name="plotGO"></param>
        /// <param name="action"> 0 : excluded / 1 : included / 2 : blacklisted / 3 : unblacklist / 4 : highlight / 5 : unhighlight </param>
        /// <param name="range"> 0 : a plot / 1 : all plots from an electrode / 2 : all plots from a patient / 3 : all highlighted / 4 : all unhighlighted 
        /// / 5 : all plots / 6 : in ROI / 7 : not in ROI / 8 : names filter </param>
        public void setPlotMask(bool allColumn, GameObject plotGO, int action = 0, int range = 0, string nameFilter = "")
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.updateMaskPlot))
            {
                Debug.LogError("-ERROR : Base3DScene::updateMaskPlot -> no acess for mode : " + modes.currentModeName());
                return;
            }
            //##################

            Column3DView col = m_CM.currentColumn();

            List<List<int>> colIdPlots = new List<List<int>>(m_CM.columns.Count);
            for(int ii = 0; ii < m_CM.columns.Count; ++ii)
            {
                colIdPlots.Add(new List<int>(m_CM.PlotsList.Count));

                if(!allColumn)
                {
                    if (ii != m_CM.idSelectedColumn)
                        continue;
                }

                switch(range)
                {
                    case 0:
                        {// one specific plot
                            colIdPlots[ii].Add(plotGO.GetComponent<Plot>().idGlobal);
                        }
                    break;
                    case 1:
                        { // all plots from an electrode
                            Transform parentElectrode = plotGO.transform.parent;
                            for (int jj = 0; jj < parentElectrode.childCount; ++jj)
                                colIdPlots[ii].Add(parentElectrode.GetChild(jj).gameObject.GetComponent<Plot>().idGlobal);
                        }
                    break;
                    case 2:
                        { // all plots from a patient
                            Transform parentPatient = plotGO.transform.parent.parent;
                            for (int jj = 0; jj < parentPatient.childCount; ++jj)
                            {
                                Transform parentElectrode = parentPatient.GetChild(jj);
                                for (int kk = 0; kk < parentElectrode.childCount; kk++)
                                {
                                    colIdPlots[ii].Add(parentElectrode.GetChild(kk).gameObject.GetComponent<Plot>().idGlobal);
                                }
                            }
                        }
                    break;
                    case 3: // all highlighted plots
                        {
                            for (int jj = 0; jj < m_CM.col(ii).Plots.Count; ++jj)
                            {                                
                                if (m_CM.col(ii).Plots[jj].highlight)
                                    colIdPlots[ii].Add(jj);
                            }
                        }
                        break;
                    case 4: // all unhighlighted plots
                        {                            
                            for (int jj = 0; jj < m_CM.col(ii).Plots.Count; ++jj)
                            {
                                if (!m_CM.col(ii).Plots[jj].highlight)
                                    colIdPlots[ii].Add(jj);
                            }
                        }
                        break;
                    case 5: // all plots
                        {
                            for (int jj = 0; jj < m_CM.col(ii).Plots.Count; ++jj)
                            {                                
                                colIdPlots[ii].Add(jj);
                            }
                        }
                        break;
                    case 6: // in ROI
                        {
                            for (int jj = 0; jj < m_CM.col(ii).Plots.Count; ++jj)
                            {
                                if (!m_CM.col(ii).Plots[jj].columnROI)
                                    colIdPlots[ii].Add(jj);
                            }
                        }
                        break;
                    case 7: // no in ROI
                        {
                            for (int jj = 0; jj < m_CM.col(ii).Plots.Count; ++jj)
                            {
                                if (m_CM.col(ii).Plots[jj].columnROI)
                                    colIdPlots[ii].Add(jj);
                            }
                        }
                        break;
                    case 8: // names filter
                        {
                            for (int jj = 0; jj < m_CM.col(ii).Plots.Count; ++jj)
                            {
                                if (m_CM.col(ii).Plots[jj].fullName.Contains(nameFilter))
                                    colIdPlots[ii].Add(jj);
                            }
                        }
                        break;
                }
            }

            // apply action
            for (int ii = 0; ii < colIdPlots.Count; ++ii)
            {
                for (int jj = 0; jj < colIdPlots[ii].Count; jj++)
                {
                    if (action == 0 || action == 1) // excluded | included
                    {
                        m_CM.col(ii).Plots[colIdPlots[ii][jj]].exclude = (action == 0);
                    }
                    else if (action == 2 || action == 3)// blacklisted | unblacklisted
                    {
                        m_CM.col(ii).Plots[colIdPlots[ii][jj]].blackList = (action == 2);
                    }
                    else // highlight | unhighlight
                    {
                        m_CM.col(ii).Plots[colIdPlots[ii][jj]].highlight = (action == 4);
                    }
                }
            }


            m_CM.updateColumnsPlotsRendering(data_);

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.updateMaskPlot);
            //##################
        }

        /// <summary>
        /// Set the mesh part to be displayed in the scene
        /// </summary>
        /// <param name="meshPartToDisplay"></param>
        public void setDisplayedMeshPart(SceneStatesInfo.MeshPart meshPartToDisplay)
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.setDisplayedMesh))
            {
                Debug.LogError("-ERROR : Base3DScene::setDisplayedMesh -> no acess for mode : " + modes.currentModeName());
                return;
            }
            //##################

            if (!data_.meshesLoaded || !data_.volumeLoaded)
                return;

            data_.meshPartToDisplay = meshPartToDisplay;
            data_.updateCutPlanes = true;

            // update textures
            for (int ii = 0; ii < m_CM.planesList.Count; ++ii)
                data_.texturesCutToUpdateMask[ii] = true;

            // update plots visibility
            m_CM.updateColumnsPlotsRendering(data_);

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.setDisplayedMesh);
            //##################
        }

        /// <summary>
        /// Set the mesh type to be displayed in the scene
        /// </summary>
        /// <param name="meshTypeToDisplay"></param>
        public void setDisplayedMeshType(SceneStatesInfo.MeshType meshTypeToDisplay)
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.setDisplayedMesh))
            {
                Debug.LogError("-ERROR : Base3DScene::setDisplayedMesh -> no acess for mode : " + modes.currentModeName());
                return;
            }
            //##################

            if (!data_.meshesLoaded || !data_.volumeLoaded)
                return;

            switch(meshTypeToDisplay)
            {
                case SceneStatesInfo.MeshType.Hemi:
                    if (!data_.hemiMeshesAvailables)
                        return;
                    break;
                case SceneStatesInfo.MeshType.White:
                    if (!data_.whiteMeshesAvailables)
                        return;
                    break;
                case SceneStatesInfo.MeshType.Inflated:
                    if (!data_.whiteInflatedMeshesAvailables)
                        return;
                    break;
            }

            data_.meshTypeToDisplay = meshTypeToDisplay;
            data_.updateCutPlanes = true;

            // update textures
            for (int ii = 0; ii < m_CM.planesList.Count; ++ii)
                data_.texturesCutToUpdateMask[ii] = true;

            // update plots visibility
            m_CM.updateColumnsPlotsRendering(data_);

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.setDisplayedMesh);
            //##################
        }

        /// <summary>
        /// Add a new cut plane
        /// </summary>
        public void addNewPlane()
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.addNewPlane))
            {
                Debug.LogError("-ERROR : Base3DScene::addNewPlane -> no acess for mode : " + modes.currentModeName());
                return;
            }
            //##################

            m_CM.planesList.Add(new Plane(new Vector3(0, 0, 0), new Vector3(1, 0, 0)));
            m_CM.idPlanesOrientationList.Add(0);
            m_CM.planesOrientationFlipList.Add(false);
            data_.removeFrontPlaneList.Add(false);
            data_.numberOfCutsPerPlane.Add(data_.defaultNbOfCutsPerPlane);
            data_.texturesCutToUpdateMask.Add(true);

            GameObject cut = Instantiate(BaseGameObjects.Cut);
            cut.name = "cut_" + (m_CM.planesList.Count - 1);
            cut.transform.parent = go_.CutsParent.transform; ;
            cut.AddComponent<MeshCollider>();
            cut.layer = LayerMask.NameToLayer(data_.MeshesLayerName);
            go_.CutsList.Add(cut);
            go_.CutsList[go_.CutsList.Count - 1].layer = LayerMask.NameToLayer(data_.MeshesLayerName);

            // update columns manager
            m_CM.updateCutsNb(go_.CutsList.Count);

            // update plots visibility
            m_CM.updateColumnsPlotsRendering(data_);

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.addNewPlane);
            //##################
        }

        /// <summary>
        /// Remove the last cut plane
        /// </summary>
        public void removeLastPlane()
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.removeLastPlane))
            {
                Debug.LogError("-ERROR : Base3DScene::removeLastPlane -> no acess for mode : " + modes.currentModeName());
                return;
            }
            //##################


            m_CM.planesList.RemoveAt(m_CM.planesList.Count - 1);
            m_CM.idPlanesOrientationList.RemoveAt(m_CM.idPlanesOrientationList.Count - 1);
            m_CM.planesOrientationFlipList.RemoveAt(m_CM.planesOrientationFlipList.Count - 1);
            data_.removeFrontPlaneList.RemoveAt(data_.removeFrontPlaneList.Count - 1);
            data_.numberOfCutsPerPlane.RemoveAt(data_.numberOfCutsPerPlane.Count - 1);
            data_.texturesCutToUpdateMask.RemoveAt(data_.texturesCutToUpdateMask.Count - 1);

            Destroy(go_.CutsList[go_.CutsList.Count - 1]);
            go_.CutsList.RemoveAt(go_.CutsList.Count - 1);

            data_.updateCutPlanes = true;

            // update columns manager
            m_CM.updateCutsNb(go_.CutsList.Count);

            // update plots visibility
            m_CM.updateColumnsPlotsRendering(data_);

            // update cut images display with the new selected column
            UpdateCutsInUI.Invoke(m_CM.getBrainCutTextureList(m_CM.idSelectedColumn, true, data_.generatorUpToDate, data_.displayLatenciesMode), m_CM.idSelectedColumn, m_CM.planesList.Count);

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.removeLastPlane);
            //##################
        }

        /// <summary>
        /// Update a cut plane
        /// </summary>
        /// <param name="idOrientation"></param>
        /// <param name="flip"></param>
        /// <param name="removeFrontPlane"></param>
        /// <param name="customNormal"></param>
        /// <param name="idPlane"></param>
        /// <param name="position"></param>
        public void updatePlane(int idOrientation, bool flip, bool removeFrontPlane, Vector3 customNormal, int idPlane, float position)
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.updatePlane))
            {
                Debug.LogError("-ERROR : Base3DScene::updatePlane -> no acess for mode : " + modes.currentModeName());
                return;
            }
            //##################

            Plane newPlane = new Plane(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
            if (idOrientation == 3 || !data_.volumeLoaded) // custom normal
            {
                if (customNormal.x != 0 || customNormal.y != 0 || customNormal.z != 0)
                    newPlane.normal = customNormal;
                else
                    newPlane.normal = new Vector3(1, 0, 0);
            }
            else
            {
                m_CM.DLLVolume.setPlaneWithOrientation(newPlane, idOrientation, flip);
            }

            m_CM.planesList[idPlane].normal = newPlane.normal;
            m_CM.idPlanesOrientationList[idPlane] = idOrientation;
            m_CM.planesOrientationFlipList[idPlane] = flip;
            data_.removeFrontPlaneList[idPlane] = removeFrontPlane;
            data_.lastIdPlaneModified = idPlane;

            // ########### cuts base on the volume
            //float offset;
            //if (data_.volumeLoaded)
            //    offset = m_CM.DLLVolume.sizeOffsetCutPlane(m_CM.planesList[idPlane], data_.numberOfCutsPerPlane[idPlane]);
            //else
            //    offset = 0.1f;
            //m_CM.planesList[idPlane].point = data_.volumeCenter + m_CM.planesList[idPlane].normal * (position - 0.5f) * offset * data_.numberOfCutsPerPlane[idPlane];

            // ########### cuts base on the mesh
            float offset;
            if (data_.meshToDisplay != null)
            {
                offset = data_.meshToDisplay.sizeOffsetCutPlane(m_CM.planesList[idPlane], data_.numberOfCutsPerPlane[idPlane]);
                offset *= 1.05f; // upsize a little bit the bbox for planes
            }
            else
                offset = 0.1f;

            m_CM.planesList[idPlane].point = data_.meshCenter + m_CM.planesList[idPlane].normal * (position - 0.5f) * offset * data_.numberOfCutsPerPlane[idPlane];

            for (int ii = 0; ii < m_CM.planesList.Count; ++ii)
            {
                data_.texturesCutToUpdateMask[ii] = false;
            }
            data_.texturesCutToUpdateMask[idPlane] = data_.updateCutPlanes = true;

            // update plots visibility
            m_CM.updateColumnsPlotsRendering(data_);

            // update cameras cuts psot display
            ModifyPlanesCuts.Invoke();

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.updatePlane);
            //##################
        }

        /// <summary>
        /// Reset the volume of the scene
        /// </summary>
        /// <param name="pathNIIBrainVolumeFile"></param>
        /// <returns></returns>
        public bool resetNIIBrainVolumeFile(string pathNIIBrainVolumeFile)
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.resetNIIBrainVolumeFile))
            {
                Debug.LogError("-ERROR : Base3DScene::resetNIIBrainVolumeFile -> no acess for mode : " + modes.currentModeName());
                return false;
            }

            data_.volumeLoaded = false;

            // checks parameter
            if (pathNIIBrainVolumeFile.Length == 0)
            {
                Debug.LogError("-ERROR : Base3DScene::resetNIIBrainVolumeFile -> path NII brain volume file is empty. ");
                return (data_.meshesLoaded = false);
            }

            // load volume
            bool loadingSuccess = m_CM.DLLNii.loadNiftiFile(pathNIIBrainVolumeFile);
            if (loadingSuccess)
            {
                m_CM.DLLNii.convertToVolume(m_CM.DLLVolume);
                data_.volumeCenter = m_CM.DLLVolume.center();
            }
            else
            {
                Debug.LogError("-ERROR : Base3DScene::resetNIIBrainVolumeFile -> load NII file failed. " + pathNIIBrainVolumeFile);
                return data_.volumeLoaded;
            }

            data_.volumeLoaded = loadingSuccess;
            UpdatePlanes.Invoke();

            // send cal values to the UI
            IRMCalValuesUpdate.Invoke(m_CM.DLLVolume.retrieveExtremeValues());

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.resetNIIBrainVolumeFile);
            //##################

            return data_.volumeLoaded;
        }

        /// <summary>
        /// Reset the rendering settings for this scene, called by each camera before rendering
        /// </summary>
        /// <param name="cameraRotation"></param>
        abstract public void resetRenderingSettings(Vector3 cameraRotation);

        /// <summary>
        /// Update the selected column of the scene
        /// </summary>
        /// <param name="idColumn"></param>
        public void updateSelectedColumn(int idColumn)
        {
            if (idColumn >= m_CM.columns.Count)
                return;
            
            m_CM.idSelectedColumn = idColumn;

            // force mode to update UI
            modes.setCurrentModeSpecifications(true);

            //  update cut images display with the new selected column
            UpdateCutsInUI.Invoke(m_CM.getBrainCutTextureList(m_CM.idSelectedColumn, true, data_.generatorUpToDate, data_.displayLatenciesMode), m_CM.idSelectedColumn, m_CM.planesList.Count);
        }

        //public bool updateIEEGColumn(int indexColumn, Column3DView iEEGCol)
        //{

        //    // check if mesh must be updated
        //    bool updateGeometry = data_.updateGeometry;


        //    return true;
        //}

        //public bool updateIRMFColumn(int indexColumn, Column3DViewIRMF IRMFCol)
        //{
        //    bool updateGeometry = data_.updateGeometry;

        //    // compute once all the IRMF cuts
        //    if (data_.meshTypeToDisplay != SceneStatesInfo.MeshType.Inflated && updateGeometry && data_.updateIRMF)
        //    {
        //        for (int ii = 0; ii < m_CM.nbIRMFCol(); ++ii)
        //            for (int jj = 0; jj < m_CM.planesList.Count; ++jj)
        //                m_CM.colorTextureWithIRMF(ii, jj);

        //        data_.updateIRMF = false;
        //    }


        //    return true;
        //}


        /// <summary>
        /// Update the data render corresponding to the column
        /// </summary>
        /// <param name="indexColumn"></param>
        /// <returns></returns>
        public bool updateColumnRender(int indexColumn)
        {
            if (indexColumn >= m_CM.columns.Count)
                Debug.Log("error : " + indexColumn + " " + m_CM.columns.Count);

            Column3DView currCol = m_CM.col(indexColumn);
            Column3DViewIEEG currIEEGCol = null;
            if (!currCol.isIRMF)
                currIEEGCol = (Column3DViewIEEG)currCol;            

            // leave cases
            if (!data_.meshesLoaded || !data_.volumeLoaded || m_CM.DLLSplittedMeshesList.Count == 0)
                return false;

            Profiler.BeginSample("TEST-updateColumnRender 0");

            // check if mesh must be updated
            bool updateGeometry = data_.updateGeometry;

            if (updateGeometry)
                data_.updateIRMF = true;

            // (only with not inflated part)
            if (data_.meshTypeToDisplay != SceneStatesInfo.MeshType.Inflated && updateGeometry)
            {
                // compute cuts mesh uv and textures 
                for (int ii = 0; ii < m_CM.planesList.Count; ++ii)
                {                    
                    if (data_.texturesCutToUpdateMask[ii]) // check if texture cut must be updated
                    {
                        // create texture                      
                        m_CM.createTextureAndUpdateMeshUV(ii);

                        // update game object mesh
                        m_CM.DLLCutsList[ii + 1].udpateMeshWithSurface(go_.CutsList[ii].GetComponent<MeshFilter>().mesh);

                        data_.texturesCutToUpdateMask[ii] = false;
                    }
                }
            }

            Profiler.EndSample();
            Profiler.BeginSample("TEST-updateColumnRender 1");

            // compute once all the IRMF cuts
            if (data_.updateIRMF)
            {
                for (int ii = 0; ii < m_CM.nbIRMFCol(); ++ii)
                    for (int jj = 0; jj < m_CM.planesList.Count; ++jj)
                        m_CM.colorTextureWithIRMF(ii, jj);

                data_.updateIRMF = false;
            }


            if (data_.updateImageCuts)
            {
                if (m_CM.idSelectedColumn == indexColumn)
                {
                    // updates right menu cut images
                    UpdateCutsInUI.Invoke(m_CM.getBrainCutTextureList(indexColumn, true, data_.generatorUpToDate, data_.displayLatenciesMode), m_CM.idSelectedColumn, m_CM.planesList.Count);
                    data_.updateImageCuts = false;
                }
            }

            // update amplitudes
            if (data_.generatorUpToDate && !currCol.isIRMF)
            {
                if (currIEEGCol.updateAmplitude)
                {
                    Profiler.BeginSample("TEST-updateColumnRender 13");

                    // brain
                    m_CM.computeSurfaceTextCoordAmplitudes((data_.meshTypeToDisplay == SceneStatesInfo.MeshType.Inflated), indexColumn);

                    Profiler.EndSample();
                    Profiler.BeginSample("TEST-updateColumnRender 14");

                    // cuts
                    for (int ii = 0; ii < m_CM.planesList.Count; ++ii)
                    {
                        // apply amplitudes on the texture                        
                        m_CM.colorTextureWithAmplitudes(indexColumn, ii); 
                    }

                    Profiler.EndSample();
                    Profiler.BeginSample("TEST-updateColumnRender 15");

                    // set the sizes and the colors of the IEEG plots
                    currIEEGCol.updatePlotsSizeAndColorsArraysForIEEG();


                    Profiler.EndSample();
                    Profiler.BeginSample("TEST-updateColumnRender 16");

                    currIEEGCol.updatePlotsRendering(data_, null);

                    // updates right menu cut images
                    if (m_CM.idSelectedColumn == indexColumn)
                    {
                        UpdateCutsInUI.Invoke(m_CM.getBrainCutTextureList(indexColumn, true, data_.generatorUpToDate, data_.displayLatenciesMode), m_CM.idSelectedColumn, m_CM.planesList.Count);
                    }

                    Profiler.EndSample();
                }
            }

            
            Profiler.EndSample();
            Profiler.BeginSample("TEST-updateColumnRender 2");

            // udpate splits mesh uv
            if (updateGeometry) // common for all columns
            {
                for (int ii = 0; ii < m_CM.meshSplitNb; ++ii)
                    go_.MeshesList[ii].GetComponent<MeshFilter>().mesh.uv = m_CM.UVCoordinatesSplits[ii];
            }

            if (data_.generatorUpToDate && !m_CM.isIRMFColumn(indexColumn) && !data_.displayLatenciesMode) // if IRMF we use only one texture
            {
                for (int ii = 0; ii < m_CM.meshSplitNb; ++ii)
                {
                    // uv 2 (alpha) 
                    go_.MeshesList[ii].GetComponent<MeshFilter>().mesh.uv2 = currIEEGCol.DLLBrainTextureGeneratorList[ii].getAlphaUV();
                    // uv 3 (color map)
                    go_.MeshesList[ii].GetComponent<MeshFilter>().mesh.uv3 = currIEEGCol.DLLBrainTextureGeneratorList[ii].getAmplitudesUV();
                }
            }
            else if(updateGeometry)
            {
                for (int ii = 0; ii < m_CM.meshSplitNb; ++ii)
                {
                    // uv 2 (alpha) 
                    go_.MeshesList[ii].GetComponent<MeshFilter>().mesh.uv2 = m_CM.uvNull[ii];
                    // uv 3 (color map)
                    go_.MeshesList[ii].GetComponent<MeshFilter>().mesh.uv3 = m_CM.uvNull[ii];
                }
            }

            Profiler.EndSample();
            Profiler.BeginSample("TEST-updateColumnRender 3");

            // update cuts texture ( except for the inflated mesh)
            if (data_.meshTypeToDisplay != SceneStatesInfo.MeshType.Inflated)
            {
                for (int ii = 0; ii < m_CM.planesList.Count; ++ii)
                    go_.CutsList[ii].GetComponent<Renderer>().material.mainTexture = m_CM.getBrainCutTexture(indexColumn,ii, false, data_.generatorUpToDate, data_.displayLatenciesMode);
            }

            data_.updateGeometry = false;

            //column is now updated
            if (!currCol.isIRMF)
                currIEEGCol.updateAmplitude = false;

            return true;
        }

        /// <summary>
        /// Update displayed amplitudes with the timeline id corresponding to global timeline mode or individual timeline mode
        /// </summary>
        /// <param name="globalTimeline"></param>
        public void updateAllTimes(bool globalTimeline)
        {
            for (int ii = 0; ii < m_CM.nbIEEGCol(); ++ii)
            {
                m_CM.IEEGCol(ii).currentTimeLineID = globalTimeline ? (int)m_CM.commonTimelineValue : m_CM.IEEGCol(ii).columnTimeLineID;
                m_CM.IEEGCol(ii).updateAmplitude = true;
            }
        }

        /// <summary>
        /// Update the brain and the cuts meshes colliders
        /// </summary>
        public void updateMeshesColliders()
        {
            if (!data_.meshesLoaded || !data_.volumeLoaded)
                return;

            // update splits colliders
            for(int ii = 0; ii < go_.MeshesList.Count; ++ii)
            {
                go_.MeshesList[ii].GetComponent<MeshCollider>().sharedMesh = null;
                go_.MeshesList[ii].GetComponent<MeshCollider>().sharedMesh = go_.MeshesList[ii].GetComponent<MeshFilter>().mesh;
            }

            // update cuts colliders
            for (int ii = 0; ii < go_.CutsList.Count; ++ii)
            {
                go_.CutsList[ii].GetComponent<MeshCollider>().sharedMesh = null;
                go_.CutsList[ii].GetComponent<MeshCollider>().sharedMesh = go_.CutsList[ii].GetComponent<MeshFilter>().mesh;
            }

            data_.collidersUpdated = true;
        }

        /// <summary>
        /// Update the textures generator
        /// </summary>
        public void updateGenerators()
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.pre_updateGenerators))
            {
                Debug.LogError("-ERROR : Base3DScene::pre_updateGenerators -> no acess for mode : " + modes.currentModeName());
                return;
            }
            //##################

            if (!data_.meshesLoaded || !data_.volumeLoaded)
                return;

            if (data_.updateCutPlanes) // if update cut plane is pending, cancel action
                return;


            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.pre_updateGenerators);
            //##################

            data_.generatorUpToDate = false;
            data_.generatorIsComputing = true;

            m_computeGeneratorsJob = new ComputeGeneratorsJob();
            m_computeGeneratorsJob.data_ = data_;
            m_computeGeneratorsJob.cm_ = m_CM;
            m_computeGeneratorsJob.Start();
        }

        /// <summary>
        /// Update the displayed amplitudes on the brain and the cuts with the slider position.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="slider"></param>
        /// <param name="globalTimeline"> if globaltime is true, update all columns with the same slider, else udapte only current selected column </param>
        public void setTime(int id, float value, bool globalTimeline)
        {
            if (!data_.meshesLoaded || !data_.volumeLoaded && !data_.timelinesLoaded)
                return;

            m_CM.globalTimeline = globalTimeline;

            if (m_CM.globalTimeline)
            {
                m_CM.commonTimelineValue = value;
                for (int ii = 0; ii < m_CM.nbIEEGCol(); ++ii)
                {
                    m_CM.IEEGCol(ii).currentTimeLineID = (int)m_CM.commonTimelineValue;
                    m_CM.IEEGCol(ii).updateAmplitude = true;
                }
            }
            else
            {
                Column3DViewIEEG currIEEGCol = (Column3DViewIEEG)m_CM.columns[id];
                currIEEGCol.columnTimeLineID = (int)value;
                currIEEGCol.currentTimeLineID = currIEEGCol.columnTimeLineID;
                currIEEGCol.updateAmplitude = true;
            }

            m_CM.updateColumnsPlotsRendering(data_);

            UpdateTimeInUI.Invoke();
        }

        /// <summary>
        /// Mouse scroll events managements
        /// </summary>
        /// <param name="scrollDelta"></param>
        public void mouseScrollAction(Vector2 scrollDelta)
        {
            // nothing for now 
            // (not not delete children classes use it)
        }

        /// <summary>
        /// Keyboard events management
        /// </summary>
        /// <param name="keyCode"></param>
        public void keyboardAction(KeyCode keyCode)
        {
            if (!data_.meshesLoaded || !data_.volumeLoaded)
                return;

            switch (keyCode)
            {
                // enable/disable holes in the cuts
                case KeyCode.H:
                    data_.holesEnabled = !data_.holesEnabled;
                    for (int ii = 0; ii < data_.texturesCutToUpdateMask.Count; ++ii)
                    {
                        data_.texturesCutToUpdateMask[ii] = true;
                    }
                    data_.updateCutPlanes = true;
                    break;
            }
        }

        /// <summary>
        /// Enter in the tri erasing mode
        /// </summary>
        public void enableTriErasingMode()
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.enableTriErasingMode))
            {
                Debug.LogError("-ERROR : Base3DScene::enableTriErasinMode -> no acess for mode : " + modes.currentModeName());
                return;
            }
            //##################

            //triErasingMode = true;

            // disables cuts and brain 
            for (int ii = 0; ii < go_.MeshesList.Count; ++ii)
            {
                go_.MeshesList[ii].layer = LayerMask.NameToLayer("Inactive");
            }
            for (int ii = 0; ii < go_.CutsList.Count; ++ii)
            {
                go_.CutsList[ii].layer = LayerMask.NameToLayer("Inactive");
            }
            for (int ii = 0; ii < go_.MeshesListTriErasing.Count; ++ii)
            {
                go_.MeshesListTriErasing[ii].layer = LayerMask.NameToLayer(data_.MeshesLayerName);
            }

            // ...


            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.enableTriErasingMode);
            //##################
        }

        /// <summary>
        /// Exit the tri erasing mode
        /// </summary>
        public void disableTriErasingMode()
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.disableTriErasingMode))
            {
                Debug.LogError("-ERROR : Base3DScene::disableTriErasingMode -> no acess for mode : " + modes.currentModeName());
                return;
            }
            //##################

            //triErasingMode = false;

            // enable cuts and brain 

            // disable tri erasing gameobjects        

            for (int ii = 0; ii < go_.MeshesList.Count; ++ii)
            {
                go_.MeshesList[ii].layer = LayerMask.NameToLayer(data_.MeshesLayerName);
            }
            for (int ii = 0; ii < go_.CutsList.Count; ++ii)
            {
                go_.CutsList[ii].layer = LayerMask.NameToLayer(data_.MeshesLayerName);
            }
            for (int ii = 0; ii < go_.MeshesListTriErasing.Count; ++ii)
            {
                go_.MeshesListTriErasing[ii].layer = LayerMask.NameToLayer("Inactive");
            }


            // ...

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.disableTriErasingMode);
            //##################
        }

        /// <summary>
        /// Return the id of the current select column in the scene
        /// </summary>
        /// <returns></returns>
        public int retrieveCurrentSelectedColumnId()
        {
            return m_CM.idSelectedColumn;
        }

        /// <summary>
        /// Update the middle of an iEEG column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"></param>
        public void updateMiddle(float value, int columnId)
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.updateMiddle))
            {
                Debug.LogError("-ERROR : Base3DScene::updateMiddle -> no acess for mode : " + modes.currentModeName());
                return;
            }
            //##################            

            // update value
            Column3DViewIEEG IEEGCol = (Column3DViewIEEG)m_CM.col(columnId);
            if (IEEGCol.middle != value)
                IEEGCol.middle = value;
            else
                return;

            for (int ii = 0; ii < m_CM.planesList.Count; ++ii)
            {
                data_.texturesCutToUpdateMask[ii] = true;
            }
            if (data_.generatorUpToDate == true)
                data_.updateCutPlanes = true;

            data_.generatorUpToDate = false;
            m_CM.updateColumnsPlotsRendering(data_);

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.updateMiddle);
            //##################
        }

        /// <summary>
        /// Update the max distance of an iEEG column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"></param>
        public void updateMaxDistance(float value, int columnId)
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.updateMiddle)) // TEMP
            {
                Debug.LogError("-ERROR : Base3DScene::updateMiddle -> no acess for mode : " + modes.currentModeName());
                return;
            }
            //##################

            // update value
            Column3DViewIEEG IEEGCol = (Column3DViewIEEG)m_CM.col(columnId);
            if (IEEGCol.maxDistanceElec != value)
                IEEGCol.maxDistanceElec = value;
            else
                return;

            for (int ii = 0; ii < m_CM.planesList.Count; ++ii)
                data_.texturesCutToUpdateMask[ii] = true;
            data_.updateCutPlanes = true;

            data_.generatorUpToDate = false;
            m_CM.updateColumnsPlotsRendering(data_);

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.updateMiddle);
            //##################
        }

        public void updateIRMCalMin(float value)
        {
            m_CM.IRMCalMinFactor = value;
            data_.updateGeometry = true;

            // update cut textures
            for (int ii = 0; ii < m_CM.planesList.Count; ++ii)
                data_.texturesCutToUpdateMask[ii] = true;
            data_.updateImageCuts = true;

            data_.generatorUpToDate = false;
            m_CM.updateColumnsPlotsRendering(data_);

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.updateMiddle);
            //##################
        }

        public void updateIRMCalMax(float value)
        {
            m_CM.IRMCalMaxFactor = value;
            data_.updateGeometry = true;

            for (int ii = 0; ii < m_CM.planesList.Count; ++ii)
                data_.texturesCutToUpdateMask[ii] = true;
            data_.updateImageCuts = true;

            data_.generatorUpToDate = false;
            m_CM.updateColumnsPlotsRendering(data_);

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.updateMiddle);
            //##################
        }

        /// <summary>
        /// Update the cal min value of the input IRMF column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"> id of the IRMF columns</param>
        public void updateIRMFCalMin(float value, int columnId)
        {
            m_CM.IRMFCol(columnId).calMin = value;
            forceIRMFUpdate();
        }

        /// <summary>
        /// Update the cal max value of the input IRMF column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"> id of the IRMF columns</param>
        public void updateIRMFCalMax(float value, int columnId)
        {
            m_CM.IRMFCol(columnId).calMax = value;
            forceIRMFUpdate();
        }

        /// <summary>
        /// Update the alpha value of the input IRMF column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"> id of the IRMF columns</param>
        public void updateIRMFAlpha(float value, int columnId)
        {
            m_CM.IRMFCol(columnId).alpha = value;
            forceIRMFUpdate();
        }

        public void forceIRMFUpdate()
        {
            data_.updateIRMF = true;
            data_.updateImageCuts = true;
        }

        /// <summary>
        /// Update the min alpha of an iEEG column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"></param>
        public void updateMinAlpha(float value, int columnId)
        {
            m_CM.IEEGCol(columnId).alphaMin = value;

            // update render
            for (int ii = 0; ii < m_CM.nbIEEGCol(); ++ii)
            {
                m_CM.IEEGCol(ii).updateAmplitude = true;
            }
        }

        /// <summary>
        /// Update the max alpha of an iEEG column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"></param>
        public void updateMaxAlpha(float value, int columnId)
        {
            m_CM.IEEGCol(columnId).alphaMax = value;

            // update render
            for (int ii = 0; ii < m_CM.nbIEEGCol(); ++ii)
            {
                m_CM.IEEGCol(ii).updateAmplitude = true;
            }
        }

        /// <summary>
        /// Update the bubbles gain of an iEEG column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"></param>
        public void updateGainBubbles(float value, int columnId)
        {
            // update value
            m_CM.IEEGCol(columnId).gainBubbles = value;

            if (!data_.meshesLoaded || !data_.volumeLoaded)
                return;

            // update visibility of the plots
            m_CM.updateColumnsPlotsRendering(data_);
        }


        /// <summary>
        /// Update the span min of an iEEG column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"></param>
        public void updateSpanMin(float value, int columnId)
        {
            // update value
            Column3DViewIEEG IEEGCol = m_CM.IEEGCol(columnId);
            if (IEEGCol.spanMin != value)
                IEEGCol.spanMin = value;
            else
                return;

            if (!data_.meshesLoaded || !data_.volumeLoaded)
                return;

            for (int ii = 0; ii < m_CM.planesList.Count; ++ii)
            {
                data_.texturesCutToUpdateMask[ii] = true;
            }
            if(data_.generatorUpToDate == true)
                data_.updateCutPlanes = true;

            data_.generatorUpToDate = false;
            m_CM.updateColumnsPlotsRendering(data_);

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.updateMiddle);
            //##################
        }

        /// <summary>
        /// Update the span max of an iEEG column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"></param>
        public void updateSpanMax(float value, int columnId)
        {
            // update value
            Column3DViewIEEG IEEGCol = m_CM.IEEGCol(columnId);
            if (IEEGCol.spanMax != value)
                IEEGCol.spanMax = value;
            else
                return;

            if (!data_.meshesLoaded || !data_.volumeLoaded)
                return;

            for (int ii = 0; ii < m_CM.planesList.Count; ++ii)
            {
                data_.texturesCutToUpdateMask[ii] = true;
            }
            if (data_.generatorUpToDate == true)
                data_.updateCutPlanes = true;

            data_.generatorUpToDate = false;
            m_CM.updateColumnsPlotsRendering(data_);

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.updateMiddle);
            //##################
        }

        /// <summary>
        /// Return the number of IRMF colums
        /// </summary>
        /// <returns></returns>
        public int getIRMFColumnsNb()
        {
            return m_CM.nbIRMFCol();
        }

        /// <summary>
        /// Load an IRMF column
        /// </summary>
        /// <param name="IRMFPath"></param>
        /// <returns></returns>
        public bool loadIRMFColumn(string IRMFPath)
        {
            if (m_CM.DLLNii.loadNiftiFile(IRMFPath))
                return true;

            Debug.LogError("-ERROR : Base3DScene::loadIRMFColumn -> load NII file failed. " + IRMFPath);
            return false;
        }

        /// <summary>
        /// Add an IRMF column
        /// </summary>
        /// <param name="IRMFPath"></param>
        /// <returns></returns>
        public bool addIRMFColumn(string IMRFLabel)
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.addIRMFColumn))
            {
                Debug.LogError("-ERROR : Base3DScene::addIRMFColumn -> no acess for mode : " + modes.currentModeName());
                return false;
            }
            //##################

            // update columns number
            int newIRMFColNb = m_CM.nbIRMFCol() + 1;
            m_CM.updateColumnsNb(m_CM.nbIEEGCol(), newIRMFColNb, m_CM.planesList.Count);

            int idCol = newIRMFColNb - 1;
            m_CM.IRMFCol(idCol).Label = IMRFLabel;

            // update plots visibility
            m_CM.updateColumnsPlotsRendering(data_);

            // convert to volume            
            m_CM.DLLNii.convertToVolume(m_CM.DLLVolumeIRMFList[idCol]);

            data_.updateIRMF = true;

            if (!singlePatient)
                AskROIUpdateEvent.Invoke(m_CM.nbIEEGCol() + idCol);

            // send parameters to UI
            IRMCalValues calValues = m_CM.DLLVolumeIRMFList[idCol].retrieveExtremeValues();

            IRMFDataParameters IRMFParams = new IRMFDataParameters();
            IRMFParams.calValues = m_CM.DLLVolumeIRMFList[idCol].retrieveExtremeValues();
            IRMFParams.columnId = idCol;
            IRMFParams.alpha  = m_CM.IRMFCol(idCol).alpha;
            IRMFParams.calMin = m_CM.IRMFCol(idCol).calMin;
            IRMFParams.calMax = m_CM.IRMFCol(idCol).calMax;
            IRMFParams.singlePatient = singlePatient;

            m_CM.IRMFCol(idCol).calMin = IRMFParams.calValues.computedCalMin;
            m_CM.IRMFCol(idCol).calMax = IRMFParams.calValues.computedCalMax;

            SendIRMFParameters.Invoke(IRMFParams);

            // update camera
            UpdateCameraTarget.Invoke(singlePatient ?  m_CM.BothHemi.boundingBox().center() : m_MNI.BothHemi.boundingBox().center());

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.addIRMFColumn);
            //##################

            return true;
        }

        /// <summary>
        /// Remove the last IRMF column
        /// </summary>
        public void removeLastIRMFColumn()
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.removeLastIRMFColumn))
            {
                Debug.LogError("-ERROR : Base3DScene::removeLastIRMFColumn -> no acess for mode : " + modes.currentModeName());
                return;
            }
            //##################
            
            // update columns number
            m_CM.updateColumnsNb(m_CM.nbIEEGCol(), m_CM.nbIRMFCol() - 1, m_CM.planesList.Count);

            // update plots visibility
            m_CM.updateColumnsPlotsRendering(data_);

            data_.updateIRMF = true;

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.removeLastIRMFColumn);
            //##################
        }

        public Texture2D getIRMFHistogram(int idColumn)
        {
            return DLL.Texture.generateDistributionHistogram(m_CM.DLLVolumeIRMFList[idColumn], 120, 100, 0f, 1f).getTexture2D();
        }

        /// <summary>
        /// Is the latency mode enabled ?
        /// </summary>
        /// <returns></returns>
        public bool isLatencyMode()
        {
            return data_.displayLatenciesMode;
        }

        /// <summary>
        /// Updat visibility of the columns 3D items
        /// </summary>
        /// <param name="displayMeshes"></param>
        /// <param name="displayPlots"></param>
        /// <param name="displayROI"></param>
        public void updateVisibility(bool displayMeshes, bool displayPlots, bool displayROI)
        {
            if (!singlePatient)
                m_CM.updateROIVisibility(displayROI);

            m_CM.updatePlotsVisibility(displayPlots);

            for (int ii = 0; ii < m_CM.nbIEEGCol(); ++ii)
            {
                m_CM.IEEGCol(ii).setVisiblePlots(displayPlots);
            }

            for (int ii = 0; ii < m_CM.nbIRMFCol(); ++ii)
            {
                m_CM.IRMFCol(ii).setVisiblePlots(displayPlots);
            }            
        }

        /// <summary>
        /// Send iEEG read min/max/middle to the IEEG menu
        /// </summary>
        public void sendIEEGDataToMenu()
        {            
            for (int ii = 0; ii < m_CM.nbIEEGCol(); ++ii)
            {
                iEEGDataParameters iEEGDataParams;
                iEEGDataParams.minAmp = m_CM.IEEGCol(ii).minAmp;
                iEEGDataParams.maxAmp = m_CM.IEEGCol(ii).maxAmp;

                iEEGDataParams.spanMin = m_CM.IEEGCol(ii).spanMin;
                iEEGDataParams.middle = m_CM.IEEGCol(ii).middle;
                iEEGDataParams.spanMax = m_CM.IEEGCol(ii).spanMax;

                iEEGDataParams.gain = m_CM.IEEGCol(ii).gainBubbles;
                iEEGDataParams.maxDistance = m_CM.IEEGCol(ii).maxDistanceElec;
                iEEGDataParams.columnId = ii;

                iEEGDataParams.alphaMin = m_CM.IEEGCol(ii).alphaMin;
                iEEGDataParams.alphaMax = m_CM.IEEGCol(ii).alphaMax;

                SendIEEGParameters.Invoke(iEEGDataParams);
            }
        }


        public void sendIRMDataToIEEGMenu()
        {
            for (int ii = 0; ii < m_CM.nbIEEGCol(); ++ii)
            {
                //IRMDataParameters IRMDataParams;
                //iEEGDataParams.minAmp = m_CM.IEEGCol(ii).minAmp;
                //iEEGDataParams.maxAmp = m_CM.IEEGCol(ii).maxAmp;

                //iEEGDataParams.spanMin = m_CM.IEEGCol(ii).spanMin;
                //iEEGDataParams.middle = m_CM.IEEGCol(ii).middle;
                //iEEGDataParams.spanMax = m_CM.IEEGCol(ii).spanMax;

                //iEEGDataParams.gain = m_CM.IEEGCol(ii).gainBubbles;
                //iEEGDataParams.maxDistance = m_CM.IEEGCol(ii).maxDistanceElec;
                //iEEGDataParams.columnId = ii;

                //iEEGDataParams.alphaMin = m_CM.IEEGCol(ii).alphaMin;
                //iEEGDataParams.alphaMax = m_CM.IEEGCol(ii).alphaMax;

                //m_sendIEEGParameters.Invoke(iEEGDataParams);
            }
        }

        /// <summary>
        /// Send an event for displaying a message on the scene screen
        /// </summary>
        /// <param name="message"></param>
        /// <param name="duration"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void displayScreenMessage(string message, float duration, int width, int height)
        {            
            DisplayScreenMessage.Invoke(message, duration, width , height);
        }

        /// <summary>
        /// Send an event for displaying a progressbar on the scene screen
        /// </summary>
        /// <param name="value"></param>
        /// <param name="duration"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void displayProgressBar(float value, float duration, int width, int height)
        {
            DisplaySceneProgressBar.Invoke(duration, width, height, value);
        }

        public void comparePlot()
        {
            displayScreenMessage("Select plot to compare ", 3f, 200, 40);
            data_.comparePlot = true;
        }

        /// <summary>
        /// Unselect the plot the corresponding column
        /// </summary>
        /// <param name="columnId"></param>
        public void unselectPlot(int columnId)
        {
            m_CM.col(columnId).idSelectedPlot = -1;
            m_CM.updateColumnsPlotsRendering(data_);
            ClickPlot.Invoke(-1); // update menu
        }

        #endregion functions

    }


    public class CutMeshJob : ThreadedJob
    {
        public CutMeshJob() { }

        public Base3DScene scene_ = null;

        protected override void ThreadFunction()
        {
            scene_.updateMeshesCuts();
        }

    }



    /// <summary>
    /// The job class for doing the textures generators computing stuff
    /// </summary>
    public class ComputeGeneratorsJob : ThreadedJob
    {
        public ComputeGeneratorsJob()
        { }

        public SceneStatesInfo data_ = null;
        public Column3DViewManager cm_ = null;

        protected override void ThreadFunction()
        {
            bool useMultiCPU = true;
            bool addValues = false;
            bool ratioDistances = true;

            data_.rwl.AcquireWriterLock(1000);
                data_.currentComputingState = 0f;
            data_.rwl.ReleaseWriterLock();

            // copy from main generators
            for (int ii = 0; ii < cm_.nbIEEGCol(); ++ii)
            {                
                for(int jj = 0; jj < cm_.meshSplitNb; ++jj)
                {
                    cm_.IEEGCol(ii).DLLBrainTextureGeneratorList[jj] = (DLL.BrainTextureGenerator)cm_.DLLCommonBrainTextureGeneratorList[jj].Clone();
                }

                for (int jj = 0; jj < cm_.planesList.Count; ++jj)
                {
                    cm_.IEEGCol(ii).DLLCutTextureGeneratorList[jj] = (DLL.CutTextureGenerator)cm_.DLLCommonCutTextureGeneratorList[jj].Clone();
                }
            }

            float offsetState = 1f / (2 * cm_.nbIEEGCol());

            // Do your threaded task. DON'T use the Unity API here
            if (data_.meshTypeToDisplay != SceneStatesInfo.MeshType.Inflated)
            {
                for (int ii = 0; ii < cm_.nbIEEGCol(); ++ii)
                {
                    data_.rwl.AcquireWriterLock(1000);
                        data_.currentComputingState += offsetState;
                    data_.rwl.ReleaseWriterLock();

                    float currentMaxDensity, currentMinInfluence, currentMaxInfluence;
                    float maxDensity = 1;

                    cm_.IEEGCol(ii).sharedMinInf = float.MaxValue;
                    cm_.IEEGCol(ii).sharedMaxInf = float.MinValue;

                    // update raw electrodes
                    cm_.IEEGCol(ii).updateDLLPlotMask();

                    // splits
                    for(int jj = 0; jj < cm_.meshSplitNb; ++jj)
                    {
                        cm_.IEEGCol(ii).DLLBrainTextureGeneratorList[jj].initOctree(cm_.IEEGCol(ii).RawElectrodes);
                        cm_.IEEGCol(ii).DLLBrainTextureGeneratorList[jj].computeDistances(cm_.IEEGCol(ii).maxDistanceElec, true);
                        cm_.IEEGCol(ii).DLLBrainTextureGeneratorList[jj].computeInfluences(cm_.IEEGCol(ii), useMultiCPU, addValues, ratioDistances);
                        currentMaxDensity = cm_.IEEGCol(ii).DLLBrainTextureGeneratorList[jj].getMaximumDensity();
                        currentMinInfluence = cm_.IEEGCol(ii).DLLBrainTextureGeneratorList[jj].getMinimumInfluence();
                        currentMaxInfluence = cm_.IEEGCol(ii).DLLBrainTextureGeneratorList[jj].getMaximumInfluence();

                        if (currentMaxDensity > maxDensity)
                            maxDensity = currentMaxDensity;

                        if (currentMinInfluence < cm_.IEEGCol(ii).sharedMinInf)
                            cm_.IEEGCol(ii).sharedMinInf = currentMinInfluence;

                        if (currentMaxInfluence > cm_.IEEGCol(ii).sharedMaxInf)
                            cm_.IEEGCol(ii).sharedMaxInf = currentMaxInfluence;

                    }

                    data_.rwl.AcquireWriterLock(1000);
                        data_.currentComputingState += offsetState;
                    data_.rwl.ReleaseWriterLock();


                    // cuts
                    for (int jj = 0; jj < cm_.planesList.Count; ++jj)
                    {
                        cm_.IEEGCol(ii).DLLCutTextureGeneratorList[jj].initOctree(cm_.IEEGCol(ii).RawElectrodes);
                        cm_.IEEGCol(ii).DLLCutTextureGeneratorList[jj].computeDistances(cm_.IEEGCol(ii).maxDistanceElec, true);
                        cm_.IEEGCol(ii).DLLCutTextureGeneratorList[jj].computeInfluences(cm_.IEEGCol(ii), useMultiCPU, addValues, ratioDistances);

                        currentMaxDensity = cm_.IEEGCol(ii).DLLCutTextureGeneratorList[jj].getMaximumDensity();
                        currentMinInfluence = cm_.IEEGCol(ii).DLLCutTextureGeneratorList[jj].getMinimumInfluence();
                        currentMaxInfluence = cm_.IEEGCol(ii).DLLCutTextureGeneratorList[jj].getMaximumInfluence();
                        
                        if (currentMaxDensity > maxDensity)
                            maxDensity = currentMaxDensity;

                        if (currentMinInfluence < cm_.IEEGCol(ii).sharedMinInf)
                            cm_.IEEGCol(ii).sharedMinInf = currentMinInfluence;

                        if (currentMaxInfluence > cm_.IEEGCol(ii).sharedMaxInf)
                            cm_.IEEGCol(ii).sharedMaxInf = currentMaxInfluence;
                    }

                    // synchronize max density
                    for (int jj = 0; jj < cm_.meshSplitNb; ++jj)
                        cm_.IEEGCol(ii).DLLBrainTextureGeneratorList[jj].synchronizeWithOthersGenerators(maxDensity, cm_.IEEGCol(ii).sharedMinInf, cm_.IEEGCol(ii).sharedMaxInf);
                    for (int jj = 0; jj < cm_.planesList.Count; ++jj)
                        cm_.IEEGCol(ii).DLLCutTextureGeneratorList[jj].synchronizeWithOthersGenerators(maxDensity, cm_.IEEGCol(ii).sharedMinInf, cm_.IEEGCol(ii).sharedMaxInf);

                    for (int jj = 0; jj < cm_.meshSplitNb; ++jj)
                        cm_.IEEGCol(ii).DLLBrainTextureGeneratorList[jj].ajustInfluencesToColormap();
                    for (int jj = 0; jj < cm_.planesList.Count; ++jj)
                        cm_.IEEGCol(ii).DLLCutTextureGeneratorList[jj].ajustInfluencesToColormap();
                }                
            }
            else // if inflated white mesh is displayed, we compute only on the complete white mesh
            {
                for (int ii = 0; ii < cm_.nbIEEGCol(); ++ii)
                {
                    data_.rwl.AcquireWriterLock(1000);
                        data_.currentComputingState += offsetState;
                    data_.rwl.ReleaseWriterLock();

                    float currentMaxDensity, currentMinInfluence, currentMaxInfluence;
                    float maxDensity = 1;

                    cm_.IEEGCol(ii).sharedMinInf = float.MaxValue;
                    cm_.IEEGCol(ii).sharedMaxInf = float.MinValue;

                    // update raw electrodes
                    cm_.IEEGCol(ii).updateDLLPlotMask();

                    // splits
                    for (int jj = 0; jj < cm_.meshSplitNb; ++jj)
                    {
                        cm_.IEEGCol(ii).DLLBrainTextureGeneratorList[jj].reset(cm_.DLLSplittedWhiteMeshesList[jj], cm_.DLLVolume); // TODO : ?
                        cm_.IEEGCol(ii).DLLBrainTextureGeneratorList[jj].initOctree(cm_.IEEGCol(ii).RawElectrodes);
                        cm_.IEEGCol(ii).DLLBrainTextureGeneratorList[jj].computeDistances(cm_.IEEGCol(ii).maxDistanceElec, true);
                        cm_.IEEGCol(ii).DLLBrainTextureGeneratorList[jj].computeInfluences(cm_.IEEGCol(ii), useMultiCPU, addValues, ratioDistances);
                        currentMaxDensity = cm_.IEEGCol(ii).DLLBrainTextureGeneratorList[jj].getMaximumDensity();
                        currentMinInfluence = cm_.IEEGCol(ii).DLLBrainTextureGeneratorList[jj].getMinimumInfluence();
                        currentMaxInfluence = cm_.IEEGCol(ii).DLLBrainTextureGeneratorList[jj].getMaximumInfluence();

                        if (currentMaxDensity > maxDensity)
                            maxDensity = currentMaxDensity;

                        if (currentMinInfluence < cm_.IEEGCol(ii).sharedMinInf)
                            cm_.IEEGCol(ii).sharedMinInf = currentMinInfluence;

                        if (currentMaxInfluence > cm_.IEEGCol(ii).sharedMaxInf)
                            cm_.IEEGCol(ii).sharedMaxInf = currentMaxInfluence;
                    }

                    data_.rwl.AcquireWriterLock(1000);
                        data_.currentComputingState += offsetState;
                    data_.rwl.ReleaseWriterLock();


                    // synchronize max density
                    for (int jj = 0; jj < cm_.meshSplitNb; ++jj)
                        cm_.IEEGCol(ii).DLLBrainTextureGeneratorList[jj].synchronizeWithOthersGenerators(maxDensity, cm_.IEEGCol(ii).sharedMinInf, cm_.IEEGCol(ii).sharedMaxInf);

                    for (int jj = 0; jj < cm_.meshSplitNb; ++jj)
                        cm_.IEEGCol(ii).DLLBrainTextureGeneratorList[jj].ajustInfluencesToColormap();
                }
            }
        }

        protected override void OnFinished()
        { }     
    }
}