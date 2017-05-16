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

namespace HBP.Module3D
{
    /// <summary>
    /// Manager for the single patient and the multi patients scenes
    /// </summary>
    public class ScenesManager : MonoBehaviour
    {
        #region Properties
        private List<Base3DScene> m_Scenes = null;
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

        private Base3DScene m_SelectedScene = null;
        /// <summary>
        /// Currently selected scene
        /// </summary>
        public Base3DScene SelectedScene
        {
            set
            {
                m_SelectedScene.ModesManager.OnChangeMode.RemoveAllListeners();
                m_SelectedScene = value;
                OnChangeSelectedScene.Invoke(value);
            }
            get
            {
                return m_SelectedScene;
            }
        }

        /// <summary>
        /// Event called when the user selects another scene
        /// </summary>
        public GenericEvent<Base3DScene> OnChangeSelectedScene = new GenericEvent<Base3DScene>();
        /// <summary>
        /// Event called when a scene is added
        /// </summary>
        public GenericEvent<Base3DScene> OnAddScene = new GenericEvent<Base3DScene>();
        /// <summary>
        /// Event called when a scene is removed
        /// </summary>
        public GenericEvent<Base3DScene> OnRemoveScene = new GenericEvent<Base3DScene>();
        /// <summary>
        /// Event called when the mode specifications are sent
        /// </summary>
        public GenericEvent<ModeSpecifications> OnSendModeSpecifications = new GenericEvent<ModeSpecifications>();
        /// <summary>
        /// Event called when the user selects a scene
        /// </summary>
        public GenericEvent<bool> OnSelectScene = new GenericEvent<bool>();

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
        /// Add a new single patient scene
        /// </summary>
        /// <param name="visualization"></param>
        /// <param name="postIRM"></param>
        /// <returns></returns>
        public bool AddSinglePatientScene(Data.Visualization.SinglePatientVisualization visualization, bool postIRM)
        {
            GameObject newSinglePatientScene = Instantiate(SinglePatientScenePrefab, transform);
            SinglePatient3DScene scene = newSinglePatientScene.GetComponent<SinglePatient3DScene>();
            // Initialize the scene
            bool success = scene.Initialize(visualization, postIRM);
            if (!success)
            {
                Debug.LogError("-ERROR : Could not initialize new single patient scene.");
            }
            else
            {
                // Add the listeners
                scene.OnSendModeSpecifications.AddListener(((specs) =>
                {
                    OnSendModeSpecifications.Invoke(specs);
                }));
                // Add the scene to the list
                m_Scenes.Add(scene);
                m_SelectedScene = scene;
                OnAddScene.Invoke(scene);
            }
            return success;
        }
        /// <summary>
        /// Add a new multi patients scene
        /// </summary>
        /// <param name="visualization"></param>
        /// <returns></returns>
        public bool AddMultiPatientsScene(Data.Visualization.MultiPatientsVisualization visualization)
        {
            GameObject newMultiPatientsScene = Instantiate(MultiPatientsScenePrefab, transform);
            MultiPatients3DScene scene = newMultiPatientsScene.GetComponent<MultiPatients3DScene>();
            // Initialize the scene
            bool success = scene.Initialize(visualization);
            if (!success)
            {
                Debug.LogError("-ERROR : Could not initialize new multi patients scene.");
            }
            else
            {
                scene.OnSendModeSpecifications.AddListener(((specs) =>
                {
                    OnSendModeSpecifications.Invoke(specs);
                }));
                // Add the scene to the list
                m_Scenes.Add(scene);
                m_SelectedScene = scene;
                OnAddScene.Invoke(scene);
            }
            return success;
        }
        /// <summary>
        /// Remove a scene
        /// </summary>
        /// <param name="scene"></param>
        public void RemoveScene(Base3DScene scene)
        {
            Destroy(scene.gameObject);
            OnRemoveScene.Invoke(scene);
            m_Scenes.Remove(scene);
        }
        #endregion
    }
}