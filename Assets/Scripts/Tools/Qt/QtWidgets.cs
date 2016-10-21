
/**
 * \file    QtWidgets.cs
 * \author  Lance Florian
 * \date    09/02/2016
 * \brief   Define QtGUI class
 */

// system
using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// unity
using UnityEngine;

namespace HBP.VISU3D
{
    public class QtWidgets : MonoBehaviour
    {
        void Awake()
        {
            DLL.QtGUI.Instance.ToString(); // dummy call in order to force the creation of the class
            DLL.QtGUI.initPlotWidget();
        }

        void OnDestroy()
        {
            for (int ii = 0; ii < widgets.Count; ++ii)
            {
                widgets[ii].Dispose();
            }

            DLL.QtGUI.Instance.Dispose(); // TODO : singleton is not the best choice
        }

        static public List<DLL.CppDLLImportBase> widgets = new List<DLL.CppDLLImportBase>();
    }
}

namespace HBP.VISU3D.DLL
{
    /// <summary>
    /// A singleton interface class which crate a QApplication and allow to communicate with Qt gui functions like QFileDialog.
    /// 
    /// Examples :
    /// 
    /// string dirPath = qtGui.getExistingDirectory("Select a directory ");
    /// string filePath = qtGui.getOpenFileName(new string[] { "txt", "exe", "png" }, "Select a file", "");
    /// string[] filesPaths = qtGui.getOpenFilesName(new string[] { "txt", "exe", "png" }, "Select files", ""); 
    /// string saveFilePath = qtGui.getSaveFileName(new string[] { "txt", "exe", "png" }, "Save file to", "");
    ///
    /// </summary>
    public sealed class QtGUI : CppDLLImportBase
    {
        #region functions

        public static IntPtr plotWidget;

        public static void initPlotWidget()
        {

        }

        public static int getActionToDoAfterError()
        {
            return getActionToDoFromErrorDialog_QtApp();
        }

        /// <summary>
        /// Open a qt file dialog and return the path of an existing directory.
        /// </summary>
        /// <param name="message"> message to be displayed in top of the file dialog </param>
        /// <param name="defaultDir"> default directory of the file dialog </param>
        /// <returns> return an empty path if no directory has been choosen or if an error occurs </returns>
        public static string getExistingDirectory(string message = "Select a directory", string defaultDir = "")
        {
            int length = 1024;
            StringBuilder str = new StringBuilder("");
            str.Insert(0, "?", length);
            bool success = getExistingDirectory_QtApp(message, defaultDir, str, length);
            if (success)
                return str.ToString();

            return "";
        }

        /// <summary>
        /// Open a qt file dialog and return the path of a selected file.
        /// </summary>
        /// <param name="filtersArray"> extension filters of the files of be displayed in the file dialog (ex: filtersArray[0] = "txt", filtersArray[1] = "png" ...) </param>
        /// <param name="message">  message to be displayed in top of the file dialog  </param>
        /// <param name="defaultDir"> default directory of the file dialog </param>
        /// <returns> return an empty path if no file has been choosen or if an error occurs </returns>
        public static string getOpenFileName(string[] filtersArray = null, string message = "Select a file", string defaultDir = "")
        {
            filtersArray = filtersArray ?? new string[] { "txt" };

            int bufferSize = 1024;
            StringBuilder str = new StringBuilder("");
            str.Insert(0, "?", bufferSize);

            string filters = "";
            for (int ii = 0; ii < filtersArray.Length - 1; ++ii)
            {
                filters += filtersArray[ii] + "*";

            }
            filters += filtersArray[filtersArray.Length - 1];

            bool success = getOpenFileName_QtApp(message, defaultDir, filters, str, bufferSize);

            if (success)
                return str.ToString();

            return "";
        }

        /// <summary>
        /// Open a qt file dialog and return the list of path of the selected files.
        /// </summary>
        /// <param name="filtersArray">  extension filters of the files of be displayed in the file dialog (ex: filtersArray[0] = "txt", filtersArray[1] = "png" ...) </param>
        /// <param name="message"> message to be displayed in top of the file dialog </param>
        /// <param name="defaultDir"> default directory of the file dialog </param>
        /// <returns> return an empty path if no file has been choosen or if an error occurs </returns>
        public static string[] getOpenFilesName(string[] filtersArray = null, string message = "Select files", string defaultDir = "")
        {
            filtersArray = filtersArray ?? new string[] { "txt" };

            int bufferSize = 10000;
            StringBuilder str = new StringBuilder("");
            str.Insert(0, "?", bufferSize);

            string filters = "";
            for (int ii = 0; ii < filtersArray.Length - 1; ++ii)
            {
                filters += filtersArray[ii] + "*";
            }
            filters += filtersArray[filtersArray.Length - 1];

            bool success = getOpenFilesName_QtApp(message, defaultDir, filters, str, bufferSize);

            if (success)
            {
                string filesDirRes = str.ToString();
                string[] splits = filesDirRes.Split(new char[] { '*' });
                return splits;
            }

            return new string[0];
        }

