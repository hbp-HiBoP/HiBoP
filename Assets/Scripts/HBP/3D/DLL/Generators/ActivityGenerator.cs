﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Module3D.DLL
{
    public abstract class ActivityGenerator : Tools.DLL.CppDLLImportBase
    {
        #region Properties
        public float Progress { get { return get_progress_ActivityGenerator(_handle); } }
        public GeneratorSurface GeneratorSurface { get; private set; }
        #endregion

        #region Public Methods
        public void Initialize(GeneratorSurface generatorSurface)
        {
            GeneratorSurface = generatorSurface;
            initialize_ActivityGenerator(_handle, generatorSurface.getHandle());
        }
        #endregion

        #region DLLImport
        [DllImport("hbp_export", EntryPoint = "initialize_ActivityGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void initialize_ActivityGenerator(HandleRef generator, HandleRef generatorSurface);
        [DllImport("hbp_export", EntryPoint = "get_progress_ActivityGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern float get_progress_ActivityGenerator(HandleRef generator);
        #endregion
    }
}