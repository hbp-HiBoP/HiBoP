
/**
 * \file    ReadMultiFilesBuffers.cs
 * \author  Lance Florian
 * \date    23/05/2016
 * \brief   Define ReadMultiFilesBuffers class
 */

// system
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// unity
using UnityEngine;

namespace HBP.Module3D.DLL
{
    /// <summary>
    /// A class for parsing several meshes file at once
    /// </summary>
    public class ReadMultiFilesBuffers : CppDLLImportBase
    {
        #region Properties
        public enum FilesTypes : int
        {
            MeshesObj, MeshesGII, MeshesTRI, VolNII, None
        }; /**< Files types */

        //List<>
        private int m_NumberFilesRead;
        private FilesTypes m_CurrentFilesType;
        //private bool m_filesParsed;
        #endregion

        #region Public Methods
        /// <summary>
        /// 
        /// </summary>
        void Reset()
        {
            // ... DLL reset

            m_NumberFilesRead = 0;
            //m_filesParsed = false;
            m_CurrentFilesType = FilesTypes.None;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pathsFiles"></param>
        /// <param name="filesType"></param>
        /// <returns></returns>
        public bool ReadBuffersFiles(List<string> pathsFiles, FilesTypes filesType)
        {
            for (int ii = 0; ii < pathsFiles.Count; ++ii)
                AddBuffer_readMultiFilesBuffers(_handle, pathsFiles[ii]);

            m_CurrentFilesType = filesType;
            m_NumberFilesRead = pathsFiles.Count;


            return true;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool ParseMeshes()
        {
            if (m_CurrentFilesType == FilesTypes.MeshesObj)
                parseObjMeshes_readMultiFilesBuffers(_handle);
            else
            {
                Debug.LogError("...");
                Reset();
            }

            return true;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<Surface> Meshes()
        {
            List<Surface> meshes = new List<Surface>(m_NumberFilesRead);
            if (m_CurrentFilesType == FilesTypes.MeshesObj)
            {
                for(int ii = 0; ii < m_NumberFilesRead; ++ii)
                {
                    meshes.Add(new Surface(retrieveSurface_readMultiFilesBuffers(_handle, ii)));
                }
            }

            return meshes;
        }
        #endregion

        #region Memory Management
        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void create_DLL_class()
        {
            _handle = new HandleRef(this, create_readMultiFilesBuffers());
        }
        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
            delete_readMultiFilesBuffers(_handle);
        }
        #endregion

        #region DLLImport

        // memory management
        [DllImport("hbp_export", EntryPoint = "create_readMultiFilesBuffers", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_readMultiFilesBuffers();

        [DllImport("hbp_export", EntryPoint = "delete_readMultiFilesBuffers", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_readMultiFilesBuffers(HandleRef handleReadMultiFilesBuffers);

        // read
        [DllImport("hbp_export", EntryPoint = "AddBuffer_readMultiFilesBuffers", CallingConvention = CallingConvention.Cdecl)]
        static private extern void AddBuffer_readMultiFilesBuffers(HandleRef handleReadMultiFilesBuffers, string pathFile);

        // parse
        [DllImport("hbp_export", EntryPoint = "parseObjMeshes_readMultiFilesBuffers", CallingConvention = CallingConvention.Cdecl)]
        static private extern void parseObjMeshes_readMultiFilesBuffers(HandleRef handleReadMultiFilesBuffers);

        // retrieve
        [DllImport("hbp_export", EntryPoint = "retrieveSurface_readMultiFilesBuffers", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr retrieveSurface_readMultiFilesBuffers(HandleRef handleReadMultiFilesBuffers, int idSurface);

        #endregion
    }
}