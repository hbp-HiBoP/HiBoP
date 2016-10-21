using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace HBP.VISU3D.DLL
{
    public class DLLDebugManager : MonoBehaviour
    {
        #region DLLimport

        [DllImport("hbp_export", EntryPoint = "checkError_DLLDebugManagerContainer", CallingConvention = CallingConvention.Cdecl)]
        static private extern bool checkError_DLLDebugManagerContainer();

        [DllImport("hbp_export", EntryPoint = "clean_DLLDebugManagerContainer", CallingConvention = CallingConvention.Cdecl)]
        static private extern void clean_DLLDebugManagerContainer();

        [DllImport("hbp_export", EntryPoint = "reset_DLLDebugManagerContainer", CallingConvention = CallingConvention.Cdecl)]
        static private extern void reset_DLLDebugManagerContainer(bool enable, bool useLog, bool unityEditor);

        #endregion DLLimport

        #region members

        static System.Threading.Thread mainThread;
        static bool pauseEditor = false;
        static bool leaveProgram = false;
        static bool delayedLeaveProgram = false;

        public bool retrieveDLLOutput = true;
        public bool writeOutputInLogFile = true;

        #endregion members;

        #region mono_behaviour

        void Awake()
        {
            mainThread = System.Threading.Thread.CurrentThread;
            clean_DLLDebugManagerContainer();
            reset(retrieveDLLOutput, writeOutputInLogFile);
        }

        void Update()
        {
            pauseEditor = false;
            if(delayedLeaveProgram)
            {
                bool editor = false;
                #if UNITY_EDITOR
                    editor = true;
                    UnityEditor.EditorApplication.isPlaying = false;
                #endif
                if (!editor)
                    Application.Quit();

                return;
            }
        }

        #endregion mono_behaviour

        #region function

        static public void checkError()
        {
            // TODO retrieve error string from DLL

            bool isError = checkError_DLLDebugManagerContainer();
            if(!isError)
                return;

            if(pauseEditor || leaveProgram)
            {
                Debug.LogError("Another DLL error occured before the end of frame, it will be ignored. ");
                return;
            }

            if(!mainThread.Equals(System.Threading.Thread.CurrentThread))
            {
                delayedLeaveProgram = true;
                Debug.LogError("A DLL error not from the main thread has been detected, the program will be aborted at the end of the current frame.");
                return;
            }

            //{ Nothing = 0, Abort = 1, Ignore = 2, Pause_Editor = 3};
            //int action = QtGUI.Instance.getActionToDoAfterError();
            int action = DLL.QtGUI.getActionToDoAfterError();
            switch (action)
            {
                case 1: // abort
                    leaveProgram = true;
                    Debug.LogError("An error has occured in the DLL and the program will be aborted at the end of the current frame.");
                    bool editor = false;
                    #if UNITY_EDITOR
                        editor = true;
                        UnityEditor.EditorApplication.isPlaying = false;
                    #endif
                    if (!editor)
                        Application.Quit();
                    break;
                case 2: // Ignore
                    Debug.LogError("An error has occured in the DLL but was ignored !!!");
                    break;
                case 3: // Pause_Editor
                    pauseEditor = true;
                    Debug.LogError("An error has occured in the DLL and the editor will be paused at the end of frame.");
                    #if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPaused = true;
                    #endif
                    break;
                case 4: // Delayed
                    Debug.LogError("An error has occured in the DLL but not in the main thread, the error will be delayed. ");
                    break;
                default:
                    Debug.LogError("Code from the DLL not managed !");
                    break;
            }
        }

        static public void reset(bool enable, bool useLog)
        {
            bool unityEditor = false;
            #if UNITY_EDITOR
                        unityEditor = true;
            #endif

            reset_DLLDebugManagerContainer(enable, useLog, unityEditor);
        }

        static public void clean()
        {
            clean_DLLDebugManagerContainer();
        }

#endregion function
    }
}