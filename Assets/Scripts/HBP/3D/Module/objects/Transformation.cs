

/**
 * \file    Transformation.cs
 * \author  Lance Florian
 * \date    11/10//2016
 * \brief   Define Transformation class
 */

// system
using System;
using System.Runtime.InteropServices;

// unity
using UnityEngine;


namespace HBP.Module3D.DLL
{
    /// <summary>
    /// A DLL mesh class, for geometry computing purposes, can be converted to a Mesh
    /// </summary>
    public class Transformation : CppDLLImportBase
    {
        #region Public Methods
        /// <summary>
        /// Load a transformation file
        /// </summary>
        /// <param name="pathTransfo"></param>
        /// <returns></returns>
        public bool Load(string pathTransfo)
        {
            bool fileLoaded = (load_Transformation3(_handle, pathTransfo) == 1);
            if (!fileLoaded)
            {
                Debug.LogError("-ERROR : Transformation::load -> can't load file : " + pathTransfo);
            }
            return fileLoaded;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 
        /// </summary>
        private void Invert()
        {
            invert_Transformation3(_handle);
        }
        #endregion

        #region Memory Management
        /// <summary>
        /// Surface default constructor
        /// </summary>
        public Transformation() : base() { }
        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void create_DLL_class()
        {
            _handle = new HandleRef(this, create_Transformation3());
        }
        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
            delete_Transformation3(_handle);
        }
        #endregion

        #region DLLImport

        // memory management
        [DllImport("hbp_export", EntryPoint = "create_Transformation3", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_Transformation3();

        [DllImport("hbp_export", EntryPoint = "delete_Transformation3", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_Transformation3(HandleRef handleTransformation);

        // save / load
        [DllImport("hbp_export", EntryPoint = "load_Transformation3", CallingConvention = CallingConvention.Cdecl)]
        static private extern int load_Transformation3(HandleRef handleTransformation, string pathFile);

        // action
        [DllImport("hbp_export", EntryPoint = "invert_Transformation3", CallingConvention = CallingConvention.Cdecl)]
        static private extern void invert_Transformation3(HandleRef handleTransformation);

        #endregion
    }
}