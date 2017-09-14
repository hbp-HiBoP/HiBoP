/**
 * \file    ScenesManager.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define ScenesManager class
 */
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections;
using CielaSpike;
using System;

namespace HBP.Module3D
{
    /// <summary>
    /// Manager for the single patient and the multi patients scenes
    /// </summary>
    public class ScenesManager : MonoBehaviour
    {
        #region Properties
        private List<Base3DScene> m_Scenes = new List<Base3DScene>();
        /// <summary>
        /// List of loaded scenes
        /// </summary>
        public ReadOnlyCollection<Base3DScene> Scenes
        {
            get { return new ReadOnlyCollection<Base3DScene>(m_Scenes); }
        }
        /// <summary>
        /// List of loaded visualizations
        /// </summary>
        public ReadOnlyCollection<Data.Visualization.Visualization> Visualizations
        {
            get
            {
                return new ReadOnlyCollection<Data.Visualization.Visualization>((from scene in m_Scenes select scene.Visualization).ToList());
            }
        }
        
        /// <summary>
        /// Currently selected scene
        /// </summary>
        public Base3DScene SelectedScene
        {
            get
            {
                return m_Scenes.Find((s) => s.IsSelected);
            }
        }

        /// <summary>
        /// Event called when the user selects another scene
        /// </summary>
        public GenericEvent<Base3DScene> OnChangeSelectedScene = new GenericEvent<Base3DScene>();
        /// <summary>
        /// Event called when the mode specifications are sent
        /// </summary>
        public GenericEvent<ModeSpecifications> OnSendModeSpecifications = new GenericEvent<ModeSpecifications>();
        /// <summary>
        /// Event called when the user selects a scene
        /// </summary>
        public GenericEvent<Base3DScene> OnSelectScene = new GenericEvent<Base3DScene>();

        /// <summary>
        /// Prefab corresponding to a single patient scene
        /// </summary>
        public GameObject SinglePatientScenePrefab;
        /// <summary>
        /// Prefab corresponding to a multi patient scene
        /// </summary>
        public GameObject MultiPatientsScenePrefab;
        #endregion

        #region Public Methods
        /// <summary>
        /// Remove a scene
        /// </summary>
        /// <param name="scene"></param>
        public void RemoveScene(Base3DScene scene)
        {
            ApplicationState.Module3D.OnRemoveScene.Invoke(scene);
            Destroy(scene.gameObject);
            m_Scenes.Remove(scene);
        }
        #endregion

        #region Coroutines
        /// <summary>
        /// Add a new single patient scene
        /// </summary>
        /// <param name="visualization"></param>
        /// <param name="postMRI"></param>
        /// <returns></returns>
        public IEnumerator c_AddSinglePatientScene(Data.Visualization.Visualization visualization, GenericEvent<float, float, string> onChangeProgress)
        {
            yield return Ninja.JumpToUnity;
            SinglePatient3DScene scene = Instantiate(SinglePatientScenePrefab, transform).GetComponent<SinglePatient3DScene>();
            scene.Initialize(visualization);
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(scene.c_Initialize(visualization, onChangeProgress));
            ApplicationState.Module3D.NumberOfScenesLoadedSinceStart++;
            // Add the listeners
            scene.OnSelectScene.AddListener((selectedScene) =>
            {
                foreach (Base3DScene s in m_Scenes)
                {
                    if (s != selectedScene)
                    {
                        s.IsSelected = false;
                    }
                }
                OnSelectScene.Invoke(selectedScene);
            });
            // Add the scene to the list
            m_Scenes.Add(scene);
            ApplicationState.Module3D.OnAddScene.Invoke(scene);
            scene.FinalizeInitialization();
        }
        /// <summary>
        /// Add a new multi patients scene
        /// </summary>
        /// <param name="visualization"></param>
        /// <returns></returns>
        public IEnumerator c_AddMultiPatientsScene(Data.Visualization.Visualization visualization, GenericEvent<float, float, string> onChangeProgress)
        {
            yield return Ninja.JumpToUnity;
            MultiPatients3DScene scene = Instantiate(MultiPatientsScenePrefab, transform).GetComponent<MultiPatients3DScene>();
            scene.Initialize(visualization);
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(scene.c_Initialize(visualization, onChangeProgress));
            ApplicationState.Module3D.NumberOfScenesLoadedSinceStart++;
            // Add the listeners
            scene.OnSelectScene.AddListener((selectedScene) =>
            {
                foreach (Base3DScene s in m_Scenes)
                {
                    if (s != selectedScene)
                    {
                        s.IsSelected = false;
                    }
                }
                OnSelectScene.Invoke(selectedScene);
            });
            // Add the scene to the list
            m_Scenes.Add(scene);
            ApplicationState.Module3D.OnAddScene.Invoke(scene);
            scene.FinalizeInitialization();
        }
        #endregion
    }
}