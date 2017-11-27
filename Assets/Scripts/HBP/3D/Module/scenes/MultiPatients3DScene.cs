


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

        private const float LOADING_MESHES_PROGRESS = 0.10f;
        private const float LOADING_COLUMNS_PROGRESS = 0.10f;
        private const float LOADING_ELECTRODES_PROGRESS = 0.10f;
        private const float SETTING_TIMELINE_PROGRESS = 0.70f;
        #endregion

        #region Coroutines
        /// <summary>
        /// Reset the scene : reload MRI, sites, and regenerate textures
        /// </summary>
        /// <param name="data"></param>
        public IEnumerator c_Initialize(Data.Visualization.Visualization visualization, GenericEvent<float, float, string> onChangeProgress, Action<Exception> outPut)
        {
            Exception exception = null;

            yield return Ninja.JumpToUnity;
            float progress = 1.0f;
            onChangeProgress.Invoke(progress, 0.0f, "");

            m_ModesManager.UpdateMode(Mode.FunctionsId.ResetScene);
            
            int sceneID = ApplicationState.Module3D.NumberOfScenesLoadedSinceStart;
            gameObject.name = "MultiPatients Scene (" + sceneID + ")";
            transform.position = new Vector3(HBP3DModule.SPACE_BETWEEN_SCENES_AND_COLUMNS * sceneID, transform.position.y, transform.position.z);

            progress += LOADING_MESHES_PROGRESS;
            onChangeProgress.Invoke(progress, 0.05f, "Loading MNI");
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
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_SetEEGData());

            m_ColumnManager.InitializeColumnsMeshes(m_DisplayedObjects.BrainSurfaceMeshesParent);
            // update scenes cameras
            OnUpdateCameraTarget.Invoke(m_ColumnManager.SelectedMesh.Both.BoundingBox.Center);

            outPut(exception);
        }
        #endregion
    }
}