


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
        public override Data.Enums.SceneType Type
        {
            get
            {
                return Data.Enums.SceneType.MultiPatients;
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
            transform.position = new Vector3(HBP3DModule.SPACE_BETWEEN_SCENES_GAME_OBJECTS * sceneID, transform.position.y, transform.position.z);

            // Checking MNI
            onChangeProgress.Invoke(progress, 0.0f, "Loading MNI");
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            yield return new WaitUntil(delegate { return ApplicationState.Module3D.MNIObjects.Loaded || watch.ElapsedMilliseconds > 5000; });
            watch.Stop();
            if (watch.ElapsedMilliseconds > 5000)
            {
                outPut(new CanNotLoadMNI());
                yield break;
            }

            // Loading MNI
            progress += loadingMNIProgress;
            onChangeProgress.Invoke(progress, loadingMNITime, "Loading MNI");
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadMNIObjects(e => exception = e));
            if (exception != null)
            {
                outPut(exception);
                yield break;
            }
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
                onChangeProgress.Invoke(progress, loadingImplantationsTime, "Loading implantations [" + (i + 1).ToString() + "/" + usableImplantations.Count + "]");
            }, e => exception = e));
            if (exception != null)
            {
                outPut(exception);
                yield break;
            }

            progress += loadingIEEGProgress;
            onChangeProgress.Invoke(progress, loadingIEEGTime, "Loading columns");
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_SetColumns(e => exception = e));
            if (exception != null)
            {
                outPut(exception);
                yield break;
            }

            m_ColumnManager.InitializeColumnsMeshes(m_DisplayedObjects.BrainSurfaceMeshesParent, SceneInformation.UseSimplifiedMeshes);
            // update scenes cameras
            OnUpdateCameraTarget.Invoke(m_ColumnManager.SelectedMesh.Both.BoundingBox.Center);

            outPut(exception);
        }
        #endregion
    }
}