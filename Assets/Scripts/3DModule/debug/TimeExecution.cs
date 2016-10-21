
/**
 * \file    TimeExecution.cs
 * \author  Lance Florian
 * \date    20/05/2016
 * \brief   Define TimeExecution
 */

// system
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

// unity
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace HBP.VISU3D
{
    /// <summary>
    /// 
    /// </summary>
    public class TimeExecution : MonoBehaviour
    {
        #region members          

        public enum ScriptsId : int
        {
            CamerasManager, Column3DView, Column3DViewEEG, Column3DViewIRMF, Column3DViewManager, ModesManager, Base3DScene, MP3DScene, ScenesManager, SP3DScene,
            UICameraManager, UIOverlayManager, UIManager, BaseGameObjects, MNIObjects, SharedMeshes, SharedMaterials
        }; /**< scripts id */

        // ...

        static int m_id = 0;

        static List<double> times = new List<double>();

        static private double m_startProgramTime;

        static DateTime epochStart;

        #endregion members

        #region mono_behaviour

        /// <summary>
        /// This function is always called before any Start functions and also just after a prefab is instantiated. (If a GameObject is inactive during start up Awake is not called until it is made active.)
        /// </summary>
        void Awake()
        {
            epochStart = new System.DateTime(1970, 1, 1, 8, 0, 0, System.DateTimeKind.Utc);
            m_startProgramTime = getWorldTime();
            // ...
        }

        /// <summary>
        /// Start is called before the first frame update only if the script instance is enabled.
        /// </summary>
        void Start()
        {
            // ...
        }

        /// <summary>
        /// Update is called once per frame. It is the main workhorse function for frame updates.
        /// </summary>
        void Update()
        {
            // ...
        }

        #endregion mono_behaviour   

        #region functions

        public static double getWorldTime()
        {
            return (System.DateTime.UtcNow - epochStart).TotalSeconds;
        }

        public static int getId() { return m_id++; }

        public static void startAwake(int id, ScriptsId scriptId)
        {
            if (id >= times.Count)
                times.Add(getWorldTime());
        }

        public static void endAwake(int id, ScriptsId scriptId, string GOName, int instanceId)
        {
            double currTime = getWorldTime();
            #if UNITY_EDITOR
                Debug.Log(Enum.GetNames(typeof(ScriptsId))[(int)scriptId] + " Awake -> id : " + id + "  Time : " + (currTime - times[id]).ToString("0.00") + "s total time : " + (currTime - m_startProgramTime).ToString("0.00") + "s " + GOName + " " + instanceId);
            #endif
        }

        public static void endAwake(int id, ScriptsId scriptId, GameObject obj)
        {
            endAwake(id, scriptId, obj.name, obj.GetInstanceID());            
        }

        #endregion functions

    }
}