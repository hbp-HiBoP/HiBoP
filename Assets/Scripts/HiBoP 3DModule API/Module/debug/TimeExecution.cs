
/**
 * \file    TimeExecution.cs
 * \author  Lance Florian
 * \date    20/05/2016
 * \brief   Define TimeExecution
 */

// system
using System;
using System.Collections.Generic;

// unity
using UnityEngine;

namespace HBP.VISU3D
{
    /// <summary>
    /// A class for measuring Awake duration in mono behaviours scripts
    /// </summary>
    public class TimeExecution : MonoBehaviour
    {
        #region members          

        public enum ScriptsId : int
        {
            CamerasManager, Column3DView, Column3DViewEEG, Column3DViewFMRI, Column3DViewManager, ModesManager, Base3DScene, MP3DScene, ScenesManager, SP3DScene,
            UICameraManager, UIOverlayManager, UIManager, MNIObjects, SharedMeshes, SharedMaterials
        }; /**< scripts id */

        static int m_id = 0; /**< id of the script */

        static List<double> times = new List<double>();

        static private double m_startProgramTime;

        static DateTime m_epochStart;

        #endregion members

        #region mono_behaviour

        void Awake()
        {
            m_epochStart = new System.DateTime(1970, 1, 1, 8, 0, 0, System.DateTimeKind.Utc);
            m_startProgramTime = get_world_time();
        }

        #endregion mono_behaviour   

        #region functions

        public static double get_world_time()
        {
            return (System.DateTime.UtcNow - m_epochStart).TotalSeconds;
        }

        public static int get_ID() { return m_id++; }

        public static void start_awake(int id, ScriptsId scriptId)
        {
            if (id >= times.Count)
                times.Add(get_world_time());
        }

        public static void end_awake(int id, ScriptsId scriptId, string GOName, int instanceId)
        {
#if UNITY_EDITOR
            double currTime = get_world_time();
            Debug.Log(Enum.GetNames(typeof(ScriptsId))[(int)scriptId] + " Awake -> id : " + id + "  Time : " + (currTime - times[id]).ToString("0.00") + "s total time : " + (currTime - m_startProgramTime).ToString("0.00") + "s " + GOName + " " + instanceId);
#endif
        }

        public static void end_awake(int id, ScriptsId scriptId, GameObject obj)
        {
            end_awake(id, scriptId, obj.name, obj.GetInstanceID());            
        }

        #endregion functions

    }
}