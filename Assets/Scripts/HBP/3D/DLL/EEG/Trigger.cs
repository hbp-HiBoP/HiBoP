using System;
using System.Runtime.InteropServices;

namespace HBP.Core.DLL.EEG
{
    public class Trigger : CppDLLImportBase
    {
        #region
        /// <summary>
        /// Code of the event
        /// </summary>
        public int Code
        {
            get
            {
                return GetTriggerCode(_handle);
            }
        }
        /// <summary>
        /// Sample of the event
        /// </summary>
        public long Sample
        {
            get
            {
                return GetTriggerSample(_handle);
            }
        }
        #endregion

        #region Memory Management
        /// <summary>
        /// File constructor with an already allocated dll File
        /// </summary>
        /// <param name="filePtr"></param>
        public Trigger(IntPtr filePtr) : base(filePtr) { }
        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void create_DLL_class()
        {
            throw new Exception("Trigger can not be created outside of a file");
        }
        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
        }
        #endregion

        #region DLLImport
        [DllImport("EEGFormat", EntryPoint = "GetTriggerCode", CallingConvention = CallingConvention.Cdecl)]
        static private extern int GetTriggerCode(HandleRef electrode);
        [DllImport("EEGFormat", EntryPoint = "GetTriggerSample", CallingConvention = CallingConvention.Cdecl)]
        static private extern long GetTriggerSample(HandleRef electrode);
        #endregion
    }
}