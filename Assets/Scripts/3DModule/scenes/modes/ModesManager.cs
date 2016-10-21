


/**
 * \file    ModesManager.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define Modes
 */

// system
using System;
using System.Collections;
using System.Collections.Generic;

// unity
using UnityEngine;
using UnityEngine.UI;

namespace HBP.VISU3D
{
    /// <summary>
    /// Class for managing the differents logic states of a scene
    /// </summary>
    [System.Serializable]
    public class ModesManager : MonoBehaviour
    {
        public Mode current = null;

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
        private Mode mpROICreation = null;
        private Mode mpTriErasing = null;
        private Mode mpAmpNeedUpdate = null;
        private Mode mpErrorMode = null;


        // events
        private Events.SendModeSpecifications m_sendModeSpecifications = new Events.SendModeSpecifications(); 
        public Events.SendModeSpecifications SendModeSpecifications { get { return m_sendModeSpecifications; } }

        /// <summary>
        /// Init all the modes associated to the input scene
        /// </summary>
        /// <param name="scene"></param>
        public void init(Base3DScene scene)
        {
            if (scene.singlePatient)
            {
                initMode(scene, out m_noSpPathDefined, "No path defined");
                initMode(scene, out minSpPathDefined, "Min path defined");
                initMode(scene, out  allSpPathDefined, "All paths defined");
                initMode(scene, out spComputingAmplitudes, "Computing amplitudes");
                initMode(scene, out spAmplitudesComputed, "Amplitudes computed");
                initMode(scene, out spTriErasing, "Tri erasing");
                initMode(scene, out spAmpNeedUpdate, "Amp need update");
                initMode(scene, out spErrorMode, "Error");
                current = m_noSpPathDefined;
            }
            else
            {
                initMode(scene, out noMpPathDefined, "No path defined");
                initMode(scene, out minMpPathDefined, "Min path defined");
                initMode(scene, out allMpPathDefined, "All paths defined");
                initMode(scene, out mpComputingAmplitudes, "Computing amplitudes");
                initMode(scene, out mpAmplitudesComputed, "Amplitudes computed");
                initMode(scene, out mpROICreation, "ROI creation");
                initMode(scene, out mpAmpNeedUpdate, "Amp need update");
                initMode(scene, out mpErrorMode, "Error");
                current = noMpPathDefined;
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
            mode.SendModeSpecifications.AddListener((specs) =>
            {
                m_sendModeSpecifications.Invoke(specs);
            });            
        }

        /// <summary>
        /// Check if the current mode can access the input function
        /// </summary>
        /// <param name="idFunction"></param>
        /// <returns></returns>
        public bool functionAccess(Mode.FunctionsId idFunction)
        {
            return current.functionsMask[(int)idFunction];
        }

        /// <summary>
        /// Return the current mode name
        /// </summary>
        /// <returns></returns>
        public string currentModeName()
        {            
            return current.name;
        }

        /// <summary>
        /// Return the current mode id
        /// </summary>
        /// <returns></returns>
        public Mode.ModesId currentIdMode()
        {
            return current.m_idMode;
        }

        /// <summary>
        /// Ask the current mode to update it's specifications
        /// </summary>
        /// <param name="force"> force the update</param>
        public void setCurrentModeSpecifications(bool force = false)
        {
            current.setModeSpecifications(force);
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
                    setMode(current.mu_resetGIIBrainSurfaceFile());
                    break;
                case Mode.FunctionsId.resetNIIBrainVolumeFile:
                    setMode(current.mu_resetNIIBrainVolumeFile());
                    break;
                case Mode.FunctionsId.resetElectrodesFile:
                    setMode(current.mu_resetElectrodesFile());
                    break;
                case Mode.FunctionsId.pre_updateGenerators:
                    setMode(current.mu_pre_updateGenerators());
                    break;
                case Mode.FunctionsId.post_updateGenerators:
                    setMode(current.mu_post_updateGenerators());
                    break;
                case Mode.FunctionsId.addNewPlane:
                    setMode(current.mu_addNewPlane());
                    break;
                case Mode.FunctionsId.updatePlane:
                    setMode(current.mu_updatePlane());
                    break;
                case Mode.FunctionsId.removeLastPlane:
                    setMode(current.mu_removeLastPlane());
                    break;
                case Mode.FunctionsId.setDisplayedMesh:
                    setMode(current.mu_setDisplayedMesh());
                    break;
                case Mode.FunctionsId.setTimelines:
                    setMode(current.mu_setTimelines());
                    break;
                case Mode.FunctionsId.enableTriErasingMode:
                    setMode(current.mu_enableTriErasingMode());
                    break;
                case Mode.FunctionsId.disableTriErasingMode:
                    setMode(current.mu_disableTriErasingMode());
                    break;
                case Mode.FunctionsId.enableROICreationMode:
                    setMode(current.mu_enableROICreationMode());
                    break;
                case Mode.FunctionsId.disableROICreationMode:
                    setMode(current.mu_disableROICreationMode());
                    break;
                case Mode.FunctionsId.updateMiddle:
                    setMode(current.mu_updateMiddle());
                    break;
                case Mode.FunctionsId.updateMaskPlot:
                    setMode(current.mu_updateMaskPlot());
                    break;
                case Mode.FunctionsId.addIRMFColumn:
                    setMode(current.mu_addIRMFColumn());
                    break;
                case Mode.FunctionsId.removeLastIRMFColumn:
                    setMode(current.mu_removeLastIRMFColumn());
                    break;
                case Mode.FunctionsId.resetScene:
                    setMode(current.mu_resetScene());
                    break;

            }

            current.updateMode();
        }

