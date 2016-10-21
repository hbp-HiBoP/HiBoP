//using UnityEngine;
//using System.Collections;


//namespace HBP.VISU3D
//{
//    // mp
//    public class NoMpPathDefined : Mode
//    {
//        public NoMpPathDefined(UIOverlayManager overlayManager, UICameraManager cameraManager, DataS3DScene data, DisplayedObjects3DView go, bool sceneSp)
//            : base(overlayManager, cameraManager, data, go, sceneSp)
//        {
//            name = "NoMpPathDefined";
//            idMode = ModesId.NoMpPathDefined;

//            uiOverlayMask[(int)UIOverlayId.planes_controller] = false;
//            uiOverlayMask[(int)UIOverlayId.timeline_controller] = false;
//            uiOverlayMask[(int)UIOverlayId.icones_controller] = false;
//            uiOverlayMask[(int)UIOverlayId.cut_display_controller] = false;

//            uiOverlayState[(int)UIOverlayId.planes_controller] = 0;
//            uiOverlayState[(int)UIOverlayId.timeline_controller] = (int)TimelineController.State.amplitudesNotComputed;
//            uiOverlayState[(int)UIOverlayId.icones_controller] = 0;
//            uiOverlayState[(int)UIOverlayId.cut_display_controller] = 1;

//            uiCameraStateMp.Add((int)idMode); // 0 : right scene buttons controller

//            functionsMask[(int)FunctionsId.resetGIIBrainSurfaceFile] = true;
//            functionsMask[(int)FunctionsId.resetNIIBrainVolumeFile] = true;
//            functionsMask[(int)FunctionsId.resetElectrodesFile] = true;

//            functionsMask[(int)FunctionsId.addNewPlane] = true;
//            functionsMask[(int)FunctionsId.updatePlane] = true;
//            functionsMask[(int)FunctionsId.setTimelines] = true;
//        }
//    }

//    public class MinMpPathDefined : Mode
//    {
//        public MinMpPathDefined(UIOverlayManager overlayManager, UICameraManager cameraManager, DataS3DScene data, DisplayedObjects3DView go, bool sceneSp)
//            : base(overlayManager, cameraManager, data, go, sceneSp)
//        {
//            name = "MinMpPathDefined";
//            idMode = ModesId.MinMpPathDefined;

//            uiOverlayMask[(int)UIOverlayId.planes_controller] = true;
//            uiOverlayMask[(int)UIOverlayId.timeline_controller] = false;
//            uiOverlayMask[(int)UIOverlayId.icones_controller] = false;
//            uiOverlayMask[(int)UIOverlayId.cut_display_controller] = true;

//            uiOverlayState[(int)UIOverlayId.planes_controller] = 0;
//            uiOverlayState[(int)UIOverlayId.timeline_controller] = (int)TimelineController.State.amplitudesNotComputed;
//            uiOverlayState[(int)UIOverlayId.icones_controller] = 0;
//            uiOverlayState[(int)UIOverlayId.cut_display_controller] = 1;

//            uiCameraStateMp.Add((int)idMode); // 0 : right scene buttons controller

//            functionsMask[(int)FunctionsId.resetGIIBrainSurfaceFile] = true;
//            functionsMask[(int)FunctionsId.resetNIIBrainVolumeFile] = true;
//            functionsMask[(int)FunctionsId.resetElectrodesFile] = true;
//            functionsMask[(int)FunctionsId.addNewPlane] = true;
//            functionsMask[(int)FunctionsId.removeLastPlane] = true;
//            functionsMask[(int)FunctionsId.updatePlane] = true;
//            functionsMask[(int)FunctionsId.setDisplayedMesh] = true;
//            functionsMask[(int)FunctionsId.setTimelines] = true;
//        }
//    }

//    public class AllMpPathDefined : Mode
//    {
//        public AllMpPathDefined(UIOverlayManager overlayManager, UICameraManager cameraManager, DataS3DScene data, DisplayedObjects3DView go, bool sceneSp)
//            : base(overlayManager, cameraManager, data, go, sceneSp)
//        {
//            name = "AllMpPathDefined";
//            idMode = ModesId.AllMpPathDefined;

//            uiOverlayMask[(int)UIOverlayId.planes_controller] = true;
//            uiOverlayMask[(int)UIOverlayId.timeline_controller] = true;
//            uiOverlayMask[(int)UIOverlayId.icones_controller] = false;
//            uiOverlayMask[(int)UIOverlayId.cut_display_controller] = true;

//            uiOverlayState[(int)UIOverlayId.planes_controller] = 0;
//            uiOverlayState[(int)UIOverlayId.timeline_controller] = (int)TimelineController.State.amplitudesNotComputed;
//            uiOverlayState[(int)UIOverlayId.icones_controller] = 0;
//            uiOverlayState[(int)UIOverlayId.cut_display_controller] = 1;

//            uiCameraStateMp.Add((int)idMode); // 0 : right scene buttons controller

//            functionsMask[(int)FunctionsId.resetGIIBrainSurfaceFile] = true;
//            functionsMask[(int)FunctionsId.resetNIIBrainVolumeFile] = true;
//            functionsMask[(int)FunctionsId.resetElectrodesFile] = true;
//            functionsMask[(int)FunctionsId.pre_updateGenerators] = true;
//            functionsMask[(int)FunctionsId.addNewPlane] = true;
//            functionsMask[(int)FunctionsId.removeLastPlane] = true;
//            functionsMask[(int)FunctionsId.updatePlane] = true;
//            functionsMask[(int)FunctionsId.setDisplayedMesh] = true;
//            functionsMask[(int)FunctionsId.setTimelines] = true;
//        }
//    }


