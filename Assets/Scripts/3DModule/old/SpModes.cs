//using UnityEngine;

//using System.Collections;

//namespace HBP.VISU3D
//{
//    [System.Serializable]
//    public class NoSpPathDefined : Mode
//    {
//        public NoSpPathDefined(UIOverlayManager overlayManager, UICameraManager cameraManager, DataS3DScene data, DisplayedObjects3DView go, bool sceneSp)
//            : base(overlayManager, cameraManager, data, go, sceneSp)
//        {
//            name = "NoSpPathDefined";
//            idMode = ModesId.NoSpPathDefined;

//            uiCameraStateSp.Add((int)idMode); // 0 : right scene buttons controller

//            uiOverlayMask[(int)UIOverlayId.planes_controller] = false;
//            uiOverlayMask[(int)UIOverlayId.timeline_controller] = false;
//            uiOverlayMask[(int)UIOverlayId.icones_controller] = false;
//            uiOverlayMask[(int)UIOverlayId.cut_display_controller] = false;

//            uiOverlayState[(int)UIOverlayId.planes_controller] = 0;
//            uiOverlayState[(int)UIOverlayId.timeline_controller] = (int)TimelineController.State.amplitudesNotComputed;
//            uiOverlayState[(int)UIOverlayId.icones_controller] = 0;
//            uiOverlayState[(int)UIOverlayId.cut_display_controller] = 0;

//            functionsMask[(int)FunctionsId.resetGIIBrainSurfaceFile] = true;
//            functionsMask[(int)FunctionsId.resetNIIBrainVolumeFile] = true;
//            functionsMask[(int)FunctionsId.resetElectrodesFile] = true;
//            functionsMask[(int)FunctionsId.addNewPlane] = true;
//            functionsMask[(int)FunctionsId.updatePlane] = true;
//            functionsMask[(int)FunctionsId.setTimelines] = true;

//            bool spScene = true;
//            bool showMeshes = true;
//            bool showPlots = true;
//            bool showROI = false;
//            bool showInactive = false;
//            layerMask = Cam.TrackBallCamera.createCullingMask(spScene, showMeshes, showPlots, showROI, showInactive); // TODO : ...
//        }
//    }
//    public class MinSpPathDefined : Mode
//    {
//        public MinSpPathDefined(UIOverlayManager overlayManager, UICameraManager cameraManager, DataS3DScene data, DisplayedObjects3DView go, bool sceneSp)
//            : base(overlayManager, cameraManager, data, go, sceneSp)
//        {
//            name = "MinSpPathDefined";
//            idMode = ModesId.MinSpPathDefined;

//            uiOverlayMask[(int)UIOverlayId.planes_controller] = true;
//            uiOverlayMask[(int)UIOverlayId.timeline_controller] = false;
//            uiOverlayMask[(int)UIOverlayId.icones_controller] = false;
//            uiOverlayMask[(int)UIOverlayId.cut_display_controller] = true;

//            uiOverlayState[(int)UIOverlayId.planes_controller] = 0;
//            uiOverlayState[(int)UIOverlayId.timeline_controller] = (int)TimelineController.State.amplitudesNotComputed;
//            uiOverlayState[(int)UIOverlayId.icones_controller] = 0;
//            uiOverlayState[(int)UIOverlayId.cut_display_controller] = 0;

//            uiCameraStateSp.Add((int)idMode); // 0 : right scene buttons controller

//            functionsMask[(int)FunctionsId.resetGIIBrainSurfaceFile] = true;
//            functionsMask[(int)FunctionsId.resetNIIBrainVolumeFile] = true;
//            functionsMask[(int)FunctionsId.resetElectrodesFile] = true;
//            functionsMask[(int)FunctionsId.addNewPlane] = true;
//            functionsMask[(int)FunctionsId.removeLastPlane] = true;
//            functionsMask[(int)FunctionsId.updatePlane] = true;
//            functionsMask[(int)FunctionsId.setTimelines] = true;
//            functionsMask[(int)FunctionsId.enableTriErasingMode] = true;
//        }
//    }

//    public class AllSpPathDefined : Mode
//    {
//        public AllSpPathDefined(UIOverlayManager overlayManager, UICameraManager cameraManager, DataS3DScene data, DisplayedObjects3DView go, bool sceneSp)
//            : base(overlayManager, cameraManager, data, go, sceneSp)
//        {
//            name = "AllSpPathDefined";
//            idMode = ModesId.AllSpPathDefined;

//            uiOverlayMask[(int)UIOverlayId.planes_controller] = true;
//            uiOverlayMask[(int)UIOverlayId.timeline_controller] = true;
//            uiOverlayMask[(int)UIOverlayId.icones_controller] = false;
//            uiOverlayMask[(int)UIOverlayId.cut_display_controller] = true;

//            uiOverlayState[(int)UIOverlayId.planes_controller] = 0;
//            uiOverlayState[(int)UIOverlayId.timeline_controller] = (int)TimelineController.State.amplitudesNotComputed;
//            uiOverlayState[(int)UIOverlayId.icones_controller] = 0;
//            uiOverlayState[(int)UIOverlayId.cut_display_controller] = 0;

//            uiCameraStateSp.Add((int)idMode); // 0 : right scene buttons controller

