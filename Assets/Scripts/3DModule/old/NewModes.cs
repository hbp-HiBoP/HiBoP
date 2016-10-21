
//// system
//using System;
//using System.Collections;
//using System.Collections.Generic;

//// unity
//using UnityEngine;
//using UnityEngine.UI;

////using UnityEditor;
//namespace HBP.VISU3D
//{

//    //// IngredientDrawer
//    //[CustomPropertyDrawer(typeof(Modes))]
//    //public class IngredientDrawer : PropertyDrawer
//    //{

//    //    // Draw the property inside the given rect
//    //    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
//    //    {
//    //        // Using BeginProperty / EndProperty on the parent property means that
//    //        // prefab override logic works on the entire property.
//    //        EditorGUI.BeginProperty(position, label, property);

//    //        // Draw label
//    //        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

//    //        // Don't make child fields be indented
//    //        var indent = EditorGUI.indentLevel;
//    //        EditorGUI.indentLevel = 0;

//    //        // Calculate rects
//    //        var amountRect = new Rect(position.x, position.y, 30, position.height);
//    //        var unitRect = new Rect(position.x + 35, position.y, 50, position.height);
//    //        var nameRect = new Rect(position.x + 90, position.y, position.width - 90, position.height);

//    //        // Draw fields - passs GUIContent.none to each so they are drawn without labels
//    //        EditorGUI.PropertyField(amountRect, property.FindPropertyRelative("amount"), GUIContent.none);
//    //        EditorGUI.PropertyField(unitRect, property.FindPropertyRelative("unit"), GUIContent.none);
//    //        EditorGUI.PropertyField(nameRect, property.FindPropertyRelative("name"), GUIContent.none);

//    //        // Set indent back to what it was
//    //        EditorGUI.indentLevel = indent;

//    //        EditorGUI.EndProperty();
//    //    }
//    //}

//    [System.Serializable]
//    public class NewModes : MonoBehaviour
//    {
//        public Text m_currentModeText;

//        private bool uiUpdated = false;
//        private Mode current;

//        // sp only        
//        private Mode m_noSpPathDefined;
//        private Mode minSpPathDefined;
//        private Mode allSpPathDefined;
//        private Mode spComputingAmplitudes;
//        private Mode spAmplitudesComputed;
//        private Mode spTriErasing;

//        // mp only
//        private Mode noMpPathDefined;
//        private Mode minMpPathDefined;
//        private Mode allMpPathDefined;
//        private Mode mpComputingAmplitudes;
//        private Mode mpAmplitudesComputed;
//        private Mode mpROICreation;

//        // both
//        private Mode errorMode;

//        public void init(UIOverlayManager overlayManager, UICameraManager cameraManager, bool sp, DataS3DScene data, DisplayedObjects3DView go)
//        {
//            //Debug.Log(GetComponent<Base3DScene>().name);

//            //UIOverlayManager overlayManager = GetComponent<Base3DScene>().m_uiOverlayManager.GetComponent<UIOverlayManager>();
//            //UICameraManager cameraManager = GetComponent<Base3DScene>().m_uiCameraManager.GetComponent<UICameraManager>();

//            // sp
//            //m_noSpPathDefined = new NoSpPathDefined(overlayManager, cameraManager, data, go, sp);
//            //minSpPathDefined = new MinSpPathDefined(overlayManager, cameraManager, data, go, sp);
//            //allSpPathDefined = new AllSpPathDefined(overlayManager, cameraManager, data, go, sp);
//            //spComputingAmplitudes = new SpComputingAmplitudes(overlayManager, cameraManager, data, go, sp);
//            //spAmplitudesComputed = new SpAmplitudesComputed(overlayManager, cameraManager, data, go, sp);
//            //spTriErasing = new SpTriErasing(overlayManager, cameraManager, data, go, sp);

//            //// mp
//            //noMpPathDefined = new NoMpPathDefined(overlayManager, cameraManager, data, go, sp);
//            //minMpPathDefined = new MinMpPathDefined(overlayManager, cameraManager, data, go, sp);
//            //allMpPathDefined = new AllMpPathDefined(overlayManager, cameraManager, data, go, sp);
//            //mpComputingAmplitudes = new MpComputingAmplitudes(overlayManager, cameraManager, data, go, sp);
//            //mpAmplitudesComputed = new MpAmplitudesComputed(overlayManager, cameraManager, data, go, sp);
//            //mpROICreation = new MpROICreation(overlayManager, cameraManager, data, go, sp);

