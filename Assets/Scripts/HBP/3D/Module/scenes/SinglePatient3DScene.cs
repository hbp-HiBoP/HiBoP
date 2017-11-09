
using CielaSpike;
/**
 * \file    SP3DScene.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define SP3DScene class
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// unity
using UnityEngine;
using UnityEngine.Events;

namespace HBP.Module3D
{
    /// <summary>
    /// The single patient scene class, inheritance of Base3DScene.
    /// </summary>
    [AddComponentMenu("Scenes/Single Patient 3D Scene")]
    public class SinglePatient3DScene : Base3DScene
    {
        #region Properties
        /// <summary>
        /// Type of the scene
        /// </summary>
        public override SceneType Type
        {
            get
            {
                return SceneType.SinglePatient;
            }
        }
        /// <summary>
        /// Patient of this scene
        /// </summary>
        public Data.Patient Patient
        {
            get
            {
                return Visualization.Patients[0];
            }
        }
        /// <summary>
        /// CCEP Labels
        /// </summary>
        private List<string> CCEPLabels = null;

        private const float LOADING_MNI = 0.15f;
        private const float LOADING_MESHES_PROGRESS = 0.35f;
        private const float LOADING_VOLUME_PROGRESS = 0.3f;
        private const float LOADING_ELECTRODES_PROGRESS = 0.1f;
        private const float SETTING_TIMELINE_PROGRESS = 0.2f;
        #endregion

        #region Coroutines
        /// <summary>
        /// Reset the scene : reload meshes, IRM, plots, and regenerate textures
        /// </summary>
        /// <param name="patient"></param>
        public IEnumerator c_Initialize(Data.Visualization.Visualization visualization, GenericEvent<float, float, string> onChangeProgress)
        {
            yield return Ninja.JumpToUnity;
            float progress = 1.0f;
            onChangeProgress.Invoke(progress, 0.0f, "");

            m_ModesManager.UpdateMode(Mode.FunctionsId.ResetScene);
            
            int sceneID = ApplicationState.Module3D.NumberOfScenesLoadedSinceStart;
            gameObject.name = "SinglePatient Scene (" + sceneID + ")";
            transform.position = new Vector3(HBP3DModule.SPACE_BETWEEN_SCENES_AND_COLUMNS * sceneID, transform.position.y, transform.position.z);

            // reset columns
            m_ColumnManager.Initialize(m_Cuts.Count);

            // Load Meshes
            float loadingMeshesProgress = LOADING_MESHES_PROGRESS / Patient.Brain.Meshes.Count;
            foreach (Data.Anatomy.Mesh mesh in Patient.Brain.Meshes)
            {
                progress += loadingMeshesProgress;
                onChangeProgress.Invoke(progress, 1.5f, "Loading Mesh: " + mesh.Name);
                yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadBrainSurface(mesh));
            }
            if (m_ColumnManager.Meshes.Count > 0)
            {
                GenerateSplit(from mesh3D in m_ColumnManager.Meshes select mesh3D.Both);
            }
            else
            {
                ResetSplitsNumber(3);
            }
            SceneInformation.MeshesLoaded = true;

            // Load MRIs
            float loadingMRIProgress = LOADING_VOLUME_PROGRESS / Patient.Brain.MRIs.Count;
            foreach (Data.Anatomy.MRI mri in Patient.Brain.MRIs)
            {
                progress += loadingMRIProgress;
                onChangeProgress.Invoke(progress, 1.5f, "Loading MRI: " + mri.Name);
                yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadBrainVolume(mri));
            }

            // Load Sites
            progress += LOADING_ELECTRODES_PROGRESS;
            onChangeProgress.Invoke(progress, 0.05f, "Loading Implantations");
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadImplantations(visualization.Patients));
            SceneInformation.MeshGeometryNeedsUpdate = true;

            // Load MNI
            progress += LOADING_MNI;
            onChangeProgress.Invoke(progress, 2.0f, "Loading MNI objects");
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadMNIObjects());

            // Set Timeline
            progress += SETTING_TIMELINE_PROGRESS;
            onChangeProgress.Invoke(progress, 0.5f, "Setting timeline");
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_SetEEGData());
            
            // TMP : to debug anatomy scene
            //m_ColumnManager.InitializeColumns(Column3D.ColumnType.Base, 1);
            //m_ColumnManager.Columns.Last().Label = Patient.Name + " (" + Patient.Place + " - " + Patient.Date + ")";

            // Finalization
            m_ColumnManager.InitializeColumnsMeshes(m_DisplayedObjects.BrainSurfaceMeshesParent);
            UpdateMeshesColliders();
            OnUpdateCameraTarget.Invoke(m_ColumnManager.SelectedMesh.Both.BoundingBox.Center);
        }
        /// <summary>
        /// Reset the meshes of the scene with GII files
        /// </summary>
        /// <param name="pathGIIBrainFiles"></param>
        /// <param name="transformation"></param>
        /// <returns></returns>
        private IEnumerator c_LoadBrainSurface(Data.Anatomy.Mesh mesh)
        {
            //####### CHECK ACESS
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.ResetGIIBrainSurfaceFile))
            {
                throw new ModeAccessException(m_ModesManager.CurrentModeName);
            }
            //##################

            SceneInformation.MeshesLoaded = false;

            // checks parameters
            if (mesh == null) throw new EmptyFilePathException("GII");
            if (!mesh.Usable) throw new EmptyFilePathException("GII"); // TODO CHANGE TO NOT USABLE

            if (mesh is Data.Anatomy.LeftRightMesh)
            {
                LeftRightMesh3D mesh3D = new LeftRightMesh3D((Data.Anatomy.LeftRightMesh)mesh);
                
                if (mesh3D.IsLoaded)
                {
                    m_ColumnManager.Meshes.Add(mesh3D);
                }
                else
                {
                    SceneInformation.MeshesLoaded = false;
                    throw new CanNotLoadGIIFile(mesh3D.Left.IsLoaded, mesh3D.Right.IsLoaded);
                }
            }
            else if(mesh is Data.Anatomy.SingleMesh)
            {
                SingleMesh3D mesh3D = new SingleMesh3D((Data.Anatomy.SingleMesh)mesh);

                if (mesh3D.IsLoaded)
                {
                    m_ColumnManager.Meshes.Add(mesh3D);
                }
                else
                {
                    SceneInformation.MeshesLoaded = false;
                    throw new CanNotLoadGIIFile(mesh3D.IsLoaded);
                }
            }
            else
            {
                Debug.LogError("Mesh not handled.");
            }

            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.ResetGIIBrainSurfaceFile);
            //##################
            yield return true;
        }
        #endregion
    }
}