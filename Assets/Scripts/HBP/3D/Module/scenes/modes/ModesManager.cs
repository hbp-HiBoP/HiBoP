


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
        protected Mode m_CurrentMode = null;
        public Mode CurrentMode
        {
            get { return m_CurrentMode; }
            set { m_CurrentMode = value; OnChangeMode.Invoke(value); }
        }
        public OnChangeMode OnChangeMode = new OnChangeMode();

        // sp         
        private Mode m_noSpPathDefined = null;
        private Mode minSpPathDefined = null;
        private Mode allSpPathDefined = null;
        private Mode spComputingAmplitudes = null;
        private Mode spAmplitudesComputed = null;
        private Mode spTriErasing = null;
        private Mode spAmpNeedUpdate = null;
        private Mode spErrorMode = null;

        // mp 
        private Mode noMpPathDefined = null;
        private Mode minMpPathDefined = null;
        private Mode allMpPathDefined = null;
        private Mode mpComputingAmplitudes = null;
        private Mode mpAmplitudesComputed = null;
        //private Mode mpROICreation = null;
        private Mode mpTriErasing = null;
        private Mode mpAmpNeedUpdate = null;
        private Mode mpErrorMode = null;


        // events
        public Events.SendModeSpecifications SendModeSpecifications = new Events.SendModeSpecifications(); 

        /// <summary>
        /// Init all the modes associated to the input scene
        /// </summary>
        /// <param name="scene"></param>
        public void init(Base3DScene scene)
        {
            switch (scene.Type)
            {
                case SceneType.SinglePatient:
                    initMode(scene, out m_noSpPathDefined, "No path defined");
                    initMode(scene, out minSpPathDefined, "Min path defined");
                    initMode(scene, out allSpPathDefined, "All paths defined");
                    initMode(scene, out spComputingAmplitudes, "Computing amplitudes");
                    initMode(scene, out spAmplitudesComputed, "Amplitudes computed");
                    initMode(scene, out spTriErasing, "Tri erasing");
                    initMode(scene, out spAmpNeedUpdate, "Amp need update");
                    initMode(scene, out spErrorMode, "Error");
                    CurrentMode = m_noSpPathDefined;
                    break;
                case SceneType.MultiPatients:
                    initMode(scene, out noMpPathDefined, "No path defined");
                    initMode(scene, out minMpPathDefined, "Min path defined");
                    initMode(scene, out allMpPathDefined, "All paths defined");
                    initMode(scene, out mpComputingAmplitudes, "Computing amplitudes");
                    initMode(scene, out mpAmplitudesComputed, "Amplitudes computed");
                    //initMode(scene, out mpROICreation, "ROI creation");
                    initMode(scene, out mpAmpNeedUpdate, "Amp need update");
                    initMode(scene, out mpErrorMode, "Error");
                    CurrentMode = noMpPathDefined;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Init the mode
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="mode"></param>
        private void initMode(Base3DScene scene, out Mode mode, string nameMode)
        {
            mode = transform.Find(nameMode).GetComponent<Mode>();            
            mode.init(scene);
            mode.SendModeSpecifications.AddListener((UnityEngine.Events.UnityAction<ModeSpecifications>)((specs) =>
            {
                this.SendModeSpecifications.Invoke(specs);
            }));            
        }

        /// <summary>
        /// Check if the current mode can access the input function
        /// </summary>
        /// <param name="idFunction"></param>
        /// <returns></returns>
        public bool functionAccess(Mode.FunctionsId idFunction)
        {
            return m_CurrentMode.functionsMask[(int)idFunction];
        }

        /// <summary>
        /// Return the current mode name
        /// </summary>
        /// <returns></returns>
        public string currentModeName()
        {            
            return m_CurrentMode.name;
        }

        /// <summary>
        /// Return the current mode id
        /// </summary>
        /// <returns></returns>
        public Mode.ModesId currentIdMode()
        {
            return m_CurrentMode.IDMode;
        }

        /// <summary>
        /// Ask the current mode to update it's specifications
        /// </summary>
        /// <param name="force"> force the update</param>
        public void set_current_mode_specifications(bool force = false)
        {
            m_CurrentMode.setModeSpecifications(force);
        }

        /// <summary>
        /// Update the current mode with the input function
        /// </summary>
        /// <param name="idLastFunction"></param>
        public void updateMode(Mode.FunctionsId idLastFunction)
        {
            switch (idLastFunction)
            {
                case Mode.FunctionsId.resetGIIBrainSurfaceFile:
                    setMode(m_CurrentMode.mu_resetGIIBrainSurfaceFile());
                    break;
                case Mode.FunctionsId.resetNIIBrainVolumeFile:
                    setMode(m_CurrentMode.mu_resetNIIBrainVolumeFile());
                    break;
                case Mode.FunctionsId.resetElectrodesFile:
                    setMode(m_CurrentMode.mu_resetElectrodesFile());
                    break;
                case Mode.FunctionsId.pre_updateGenerators:
                    setMode(m_CurrentMode.mu_pre_updateGenerators());
                    break;
                case Mode.FunctionsId.post_updateGenerators:
                    setMode(m_CurrentMode.mu_post_updateGenerators());
                    break;
                case Mode.FunctionsId.addNewPlane:
                    setMode(m_CurrentMode.mu_addNewPlane());
                    break;
                case Mode.FunctionsId.updatePlane:
                    setMode(m_CurrentMode.mu_updatePlane());
                    break;
                case Mode.FunctionsId.removeLastPlane:
                    setMode(m_CurrentMode.mu_removeLastPlane());
                    break;
                case Mode.FunctionsId.setDisplayedMesh:
                    setMode(m_CurrentMode.mu_setDisplayedMesh());
                    break;
                case Mode.FunctionsId.setTimelines:
                    setMode(m_CurrentMode.mu_setTimelines());
                    break;
                case Mode.FunctionsId.enableTriErasingMode:
                    setMode(m_CurrentMode.mu_enableTriErasingMode());
                    break;
                case Mode.FunctionsId.disableTriErasingMode:
                    setMode(m_CurrentMode.mu_disableTriErasingMode());
                    break;
                case Mode.FunctionsId.updateMiddle:
                    setMode(m_CurrentMode.mu_updateMiddle());
                    break;
                case Mode.FunctionsId.updateMaskPlot:
                    setMode(m_CurrentMode.mu_updateMaskPlot());
                    break;
                case Mode.FunctionsId.add_FMRI_column:
                    setMode(m_CurrentMode.mu_addIRMFColumn());
                    break;
                case Mode.FunctionsId.removeLastIRMFColumn:
                    setMode(m_CurrentMode.mu_removeLastIRMFColumn());
                    break;
                case Mode.FunctionsId.resetScene:
                    setMode(m_CurrentMode.mu_resetScene());
                    break;

            }

            m_CurrentMode.updateMode();
        }

        /// <summary>
        /// Set the current mode
        /// </summary>
        /// <param name="idMode"></param>
        private void setMode(Mode.ModesId idMode)
        {
            switch (m_CurrentMode.m_Type)
            {
                case SceneType.SinglePatient:
                    switch (idMode)
                    {
                        case Mode.ModesId.NoPathDefined:
                            CurrentMode = m_noSpPathDefined;
                            break;
                        case Mode.ModesId.MinPathDefined:
                            CurrentMode = minSpPathDefined;
                            break;
                        case Mode.ModesId.AllPathDefined:
                            CurrentMode = allSpPathDefined;
                            break;
                        case Mode.ModesId.ComputingAmplitudes:
                            CurrentMode = spComputingAmplitudes;
                            break;
                        case Mode.ModesId.AmplitudesComputed:
                            CurrentMode = spAmplitudesComputed;
                            break;
                        case Mode.ModesId.TriErasing:
                            CurrentMode = spTriErasing;
                            break;
                        case Mode.ModesId.AmpNeedUpdate:
                            CurrentMode = spAmpNeedUpdate;
                            break;
                        case Mode.ModesId.Error:
                            CurrentMode = spErrorMode;
                            break;
                        default:
                            CurrentMode = spErrorMode;
                            break;
                    }
                    break;
                case SceneType.MultiPatients:
                    switch (idMode)
                    {
                        case Mode.ModesId.NoPathDefined:
                            CurrentMode = noMpPathDefined;
                            break;
                        case Mode.ModesId.MinPathDefined:
                            CurrentMode = minMpPathDefined;
                            break;
                        case Mode.ModesId.AllPathDefined:
                            CurrentMode = allMpPathDefined;
                            break;
                        case Mode.ModesId.ComputingAmplitudes:
                            CurrentMode = mpComputingAmplitudes;
                            break;
                        case Mode.ModesId.AmplitudesComputed:
                            CurrentMode = mpAmplitudesComputed;
                            break;
                        case Mode.ModesId.TriErasing:
                            CurrentMode = mpTriErasing;
                            break;
                        case Mode.ModesId.AmpNeedUpdate:
                            CurrentMode = mpAmpNeedUpdate;
                            break;
                        case Mode.ModesId.Error:
                            CurrentMode = mpErrorMode;
                            break;
                        default:
                            CurrentMode = mpErrorMode;
                            break;
                    }
                    break;
                default:
                    break;
            }
        }
    }
}