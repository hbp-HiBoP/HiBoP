
/**
 * \file    SP3DScene.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define SP3DScene class
 */

// system
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

// unity
using UnityEngine;
using UnityEngine.UI;
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

        Data.Patient.Patient m_patient = null;

        // events
        private Events.UpdateLatencies m_updateLatencies = new Events.UpdateLatencies();
        public Events.UpdateLatencies UpdateLatencies { get { return m_updateLatencies; } }

        private List<string> CCEPLabels = null;

        #endregion members



        #region functions


        

        /// <summary>
        /// Update the meshes cuts
        /// </summary>
        public override void updateMeshesCuts()
        {
            //Profiler.BeginSample("TEST-updateCutPlanes 0 ");

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
                    // ...
                    break;
                case SceneStatesInfo.MeshType.Inflated:
                    // ...
                    break;
            }

            // get the middle
            data_.meshCenter = data_.meshToDisplay.boundingBox().center();

            // cut the mesh
            if (m_CM.planesList.Count > 0)
                m_CM.DLLCutsList = new List<DLL.Surface>(data_.meshToDisplay.cutSurface(m_CM.planesCutsCopy.ToArray(), data_.removeFrontPlaneList.ToArray(), !data_.holesEnabled));
            else
                m_CM.DLLCutsList = new List<DLL.Surface>() { (DLL.Surface)data_.meshToDisplay.Clone() };

            //Profiler.EndSample();
            //Profiler.BeginSample("TEST-updateCutPlanes 1 ");

            // split the cut mesh
            m_CM.DLLSplittedMeshesList = new List<DLL.Surface>(m_CM.DLLCutsList[0].splitToSurfaces(m_CM.meshSplitNb));

            // reset brain texture generator
            for (int ii = 0; ii < m_CM.meshSplitNb; ++ii)
                m_CM.DLLCommonBrainTextureGeneratorList[ii].reset(m_CM.DLLSplittedMeshesList[ii], m_CM.DLLVolume);

            // update cuts generators
            for (int ii = 0; ii < m_CM.planesList.Count; ++ii)
                if (data_.texturesCutToUpdateMask[ii])
                    for (int jj = 0; jj < m_CM.nbIEEGCol(); ++jj)
                        m_CM.DLLCommonCutTextureGeneratorList[ii].reset(m_CM.DLLVolume, m_CM.DLLCutsList[ii + 1], m_CM.planesCutsCopy[ii]);
        }

        public override void endMeshesCuts()
        {
            //Profiler.BeginSample("TEST-updateCutPlanes 2 ");

            // update cut brain mesh object mesh filter
            for (int ii = 0; ii < m_CM.meshSplitNb; ++ii)
                m_CM.DLLSplittedMeshesList[ii].updateMeshMashall(go_.MeshesList[ii].GetComponent<MeshFilter>().mesh);

            // create null uv2/uv3 arrays
            m_CM.uvNull = new List<Vector2[]>(m_CM.meshSplitNb);
            for (int ii = 0; ii < m_CM.meshSplitNb; ++ii)
            {
                m_CM.uvNull.Add(new Vector2[go_.MeshesList[ii].GetComponent<MeshFilter>().mesh.vertexCount]);
                Extensions.ArrayExtensions.Fill(m_CM.uvNull[ii], new Vector2(0.01f, 1f));
            }

            for (int ii = 0; ii < m_CM.planesCutsCopy.Count; ++ii)
                if (data_.texturesCutToUpdateMask[ii])
                    go_.CutsList[ii].SetActive(true); // enable cuts gameobject

            data_.collidersUpdated = false; // colliders are now longer up to date
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
        /// <param name="patient"></param>
        public bool reset(Data.Patient.Patient patient, bool postIRM)
        {
            modes.updateMode(Mode.FunctionsId.resetScene);

            m_patient = patient;

            List<string> ptsFiles = new List<string>(), namePatients = new List<string>();
            ptsFiles.Add(m_patient.Brain.PatientBasedImplantation);
            namePatients.Add(m_patient.Place + "_" + m_patient.Date + "_" + m_patient.Name);

            List<string> meshesFiles = new List<string>();
            meshesFiles.Add(m_patient.Brain.LeftMesh);
            meshesFiles.Add(m_patient.Brain.RightMesh);
            
            // reset columns
            m_CM.reset();

            Debug.Log("SP TRANSFO ->" + m_patient.Brain.PreToScannerBasedTransformation);
            // load meshes
            bool success = resetGIIBrainSurfaceFile(meshesFiles, m_patient.Brain.PreToScannerBasedTransformation);

            // load volume
            if (success)
                success = resetNIIBrainVolumeFile(m_patient.Brain.PreIRM);

            // load electrodes
            if(success)
                success = resetElectrodesFile(ptsFiles, namePatients);

            if(success)
                for (int ii = 0; ii < data_.texturesCutToUpdateMask.Count; ++ii)
                    data_.texturesCutToUpdateMask[ii] = true;

            if (success)
                data_.updateCutPlanes = true;

            if(!success)
            {
                Debug.LogError("-ERROR : SP3DScene : reset failed. ");
                data_.reset();
                m_CM.reset();
                resetGameObjets();
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
        public bool resetGIIBrainSurfaceFile(List<string> pathGIIBrainFiles, string pathTransformFile)
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


            // load left hemi
            bool leftMeshLoaded = m_CM.LHemi.loadGIIFile(pathGIIBrainFiles[0], true, pathTransformFile);            
            m_CM.LHemi.flipSurface();  // the transformation inverses one axis, so faces of the surface must be flipped (TODO : case with no transformation or no inverse axi transfo)
            m_CM.LHemi.computeNormals();

            // load right hemi
            bool rightMeshLoaded = m_CM.RHemi.loadGIIFile(pathGIIBrainFiles[1], true, pathTransformFile);
            m_CM.RHemi.flipSurface();  // the transformation inverses one axis, so faces of the surface must be flipped (TODO : case with no transformation or no inverse axi transfo)
            m_CM.RHemi.computeNormals();

            // fusion
            if (leftMeshLoaded && rightMeshLoaded)
            {
                // copy left
                m_CM.BothHemi = (DLL.Surface)m_CM.LHemi.Clone();

                // add right
                m_CM.BothHemi.addToSurface(m_CM.RHemi);
                data_.meshesLoaded = true;

                // get the middle
                data_.meshCenter = m_CM.BothHemi.boundingBox().center();
            }
            else
            {
                data_.meshesLoaded = false;
                Debug.LogError("-ERROR : Base3DScene::resetGIIBrainSurfaceFile -> load GII file failed, left : " + leftMeshLoaded + " right : " + rightMeshLoaded);
                return false;
            }

            // update scenes cameras
            UpdateCameraTarget.Invoke(m_CM.BothHemi.boundingBox().center());
           
            // set the transform as the mesh center
            data_.hemiMeshesAvailables = true;

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.resetGIIBrainSurfaceFile);
            //##################

            return data_.meshesLoaded;
        }


        /// <summary>
        /// Reset all the plots with a new list of pts files
        /// </summary>
        /// <param name="pathsElectrodesPtsFile"></param>
        /// <returns></returns>
        public bool resetElectrodesFile(List<string> pathsElectrodesPtsFile, List<string> namePatients)
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.resetElectrodesFile))
            {
                Debug.LogError("-ERROR : SinglePatient3DScene::resetElectrodesFile -> no acess for mode : " + modes.currentModeName());
                return false;
            }
            //##################

            // load list of pts files
            data_.electodesLoaded = m_CM.DLLLoadedPatientsElectrodes.loadNamePtsFiles(pathsElectrodesPtsFile, namePatients);


            //Gre_2009_CHIm.pts

            // destroy previous electrodes gameobjects
            for (int ii = 0; ii < m_CM.PlotsList.Count; ++ii)
            {
                //Destroy(go_.PlotsList[ii].GetComponent<MeshFilter>().mesh); // now it's a shared mesh
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
                            Vector3 positionInverted = m_CM.DLLLoadedPatientsElectrodes.getPlotPos(ii, jj, kk);
                            positionInverted.x = -positionInverted.x;


                            GameObject plotGO = Instantiate(BaseGameObjects.Plot);
                            plotGO.name = m_CM.DLLLoadedPatientsElectrodes.getPlotName(ii, jj, kk);

                            plotGO.transform.position = positionInverted;// + go_.PlotsParent.transform.position; // TODO : ?
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
                            plot.fullName = namePatients[ii] + "_" + plotGO.name;

                            m_CM.PlotsList.Add(plotGO);
                        }
                    }
                }
            }

            // reset selected plot
            for (int ii = 0; ii < m_CM.columns.Count; ++ii)
            {
                m_CM.col(ii).idSelectedPlot = -1;
            }

            // update latencies
            m_CM.DLLLoadedRawPlotsList = new DLL.RawPlotList();
            m_CM.DLLLoadedPatientsElectrodes.extractRawPlotList(m_CM.DLLLoadedRawPlotsList);


            // reset latencies
            m_CM.latenciesFiles = new List<Latencies>();
            CCEPLabels = new List<string>();
            for(int ii = 0; ii <  m_patient.Brain.Connectivities.Count; ++ii)
            {
                Latencies latencies = null;
                
                if (m_patient.Brain.Connectivities[ii].Path == "dummyPath")
                {
                    // generate dummy latencies
                    latencies = m_CM.DLLLoadedRawPlotsList.generateDummyLatencies();
                }
                else
                {
                    // load latency file
                    latencies = m_CM.DLLLoadedRawPlotsList.updateLatenciesWithFile(m_patient.Brain.Connectivities[ii].Path);
                }

                //latencies = m_CM.DLLLoadedRawPlotsList.updateLatenciesWithFile("C:/Users/Florian/Desktop/amplitudes_latencies/amplitudes_latencies/SIEJO_amplitudes_latencies.txt");
                latencies.name = m_patient.Brain.Connectivities[ii].Label;
                m_CM.latenciesFiles.Add(latencies);
                CCEPLabels.Add(latencies.name);
            }

            m_CM.latencyFilesDefined = (m_patient.Brain.Connectivities.Count > 0);
            m_updateLatencies.Invoke(CCEPLabels);

            //Debug.Log("cm_.currentLatencyFile : " + cm_.currentLatencyFile);


            // for mani
            // load color files
            data_.colorMani = false;
            //string pathFile = "D:/_projects/HBP/HBP-flo/data/LYONNEURO_2014_SIEj_MNI_Name_col.txt";
            //cm_.colorsPlots = cm_.DLLLoadedRawPlotsList.loadColors(pathFile);
            //data_.colorMani = true;




            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.resetElectrodesFile);
            //##################

            return data_.electodesLoaded;
        }


        /// <summary>
        /// Define the timeline data with a patient and a list of column data
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="columnDataList"></param>
        public void setTimelinesData(HBP.Data.Patient.Patient patient, List<HBP.Data.Visualisation.ColumnData> columnDataList)
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
            m_CM.setSpTimelinesData(patient, columnDataList);

            // update plots visibility
            m_CM.updateColumnsPlotsRendering(data_);

            // set flag
            data_.timelinesLoaded = true;


            // send data to UI
            sendIEEGDataToMenu();
            

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.setTimelines);
            //##################
        }

        /// <summary>
        /// Set the current plot to be selected in all the columns
        /// </summary>
        /// <param name="idSelectedPlot"></param>
        public void defineSelectedPlot(int idSelectedPlot)
        {
            m_updateLatencies.Invoke(CCEPLabels);

            for (int ii = 0; ii < m_CM.nbIEEGCol(); ++ii)
            {
                m_CM.IEEGCol(ii).idSelectedPlot = idSelectedPlot;
                ClickPlot.Invoke(ii);
            }

            //Vector3 posPlot = go_.PlotsList[idSelectedPlot].transform.position;
            //Debug.Log("vol center : " + data_.volumeCenter);

            //for (int ii = 0; ii < 1; ++ii) // data_.planesList.Count
            //{
            //    float offset = cm_.DLLVolume.sizeOffesetCutPlane(data_.planesList[ii], data_.numberOfCutsPerPlane[ii]);
            //    Vector3 extremPoint = data_.volumeCenter + data_.planesList[ii].normal * 0.5f * offset * data_.numberOfCutsPerPlane[ii];

            //    //Vector3 projectedPoint = Geometry.projectionPointPlane(posPlot, data_.planesList[ii]);

            //    Vector3 centerPlot = Geometry.vec(data_.volumeCenter, posPlot);
            //    Vector3 v = data_.planesList[ii].normal;// Geometry.vec(data_.volumeCenter, extremPoint).normalized;

            //    Vector3 bh = Vector3.Dot(centerPlot, v) * v;

            //    float norm = (data_.volumeCenter - extremPoint).magnitude;
            //    Debug.Log("offset : " + offset);
            //    Debug.Log("extremPoint : " + extremPoint);
            //    Debug.Log("v : " + v);
            //    Debug.Log("bh : " + bh);

            //    Debug.Log("d1 : " + bh.magnitude);
            //    Debug.Log("norm : " + norm);
            //    Debug.Log("result : " + (bh.magnitude / norm));


            //    //updatePlane(data_.idPlanesOrientationList[ii], data_.planesOrientationFlipList[ii], data_.removeFrontPlaneList[ii], data_.planesList[ii].normal, ii, (bh.magnitude / norm));
            //}
        }

        /// <summary>
        /// Update the columns masks of the scene
        /// </summary>
        /// <param name="blacklistMasks"></param>
        /// <param name="excludedMasks"></param>
        public void setColumnsPlotsMasks(List<List<bool>> blacklistMasks, List<List<bool>> excludedMasks, List<List<bool>> hightLightedMasks)
        {
            for(int ii = 0; ii < m_CM.nbIEEGCol(); ++ii)
            {
                for (int jj = 0; jj < m_CM.PlotsList.Count; ++jj)
                {
                    m_CM.IEEGCol(ii).Plots[jj].blackList = blacklistMasks[ii][jj];
                    m_CM.IEEGCol(ii).Plots[jj].exclude = excludedMasks[ii][jj];
                    m_CM.IEEGCol(ii).Plots[jj].highlight = hightLightedMasks[ii][jj];
                }
            }

            m_CM.updateColumnsPlotsRendering(data_);
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
        /// Manage the clicks event in the scene
        /// </summary>
        /// <param name="ray"></param>
        public void clickOnScene(Ray ray)
        {
            // scene not loaded
            if (!data_.volumeLoaded)
                return;

            // update colliders if necessary (SLOW)
            if (!data_.collidersUpdated)
                updateMeshesColliders();

            // retrieve layer
            int layerMask = 0;
            layerMask |= 1 << LayerMask.NameToLayer(m_CM.currentColumn().layerColumn);
            layerMask |= 1 << LayerMask.NameToLayer("Meshes_SP");

            // collision with all colliders
            RaycastHit hit;
            bool isCollision = Physics.Raycast(ray.origin, ray.direction, out hit, Mathf.Infinity, layerMask);
            if (!isCollision) // no hit
                return;

            if(hit.collider.gameObject.layer == LayerMask.NameToLayer("Meshes_SP")) // mesh hit
                return;

            // plot hit            
            int clickedPlotID = hit.collider.gameObject.GetComponent<Plot>().idGlobal;
            Column3DView currColumn = m_CM.currentColumn();
            currColumn.idSelectedPlot = clickedPlotID;

            if (!m_CM.isIRMFCurrentColumn())
            {
                Column3DViewIEEG dataC = (Column3DViewIEEG)currColumn;

                m_CM.latencyFileAvailable = (dataC.currentLatencyFile != -1);
                dataC.sourceDefined = false;
                dataC.plotIsASource = false;
                dataC.plotLatencyData = false;

                if (m_CM.latencyFileAvailable)
                {
                    dataC.sourceDefined = (dataC.idSourceSelected != -1);
                    if (m_CM.latenciesFiles[dataC.currentLatencyFile].isPlotASource(dataC.idSelectedPlot))
                    {
                        dataC.plotIsASource = true;
                    }

                    if (dataC.sourceDefined)
                    {
                        dataC.plotLatencyData = m_CM.latenciesFiles[dataC.currentLatencyFile].isPlotResponsiveForSource(dataC.idSelectedPlot, dataC.idSourceSelected);
                    }
                }
            }

            ClickPlot.Invoke(-1);            
            m_CM.updateColumnsPlotsRendering(data_);            
        }

        /// <summary>
        /// anage the mouse movments event in the scene
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="mousePosition"></param>
        /// /// <param name="idColumn"></param>
        public void moveMouseOnScene(Ray ray, Vector3 mousePosition, int idColumn)
        {
            // scene not loaded
            if (!data_.volumeLoaded)
                return;

            // current column is different : we display only for the focused column
            if (m_CM.idSelectedColumn != idColumn)
                return;

            // retrieve layer
            int layerMask = 0;
            layerMask |= 1 << LayerMask.NameToLayer(m_CM.currentColumn().layerColumn);
            layerMask |= 1 << LayerMask.NameToLayer("Meshes_SP");

            RaycastHit hit;
            bool isCollision = Physics.Raycast(ray.origin, ray.direction, out hit, Mathf.Infinity, layerMask);
            if (!isCollision)
            {
                UpdateDisplayedPlotsInfo.Invoke(new plotInfo(false, mousePosition, false));
                return;
            }

            if (hit.collider.transform.parent.name == "cuts" || hit.collider.transform.parent.name == "meshes")
            {
                UpdateDisplayedPlotsInfo.Invoke(new plotInfo(false, mousePosition, false));
                return;
            }

            // IRMF
            if (m_CM.isIRMFColumn(m_CM.idSelectedColumn))
            {
                UpdateDisplayedPlotsInfo.Invoke(new plotInfo(true, mousePosition, true, false, hit.collider.gameObject.name));
                return;
            }

            // IEEG
            Column3DViewIEEG currIEEGCol = (Column3DViewIEEG)m_CM.currentColumn();
            int idPlot = hit.collider.gameObject.GetComponent<Plot>().idPlotPatient;

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
                    if(latencyFile.isPlotResponsiveForSource(idPlot, currIEEGCol.idSourceSelected)) // data available
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

            UpdateDisplayedPlotsInfo.Invoke(new plotInfo(true, mousePosition, m_CM.isIRMFCurrentColumn(), data_.displayLatenciesMode,
                hit.collider.GetComponent<Plot>().fullName, "" + amp, height, latency));
        }

        public void disablePlotDisplayWindows(int idColumn)
        {
            UpdateDisplayedPlotsInfo.Invoke(new plotInfo(false, new Vector3(0, 0, 0), false));
        }

        /// <summary>
        /// Update the display mode of the scene
        /// </summary>
        /// <param name="isLatencyMode"></param>
        public void setLatencyDisplayMode(bool isLatencyMode)
        {
            data_.displayLatenciesMode = isLatencyMode;
            UpdateCutsInUI.Invoke(m_CM.getBrainCutTextureList(m_CM.idSelectedColumn, true, data_.generatorUpToDate, data_.displayLatenciesMode), m_CM.idSelectedColumn, m_CM.planesList.Count);

            m_CM.updateColumnsPlotsRendering(data_);

            // force mode to update UI
            modes.setCurrentModeSpecifications(true);
        }

        /// <summary>
        /// Define the current plot as the source
        /// </summary>
        public void setCurrentPlotAsSource()
        {
            if (m_CM.currentColumn().isIRMF)
                return;

            Column3DViewIEEG currCol = (Column3DViewIEEG)m_CM.currentColumn();
            currCol.idSourceSelected = currCol.idSelectedPlot;
            m_CM.updateColumnsPlotsRendering(data_);
        }

        /// <summary>
        /// Undefine the current plot as the source
        /// </summary>
        public void undefineCurrentSource()
        {
            if (m_CM.currentColumn().isIRMF)
                return;

            Column3DViewIEEG currCol = (Column3DViewIEEG)m_CM.currentColumn();
            currCol.idSourceSelected = -1;
            m_CM.updateColumnsPlotsRendering(data_);
        }

        /// <summary>
        /// Send additionnal plot info to hight level UI
        /// </summary>
        public override void  sendAdditionnalPlotInfoRequest(Plot previousPlot = null) // TODO : deporter dans c manager
        {
            if (m_CM.currentColumn().isIRMF)
                return;

            if (m_CM.currentColumn().idSelectedPlot != -1)
            {                
                List<List<bool>> masksColumnsData = new List<List<bool>>(m_CM.nbIEEGCol());                
                for (int ii = 0; ii < m_CM.nbIEEGCol(); ++ii)
                {
                    masksColumnsData.Add(new List<bool>(m_CM.IEEGCol(ii).Plots.Count));

                    for (int jj = 0; jj < m_CM.IEEGCol(ii).Plots.Count; ++jj)
                    {
                        Plot p = m_CM.IEEGCol(ii).Plots[jj];
                        bool keep = (!p.blackList && !p.exclude && !p.columnMask);
                        masksColumnsData[ii].Add(keep);
                    }
                }
               
                plotRequest request = new plotRequest();
                request.spScene = true;
                request.idPlot = m_CM.currentColumn().idSelectedPlot;
                request.idPlot2 = (previousPlot == null) ? - 1 : previousPlot.idPlotPatient;
                request.idPatient = m_patient.ID;
                request.idPatient2 = m_patient.ID;
                request.maskColumn = masksColumnsData;

                //request.display();
                PlotInfoRequest.Invoke(request);
            }
        }

        /// <summary>
        /// Update the id of the latency file
        /// </summary>
        /// <param name="id"></param>
        public void updateCurrentLatencyFile(int id)
        {
            if (m_CM.currentColumn().isIRMF)
                return;

            Column3DViewIEEG currCol = (Column3DViewIEEG)m_CM.currentColumn();
            currCol.currentLatencyFile = id;
            undefineCurrentSource();
        }

        #endregion functions
    }
}