//            functionsMask[(int)FunctionsId.resetGIIBrainSurfaceFile] = true;
//            functionsMask[(int)FunctionsId.resetNIIBrainVolumeFile] = true;
//            functionsMask[(int)FunctionsId.resetElectrodesFile] = true;
//            functionsMask[(int)FunctionsId.pre_updateGenerators] = true;
//            functionsMask[(int)FunctionsId.addNewPlane] = true;
//            functionsMask[(int)FunctionsId.removeLastPlane] = true;
//            functionsMask[(int)FunctionsId.updatePlane] = true;
//            functionsMask[(int)FunctionsId.setTimelines] = true;
//            functionsMask[(int)FunctionsId.enableTriErasingMode] = true;
//        }
//    }

//    public class SpComputingAmplitudes : Mode
//    {
//        public SpComputingAmplitudes(UIOverlayManager overlayManager, UICameraManager cameraManager, DataS3DScene data, DisplayedObjects3DView go, bool sceneSp)
//            : base(overlayManager, cameraManager, data, go, sceneSp)
//        {
//            name = "SpComputingAmplitudes";
//            idMode = ModesId.SpComputingAmplitudes;

//            uiOverlayMask[(int)UIOverlayId.planes_controller] = false;
//            uiOverlayMask[(int)UIOverlayId.timeline_controller] = true;
//            uiOverlayMask[(int)UIOverlayId.icones_controller] = false;
//            uiOverlayMask[(int)UIOverlayId.cut_display_controller] = true;

//            uiOverlayState[(int)UIOverlayId.planes_controller] = 0;
//            uiOverlayState[(int)UIOverlayId.timeline_controller] = (int)TimelineController.State.isComputingAmplitudes;
//            uiOverlayState[(int)UIOverlayId.icones_controller] = 0;
//            uiOverlayState[(int)UIOverlayId.cut_display_controller] = 0;

//            uiCameraStateSp.Add((int)idMode); // 0 : right scene buttons controller

//            functionsMask[(int)FunctionsId.post_updateGenerators] = true;
//        }
//    }


//    public class SpAmplitudesComputed : Mode
//    {
//        public SpAmplitudesComputed(UIOverlayManager overlayManager, UICameraManager cameraManager, DataS3DScene data, DisplayedObjects3DView go, bool sceneSp)
//            : base(overlayManager, cameraManager, data, go, sceneSp)
//        {
//            name = "SpAmplitudesComputed";
//            idMode = ModesId.SpAmplitudesComputed;

//            uiOverlayMask[(int)UIOverlayId.planes_controller] = true;
//            uiOverlayMask[(int)UIOverlayId.timeline_controller] = true;
//            uiOverlayMask[(int)UIOverlayId.icones_controller] = true;
//            uiOverlayMask[(int)UIOverlayId.cut_display_controller] = true;

//            uiOverlayState[(int)UIOverlayId.planes_controller] = 0;
//            uiOverlayState[(int)UIOverlayId.timeline_controller] = (int)TimelineController.State.amplitudesComputed;
//            uiOverlayState[(int)UIOverlayId.icones_controller] = 0;
//            uiOverlayState[(int)UIOverlayId.cut_display_controller] = 0;

//            uiCameraStateSp.Add((int)idMode); // 0 : right scene buttons controller

//            functionsMask[(int)FunctionsId.resetGIIBrainSurfaceFile] = true;
//            functionsMask[(int)FunctionsId.resetNIIBrainVolumeFile] = true;
//            functionsMask[(int)FunctionsId.resetElectrodesFile] = true;
//            functionsMask[(int)FunctionsId.addNewPlane] = true;
//            functionsMask[(int)FunctionsId.removeLastPlane] = true;
//            functionsMask[(int)FunctionsId.updatePlane] = true;
//            functionsMask[(int)FunctionsId.setTimelines] = true;
//            functionsMask[(int)FunctionsId.enableTriErasingMode] = true;
//        }
//    }

//    public class SpTriErasing : Mode
//    {
//        public SpTriErasing(UIOverlayManager overlayManager, UICameraManager cameraManager, DataS3DScene data, DisplayedObjects3DView go, bool sceneSp)
//            : base(overlayManager, cameraManager, data, go, sceneSp)
//        {
//            name = "SpTriErasing";
//            idMode = ModesId.SpTriErasing;

//            uiOverlayMask[(int)UIOverlayId.planes_controller] = false;
//            uiOverlayMask[(int)UIOverlayId.timeline_controller] = false;
//            uiOverlayMask[(int)UIOverlayId.icones_controller] = false;
//            uiOverlayMask[(int)UIOverlayId.cut_display_controller] = false;

//            uiOverlayState[(int)UIOverlayId.planes_controller] = 0;
//            uiOverlayState[(int)UIOverlayId.timeline_controller] = (int)TimelineController.State.amplitudesNotComputed;
//            uiOverlayState[(int)UIOverlayId.icones_controller] = 0;
//            uiOverlayState[(int)UIOverlayId.cut_display_controller] = 0;

//            uiCameraStateSp.Add((int)idMode); // 0 : right scene buttons controller

//            functionsMask[(int)FunctionsId.disableTriErasingMode] = true;
//            functionsMask[(int)FunctionsId.resetGIIBrainSurfaceFile] = true;
//            functionsMask[(int)FunctionsId.resetNIIBrainVolumeFile] = true;
//            functionsMask[(int)FunctionsId.resetElectrodesFile] = true;
//        }
//    }
//}