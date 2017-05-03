/**
 * \file    ScenesManager.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define ScenesManager class
 */
using UnityEngine;
using UnityEngine.Events;
using HBP.Module3D.Cam;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;

namespace HBP.Module3D
{
    namespace Events
    {
        public class FocusOnScene : UnityEvent<bool> { }
        public class OnChangeSelectedScene : UnityEvent<Base3DScene> { }
        public class OnAddScene : UnityEvent<Base3DScene> { }
        public class OnRemoveScene : UnityEvent<Base3DScene> { }
        public class OnAddFMRIColumn : UnityEvent { }
        public class OnRemoveFMRIColumn : UnityEvent { }
    }

    /// <summary>
    /// Manager for the single patient and the multi patients scenes
    /// </summary>
    public class ScenesManager : MonoBehaviour
    {
        #region Properties
        private List<Base3DScene> m_Scenes = null;
        public ReadOnlyCollection<Base3DScene> Scenes
        {
            get { return new ReadOnlyCollection<Base3DScene>(m_Scenes); }
        }
        private Base3DScene m_SelectedScene = null;
        public Base3DScene SelectedScene
        {
            set
            {
                m_SelectedScene.ModesManager.OnChangeMode.RemoveAllListeners();
                m_SelectedScene = value;
                OnChangeSelectedScene.Invoke(value);
            }
            get { return m_SelectedScene; }
        }

        // Events
        public Events.OnChangeSelectedScene OnChangeSelectedScene = new Events.OnChangeSelectedScene();
        public Events.OnAddScene OnAddScene = new Events.OnAddScene();
        public Events.OnRemoveScene OnRemoveScene = new Events.OnRemoveScene();
        public Events.OnAddFMRIColumn OnAddFMRIColumn = new Events.OnAddFMRIColumn();
        public Events.OnRemoveFMRIColumn OnRemoveFMRIColumn = new Events.OnRemoveFMRIColumn();

        // managers
        /*
        private CamerasManager m_CamerasManager = null;
        public CamerasManager CamerasManager
        {
            get { return m_CamerasManager; }
        }
        */
        // Scene prefabs
        public GameObject SinglePatientScenePrefab;
        public GameObject MultiPatientsScenePrefab;

        // canvas
        public GameObject m_Canvas = null;

        // UI elements
        public GameObject m_ModulePanel = null;
        /*
        public Transform SPPanel { get { return m_CamerasManager.SinglePatientPanel; } }
        public Transform MPPanel { get { return m_CamerasManager.MultiPatientsPanel; } }
        public Transform SPCameras { get { return m_CamerasManager.SinglePatientCamerasContainer; } }
        public Transform MPCameras { get { return m_CamerasManager.MultiPatientsCamerasContainer; } }
        */
        // events
        public Events.SendModeSpecifications SendModeSpecifications = new Events.SendModeSpecifications();
        public Events.FocusOnScene FocusOnScene = new Events.FocusOnScene();
        #endregion

