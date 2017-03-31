

/**
 * \file    Mode.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define Mode, ModeSpecifications
 */

// system
using System;
using System.Collections.Generic;

// unity
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HBP.VISU3D
{

#if UNITY_EDITOR
    [CustomEditor(typeof(Mode))]
    public class Mode_Editor : Editor
    {
        SerializedProperty IDMode;
        SerializedProperty FunctionsMask;
        SerializedProperty UIOverlayMask;
        SerializedProperty DisplayItems;


        bool showFunctions = true;
        bool showUIOverlay = true;
        bool showDisplayItems = true;

        void OnEnable()
        {
            IDMode = serializedObject.FindProperty("m_idMode");
            FunctionsMask = serializedObject.FindProperty("functionsMask");
            UIOverlayMask = serializedObject.FindProperty("uiOverlayMask");
            DisplayItems = serializedObject.FindProperty("m_displayItems");
        }

        public override void OnInspectorGUI()
        {
            // fill arrays
            int nbFunctionsId = Enum.GetNames(typeof(Mode.FunctionsId)).Length;
            if (FunctionsMask.arraySize != nbFunctionsId)
            {
                if (FunctionsMask.arraySize < nbFunctionsId)
                {
                    int diff = nbFunctionsId - FunctionsMask.arraySize;
                    for (int ii = 0; ii < diff; ++ii)
                        FunctionsMask.InsertArrayElementAtIndex(0);
                }
                else
                {
                    int diff = FunctionsMask.arraySize - nbFunctionsId;
                    for (int ii = 0; ii < diff; ++ii)
                        FunctionsMask.DeleteArrayElementAtIndex(0);
                }
            }

            int nbUIOverlayId = Enum.GetNames(typeof(Mode.UIOverlayId)).Length;
            if (UIOverlayMask.arraySize != nbUIOverlayId)
            {
                if (UIOverlayMask.arraySize < nbUIOverlayId)
                {
                    int diff = nbUIOverlayId - UIOverlayMask.arraySize;
                    for (int ii = 0; ii < diff; ++ii)
                        UIOverlayMask.InsertArrayElementAtIndex(0);
                }
                else
                {
                    int diff = UIOverlayMask.arraySize - nbUIOverlayId;
                    for (int ii = 0; ii < diff; ++ii)
                        UIOverlayMask.DeleteArrayElementAtIndex(0);
                }
            }

            int nbDisplayItems= Enum.GetNames(typeof(Cam.DisplayedItems)).Length;

            if (DisplayItems.arraySize != nbDisplayItems)
            {
                if (DisplayItems.arraySize < nbDisplayItems)
                {
                    int diff = nbDisplayItems - DisplayItems.arraySize;
                    for (int ii = 0; ii < diff; ++ii)
                        DisplayItems.InsertArrayElementAtIndex(0);
                }
                else
                {
                    int diff = DisplayItems.arraySize - nbDisplayItems;
                    for (int ii = 0; ii < diff; ++ii)
                        DisplayItems.DeleteArrayElementAtIndex(0);
                }
            }


            EditorGUILayout.PropertyField(IDMode, new GUIContent("ID : "), true);

            showFunctions = EditorGUILayout.Foldout(showFunctions, "Functions");
            if(showFunctions)
            {
                EditorGUI.indentLevel++;
                for (int ii = 0; ii < FunctionsMask.arraySize; ++ii)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUIUtility.labelWidth = 70;
                    EditorGUILayout.LabelField(Enum.GetNames(typeof(Mode.FunctionsId))[ii]);
                    EditorGUILayout.PropertyField(FunctionsMask.GetArrayElementAtIndex(ii), new GUIContent("mask : "), true);
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }

            showUIOverlay = EditorGUILayout.Foldout(showUIOverlay, "UI overlay");
            if (showUIOverlay)
            {
                EditorGUI.indentLevel++;
                for (int ii = 0; ii < UIOverlayMask.arraySize; ++ii)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUIUtility.labelWidth = 70;
                    EditorGUILayout.LabelField(Enum.GetNames(typeof(Mode.UIOverlayId))[ii]);
                    EditorGUILayout.PropertyField(UIOverlayMask.GetArrayElementAtIndex(ii), new GUIContent("mask : "), true);
                    EditorGUILayout.EndHorizontal();

                }
                EditorGUI.indentLevel--;
            }

            showDisplayItems = EditorGUILayout.Foldout(showDisplayItems, "Displayed items");
            if (showDisplayItems)
            {
                EditorGUI.indentLevel++;
                for (int ii = 0; ii < DisplayItems.arraySize; ++ii)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUIUtility.labelWidth = 70;
                    EditorGUILayout.LabelField(Enum.GetNames(typeof(Cam.DisplayedItems))[ii]);
                    EditorGUILayout.PropertyField(DisplayItems.GetArrayElementAtIndex(ii), new GUIContent("display : "), true);
                    EditorGUILayout.EndHorizontal();

                }
                EditorGUI.indentLevel--;
            }


            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
#endif

    /// <summary>
    /// Specifications for updating the UI and the display with the mode
    /// </summary>
    public class ModeSpecifications
    {
        public Mode mode;
        public List<bool> uiOverlayMask;
        public List<bool> itemMaskDisplay;
    }

    namespace Events
    {
        /// <summary>
        /// Event for sending mode specifications
        /// </summary>
        public class SendModeSpecifications : UnityEvent<ModeSpecifications> { }
    }


    /// <summary>
    /// Logic state of the scene
    /// </summary>
    [Serializable]
    public class Mode : MonoBehaviour
    {
        #region members

        public enum ModesId : int
        {
            NoPathDefined, MinPathDefined, AllPathDefined, ComputingAmplitudes, AmplitudesComputed, TriErasing, ROICreation, AmpNeedUpdate, Error,
        }; /**< modes id */
        public enum FunctionsId : int
        {
            resetGIIBrainSurfaceFile, resetNIIBrainVolumeFile, resetElectrodesFile, pre_updateGenerators, post_updateGenerators,
            addNewPlane, removeLastPlane, updatePlane, setDisplayedMesh, setTimelines, enableTriErasingMode, disableTriErasingMode, enableROICreationMode, disableROICreationMode,
            updateMiddle, updateMaskPlot, add_FMRI_column, removeLastIRMFColumn, resetScene
        }; /**< scene functions id */

        public enum UIOverlayId : int { planes_controller, timeline_controller, icones_controller, cut_display_controller, colormap_controller, minimize_controller, time_display_controller}; /**< UI overlay elements */
        
        private bool m_needsUpdate = true; /**< is the mode has to update it's specifications ? */
        public bool m_sceneSp;  /**< is the mode associated to a single patient scene ? */
        public ModesId m_idMode; /**< id of the mode */
        public SceneStatesInfo m_sceneStates = null; /**< scene states info */

        public List<bool> uiOverlayMask = null; /**< ui overlay mask for this mod */
        public List<bool> functionsMask = null; /**< functions mask for this mode */
        public List<bool> m_displayItems = null;  /**< items to be displayed in this mode  0 : meshes, 1 : plots, 2 : ROI */

        private ModeSpecifications m_modeSpecs = new ModeSpecifications();

        // events
        public Events.SendModeSpecifications SendModeSpecifications = new Events.SendModeSpecifications();

        #endregion memebers

        #region functions 

        /// <summary>
        /// Init the mode
        /// </summary>
        /// <param name="scene"></param>
        public void init(Base3DScene scene)
        {
            m_sceneStates = scene.data_;
            m_sceneSp = scene.singlePatient;
        }

        /// <summary>
        /// Ask the mode to 
        /// </summary>
        public void updateMode()
        {
            m_needsUpdate = true;
        }

        /// <summary>
        /// Ask the mode for send it's specifications
        /// </summary>
        /// <param name="force"> dot it even when needsUpdate is false </param>
        public void setModeSpecifications(bool force)
        {
            if (!m_needsUpdate && force == false)
                return;

            m_modeSpecs.mode = GetComponent<Mode>();
            m_modeSpecs.itemMaskDisplay = m_displayItems;
            m_modeSpecs.uiOverlayMask = uiOverlayMask;
            SendModeSpecifications.Invoke(m_modeSpecs);
            m_needsUpdate = false;
        }



        public Mode.ModesId mu_resetGIIBrainSurfaceFile()
        {
            // sp
            if(m_sceneSp)
            {
                if (m_idMode == ModesId.NoPathDefined || m_idMode == ModesId.MinPathDefined || m_idMode == ModesId.AllPathDefined || m_idMode == ModesId.AmplitudesComputed || m_idMode == ModesId.TriErasing || m_idMode == ModesId.AmpNeedUpdate)
                {
                    if (m_sceneStates.mriLoaded && m_sceneStates.meshesLoaded)
                    {
                        if (!m_sceneStates.sitesLoaded || !m_sceneStates.timelinesLoaded)
                            return ModesId.MinPathDefined;
                        else
                            return ModesId.AllPathDefined;
                    }
                    return ModesId.NoPathDefined;
                }
            }
            else
            {
                if (m_idMode == ModesId.NoPathDefined || m_idMode == ModesId.MinPathDefined || m_idMode == ModesId.AllPathDefined || m_idMode == ModesId.AmplitudesComputed || m_idMode == ModesId.TriErasing || m_idMode == ModesId.AmpNeedUpdate)
                {
                    if (m_sceneStates.mriLoaded && m_sceneStates.meshesLoaded)
                    {
                        if (!m_sceneStates.sitesLoaded || !m_sceneStates.timelinesLoaded)
                            return ModesId.MinPathDefined;
                        else
                            return ModesId.AllPathDefined;
                    }
                    return ModesId.NoPathDefined;
                }
            }

            // default
            return Mode.ModesId.Error;
        }


        public Mode.ModesId mu_resetNIIBrainVolumeFile()
        {
            // sp
            if(m_sceneSp)
            {
                if (m_idMode == ModesId.NoPathDefined || m_idMode == ModesId.MinPathDefined || m_idMode == ModesId.AllPathDefined || m_idMode == ModesId.AmplitudesComputed || m_idMode == ModesId.TriErasing || m_idMode == ModesId.AmpNeedUpdate)
                {
                    if (m_sceneStates.mriLoaded && m_sceneStates.meshesLoaded)
                    {
                        if (!m_sceneStates.sitesLoaded || !m_sceneStates.timelinesLoaded)
                            return ModesId.MinPathDefined;
                        else
                            return ModesId.AllPathDefined;
                    }
                    return ModesId.NoPathDefined;
                }
            }
            else
            {
                if (m_idMode == ModesId.NoPathDefined || m_idMode == ModesId.MinPathDefined || m_idMode == ModesId.AllPathDefined || m_idMode == ModesId.AmplitudesComputed || m_idMode == ModesId.TriErasing || m_idMode == ModesId.AmpNeedUpdate)
                {
                    if (m_sceneStates.mriLoaded && m_sceneStates.meshesLoaded)
                    {
                        if (!m_sceneStates.sitesLoaded || !m_sceneStates.timelinesLoaded)
                            return ModesId.MinPathDefined;
                        else
                            return ModesId.AllPathDefined;
                    }
                    return ModesId.NoPathDefined;
                }
            }


            // default
            return Mode.ModesId.Error;
        }

        public Mode.ModesId mu_resetElectrodesFile()
        {
            // sp
            if(m_sceneSp)
            {
                if (m_idMode == ModesId.NoPathDefined || m_idMode == ModesId.MinPathDefined || m_idMode == ModesId.AllPathDefined || m_idMode == ModesId.AmplitudesComputed || m_idMode == ModesId.TriErasing || m_idMode == ModesId.AmpNeedUpdate)
                {
                    if (m_sceneStates.mriLoaded && m_sceneStates.meshesLoaded)
                    {
                        if (!m_sceneStates.sitesLoaded || !m_sceneStates.timelinesLoaded)
                            return ModesId.MinPathDefined;
                        else
                            return ModesId.AllPathDefined;
                    }
                    return ModesId.NoPathDefined;
                }
            }
            else
            {
                if (m_idMode == ModesId.NoPathDefined || m_idMode == ModesId.MinPathDefined || m_idMode == ModesId.AllPathDefined || m_idMode == ModesId.AmplitudesComputed || m_idMode == ModesId.TriErasing || m_idMode == ModesId.AmpNeedUpdate)
                {
                    if (m_sceneStates.mriLoaded && m_sceneStates.meshesLoaded)
                    {
                        if (!m_sceneStates.sitesLoaded || !m_sceneStates.timelinesLoaded)
                            return ModesId.MinPathDefined;
                        else
                            return ModesId.AllPathDefined;
                    }
                    return ModesId.NoPathDefined;
                }
            }
           

            // default
            return Mode.ModesId.Error;
        }

        public Mode.ModesId mu_setTimelines()
        {
            // sp
            if(m_sceneSp)
            {
                if (m_idMode == ModesId.NoPathDefined || m_idMode == ModesId.MinPathDefined || m_idMode == ModesId.AllPathDefined || m_idMode == ModesId.AmplitudesComputed || m_idMode == ModesId.AmpNeedUpdate)
                {
                    if (m_sceneStates.mriLoaded && m_sceneStates.meshesLoaded)
                    {
                        if (!m_sceneStates.sitesLoaded || !m_sceneStates.timelinesLoaded)
                            return ModesId.MinPathDefined;
                        else
                            return ModesId.AllPathDefined;
                    }
                    return ModesId.NoPathDefined;
                }
            }
            else
            {
                if (m_idMode == ModesId.NoPathDefined || m_idMode == ModesId.MinPathDefined || m_idMode == ModesId.AllPathDefined || m_idMode == ModesId.AmplitudesComputed || m_idMode == ModesId.AmpNeedUpdate)
                {
                    if (m_sceneStates.mriLoaded && m_sceneStates.meshesLoaded)
                    {
                        if (!m_sceneStates.sitesLoaded || !m_sceneStates.timelinesLoaded)
                            return ModesId.MinPathDefined;
                        else
                            return ModesId.AllPathDefined;
                    }
                    return ModesId.NoPathDefined;
                }
            }          

            // default
            return Mode.ModesId.Error;
        }


        public Mode.ModesId mu_pre_updateGenerators()
        {
            // sp
            if(m_sceneSp)
            {
                if (m_idMode == ModesId.AllPathDefined || (m_idMode == Mode.ModesId.AmpNeedUpdate))
                {
                    return ModesId.ComputingAmplitudes;
                }
            }
            else
            {
                if ((m_idMode == ModesId.AllPathDefined) /** || (m_idMode == Mode.ModesId.ROICreation) */ || (m_idMode == Mode.ModesId.AmpNeedUpdate) || (m_idMode == Mode.ModesId.AmplitudesComputed))
                {
                    return ModesId.ComputingAmplitudes;
                }
            }

            // default
            return Mode.ModesId.Error;
        }

        public Mode.ModesId mu_post_updateGenerators()
        {
            // sp
            if (m_sceneSp)
            {
                if (m_idMode == ModesId.ComputingAmplitudes)
                {
                    return ModesId.AmplitudesComputed;
                }
            }
            else
            {
                if (m_idMode == ModesId.ComputingAmplitudes)
                {
                    //if (m_sceneStates.ROICreationMode)
                    //    return ModesId.ROICreation;
                    //else
                        return ModesId.AmplitudesComputed;
                }
            }

            // default
            return Mode.ModesId.Error;
        }

        public Mode.ModesId mu_addNewPlane()
        {
            // sp
            if(m_sceneSp)
            {
                if (m_idMode == Mode.ModesId.NoPathDefined || m_idMode == Mode.ModesId.MinPathDefined || m_idMode == Mode.ModesId.AllPathDefined)
                {
                    return m_idMode;
                }
                if ((m_idMode == Mode.ModesId.AmplitudesComputed) || (m_idMode == Mode.ModesId.AmpNeedUpdate))
                {
                    return Mode.ModesId.AllPathDefined;
                }
            }
            else
            {
                if (m_idMode == Mode.ModesId.NoPathDefined || m_idMode == Mode.ModesId.MinPathDefined || m_idMode == Mode.ModesId.AllPathDefined)
                {
                    return m_idMode;
                }
                if ((m_idMode == Mode.ModesId.AmplitudesComputed) || (m_idMode == Mode.ModesId.AmpNeedUpdate))
                {
                    return Mode.ModesId.AllPathDefined;
                }
                //if (m_idMode == Mode.ModesId.ROICreation)
                //{
                //    return Mode.ModesId.ROICreation;
                //}
            }

        

            // default
            return Mode.ModesId.Error;
        }

        public Mode.ModesId mu_removeLastPlane()
        {
            // sp
            if(m_sceneSp)
            {
                if (m_idMode == Mode.ModesId.NoPathDefined || m_idMode == Mode.ModesId.MinPathDefined || m_idMode == Mode.ModesId.AllPathDefined)
                {
                    return m_idMode;
                }
                if ((m_idMode == Mode.ModesId.AmplitudesComputed) || (m_idMode == Mode.ModesId.AmpNeedUpdate))
                {
                    return Mode.ModesId.AllPathDefined;
                }
            }
            else
            {
                if (m_idMode == Mode.ModesId.NoPathDefined || m_idMode == Mode.ModesId.MinPathDefined || m_idMode == Mode.ModesId.AllPathDefined)
                {
                    return m_idMode;
                }
                if ((m_idMode == Mode.ModesId.AmplitudesComputed) || (m_idMode == Mode.ModesId.AmpNeedUpdate))
                {
                    return Mode.ModesId.AllPathDefined;
                }
                //if (m_idMode == Mode.ModesId.ROICreation)
                //{
                //    return Mode.ModesId.ROICreation;
                //}
            }

            // default
            return Mode.ModesId.Error;
        }

        public Mode.ModesId mu_updatePlane()
        {
            // sp
            if(m_sceneSp)
            {
                if (m_idMode == Mode.ModesId.NoPathDefined || m_idMode == Mode.ModesId.MinPathDefined || m_idMode == Mode.ModesId.AllPathDefined)
                {
                    return m_idMode;
                }
                if ((m_idMode == Mode.ModesId.AmplitudesComputed) || (m_idMode == Mode.ModesId.AmpNeedUpdate))
                {
                    return Mode.ModesId.AllPathDefined;
                }
            }
            else
            {
                if (m_idMode == Mode.ModesId.NoPathDefined || m_idMode == Mode.ModesId.MinPathDefined || m_idMode == Mode.ModesId.AllPathDefined)
                {
                    return m_idMode;
                }
                if ((m_idMode == Mode.ModesId.AmplitudesComputed) || (m_idMode == Mode.ModesId.AmpNeedUpdate))
                {
                    return Mode.ModesId.AllPathDefined;
                }
                //if (m_idMode == Mode.ModesId.ROICreation)
                //{
                //    return Mode.ModesId.ROICreation;
                //}
            }


            // default
            return Mode.ModesId.Error;
        }

        public Mode.ModesId mu_setDisplayedMesh()
        {
            // mp
            if ((m_idMode == Mode.ModesId.MinPathDefined) || (m_idMode == Mode.ModesId.AllPathDefined))
            {
                return m_idMode;
            }
            if ((m_idMode == Mode.ModesId.AmplitudesComputed) || (m_idMode == Mode.ModesId.AmpNeedUpdate))
            {
                return Mode.ModesId.AllPathDefined;
            }

            // default
            return Mode.ModesId.Error;
        }

        //public Mode.ModesId mu_enableROICreationMode()
        //{
        //    // mp
        //    if (!m_sceneSp)
        //    {
        //        if (m_idMode == ModesId.AmplitudesComputed || m_idMode == ModesId.AllPathDefined)
        //        {
        //            return Mode.ModesId.ROICreation;
        //        }
        //    }

        //    // default
        //    return Mode.ModesId.Error;
        //}

        //public Mode.ModesId mu_disableROICreationMode()
        //{
        //    // mp
        //    if (!m_sceneSp)
        //    {
        //        if (m_idMode == ModesId.ROICreation)
        //        {
        //            if (m_sceneStates.generatorUpToDate)
        //            {
        //                return ModesId.AmplitudesComputed;
        //            }
        //            else
        //            {
        //                return ModesId.AllPathDefined;
        //            }
        //        }
        //    }

        //    // default
        //    return Mode.ModesId.Error;
        //}

        public Mode.ModesId mu_updateMiddle()
        {
            // sp
            if(m_sceneSp)
            {
                if ((m_idMode == Mode.ModesId.AmplitudesComputed) || (m_idMode == Mode.ModesId.AmpNeedUpdate))
                {
                    return Mode.ModesId.AllPathDefined;
                }
            }
            else
            {
                if ((m_idMode == Mode.ModesId.AmplitudesComputed) || (m_idMode == Mode.ModesId.AmpNeedUpdate))
                {
                    return Mode.ModesId.AllPathDefined;
                }
            }

            //if ((idMode == Mode.ModesId.SpAmplitudesComputed) || (idMode == Mode.ModesId.SpAmpNeedUpdate))
            //{
            //    return Mode.ModesId.AllSpPathDefined;
            //}

            //if ((idMode == Mode.ModesId.MpAmplitudesComputed) || (idMode == Mode.ModesId.MpAmpNeedUpdate))
            //{
            //    return Mode.ModesId.AllMpPathDefined;
            //}

            return m_idMode;
        }

        public Mode.ModesId mu_updateMaskPlot()
        {
            // sp
            if(m_sceneSp)
            {
                if (m_idMode == Mode.ModesId.AmplitudesComputed)
                {
                    return Mode.ModesId.AmpNeedUpdate;
                }
            }
            else
            {
                if (m_idMode == Mode.ModesId.AmplitudesComputed)
                {
                    return Mode.ModesId.AmpNeedUpdate;
                }
            }

            //if (idMode == Mode.ModesId.SpAmplitudesComputed)
            //{
            //    return Mode.ModesId.SpAmpNeedUpdate;
            //}

            //// mp
            //if (idMode == Mode.ModesId.MpAmplitudesComputed)
            //{
            //    return Mode.ModesId.MpAmpNeedUpdate;
            //}

            return m_idMode;
        }

        public ModesId mu_addIRMFColumn()
        {
            if (m_idMode == Mode.ModesId.NoPathDefined || m_idMode == Mode.ModesId.ComputingAmplitudes)
                return Mode.ModesId.Error;

            return m_idMode;
        }

        public ModesId mu_removeLastIRMFColumn()
        {
            if (m_idMode == Mode.ModesId.NoPathDefined || m_idMode == Mode.ModesId.ComputingAmplitudes)
                return Mode.ModesId.Error;

            return m_idMode;
        }


        // TODO
        public Mode.ModesId mu_enableTriErasingMode()
        {
            // TODO : needs work

            //// sp
            //if (idMode == Mode.ModesId.MinSpPathDefined || idMode == Mode.ModesId.AllSpPathDefined || idMode == Mode.ModesId.SpAmplitudesComputed)
            //{
            //    return Mode.ModesId.SpTriErasing;
            //}

            //// mp
            //if (idMode == Mode.ModesId.MinMpPathDefined || idMode == Mode.ModesId.AllMpPathDefined || idMode == Mode.ModesId.MpAmplitudesComputed)
            //{
            //    return Mode.ModesId.MpTriErasing;
            //}

            // default
            return Mode.ModesId.Error;
        }

        public Mode.ModesId mu_disableTriErasingMode()
        {
            // TODO : needs work

            //// sp
            //if (idMode == Mode.ModesId.SpTriErasing)
            //{
            //    if (data_.volumeLoaded && data_.meshesLoaded)
            //    {
            //        if (!data_.electodesLoaded || !data_.timelinesLoaded)
            //            return ModesId.MinSpPathDefined;
            //        else
            //        {
            //            if (data_.generatorUpToDate)
            //            {
            //                return ModesId.SpAmplitudesComputed;
            //            }
            //            else
            //            {
            //                return ModesId.AllSpPathDefined;
            //            }
            //        }
            //    }
            //}

            ////// mp
            ////if (idMode == BaseMode.ModesId.MpTriErasing)
            ////{
            ////    return BaseMode.ModesId.MpTriErasing;
            ////}

            // default
            return Mode.ModesId.Error;
        }

        /// <summary>
        /// Reset to start mode
        /// </summary>
        /// <returns></returns>
        public Mode.ModesId mu_resetScene()
        {
            return Mode.ModesId.NoPathDefined;
        }

        #endregion functions

    }
}