//            //// both
//            //errorMode = new ErrorMode(overlayManager, cameraManager, data, go, sp);

//            if (sp)
//                current = m_noSpPathDefined;
//            else
//                current = noMpPathDefined;

//            m_currentModeText.text = current.name;
//        }

//        public bool functionAccess(Mode.FunctionsId idFunction)
//        {
//            return current.functionsMask[(int)idFunction];
//        }

//        public string currentModeName()
//        {
//            return current.name;
//        }

//        public void updateUI(bool forceUIUpdate)
//        {
//            if (!uiUpdated || forceUIUpdate)
//            {
//                current.applyUIMask();
//            }

//            uiUpdated = true;
//        }


//        public void updateMode(Mode.FunctionsId idLastFunction)
//        {
//            switch (idLastFunction)
//            {
//                case Mode.FunctionsId.resetGIIBrainSurfaceFile:
//                    setMode(current.mu_resetGIIBrainSurfaceFile());
//                    break;
//                case Mode.FunctionsId.resetNIIBrainVolumeFile:
//                    setMode(current.mu_resetNIIBrainVolumeFile());
//                    break;
//                case Mode.FunctionsId.resetElectrodesFile:
//                    setMode(current.mu_resetElectrodesFile());
//                    break;
//                case Mode.FunctionsId.pre_updateGenerators:
//                    setMode(current.mu_pre_updateGenerators());
//                    break;
//                case Mode.FunctionsId.post_updateGenerators:
//                    setMode(current.mu_post_updateGenerators());
//                    break;
//                case Mode.FunctionsId.addNewPlane:
//                    setMode(current.mu_addNewPlane());
//                    break;
//                case Mode.FunctionsId.updatePlane:
//                    setMode(current.mu_updatePlane());
//                    break;
//                case Mode.FunctionsId.removeLastPlane:
//                    setMode(current.mu_removeLastPlane());
//                    break;
//                case Mode.FunctionsId.setDisplayedMesh:
//                    setMode(current.mu_setDisplayedMesh());
//                    break;
//                case Mode.FunctionsId.setTimelines:
//                    setMode(current.mu_setTimelines());
//                    break;
//                case Mode.FunctionsId.enableTriErasingMode:
//                    setMode(current.mu_enableTriErasingMode());
//                    break;
//                case Mode.FunctionsId.disableTriErasingMode:
//                    setMode(current.mu_disableTriErasingMode());
//                    break;
//                case Mode.FunctionsId.enableROICreationMode:
//                    setMode(current.mu_enableROICreationMode());
//                    break;
//                case Mode.FunctionsId.disableROICreationMode:
//                    setMode(current.mu_disableROICreationMode());
//                    break;
//            }
//        }


//        private void setMode(Mode.ModesId idMode)
//        {
//            switch (idMode)
//            {
//                case Mode.ModesId.NoSpPathDefined:
//                    current = m_noSpPathDefined;
//                    break;
//                case Mode.ModesId.MinSpPathDefined:
//                    current = minSpPathDefined;
//                    break;
//                case Mode.ModesId.AllSpPathDefined:
//                    current = allSpPathDefined;
//                    break;
//                case Mode.ModesId.SpComputingAmplitudes:
//                    current = spComputingAmplitudes;
//                    break;
//                case Mode.ModesId.SpAmplitudesComputed:
//                    current = spAmplitudesComputed;
//                    break;
//                case Mode.ModesId.NoMpPathDefined:
//                    current = noMpPathDefined;
//                    break;
//                case Mode.ModesId.MinMpPathDefined:
//                    current = minMpPathDefined;
//                    break;
//                case Mode.ModesId.AllMpPathDefined:
//                    current = allMpPathDefined;
//                    break;
//                case Mode.ModesId.MpComputingAmplitudes:
//                    current = mpComputingAmplitudes;
//                    break;
//                case Mode.ModesId.MpAmplitudesComputed:
//                    current = mpAmplitudesComputed;
//                    break;
//                case Mode.ModesId.ErrorMode:
//                    current = errorMode;
//                    break;
//                case Mode.ModesId.SpTriErasing:
//                    current = spTriErasing;
//                    break;
//                case Mode.ModesId.MpROICreation:
//                    current = mpROICreation;
//                    break;
//                default:
//                    break;
//            }

//            m_currentModeText.text = current.name;
//            uiUpdated = false;
//        }


//    }


//}