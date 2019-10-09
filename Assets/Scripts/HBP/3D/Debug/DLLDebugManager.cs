using System.Runtime.InteropServices;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace HBP.Module3D.DLL
{
    /// <summary>
    /// A class for managing the debugging  of the DLL
    /// </summary>
    public class DLLDebugManager : MonoBehaviour
    {

        #region Internal Classes
        public class DLLObject
        {
            public string Type;
            public string StackTrace;
            public Guid ID;
            public CleanedBy CleanedBy;
        }
        #endregion

        #region Properties
        public bool retrieveDLLOutput = true;
        public bool writeOutputInLogFile = true;
        public bool GetInformationAboutDLLObjects = true;

        public enum CleanedBy { NotCleaned, GC, Dispose }
        public List<DLLObject> DLLObjects = new List<DLLObject>();

        private LoggerDelegate m_LogCallbackDelegate;
        private IntPtr m_LogCallbackIntPtr;
        #endregion;

        #region Private Methods
        void Awake()
        {
            if (retrieveDLLOutput)
            {
                m_LogCallbackDelegate = new LoggerDelegate(LogCallback);
                m_LogCallbackIntPtr = Marshal.GetFunctionPointerForDelegate(m_LogCallbackDelegate);
                set_debug_callback_Logger(m_LogCallbackIntPtr);
            }
            if (writeOutputInLogFile)
            {

            }
        }
        private void LogCallback(string str, int type)
        {
            switch (type)
            {
                case 0:
                    Debug.Log(str);
                    return;
                case 1:
                    Debug.LogWarning(str);
                    return;
                case 2:
                    Debug.LogError(str);
                    return;
            }
        }
        #endregion

        #region Public Methods
        public void AddDLLObject(string typeString, Guid id)
        {
            if (GetInformationAboutDLLObjects)
            {
                DLLObjects.Add(new DLLObject()
                {
                    Type = typeString,
                    StackTrace = Environment.StackTrace,
                    ID = id,
                    CleanedBy = CleanedBy.NotCleaned
                });
            }
        }
        public void RemoveDLLOBject(string typeString, Guid id, CleanedBy cleanedBy)
        {
            if (GetInformationAboutDLLObjects)
            {
                var objectToRemove = DLLObjects.Find(d => d.Type == typeString && d.ID == id);
                if (objectToRemove != null) objectToRemove.CleanedBy = cleanedBy;
            }
        }

        #endregion

        #region DllImport
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LoggerDelegate(string str, int type);
        [DllImport("hbp_export", EntryPoint = "set_debug_callback_Logger", CallingConvention = CallingConvention.Cdecl)]
        static private extern void set_debug_callback_Logger(IntPtr logCallback);
        [DllImport("hbp_export", EntryPoint = "redirect_standard_output_to_file_Logger", CallingConvention = CallingConvention.Cdecl)]
        static private extern void redirect_standard_output_to_file_Logger(string pathToFile);
        #endregion
    }
}