using System.Runtime.InteropServices;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using HBP.Core.Tools;
using HBP.Core.DLL.HbpCore;
using AOT;

namespace HBP.Core.DLL
{
    /// <summary>
    /// A class for managing the debugging of the DLL
    /// </summary>
    public class DLLDebugManager : Manager<DLLDebugManager>
    {
        private static readonly LoggerDelegate s_LogCallback = LogCallback;

        #region Internal Classes
        /// <summary>
        /// Class containing information about the instance of a object inheriting from <see cref="Tools.DLL.CppDLLImportBase"/>
        /// </summary>
        public class DLLObject
        {
            public string Type;
            public string StackTrace;
            public Guid ID;
            public CleanedBy CleanedBy;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Do we log all DLL messages to the Unity console ?
        /// </summary>
        [SerializeField] private bool m_LogDLLToUnity = true;
        /// <summary>
        /// Do we log all DLL messages to a file ?
        /// </summary>
        [SerializeField] private bool m_LogDLLToFile = true;
        /// <summary>
        /// Do we capture information about DLL objects
        /// </summary>
        [SerializeField] private bool m_GetInformationAboutDLLObjects = true;

        /// <summary>
        /// Enum used to know how a DLL object has been cleaned
        /// </summary>
        public enum CleanedBy { NotCleaned, GC, Dispose }
        /// <summary>
        /// List of all DLL objects created during this instance of the program
        /// </summary>
        public List<DLLObject> DLLObjects { get; private set; } = new List<DLLObject>();
        public static string CurrentLogFilePath { get; private set; } = string.Empty;
        #endregion;

        #region Private Methods
        protected override void Initialization()
        {
            base.Initialization();
            if (m_LogDLLToUnity)
            {
                TryAttachHbpCoreLogger(out _);
            }
            if (m_LogDLLToFile)
            {
                string logDirectory = Path.Combine(Application.persistentDataPath, "Logs");
                Directory.CreateDirectory(logDirectory);
                string logPath = Path.Combine(logDirectory, $"HiBoP_hbp_core_{DateTime.Now:yyyy_MM_dd__HH_mm_ss_fff}.log");
                if (HbpCoreRuntime.TrySetLogFile(logPath, out string error))
                {
                    CurrentLogFilePath = logPath;
                }
                else
                {
                    Debug.LogWarning($"Unable to initialize the hbp_core log file: {error}");
                }
            }
        }
        private void OnDestroy()
        {
            ResetNativeLoggers();
        }
        /// <summary>
        /// Log callback when calling the log method within the DLL
        /// </summary>
        /// <param name="str">String to be passed from the DLL to Unity</param>
        /// <param name="type">Type of the log (log, warning, error)</param>
        [MonoPInvokeCallback(typeof(LoggerDelegate))]
        private static void LogCallback([MarshalAs(UnmanagedType.LPUTF8Str)] string str, int type)
        {
            switch (type)
            {
                case 0: Debug.Log(str); return;
                case 1: Debug.LogWarning(str); return;
                case 2: Debug.LogError(str); return;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Method to be used to add a DLL object to the list
        /// </summary>
        /// <param name="typeString">Type of the object as a string</param>
        /// <param name="id">ID of the object</param>
        public static void AddDLLObject(string typeString, Guid id)
        {
            if (m_Instance == null) return;

            if (m_Instance.m_GetInformationAboutDLLObjects)
            {
                if (typeString == "Tools.CSharp.EEG.Trigger") return;
                m_Instance.DLLObjects.Add(new DLLObject()
                {
                    Type = typeString,
                    StackTrace = Environment.StackTrace,
                    ID = id,
                    CleanedBy = CleanedBy.NotCleaned
                });
            }
        }
        /// <summary>
        /// Remove a DLL object from the list
        /// </summary>
        /// <param name="typeString">Type of the object as a string</param>
        /// <param name="id">ID of the object</param>
        /// <param name="cleanedBy">How do we remove this object ?</param>
        public static void RemoveDLLOBject(string typeString, Guid id, CleanedBy cleanedBy)
        {
            if (m_Instance == null) return;

            if (m_Instance.m_GetInformationAboutDLLObjects)
            {
                var objectToRemove = m_Instance.DLLObjects.Find(d => d.Type == typeString && d.ID == id);
                if (objectToRemove != null) objectToRemove.CleanedBy = cleanedBy;
            }
        }

        public static bool TryAttachHbpCoreLogger(out string error)
        {
            return HbpCoreRuntime.TrySetDebugCallback(s_LogCallback, out error);
        }

        public static bool TryResetHbpCoreLogger(out string error)
        {
            return HbpCoreRuntime.TryResetDebugCallback(out error);
        }

        public static void ResetNativeLoggers()
        {
            TryResetHbpCoreLogger(out _);
            HbpCoreRuntime.TryResetLogFile(out _);
            CurrentLogFilePath = string.Empty;
        }

        #endregion

        #region DllImport
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LoggerDelegate([MarshalAs(UnmanagedType.LPUTF8Str)] string str, int type);
        #endregion
    }
}