        #region Private Methods
        void Awake()
        {
            int idScript = TimeExecution.get_ID();
            TimeExecution.start_awake(idScript, TimeExecution.ScriptsId.ScenesManager);
            
            // retrieve 
            //  managers
            //m_CamerasManager = transform.Find("Cameras").GetComponent<CamerasManager>();
            
            TimeExecution.end_awake(idScript, TimeExecution.ScriptsId.ScenesManager, gameObject);
        }
        /// <summary>
        /// Load IRMF dialog
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        private bool LoadfMRIDialogOfSelectedScene(out string path)
        {
            bool loaded = true;
            string[] filters = new string[] { "nii", "img" };
            path = "";
            path = DLL.QtGUI.GetExistingFileName(filters, "Select an fMRI file");

            if (!string.IsNullOrEmpty(path))
            {
                bool result = SelectedScene.LoadFMRIFile(path);
                if (!result)
                {
                    Debug.LogError("-ERROR : ScenesManager::addIRMF -> can't load IRMF");
                    loaded = false;
                }
            }
            else
            {
                loaded = false;
            }
            //m_CamerasManager.AddColumnCamera(type, CameraType.fMRI); // Maybe TODO : add this to the initialization of the fMRI column
            return loaded;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Add a new single patient scene
        /// </summary>
        /// <param name="visualisation"></param>
        /// <param name="postIRM"></param>
        /// <returns></returns>
        public bool AddSinglePatientScene(Data.Visualisation.SinglePatientVisualisation visualisation, bool postIRM)
        {
            GameObject newSinglePatientScene = Instantiate(SinglePatientScenePrefab, transform);
            SinglePatient3DScene scene = newSinglePatientScene.GetComponent<SinglePatient3DScene>();
            // Initialize the scene
            bool success = scene.Initialize(visualisation, postIRM);
            if (!success)
            {
                Debug.LogError("-ERROR : Could not initialize new single patient scene.");
                return false;
            }
            // Add the listeners
            scene.SendModeSpecifications.AddListener((UnityAction<ModeSpecifications>)((specs) =>
            {
                this.SendModeSpecifications.Invoke(specs);
            }));
            scene.UpdateCameraTarget.AddListener((target) =>
            {
                //m_CamerasManager.UpdateCamerasTarget(SceneType.SinglePatient, target);
            });
            // Add the scene to the list
            m_Scenes.Add(scene);
            m_SelectedScene = scene;
            OnAddScene.Invoke(scene);
            return true;
        }
        /// <summary>
        /// Add a new multi patients scene
        /// </summary>
        /// <param name="visualisation"></param>
        /// <returns></returns>
        public bool AddMultiPatientsScene(Data.Visualisation.MultiPatientsVisualisation visualisation)
        {
            GameObject newMultiPatientsScene = Instantiate(MultiPatientsScenePrefab, transform);
            MultiPatients3DScene scene = newMultiPatientsScene.GetComponent<MultiPatients3DScene>();
            // Initialize the scene
            bool success = scene.Initialize(visualisation);
            if (!success)
            {
                Debug.LogError("-ERROR : Could not initialize new multi patients scene.");
                return false;
            }
            // Add the listeners
            scene.ApplySceneCamerasToIndividualScene.AddListener(() =>
            {
                //m_CamerasManager.ApplyMultiPatientsCamerasSettingsToSinglePatientCameras();
            });
            scene.SendModeSpecifications.AddListener((UnityAction<ModeSpecifications>)((specs) =>
            {
                this.SendModeSpecifications.Invoke(specs);
            }));
            scene.UpdateCameraTarget.AddListener((target) =>
            {
               // m_CamerasManager.UpdateCamerasTarget(SceneType.MultiPatients, target);
            });
            // Add the scene to the list
            m_Scenes.Add(scene);
            m_SelectedScene = scene;
            OnAddScene.Invoke(scene);
            return true;
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        public void SetModuleFocusState(bool state)
        {
            //m_CamerasManager.SetModuleFocus(state);            
        }
        /// <summary>
        /// Display a message in the scene
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="message"></param>
        /// <param name="duration"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void DisplayMessageInScene(Base3DScene scene, string message, float duration, int width, int height)
        {
            scene.DisplayScreenMessage(message, duration, width, height);
        }
        /// <summary>
        /// Define the visibility of the scenes
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="mpScene"></param>
        public void SetScenesVisibility(bool spScene, bool mpScene) // TODO : delete or change
        {
            if (spScene)
                FocusOnScene.Invoke(true);
            if(mpScene)
                FocusOnScene.Invoke(false);

            float value;

            if (spScene && mpScene)
            {
                value = 0.5f;
            }
            else if (spScene)
            {
                value = 1f;
            }
            else if (mpScene)
            {
                value = 0f;
            }
            else
            {
                value = 0.5f;
            }

            //if (m_scenesRatioSlider.value == value)
            //{
            //    if (m_scenesRatioSlider.value != 1f)
            //    {
            //        m_scenesRatioSlider.value = 1f; // force callback
            //    }
            //    else
            //    {
            //        m_scenesRatioSlider.value = 0f; // force callback
            //    }
            //}

            //m_scenesRatioSlider.value = value;
        }
        /// <summary>
        /// Add a FMRI column to the selected scene
        /// </summary>
        /// <returns></returns>
        public bool AddfMRIColumnToSelectedScene()
        {
            string fMRIPath;
            if (!LoadfMRIDialogOfSelectedScene(out fMRIPath))
                return false;

            string[] split = fMRIPath.Split(new Char[] { '/', '\\' });
            string fmriLabel = split[split.Length - 1];
            
            bool result = SelectedScene.AddfMRIColumn(fmriLabel);
            if (!result)
            {
                Debug.LogError("-ERROR : ScenesManager::addIRMF -> can't add IRMF column");
                return false;
            }
            OnAddFMRIColumn.Invoke();

            return true;
        }
        /// <summary>
        /// Remove the last FMRI column from the selected scene
        /// </summary>
        public void RemoveLastfMRIColumnFromSelectedScene()
        {
            if (GetNumberOffMRIColumnsOfSelectedScene() > 0)
            {
                SelectedScene.RemoveLastFMRIColumn();
                OnRemoveFMRIColumn.Invoke();
            }
        }
        /// <summary>
        /// Get the number of FMRI column of the selected scene
        /// </summary>
        /// <returns></returns>
        public int GetNumberOffMRIColumnsOfSelectedScene() // maybe useless
        {
            return SelectedScene.GetNumberOffMRIColumns();
        }
        #endregion
    }
}