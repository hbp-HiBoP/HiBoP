


/**
 * \file    ModesManager.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define Modes
 */

// unity
using UnityEngine;
using UnityEngine.Events;

namespace HBP.Module3D
{
    public class OnChangeMode : UnityEvent<Mode> { };
    /// <summary>
    /// Class for managing the differents logic states of a scene
    /// </summary>
    [System.Serializable]
    public class ModesManager : MonoBehaviour
    {
        #region Properties
        protected Mode m_CurrentMode = null;
        public Mode CurrentMode
        {
            get { return m_CurrentMode; }
            set { m_CurrentMode = value; OnChangeMode.Invoke(value); }
        }
        public string CurrentModeName
        {
            get
            {
                return m_CurrentMode.name;
            }
        }
        public Mode.ModesId CurrentModeID
        {
            get
            {
                return m_CurrentMode.ID;
            }
        }
        public OnChangeMode OnChangeMode = new OnChangeMode();

        // sp         
        private Mode m_NoSinglePatientPathDefined = null;
        private Mode m_MinSinglePatientPathDefined = null;
        private Mode m_AllSinglePatientPathDefined = null;
        private Mode m_SinglePatientComputingAmplitudes = null;
        private Mode m_SinglePatientAmplitudesComputed = null;
        private Mode m_SinglePatientTriangleErasing = null;
        private Mode m_SinglePatientAmpNeedUpdate = null;
        private Mode m_SinglePatientErrorMode = null;

        // mp 
        private Mode m_NoMultiPatientsPathDefined = null;
        private Mode m_MinMultiPatientsPathDefined = null;
        private Mode m_AllMultiPatientsPathDefined = null;
        private Mode m_MultiPatientsComputingAmplitudes = null;
        private Mode m_MultiPatientsAmplitudesComputed = null;
        //private Mode mpROICreation = null;
        private Mode m_MultiPatientsTriangleErasing = null;
        private Mode m_MultiPatientsAmpNeedUpdate = null;
        private Mode m_MultiPatientsErrorMode = null;

        /// <summary>
        /// Event for sending mode specifications
        /// </summary>
        public GenericEvent<ModeSpecifications> SendModeSpecifications = new GenericEvent<ModeSpecifications>();
        #endregion

