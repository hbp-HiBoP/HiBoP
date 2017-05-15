
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

namespace HBP.Module3D
{
    /// <summary>
    /// A class for measuring Awake duration in mono behaviours scripts
    /// </summary>
    public class TimeExecution : MonoBehaviour
    {
        #region Properties
        public enum ScriptsId
        {
            CamerasManager, Column3DView, Column3DViewEEG, Column3DViewFMRI, Column3DViewManager, ModesManager, Base3DScene, MP3DScene, ScenesManager, SP3DScene,
            UICameraManager, UIOverlayManager, UIManager, MNIObjects, SharedMeshes, SharedMaterials
        };
        static int m_ID = 0;
        /// <summary>
        /// ID of the script
        /// </summary>
        public static int ID
        {
            get
            {
                return m_ID++;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        static List<double> m_Times = new List<double>();
        /// <summary>
        /// 
        /// </summary>
        static private double m_StartProgramTime;
        /// <summary>
        /// 
        /// </summary>
        static DateTime m_EpochStart;
        #endregion

        #region Private Methods
        void Awake()
        {
            m_EpochStart = new System.DateTime(1970, 1, 1, 8, 0, 0, System.DateTimeKind.Utc);
            m_StartProgramTime = GetWorldTime();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static double GetWorldTime()
        {
            return (System.DateTime.UtcNow - m_EpochStart).TotalSeconds;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="scriptId"></param>
        public static void StartAwake(int id, ScriptsId scriptId)
        {
            if (id >= m_Times.Count)
                m_Times.Add(GetWorldTime());
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="scriptId"></param>
        /// <param name="GOName"></param>
        /// <param name="instanceId"></param>
        public static void EndAwake(int id, ScriptsId scriptId, string GOName, int instanceId)
        {
#if UNITY_EDITOR
            double currTime = GetWorldTime();
            Debug.Log(Enum.GetNames(typeof(ScriptsId))[(int)scriptId] + " Awake -> id : " + id + "  Time : " + (currTime - m_Times[id]).ToString("0.00") + "s total time : " + (currTime - m_StartProgramTime).ToString("0.00") + "s " + GOName + " " + instanceId);
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="scriptId"></param>
        /// <param name="obj"></param>
        public static void EndAwake(int id, ScriptsId scriptId, GameObject obj)
        {
            EndAwake(id, scriptId, obj.name, obj.GetInstanceID());            
        }
        #endregion

    }
}