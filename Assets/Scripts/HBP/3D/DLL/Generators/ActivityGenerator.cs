using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Module3D.DLL
{
    public abstract class ActivityGenerator : Tools.DLL.CppDLLImportBase
    {
        #region Properties
        public Surface Surface { get; private set; }
        public Volume Volume { get; private set; }
        #endregion

        #region Public Methods
        public void Initialize(Surface surface, Volume volume, int dimension)
        {
            Surface = surface;
            Volume = volume;
            initialize_ActivityGenerator(_handle, surface.getHandle(), volume.getHandle(), dimension);
        }
        #endregion

        #region DLLImport
        [DllImport("hbp_export", EntryPoint = "initialize_ActivityGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void initialize_ActivityGenerator(HandleRef generator, HandleRef surface, HandleRef volume, int dimension);
        #endregion
    }
}