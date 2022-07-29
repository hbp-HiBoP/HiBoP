using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Core.DLL.EEG
{
    public class Note : CppDLLImportBase
    {
        #region Properties
        /// <summary>
        /// Description of the note
        /// </summary>
        public string Description
        {
            get
            {
                return Marshal.PtrToStringAnsi(GetNoteDescription(_handle));
            }
        }
        /// <summary>
        /// Sample of the note
        /// </summary>
        public long Sample
        {
            get
            {
                return GetNoteSample(_handle);
            }
        }
        #endregion

        #region Memory Management
        /// <summary>
        /// File constructor with an already allocated dll File
        /// </summary>
        /// <param name="filePtr"></param>
        public Note(IntPtr filePtr) : base(filePtr) { }
        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void create_DLL_class()
        {
            throw new Exception("Note can not be created outside of a file");
        }
        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
        }
        #endregion

        #region DLLImport
        [DllImport("EEGFormat", EntryPoint = "GetNoteDescription", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr GetNoteDescription(HandleRef electrode);
        [DllImport("EEGFormat", EntryPoint = "GetNoteSample", CallingConvention = CallingConvention.Cdecl)]
        static private extern long GetNoteSample(HandleRef electrode);
        #endregion
    }
}