        #region Private Methods
        /// <summary>
        /// Init the mode
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="mode"></param>
        private void InitializeMode(Base3DScene scene, out Mode mode, string nameMode)
        {
            mode = transform.Find(nameMode).GetComponent<Mode>();
            mode.Initialize(scene);
            mode.SendModeSpecifications.AddListener((UnityEngine.Events.UnityAction<ModeSpecifications>)((specs) =>
            {
                this.SendModeSpecifications.Invoke(specs);
            }));
        }
        /// <summary>
        /// Set the current mode
        /// </summary>
        /// <param name="idMode"></param>
        private void SetMode(Mode.ModesId idMode)
        {
            switch (m_CurrentMode.Type)
            {
                case SceneType.SinglePatient:
                    switch (idMode)
                    {
                        case Mode.ModesId.NoPathDefined:
                            CurrentMode = m_NoSinglePatientPathDefined;
                            break;
                        case Mode.ModesId.MinPathDefined:
                            CurrentMode = m_MinSinglePatientPathDefined;
                            break;
                        case Mode.ModesId.AllPathDefined:
                            CurrentMode = m_AllSinglePatientPathDefined;
                            break;
                        case Mode.ModesId.ComputingAmplitudes:
                            CurrentMode = m_SinglePatientComputingAmplitudes;
                            break;
                        case Mode.ModesId.AmplitudesComputed:
                            CurrentMode = m_SinglePatientAmplitudesComputed;
                            break;
                        case Mode.ModesId.TriErasing:
                            CurrentMode = m_SinglePatientTriangleErasing;
                            break;
                        case Mode.ModesId.AmpNeedUpdate:
                            CurrentMode = m_SinglePatientAmpNeedUpdate;
                            break;
                        case Mode.ModesId.Error:
                            CurrentMode = m_SinglePatientErrorMode;
                            break;
                        default:
                            CurrentMode = m_SinglePatientErrorMode;
                            break;
                    }
                    break;
                case SceneType.MultiPatients:
                    switch (idMode)
                    {
                        case Mode.ModesId.NoPathDefined:
                            CurrentMode = m_NoMultiPatientsPathDefined;
                            break;
                        case Mode.ModesId.MinPathDefined:
                            CurrentMode = m_MinMultiPatientsPathDefined;
                            break;
                        case Mode.ModesId.AllPathDefined:
                            CurrentMode = m_AllMultiPatientsPathDefined;
                            break;
                        case Mode.ModesId.ComputingAmplitudes:
                            CurrentMode = m_MultiPatientsComputingAmplitudes;
                            break;
                        case Mode.ModesId.AmplitudesComputed:
                            CurrentMode = m_MultiPatientsAmplitudesComputed;
                            break;
                        case Mode.ModesId.TriErasing:
                            CurrentMode = m_MultiPatientsTriangleErasing;
                            break;
                        case Mode.ModesId.AmpNeedUpdate:
                            CurrentMode = m_MultiPatientsAmpNeedUpdate;
                            break;
                        case Mode.ModesId.Error:
                            CurrentMode = m_MultiPatientsErrorMode;
                            break;
                        default:
                            CurrentMode = m_MultiPatientsErrorMode;
                            break;
                    }
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Init all the modes associated to the input scene
        /// </summary>
        /// <param name="scene"></param>
        public void Initialize(Base3DScene scene)
        {
            switch (scene.Type)
            {
                case SceneType.SinglePatient:
                    InitializeMode(scene, out m_NoSinglePatientPathDefined, "No path defined");
                    InitializeMode(scene, out m_MinSinglePatientPathDefined, "Min path defined");
                    InitializeMode(scene, out m_AllSinglePatientPathDefined, "All paths defined");
                    InitializeMode(scene, out m_SinglePatientComputingAmplitudes, "Computing amplitudes");
                    InitializeMode(scene, out m_SinglePatientAmplitudesComputed, "Amplitudes computed");
                    InitializeMode(scene, out m_SinglePatientTriangleErasing, "Tri erasing");
                    InitializeMode(scene, out m_SinglePatientAmpNeedUpdate, "Amp need update");
                    InitializeMode(scene, out m_SinglePatientErrorMode, "Error");
                    CurrentMode = m_NoSinglePatientPathDefined;
                    break;
                case SceneType.MultiPatients:
                    InitializeMode(scene, out m_NoMultiPatientsPathDefined, "No path defined");
                    InitializeMode(scene, out m_MinMultiPatientsPathDefined, "Min path defined");
                    InitializeMode(scene, out m_AllMultiPatientsPathDefined, "All paths defined");
                    InitializeMode(scene, out m_MultiPatientsComputingAmplitudes, "Computing amplitudes");
                    InitializeMode(scene, out m_MultiPatientsAmplitudesComputed, "Amplitudes computed");
                    //initMode(scene, out mpROICreation, "ROI creation");
                    InitializeMode(scene, out m_MultiPatientsAmpNeedUpdate, "Amp need update");
                    InitializeMode(scene, out m_MultiPatientsErrorMode, "Error");
                    CurrentMode = m_NoMultiPatientsPathDefined;
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// Check if the current mode can access the input function
        /// </summary>
        /// <param name="idFunction"></param>
        /// <returns></returns>
        public bool FunctionAccess(Mode.FunctionsId idFunction)
        {
            return m_CurrentMode.FunctionsMask[(int)idFunction];
        }
        /// <summary>
        /// Ask the current mode to update it's specifications
        /// </summary>
        /// <param name="force"> force the update</param>
        public void SetCurrentModeSpecifications(bool force = false)
        {
            m_CurrentMode.SetModeSpecifications(force);
        }
        /// <summary>
        /// Update the current mode with the input function
        /// </summary>
        /// <param name="idLastFunction"></param>
        public void UpdateMode(Mode.FunctionsId idLastFunction)
        {
            switch (idLastFunction)
            {
                case Mode.FunctionsId.ResetGIIBrainSurfaceFile:
                    SetMode(m_CurrentMode.mu_ResetGIIBrainSurfaceFile());
                    break;
                case Mode.FunctionsId.ResetNIIBrainVolumeFile:
                    SetMode(m_CurrentMode.mu_ResetNIIBrainVolumeFile());
                    break;
                case Mode.FunctionsId.ResetElectrodesFile:
                    SetMode(m_CurrentMode.mu_ResetElectrodesFile());
                    break;
                case Mode.FunctionsId.PreUpdateGenerators:
                    SetMode(m_CurrentMode.mu_PreUpdateGenerators());
                    break;
                case Mode.FunctionsId.PostUpdateGenerators:
                    SetMode(m_CurrentMode.mu_PostUpdateGenerators());
                    break;
                case Mode.FunctionsId.AddNewPlane:
                    SetMode(m_CurrentMode.mu_AddNewPlane());
                    break;
                case Mode.FunctionsId.UpdatePlane:
                    SetMode(m_CurrentMode.mu_UpdatePlane());
                    break;
                case Mode.FunctionsId.RemoveLastPlane:
                    SetMode(m_CurrentMode.mu_RemoveLastPlane());
                    break;
                case Mode.FunctionsId.SetDisplayedMesh:
                    SetMode(m_CurrentMode.mu_SetDisplayedMesh());
                    break;
                case Mode.FunctionsId.SetTimelines:
                    SetMode(m_CurrentMode.mu_SetTimelines());
                    break;
                case Mode.FunctionsId.AnableTriangleErasingMode:
                    SetMode(m_CurrentMode.mu_EnableTriangleErasingMode());
                    break;
                case Mode.FunctionsId.DisableTriangleErasingMode:
                    SetMode(m_CurrentMode.mu_DisableTriangleErasingMode());
                    break;
                case Mode.FunctionsId.UpdateMiddle:
                    SetMode(m_CurrentMode.mu_UpdateMiddle());
                    break;
                case Mode.FunctionsId.UpdateMaskPlot:
                    SetMode(m_CurrentMode.mu_UpdateMaskPlot());
                    break;
                case Mode.FunctionsId.AddFMRIColumn:
                    SetMode(m_CurrentMode.mu_AddFMRIColumn());
                    break;
                case Mode.FunctionsId.RemoveLastFMRIColumn:
                    SetMode(m_CurrentMode.mu_RemoveLastFMRIColumn());
                    break;
                case Mode.FunctionsId.ResetScene:
                    SetMode(m_CurrentMode.mu_ResetScene());
                    break;

            }

            m_CurrentMode.UpdateMode();
        }
        #endregion
    }
}