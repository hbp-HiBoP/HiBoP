

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

namespace HBP.Module3D
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
            IDMode = serializedObject.FindProperty("ID");
            FunctionsMask = serializedObject.FindProperty("FunctionsMask");
            UIOverlayMask = serializedObject.FindProperty("UIOverlayMask");
            DisplayItems = serializedObject.FindProperty("DisplayItems");
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
        public Mode Mode;
        public List<bool> UIOverlayMask;
        public List<bool> ItemMaskDisplay;
    }

    /// <summary>
    /// Logic state of the scene
    /// </summary>
    [Serializable]
    public class Mode : MonoBehaviour
    {
        #region Properties
        public enum ModesId
        {
            NoPathDefined, MinPathDefined, AllPathDefined, ComputingAmplitudes, AmplitudesComputed, TriErasing, ROICreation, AmpNeedUpdate, Error
        }; /**< modes id */
        public enum FunctionsId
        {
            ResetGIIBrainSurfaceFile, ResetNIIBrainVolumeFile, ResetElectrodesFile, PreUpdateGenerators, PostUpdateGenerators,
            AddNewPlane, RemoveLastPlane, UpdatePlane, SetDisplayedMesh, SetTimelines, AnableTriangleErasingMode, DisableTriangleErasingMode, EnableROICreationMode, DisableROICreationMode,
            ResetIEEG, UpdateMaskPlot, AddFMRIColumn, RemoveLastFMRIColumn, ResetScene
        }; /**< scene functions id */
        public enum UIOverlayId { PlanesController, TimelineController, IconsController, CutDisplayController, ColormapController, MinimizeController, TimeDisplayController }; /**< UI overlay elements */       
        private bool m_NeedsUpdate = true; /**< is the mode has to update it's specifications ? */
        public SceneType Type;  /**< is the mode associated to a single patient scene ? */
        public ModesId ID;
        public SceneStatesInfo SceneInformation = null; /**< scene states info */

        public List<bool> UIOverlayMask = null; /**< ui overlay mask for this mod */
        public List<bool> FunctionsMask = null; /**< functions mask for this mode */
        public List<bool> DisplayItems = null;  /**< items to be displayed in this mode  0 : meshes, 1 : plots, 2 : ROI */

        private ModeSpecifications m_Specifications = new ModeSpecifications();

        /// <summary>
        /// Event for sending mode specifications
        /// </summary>
        public GenericEvent<ModeSpecifications> SendModeSpecifications = new GenericEvent<ModeSpecifications>();
        #endregion

        #region Public Methods 
        /// <summary>
        /// Init the mode
        /// </summary>
        /// <param name="scene"></param>
        public void Initialize(Base3DScene scene)
        {
            SceneInformation = scene.SceneInformation;
            Type = scene.Type;
        }
        /// <summary>
        /// Ask the mode to 
        /// </summary>
        public void UpdateMode()
        {
            m_NeedsUpdate = true;
        }
        /// <summary>
        /// Ask the mode for send it's specifications
        /// </summary>
        /// <param name="force"> dot it even when needsUpdate is false </param>
        public void SetModeSpecifications(bool force)
        {
            if (!m_NeedsUpdate && force == false)
                return;

            m_Specifications.Mode = GetComponent<Mode>();
            m_Specifications.ItemMaskDisplay = DisplayItems;
            m_Specifications.UIOverlayMask = UIOverlayMask;
            m_NeedsUpdate = false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ModesId mu_ResetGIIBrainSurfaceFile()
        {
            if (ID == ModesId.NoPathDefined || ID == ModesId.MinPathDefined || ID == ModesId.AllPathDefined || ID == ModesId.AmplitudesComputed || ID == ModesId.TriErasing || ID == ModesId.AmpNeedUpdate)
            {
                if (SceneInformation.MRILoaded && SceneInformation.MeshesLoaded)
                {
                    if (!SceneInformation.SitesLoaded || !SceneInformation.TimelinesLoaded)
                        return ModesId.MinPathDefined;
                    else
                        return ModesId.AllPathDefined;
                }
                return ModesId.NoPathDefined;
            }
            return Mode.ModesId.Error;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ModesId mu_ResetNIIBrainVolumeFile()
        {
            if (ID == ModesId.NoPathDefined || ID == ModesId.MinPathDefined || ID == ModesId.AllPathDefined || ID == ModesId.AmplitudesComputed || ID == ModesId.TriErasing || ID == ModesId.AmpNeedUpdate)
            {
                if (SceneInformation.MRILoaded && SceneInformation.MeshesLoaded)
                {
                    if (!SceneInformation.SitesLoaded || !SceneInformation.TimelinesLoaded)
                        return ModesId.MinPathDefined;
                    else
                        return ModesId.AllPathDefined;
                }
                return ModesId.NoPathDefined;
            }
            return Mode.ModesId.Error;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ModesId mu_ResetElectrodesFile()
        {
            if (ID == ModesId.NoPathDefined || ID == ModesId.MinPathDefined || ID == ModesId.AllPathDefined || ID == ModesId.AmplitudesComputed || ID == ModesId.TriErasing || ID == ModesId.AmpNeedUpdate)
            {
                if (SceneInformation.MRILoaded && SceneInformation.MeshesLoaded)
                {
                    if (!SceneInformation.SitesLoaded || !SceneInformation.TimelinesLoaded)
                        return ModesId.MinPathDefined;
                    else
                        return ModesId.AllPathDefined;
                }
                return ModesId.NoPathDefined;
            }
            return Mode.ModesId.Error;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ModesId mu_SetTimelines()
        {
            if (ID == ModesId.NoPathDefined || ID == ModesId.MinPathDefined || ID == ModesId.AllPathDefined || ID == ModesId.AmplitudesComputed || ID == ModesId.AmpNeedUpdate)
            {
                if (SceneInformation.MRILoaded && SceneInformation.MeshesLoaded)
                {
                    if (!SceneInformation.SitesLoaded || !SceneInformation.TimelinesLoaded)
                        return ModesId.MinPathDefined;
                    else
                        return ModesId.AllPathDefined;
                }
                return ModesId.NoPathDefined;
            }
            return Mode.ModesId.Error;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ModesId mu_PreUpdateGenerators()
        {
            switch (Type)
            {
                case SceneType.SinglePatient:
                    if (ID == ModesId.AllPathDefined || (ID == Mode.ModesId.AmpNeedUpdate))
                    {
                        return ModesId.ComputingAmplitudes;
                    }
                    break;
                case SceneType.MultiPatients:
                    if ((ID == ModesId.AllPathDefined) || (ID == Mode.ModesId.AmpNeedUpdate) || (ID == Mode.ModesId.AmplitudesComputed))
                    {
                        return ModesId.ComputingAmplitudes;
                    }
                    break;
                default:
                    break;
            }
            return Mode.ModesId.Error;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ModesId mu_PostUpdateGenerators()
        {
            if (ID == ModesId.ComputingAmplitudes)
            {
                return ModesId.AmplitudesComputed;
            }
            return Mode.ModesId.Error;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ModesId mu_AddNewPlane()
        {
            if (ID == Mode.ModesId.NoPathDefined || ID == Mode.ModesId.MinPathDefined || ID == Mode.ModesId.AllPathDefined)
            {
                return ID;
            }
            if ((ID == Mode.ModesId.AmplitudesComputed) || (ID == Mode.ModesId.AmpNeedUpdate))
            {
                return Mode.ModesId.AllPathDefined;
            }
            return Mode.ModesId.Error;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ModesId mu_RemoveLastPlane()
        {
            if (ID == Mode.ModesId.NoPathDefined || ID == Mode.ModesId.MinPathDefined || ID == Mode.ModesId.AllPathDefined)
            {
                return ID;
            }
            if ((ID == Mode.ModesId.AmplitudesComputed) || (ID == Mode.ModesId.AmpNeedUpdate))
            {
                return Mode.ModesId.AllPathDefined;
            }
            return Mode.ModesId.Error;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ModesId mu_UpdatePlane()
        {
            if (ID == Mode.ModesId.NoPathDefined || ID == Mode.ModesId.MinPathDefined || ID == Mode.ModesId.AllPathDefined)
            {
                return ID;
            }
            if ((ID == Mode.ModesId.AmplitudesComputed) || (ID == Mode.ModesId.AmpNeedUpdate))
            {
                return Mode.ModesId.AllPathDefined;
            }
            return Mode.ModesId.Error;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ModesId mu_SetDisplayedMesh()
        {
            if ((ID == Mode.ModesId.MinPathDefined) || (ID == Mode.ModesId.AllPathDefined))
            {
                return ID;
            }
            if ((ID == Mode.ModesId.AmplitudesComputed) || (ID == Mode.ModesId.AmpNeedUpdate))
            {
                return Mode.ModesId.AllPathDefined;
            }
            return Mode.ModesId.Error;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ModesId mu_UpdateMiddle()
        {
            if ((ID == Mode.ModesId.AmplitudesComputed) || (ID == Mode.ModesId.AmpNeedUpdate))
            {
                return Mode.ModesId.AllPathDefined;
            }
            return ID;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ModesId mu_UpdateMaskPlot()
        {
            if (ID == Mode.ModesId.AmplitudesComputed)
            {
                return Mode.ModesId.AmpNeedUpdate;
            }
            return ID;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ModesId mu_AddFMRIColumn()
        {
            if (ID == Mode.ModesId.NoPathDefined || ID == Mode.ModesId.ComputingAmplitudes)
            {
                return Mode.ModesId.Error;
            }
            return ID;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ModesId mu_RemoveLastFMRIColumn()
        {
            if (ID == Mode.ModesId.NoPathDefined || ID == Mode.ModesId.ComputingAmplitudes)
            {
                return Mode.ModesId.Error;
            }
            return ID;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Mode.ModesId mu_EnableTriangleErasingMode()
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
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Mode.ModesId mu_DisableTriangleErasingMode()
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
        public Mode.ModesId mu_ResetScene()
        {
            return Mode.ModesId.NoPathDefined;
        }
        #endregion
    }
}