
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
        #endregion

        #region Coroutines
        /// <summary>
        /// Reset the scene : reload meshes, IRM, plots, and regenerate textures
        /// </summary>
        /// <param name="patient"></param>
        public IEnumerator c_Initialize(Data.Visualization.Visualization visualization, GenericEvent<float, float, string> onChangeProgress, Action<Exception> outPut)
        {
            Exception exception = null;

            // Compute progress variables
            List<string> usableImplantations = visualization.FindUsableImplantations();
            float totalTime = Patient.Brain.Meshes.Count * LOADING_MESH_WEIGHT + Patient.Brain.MRIs.Count * LOADING_MRI_WEIGHT + usableImplantations.Count * LOADING_IMPLANTATIONS_WEIGHT + LOADING_MNI_WEIGHT + LOADING_IEEG_WEIGHT;
            float loadingMeshProgress = LOADING_MESH_WEIGHT / totalTime;
            float loadingMeshTime = LOADING_MESH_WEIGHT / 1000.0f;
            float loadingMRIProgress = LOADING_MRI_WEIGHT / totalTime;
            float loadingMRITime = LOADING_MRI_WEIGHT / 1000.0f;
            float loadingImplantationsProgress = LOADING_IMPLANTATIONS_WEIGHT / totalTime;
            float loadingImplantationsTime = LOADING_IMPLANTATIONS_WEIGHT / 1000.0f;
            float loadingMNIProgress = LOADING_MNI_WEIGHT / totalTime;
            float loadingMNITime = LOADING_MNI_WEIGHT / 1000.0f;
            float loadingIEEGProgress = LOADING_IEEG_WEIGHT / totalTime;
            float loadingIEEGTime = LOADING_IEEG_WEIGHT / 1000.0f;

            yield return Ninja.JumpToUnity;
            float progress = 1.0f;
            onChangeProgress.Invoke(progress, 0.0f, "");
            
            int sceneID = ApplicationState.Module3D.NumberOfScenesLoadedSinceStart;
            gameObject.name = "SinglePatient Scene (" + sceneID + ")";
            transform.position = new Vector3(HBP3DModule.SPACE_BETWEEN_SCENES_AND_COLUMNS * sceneID, transform.position.y, transform.position.z);

            // reset columns
            m_ColumnManager.Initialize(m_Cuts.Count);

            // Load Meshes
            foreach (Data.Anatomy.Mesh mesh in Patient.Brain.Meshes)
            {
                progress += loadingMeshProgress;
                onChangeProgress.Invoke(progress, loadingMeshTime, "Loading Mesh: " + mesh.Name);
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
            foreach (Data.Anatomy.MRI mri in Patient.Brain.MRIs)
            {
                progress += loadingMRIProgress;
                onChangeProgress.Invoke(progress, loadingMRITime, "Loading MRI: " + mri.Name);
                yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadBrainVolume(mri));
            }

            // Load Sites
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadImplantations(visualization.Patients, usableImplantations, (i) =>
            {
                progress += loadingImplantationsProgress;
                onChangeProgress.Invoke(progress, loadingImplantationsTime, "Loading implantations [" + i + "/" + usableImplantations.Count + "]");
            }));
            SceneInformation.MeshGeometryNeedsUpdate = true;

            // Load MNI
            progress += loadingMNIProgress;
            onChangeProgress.Invoke(progress, loadingMNITime, "Loading MNI objects");
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadMNIObjects());

            // Set Timeline
            progress += loadingIEEGProgress;
            onChangeProgress.Invoke(progress, loadingIEEGTime, "Loading Columns");
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_SetEEGData());

            // Finalization
            m_ColumnManager.InitializeColumnsMeshes(m_DisplayedObjects.BrainSurfaceMeshesParent);
            UpdateMeshesColliders();
            OnUpdateCameraTarget.Invoke(m_ColumnManager.SelectedMesh.Both.BoundingBox.Center);

            outPut(exception);
        }
        /// <summary>
        /// Reset the meshes of the scene with GII files
        /// </summary>
        /// <param name="pathGIIBrainFiles"></param>
        /// <param name="transformation"></param>
        /// <returns></returns>
        private IEnumerator c_LoadBrainSurface(Data.Anatomy.Mesh mesh)
        {
            SceneInformation.MeshesLoaded = false;

            // checks parameters
            //if (mesh == null) throw new EmptyFilePathException("GII"); // FIXME
            //if (!mesh.Usable) throw new EmptyFilePathException("GII"); // TODO CHANGE TO NOT USABLE

            if (mesh.Usable)
            {
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
                else if (mesh is Data.Anatomy.SingleMesh)
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
            }
            yield return true;
        }
        #endregion
    }
}