//    public class MpComputingAmplitudes : Mode
//    {
//        public MpComputingAmplitudes(UIOverlayManager overlayManager, UICameraManager cameraManager, DataS3DScene data, DisplayedObjects3DView go, bool sceneSp)
//            : base(overlayManager, cameraManager, data, go, sceneSp)
//        {
//            name = "MpComputingAmplitudes";
//            idMode = ModesId.MpComputingAmplitudes;

//            uiOverlayMask[(int)UIOverlayId.planes_controller] = false;
//            uiOverlayMask[(int)UIOverlayId.timeline_controller] = true;
//            uiOverlayMask[(int)UIOverlayId.icones_controller] = false;
//            uiOverlayMask[(int)UIOverlayId.cut_display_controller] = true;

//            uiOverlayState[(int)UIOverlayId.planes_controller] = 0;
//            uiOverlayState[(int)UIOverlayId.timeline_controller] = (int)TimelineController.State.isComputingAmplitudes;
//            uiOverlayState[(int)UIOverlayId.icones_controller] = 0;
//            uiOverlayState[(int)UIOverlayId.cut_display_controller] = 1;

//            uiCameraStateMp.Add((int)idMode); // 0 : right scene buttons controller

//            functionsMask[(int)FunctionsId.post_updateGenerators] = true;
//        }
//    }

//    public class MpAmplitudesComputed : Mode
//    {
//        public MpAmplitudesComputed(UIOverlayManager overlayManager, UICameraManager cameraManager, DataS3DScene data, DisplayedObjects3DView go, bool sceneSp)
//            : base(overlayManager, cameraManager, data, go, sceneSp)
//        {
//            name = "MpAmplitudesComputed";
//            idMode = ModesId.MpAmplitudesComputed;

//            uiOverlayMask[(int)UIOverlayId.planes_controller] = true;
//            uiOverlayMask[(int)UIOverlayId.timeline_controller] = true;
//            uiOverlayMask[(int)UIOverlayId.icones_controller] = true;
//            uiOverlayMask[(int)UIOverlayId.cut_display_controller] = true;

//            uiOverlayState[(int)UIOverlayId.planes_controller] = 0;
//            uiOverlayState[(int)UIOverlayId.timeline_controller] = (int)TimelineController.State.amplitudesComputed;
//            uiOverlayState[(int)UIOverlayId.icones_controller] = 0;
//            uiOverlayState[(int)UIOverlayId.cut_display_controller] = 1;

//            uiCameraStateMp.Add((int)idMode); // 0 : right scene buttons controller

//            functionsMask[(int)FunctionsId.resetGIIBrainSurfaceFile] = true;
//            functionsMask[(int)FunctionsId.resetNIIBrainVolumeFile] = true;
//            functionsMask[(int)FunctionsId.resetElectrodesFile] = true;
//            functionsMask[(int)FunctionsId.addNewPlane] = true;
//            functionsMask[(int)FunctionsId.removeLastPlane] = true;
//            functionsMask[(int)FunctionsId.updatePlane] = true;
//            functionsMask[(int)FunctionsId.setDisplayedMesh] = true;
//            functionsMask[(int)FunctionsId.setTimelines] = true;
//            functionsMask[(int)FunctionsId.enableROICreationMode] = true;
//        }
//    }

//    public class MpROICreation : Mode
//    {
//        public MpROICreation(UIOverlayManager overlayManager, UICameraManager cameraManager, DataS3DScene data, DisplayedObjects3DView go, bool sceneSp)
//            : base(overlayManager, cameraManager, data, go, sceneSp)
//        {
//            name = "MpROICreation";
//            idMode = ModesId.MpROICreation;

//            uiOverlayMask[(int)UIOverlayId.planes_controller] = true;
//            uiOverlayMask[(int)UIOverlayId.timeline_controller] = true;
//            uiOverlayMask[(int)UIOverlayId.icones_controller] = false;
//            uiOverlayMask[(int)UIOverlayId.cut_display_controller] = true;

//            uiOverlayState[(int)UIOverlayId.planes_controller] = 0;

//            uiOverlayState[(int)UIOverlayId.timeline_controller] = (int)TimelineController.State.ROIcreation;
//            uiOverlayState[(int)UIOverlayId.icones_controller] = 0;
//            uiOverlayState[(int)UIOverlayId.cut_display_controller] = 1;

//            uiCameraStateMp.Add((int)idMode); // 0 : right scene buttons controller

//            functionsMask[(int)FunctionsId.resetGIIBrainSurfaceFile] = true;
//            functionsMask[(int)FunctionsId.resetNIIBrainVolumeFile] = true;
//            functionsMask[(int)FunctionsId.resetElectrodesFile] = true;
//            functionsMask[(int)FunctionsId.addNewPlane] = true;
//            functionsMask[(int)FunctionsId.removeLastPlane] = true;
//            functionsMask[(int)FunctionsId.updatePlane] = true;
//            functionsMask[(int)FunctionsId.setDisplayedMesh] = true;
//            functionsMask[(int)FunctionsId.setTimelines] = true;
//            functionsMask[(int)FunctionsId.disableROICreationMode] = true;
//            functionsMask[(int)FunctionsId.pre_updateGenerators] = true;
//        }
//    }

//}