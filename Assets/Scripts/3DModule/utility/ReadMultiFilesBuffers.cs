
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

namespace HBP.VISU3D.DLL
{
    /// <summary>
    /// 
    /// </summary>
    public class ReadMultiFilesBuffers : CppDLLImportBase
    {
        #region members

        public enum FilesTypes : int
        {
            MeshesObj, MeshesGII, MeshesTRI, VolNII, None
        }; /**< Files types */

        //List<>
        private int m_nbFilesRead;
        private FilesTypes m_currentFilesType;
        private bool m_filesParsed;

        #endregion members

        #region functions

        void reset()
        {
            // ... DLL reset

            m_nbFilesRead = 0;
            m_filesParsed = false;
            m_currentFilesType = FilesTypes.None;
        }

        public bool readBuffersFiles(List<string> pathsFiles, FilesTypes filesType)
        {

            // ...
            for (int ii = 0; ii < pathsFiles.Count; ++ii)
            {
                if (!AddBuffer_readMultiFilesBuffers(_handle, pathsFiles[ii]))
                {
                    Debug.LogError("...");
                    reset();
                    return false;
                }
            }

            m_currentFilesType = filesType;
            m_nbFilesRead = pathsFiles.Count;


            return true;
        }


        public bool parseMeshes()
        {
            if (m_currentFilesType == FilesTypes.MeshesObj)
            {
                m_filesParsed = parseObjMeshes_readMultiFilesBuffers(_handle);
                if (!m_filesParsed)
                {
                    Debug.LogError("...");
                    reset();
                    return false;
                }
            }
            else
            {
                Debug.LogError("...");
                reset();
            }

            return true;
        }

        public List<Surface> getMeshes()
        {
            List<Surface> meshes = new List<Surface>(m_nbFilesRead);
            if (m_currentFilesType == FilesTypes.MeshesObj)
            {
                for(int ii = 0; ii < m_nbFilesRead; ++ii)
                {
                    meshes.Add(new Surface(retrieveSurface_readMultiFilesBuffers(_handle, ii)));
                }
            }

            return meshes;
        }


        //public bool loadNiftiFile(string pathNiftiFile)
        //{
        //    return loadNiiFile_NIFTI(_handle, pathNiftiFile);
        //}

        //public void convertToVolume(Volume volume)
        //{
        //    convertToVolume_NIFTI(_handle, volume.getHandle());
        //}

        #endregion functions

        #region memory_management

        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void createDLLClass()
        {
            _handle = new HandleRef(this, create_readMultiFilesBuffers());
        }

        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void deleteDLLClass()
        {
            delete_readMultiFilesBuffers(_handle);
        }

        #endregion memory_management

        #region DLLImport

        // memory management
        [DllImport("hbp_export", EntryPoint = "create_readMultiFilesBuffers", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_readMultiFilesBuffers();

        [DllImport("hbp_export", EntryPoint = "delete_readMultiFilesBuffers", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_readMultiFilesBuffers(HandleRef handleReadMultiFilesBuffers);

        // read
        [DllImport("hbp_export", EntryPoint = "AddBuffer_readMultiFilesBuffers", CallingConvention = CallingConvention.Cdecl)]
        static private extern bool AddBuffer_readMultiFilesBuffers(HandleRef handleReadMultiFilesBuffers, string pathFile);

        // parse
        [DllImport("hbp_export", EntryPoint = "parseObjMeshes_readMultiFilesBuffers", CallingConvention = CallingConvention.Cdecl)]
        static private extern bool parseObjMeshes_readMultiFilesBuffers(HandleRef handleReadMultiFilesBuffers);

        // retrieve
        [DllImport("hbp_export", EntryPoint = "retrieveSurface_readMultiFilesBuffers", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr retrieveSurface_readMultiFilesBuffers(HandleRef handleReadMultiFilesBuffers, int idSurface);

        #endregion DLLImport
    }
}