        /// <summary>
        /// Open a qt file dialog and return the path of a saved file
        /// </summary>
        /// <param name="filtersArray"> extension filters of the files of be displayed in the file dialog (ex: filtersArray[0] = "txt", filtersArray[1] = "png" ...) </param>
        /// <param name="message">  message to be displayed in top of the file dialog  </param>
        /// <param name="defaultDir"> default directory of the file dialog </param>
        /// <returns> return an empty path if no file has been choosen or if an error occurs </returns>
        public static string getSaveFileName(string[] filtersArray = null, string message = "Save to", string defaultDir = "")
        {
            filtersArray = filtersArray ?? new string[] { "txt" };

            int bufferSize = 1024;
            StringBuilder str = new StringBuilder("");
            str.Insert(0, "?", bufferSize);

            string filters = "";
            for (int ii = 0; ii < filtersArray.Length - 1; ++ii)
            {
                filters += filtersArray[ii] + "*";
            }
            filters += filtersArray[filtersArray.Length - 1];

            bool success = getSaveFileName_QtApp(message, defaultDir, filters, str, bufferSize);
            if (success)
                return str.ToString();
            return "";
        }


        #endregion functions

        #region memory_management

        /// <summary>
        /// Allocate ourselves.
        /// We have a private constructor, so no one else can.
        /// </summary>
        static readonly QtGUI _instance = new QtGUI();

        /// <summary>
        /// Access SiteStructure.Instance to get the singleton object.
        /// Then call methods on that instance.
        /// </summary>
        public static QtGUI Instance
        {
            get { return _instance; }
        }

        /// <summary>
        /// Private default constructor of QtGUi, singleton design
        /// </summary>
        private QtGUI() : base() {}

        /// <summary>
        /// Private constructor with pointer of QtGUi, singleton design
        /// </summary>
        /// <param name="ptr"></param>
        private QtGUI(IntPtr ptr) : base(ptr) {}

        /// <summary>
        /// Overrided dispose, this function is useless with singleton design
        /// </summary>
        //public override void Dispose(){}

        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void createDLLClass()
        {
            _handle = new HandleRef(this, create_QtApp());
        }

        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void deleteDLLClass()
        {   
            delete_QtApp(_handle);
        }

        #endregion memory_management

        #region DLLImport

        //  memory management
        [DllImport("hbp_export", EntryPoint = "create_QtApp", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_QtApp();

        [DllImport("hbp_export", EntryPoint = "delete_QtApp", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_QtApp(HandleRef handleQtApp);

        // actions
        [DllImport("hbp_export", EntryPoint = "getExistingDirectory_QtApp", CallingConvention = CallingConvention.Cdecl)]
        static private extern bool getExistingDirectory_QtApp(string message, string defaultDir, StringBuilder dirPathRes, int bufferSize);

        [DllImport("hbp_export", EntryPoint = "getOpenFileName_QtApp", CallingConvention = CallingConvention.Cdecl)]
        static private extern bool getOpenFileName_QtApp(string message, string defaultDir, string filters, StringBuilder filePathRes, int bufferSize);

        [DllImport("hbp_export", EntryPoint = "getOpenFilesName_QtApp", CallingConvention = CallingConvention.Cdecl)]
        static private extern bool getOpenFilesName_QtApp(string message, string defaultDir, string filters, StringBuilder filesPathRes, int bufferSize);

        [DllImport("hbp_export", EntryPoint = "getSaveFileName_QtApp", CallingConvention = CallingConvention.Cdecl)]
        static private extern bool getSaveFileName_QtApp(string message, string defaultDir, string filters, StringBuilder savedfilePathRes, int bufferSize);

        [DllImport("hbp_export", EntryPoint = "getActionToDoFromErrorDialog_QtApp", CallingConvention = CallingConvention.Cdecl)]
        static private extern int getActionToDoFromErrorDialog_QtApp();

        #endregion DLLImport
    }
}