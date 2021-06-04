using System.Runtime.InteropServices;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace HBP.Module3D.DLL
{
    /// <summary>
    /// A class for managing the debugging of the DLL
    /// </summary>
    public class DLLDebugManager : MonoBehaviour
    {

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

        /// <summary>
        /// Delegate for the log callback method
        /// </summary>
        private LoggerDelegate m_LogCallbackDelegate;
        /// <summary>
        /// Pointer to the log callback delegate
        /// </summary>
        private IntPtr m_LogCallbackIntPtr;
        #endregion;

        #region Private Methods
        private void Awake()
        {
            if (m_LogDLLToUnity)
            {
                m_LogCallbackDelegate = new LoggerDelegate(LogCallback);
                m_LogCallbackIntPtr = Marshal.GetFunctionPointerForDelegate(m_LogCallbackDelegate);
                set_debug_callback_Logger(m_LogCallbackIntPtr);
            }
            if (m_LogDLLToFile)
            {
                redirect_standard_output_to_file_Logger(string.Format("HiBoP_DLL_LOG_{0}_{1}_{2}__{3}_{4}_{5}.log", DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second));
            }
        }
        private void OnDestroy()
        {
            reset_Logger();
        }
        /// <summary>
        /// Log callback when calling the log method within the DLL
        /// </summary>
        /// <param name="str">String to be passed from the DLL to Unity</param>
        /// <param name="type">Type of the log (log, warning, error)</param>
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
        /// <summary>
        /// Method to be used to add a DLL object to the list
        /// </summary>
        /// <param name="typeString">Type of the object as a string</param>
        /// <param name="id">ID of the object</param>
        public void AddDLLObject(string typeString, Guid id)
        {
            if (m_GetInformationAboutDLLObjects)
            {
                if (typeString == "Tools.CSharp.EEG.Trigger") return;
                DLLObjects.Add(new DLLObject()
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
        public void RemoveDLLOBject(string typeString, Guid id, CleanedBy cleanedBy)
        {
            if (m_GetInformationAboutDLLObjects)
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
        [DllImport("hbp_export", EntryPoint = "reset_Logger", CallingConvention = CallingConvention.Cdecl)]
        static private extern void reset_Logger();
        #endregion
    }
}