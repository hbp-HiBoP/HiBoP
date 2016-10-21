
// system
using System.Collections;
using System.Collections.Generic;

// unity
using UnityEngine;
using UnityEngine.UI;

// hbp
using HBP.VISU3D.Cam;


namespace HBP.VISU3D
{

    public class Listeners : MonoBehaviour
    {
        static public void addView(CamerasManager cameraManager, bool singlePatientScene)
        {
            cameraManager.addViewLineCameras(singlePatientScene);
        }

        static public void removeView(CamerasManager cameraManager, bool singlePatientScene)
        {
            cameraManager.removeViewLineCameras(singlePatientScene);
        }


        static public void computeInfluences(Base3DScene scene)
        {
            scene.updateGenerators();
        } 
    }
}