
/**
 * \file    CppDLLImportBase.cs
 * \author  Lance Florian
 * \date    10/02/2016
 * \brief   Define CppDLLImportBase
 */

// system
using System;
using System.Runtime.InteropServices;

namespace HBP.Module3D.DLL
{
    /// <summary>
    /// Base class for creating C++ Dll import classes
    /// </summary>
    public abstract class CppDLLImportBase : IDisposable
    {
        #region Properties

        /// <summary>
        /// pointer to C+ dll class
        /// </summary>
        protected HandleRef _handle;

        #endregion

        #region Public Methods

        /// <summary>
        /// CppDLLImportBase default constructor
        /// </summary>
        public CppDLLImportBase()
        {
            create_DLL_class();
        }

        /// <summary>
        /// CppDLLImportBase constructor with an already allocated dll class
        /// </summary>
        /// <param name="ptr"></param>
        public CppDLLImportBase(IntPtr ptr)
        {
            _handle = new HandleRef(this, ptr);
        }

        /// <summary>
        /// CppDLLImportBase Destructor
        /// </summary>
        ~CppDLLImportBase()
        {
            Cleanup();
        }

        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        abstract protected void create_DLL_class();

        /// <summary>
        /// Clean DLL memory
        /// </summary>
        abstract protected void delete_DLL_class();

        /// <summary>
        /// Force delete C++ DLL data (remove GC for this object)
        /// </summary>
        public virtual void Dispose()
        {
            Cleanup();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Delete C+ DLL data, and set handle to IntPtr.Zero
        /// </summary>
        private void Cleanup()
        {
            delete_DLL_class();
            _handle = new HandleRef(this, IntPtr.Zero);
        }

        /// <summary>
        /// Return pointer to C++ DLL
        /// </summary>
        /// <returns></returns>
        public HandleRef getHandle()
        {
            return _handle;
        }

        #endregion
    }
}