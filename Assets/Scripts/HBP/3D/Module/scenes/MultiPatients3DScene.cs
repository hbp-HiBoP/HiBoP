


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
        public void UpdateSitesMasks(int idColumn, List<List<List<Site>>> plots, List<string> patientsName)
        {
            // reset previous masks
            for (int ii = 0; ii < ColumnManager.Columns[idColumn].Sites.Count; ++ii)
            {
                ColumnManager.Columns[idColumn].Sites[ii].State.IsExcluded = false;
                ColumnManager.Columns[idColumn].Sites[ii].State.IsBlackListed = false;
                ColumnManager.Columns[idColumn].Sites[ii].State.IsHighlighted = false;
                ColumnManager.Columns[idColumn].Sites[ii].State.IsMasked = false;
            }

            // update masks
            for (int ii = 0; ii < ColumnManager.Columns[idColumn].Sites.Count; ii++)
            {
                for (int jj = 0; jj < plots.Count; ++jj) // patient
                {
                    if (patientsName[jj] != ColumnManager.Columns[idColumn].Sites[ii].Information.PatientID)
                        continue;                    

                    for (int kk = 0; kk < plots[jj].Count; kk++) // electrode
                    {
                        for(int ll = 0; ll < plots[jj][kk].Count; ll++) // plot
                        {
                            string namePlot = plots[jj][kk][ll].Information.PatientID + "_" + plots[jj][kk][ll].Information.FullID;
                            if (namePlot != ColumnManager.Columns[idColumn].Sites[ii].Information.FullID)
                                continue;

                            ColumnManager.Columns[idColumn].Sites[ii].State.IsExcluded = plots[jj][kk][ll].State.IsExcluded;
                            ColumnManager.Columns[idColumn].Sites[ii].State.IsBlackListed = plots[jj][kk][ll].State.IsBlackListed;
                            ColumnManager.Columns[idColumn].Sites[ii].State.IsHighlighted = plots[jj][kk][ll].State.IsHighlighted;
                            ColumnManager.Columns[idColumn].Sites[ii].State.IsMasked = plots[jj][kk][ll].State.IsMasked;
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
            onChangeProgress.Invoke(progress, 4.0f, "Loading MNI");
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadMNIObjects());
            yield return Ninja.JumpBack;

            // MNI meshes are preloaded
            SceneInformation.MeshesLoaded = true;
            SceneInformation.IsROICreationModeEnabled = false;

            yield return Ninja.JumpToUnity;
            progress += LOADING_COLUMNS_PROGRESS;
            onChangeProgress.Invoke(progress, 0.05f, "Loading columns");
            // reset columns
            m_ColumnManager.Initialize(Cuts.Count);

            yield return Ninja.JumpBack;
            // retrieve MNI IRM volume
            SceneInformation.MRILoaded = true;

            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.ResetNIIBrainVolumeFile);
            //##################

            // reset electrodes
            yield return Ninja.JumpToUnity;
            progress += LOADING_ELECTRODES_PROGRESS;
            onChangeProgress.Invoke(progress, 0.05f, "Loading implantations");
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadImplantations(visualization.Patients));

            // define meshes splits nb
            ResetSplitsNumber(3);
            
            SceneInformation.MeshGeometryNeedsUpdate = true;

            progress += SETTING_TIMELINE_PROGRESS;
            onChangeProgress.Invoke(progress, 0.5f, "Setting timeline");
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_SetTimelineData());

            m_ColumnManager.InitializeColumnsMeshes(m_DisplayedObjects.BrainSurfaceMeshesParent);
            // update scenes cameras
            OnUpdateCameraTarget.Invoke(m_ColumnManager.SelectedMesh.Both.BoundingBox.Center);
        }
        #endregion
    }
}