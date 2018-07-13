
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
        public override Data.Enums.SceneType Type
        {
            get
            {
                return Data.Enums.SceneType.SinglePatient;
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

            // reset columns
            m_ColumnManager.Initialize(m_Cuts.Count);
            
            // Load MNI
            progress += loadingMNIProgress;
            onChangeProgress.Invoke(progress, loadingMNITime, "Loading MNI objects");
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadMNIObjects(e => exception = e));
            if (exception != null)
            {
                outPut(exception);
                yield break;
            }

            // Load Meshes
            for (int i = 0; i < Patient.Brain.Meshes.Count; ++i)
            {
                Data.Anatomy.Mesh mesh = Patient.Brain.Meshes[i];
                progress += loadingMeshProgress;
                onChangeProgress.Invoke(progress, loadingMeshTime, "Loading Mesh: " + mesh.Name + " [" + (i + 1).ToString() + "/" + Patient.Brain.Meshes.Count + "]");
                yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadBrainSurface(mesh, e => exception = e));
            }
            if (exception != null)
            {
                outPut(exception);
                yield break;
            }

            if (m_ColumnManager.Meshes.Count > 0)
            {
                if (ApplicationState.UserPreferences.Data.Anatomy.PreloadMeshes)
                {
                    GenerateSplit(from mesh3D in m_ColumnManager.Meshes select mesh3D.Both);
                }
                else
                {
                    ResetSplitsNumber(10);
                }
            }
            else
            {
                ResetSplitsNumber(3);
            }
            SceneInformation.MeshesLoaded = true;

            // Load MRIs
            for (int i = 0; i < Patient.Brain.MRIs.Count; ++i)
            {
                Data.Anatomy.MRI mri = Patient.Brain.MRIs[i];
                progress += loadingMRIProgress;
                onChangeProgress.Invoke(progress, loadingMRITime, "Loading MRI: " + mri.Name + " [" + (i + 1).ToString() + "/" + Patient.Brain.MRIs.Count + "]");
                yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadBrainVolume(mri, e => exception = e));
            }
            if (exception != null)
            {
                outPut(exception);
                yield break;
            }
            SceneInformation.MRILoaded = true;

            // Load Sites
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
            SceneInformation.MeshGeometryNeedsUpdate = true;

            // Set Timeline
            progress += loadingIEEGProgress;
            onChangeProgress.Invoke(progress, loadingIEEGTime, "Loading columns");
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_SetColumns(e => exception = e));
            if (exception != null)
            {
                outPut(exception);
                yield break;
            }

            // Finalization
            m_ColumnManager.InitializeColumnsMeshes(m_DisplayedObjects.BrainSurfaceMeshesParent);
            OnUpdateCameraTarget.Invoke(m_ColumnManager.SelectedMesh.Both.BoundingBox.Center);

            outPut(exception);
        }
        /// <summary>
        /// Reset the meshes of the scene with GII files
        /// </summary>
        /// <param name="pathGIIBrainFiles"></param>
        /// <param name="transformation"></param>
        /// <returns></returns>
        private IEnumerator c_LoadBrainSurface(Data.Anatomy.Mesh mesh, Action<Exception> outPut)
        {
            SceneInformation.MeshesLoaded = false;
            try
            {
                if (mesh.Usable)
                {
                    if (mesh is Data.Anatomy.LeftRightMesh)
                    {
                        LeftRightMesh3D mesh3D = new LeftRightMesh3D((Data.Anatomy.LeftRightMesh)mesh);

                        if (ApplicationState.UserPreferences.Data.Anatomy.PreloadMeshes)
                        {
                            if (mesh3D.IsLoaded)
                            {
                                m_ColumnManager.Meshes.Add(mesh3D);
                            }
                            else
                            {
                                SceneInformation.MeshesLoaded = false;
                                throw new CanNotLoadGIIFile(mesh.Name);
                            }
                        }
                        else
                        {
                            string name = !string.IsNullOrEmpty(Visualization.Configuration.MeshName) ? Visualization.Configuration.MeshName : "Grey matter";
                            if (mesh3D.Name == name) mesh3D.Load();
                            m_ColumnManager.Meshes.Add(mesh3D);
                        }
                    }
                    else if (mesh is Data.Anatomy.SingleMesh)
                    {
                        SingleMesh3D mesh3D = new SingleMesh3D((Data.Anatomy.SingleMesh)mesh);

                        if (ApplicationState.UserPreferences.Data.Anatomy.PreloadMeshes)
                        {
                            if (mesh3D.IsLoaded)
                            {
                                m_ColumnManager.Meshes.Add(mesh3D);
                            }
                            else
                            {
                                SceneInformation.MeshesLoaded = false;
                                throw new CanNotLoadGIIFile(mesh.Name);
                            }
                        }
                        else
                        {
                            string name = !string.IsNullOrEmpty(Visualization.Configuration.MeshName) ? Visualization.Configuration.MeshName : "Grey matter";
                            if (mesh3D.Name == name) mesh3D.Load();
                            m_ColumnManager.Meshes.Add(mesh3D);
                        }
                    }
                    else
                    {
                        Debug.LogError("Mesh not handled.");
                    }
                }
            }
            catch (Exception e)
            {
                outPut(new CanNotLoadGIIFile(mesh.Name));
                yield break;
            }
            yield return true;
        }
        #endregion
    }
}