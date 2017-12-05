


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
        #endregion

        #region Coroutines
        /// <summary>
        /// Reset the scene : reload MRI, sites, and regenerate textures
        /// </summary>
        /// <param name="data"></param>
        public IEnumerator c_Initialize(Data.Visualization.Visualization visualization, GenericEvent<float, float, string> onChangeProgress, Action<Exception> outPut)
        {
            Exception exception = null;

            // Compute progress variables
            List<string> usableImplantations = visualization.FindUsableImplantations();
            float implantationsCount = usableImplantations.Count * Patients.Count;
            float patientsCount = Patients.Count;
            float totalTime = implantationsCount * LOADING_IMPLANTATIONS_WEIGHT + LOADING_MNI_WEIGHT + patientsCount * LOADING_IEEG_WEIGHT;
            float loadingImplantationsProgress = (Patients.Count * LOADING_IMPLANTATIONS_WEIGHT) / totalTime;
            float loadingImplantationsTime = (Patients.Count * LOADING_IMPLANTATIONS_WEIGHT) / 1000.0f;
            float loadingMNIProgress = LOADING_MNI_WEIGHT / totalTime;
            float loadingMNITime = LOADING_MNI_WEIGHT / 1000.0f;
            float loadingIEEGProgress = (patientsCount * LOADING_IEEG_WEIGHT) / totalTime;
            float loadingIEEGTime = (patientsCount * LOADING_IEEG_WEIGHT) / 1000.0f;

            yield return Ninja.JumpToUnity;
            float progress = 1.0f;
            onChangeProgress.Invoke(progress, 0.0f, "");
            
            int sceneID = ApplicationState.Module3D.NumberOfScenesLoadedSinceStart;
            gameObject.name = "MultiPatients Scene (" + sceneID + ")";
            transform.position = new Vector3(HBP3DModule.SPACE_BETWEEN_SCENES_AND_COLUMNS * sceneID, transform.position.y, transform.position.z);

            // Loading MNI
            progress += loadingMNIProgress;
            onChangeProgress.Invoke(progress, loadingMNITime, "Loading MNI");
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadMNIObjects());
            ResetSplitsNumber(3);
            SceneInformation.MeshesLoaded = true;
            SceneInformation.MRILoaded = true;
            SceneInformation.IsROICreationModeEnabled = false;
            SceneInformation.MeshGeometryNeedsUpdate = true;

            // reset columns
            m_ColumnManager.Initialize(Cuts.Count);

            // reset electrodes
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadImplantations(visualization.Patients, usableImplantations, (i) =>
            {
                progress += loadingImplantationsProgress;
                onChangeProgress.Invoke(progress, loadingImplantationsTime, "Loading implantations [" + i + "/" + usableImplantations.Count + "]");
            }));

            progress += loadingIEEGProgress;
            onChangeProgress.Invoke(progress, loadingIEEGTime, "Setting timeline");
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_SetEEGData());

            m_ColumnManager.InitializeColumnsMeshes(m_DisplayedObjects.BrainSurfaceMeshesParent);
            // update scenes cameras
            OnUpdateCameraTarget.Invoke(m_ColumnManager.SelectedMesh.Both.BoundingBox.Center);

            outPut(exception);
        }
        #endregion
    }
}