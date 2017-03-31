
/**
 * \file    DLLDebugManager.cs
 * \author  Lance Florian
 * \date    2016
 * \brief   Define DLLDebugManager class 
 */

// system
using System.Text;
using System.Runtime.InteropServices;

// unity
using UnityEngine;

namespace HBP.VISU3D.DLL
{
    /// <summary>
    /// A class for managing the debugging  of the DLL
    /// </summary>
    public class DLLDebugManager : MonoBehaviour
    {               
        #region members

        static System.Threading.Thread mainThread; /**< main thread */
        static bool pauseEditor = false; /**< if true, pause the unity editor  */
        static bool leaveProgram = false; /**< if true, leave the program */
        static bool delayedLeaveProgram = false; /**< if true, leave the program at the next frame */

        public bool retrieveDLLOutput = true;   /**< retrieve the output of the DLL */
        public bool writeOutputInLogFile = true; /**< write the log in a file */

        #endregion members;

        #region mono_behaviour

        void Awake()
        {
            mainThread = System.Threading.Thread.CurrentThread;
            clean();
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

        /// <summary>
        /// Check if an error has occured in the DLL
        /// </summary>
        public void check_error()
        {
            bool isError = checkError_DLLDebugManagerContainer() == 1;
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
            string errorMsg = retrieve_error_message();
            int action = VISU3D.DLL.QtGUI.get_action_to_do_from_error_dialog_test(errorMsg, Application.isEditor);
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

        /// <summary>
        /// Reset the manager
        /// </summary>
        /// <param name="enable"></param>
        /// <param name="useLog"></param>
        public void reset(bool enable, bool useLog)
        {
            bool unityEditor = false;
            #if UNITY_EDITOR
                        unityEditor = true;
            #endif

            reset_DLLDebugManagerContainer(enable ? 1 : 0, useLog ? 1 : 0, unityEditor ? 1 : 0);
        }

        /// <summary>
        /// Return the last error message
        /// </summary>
        /// <returns></returns>
        public string retrieve_error_message()
        {
            int length = 200;
            StringBuilder str = new StringBuilder();
            str.Append('?', length);
            retrieve_error_message_DLLDebugManagerContainer(str, length);

            return str.ToString().Replace("?", string.Empty);
        }

        /// <summary>
        /// Clean the manager
        /// </summary>
        public void clean()
        {
            clean_DLLDebugManagerContainer();
        }

        #endregion function

        #region DLLimport

        [DllImport("hbp_export", EntryPoint = "checkError_DLLDebugManagerContainer", CallingConvention = CallingConvention.Cdecl)]
        static private extern int checkError_DLLDebugManagerContainer();

        [DllImport("hbp_export", EntryPoint = "clean_DLLDebugManagerContainer", CallingConvention = CallingConvention.Cdecl)]
        static private extern void clean_DLLDebugManagerContainer();

        [DllImport("hbp_export", EntryPoint = "reset_DLLDebugManagerContainer", CallingConvention = CallingConvention.Cdecl)]
        static private extern void reset_DLLDebugManagerContainer(int enable, int useLog, int unityEditor);

        [DllImport("hbp_export", EntryPoint = "retrieve_error_message_DLLDebugManagerContainer", CallingConvention = CallingConvention.Cdecl)]
        static private extern void retrieve_error_message_DLLDebugManagerContainer(StringBuilder errorMessage, int length);

        #endregion DLLimport
    }
}