        /// <summary>
        /// Set the current mode
        /// </summary>
        /// <param name="idMode"></param>
        private void setMode(Mode.ModesId idMode)
        {
            if(current.m_sceneSp)
            {
                switch (idMode)
                {
                    case Mode.ModesId.NoPathDefined:
                        current = m_noSpPathDefined;
                        break;
                    case Mode.ModesId.MinPathDefined:
                        current = minSpPathDefined;
                        break;
                    case Mode.ModesId.AllPathDefined:
                        current = allSpPathDefined;
                        break;
                    case Mode.ModesId.ComputingAmplitudes:
                        current = spComputingAmplitudes;
                        break;
                    case Mode.ModesId.AmplitudesComputed:
                        current = spAmplitudesComputed;
                        break;
                    case Mode.ModesId.TriErasing:
                        current = spTriErasing;
                        break;
                    case Mode.ModesId.AmpNeedUpdate:
                        current = spAmpNeedUpdate;
                        break;
                    case Mode.ModesId.Error:
                        current = spErrorMode;
                        break;
                    default:
                        current = spErrorMode;
                        break;
                }
            }
            else
            {
                switch (idMode)
                {
                    case Mode.ModesId.NoPathDefined:
                        current = noMpPathDefined;
                        break;
                    case Mode.ModesId.MinPathDefined:
                        current = minMpPathDefined;
                        break;
                    case Mode.ModesId.AllPathDefined:
                        current = allMpPathDefined;
                        break;
                    case Mode.ModesId.ComputingAmplitudes:
                        current = mpComputingAmplitudes;
                        break;
                    case Mode.ModesId.AmplitudesComputed:
                        current = mpAmplitudesComputed;
                        break;
                    case Mode.ModesId.TriErasing:
                        current = mpTriErasing;
                        break;
                    case Mode.ModesId.ROICreation:
                        current = mpROICreation;
                        break;
                    case Mode.ModesId.AmpNeedUpdate:
                        current = mpAmpNeedUpdate;
                        break;
                    case Mode.ModesId.Error:
                        current = mpErrorMode;
                        break;
                    default:
                        current = mpErrorMode;
                        break;
                }
            }
        }
    }
}