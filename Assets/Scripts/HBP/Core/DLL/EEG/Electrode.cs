using System;
using System.Runtime.InteropServices;

namespace HBP.Core.DLL.EEG
{
    public class Electrode : CppDLLImportBase
    {
        #region Properties
        /// <summary>
        /// Name of the electrode
        /// </summary>
        public string Label
        {
            get
            {
                return Marshal.PtrToStringAnsi(GetElectrodeLabel(_handle));
            }
        }
        /// <summary>
        /// Unit in which the data of the electrode is stored
        /// </summary>
        public string Unit
        {
            get
            {
                return Marshal.PtrToStringAnsi(GetElectrodeUnit(_handle));
            }
        }
        /// <summary>
        /// Physical minimum of the electrode
        /// </summary>
        public int PhysicalMinimum
        {
            get
            {
                return GetElectrodePhysicalMinimum(_handle);
            }
        }
        /// <summary>
        /// Physical maximum of the electrode
        /// </summary>
        public int PhysicalMaximum
        {
            get
            {
                return GetElectrodePhysicalMaximum(_handle);
            }
        }
        /// <summary>
        /// Digital maximum of the electrode
        /// </summary>
        public int DigitalMinimum
        {
            get
            {
                return GetElectrodeDigitalMinimum(_handle);
            }
        }
        /// <summary>
        /// Digital minimum of the electrode
        /// </summary>
        public int DigitalMaximum
        {
            get
            {
                return GetElectrodeDigitalMaximum(_handle);
            }
        }
        /// <summary>
        /// Digital ground of the electrode
        /// </summary>
        public int DigitalGround
        {
            get
            {
                return GetElectrodeDigitalGround(_handle);
            }
        }
        /// <summary>
        /// Label of the reference electrode of this electrode
        /// </summary>
        public string ReferenceLabel
        {
            get
            {
                return Marshal.PtrToStringAnsi(GetElectrodeReferenceLabel(_handle));
            }
        }
        /// <summary>
        /// Prefiltering high pass limit of the electrode
        /// </summary>
        public int PrefilteringHighPassLimit
        {
            get
            {
                return GetElectrodePrefilteringHighPassLimit(_handle);
            }
        }
        /// <summary>
        /// Prefiltering low pass limit of the electrode
        /// </summary>
        public int PrefilteringLowPassLimit
        {
            get
            {
                return GetElectrodePrefilteringLowPassLimit(_handle);
            }
        }
        /// <summary>
        /// Type of the electrode
        /// </summary>
        public int Type
        {
            get
            {
                return GetElectrodeType(_handle);
            }
        }
        /// <summary>
        /// Data of this electrode
        /// </summary>
        public float[] Data { get; private set; }
        #endregion

        #region Memory Management
        /// <summary>
        /// File constructor with an already allocated dll File
        /// </summary>
        /// <param name="filePtr"></param>
        public Electrode(IntPtr filePtr, float[] data) : base(filePtr)
        {
            Data = data;
        }
        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void create_DLL_class()
        {
            throw new Exception("Electrode can not be created outside of a file");
        }
        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
        }
        #endregion

        #region DLLImport
        [DllImport("EEGFormat", EntryPoint = "GetElectrodeLabel", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr GetElectrodeLabel(HandleRef electrode);
        [DllImport("EEGFormat", EntryPoint = "GetElectrodeUnit", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr GetElectrodeUnit(HandleRef electrode);
        [DllImport("EEGFormat", EntryPoint = "GetElectrodePhysicalMinimum", CallingConvention = CallingConvention.Cdecl)]
        static private extern int GetElectrodePhysicalMinimum(HandleRef electrode);
        [DllImport("EEGFormat", EntryPoint = "GetElectrodePhysicalMaximum", CallingConvention = CallingConvention.Cdecl)]
        static private extern int GetElectrodePhysicalMaximum(HandleRef electrode);
        [DllImport("EEGFormat", EntryPoint = "GetElectrodeDigitalMinimum", CallingConvention = CallingConvention.Cdecl)]
        static private extern int GetElectrodeDigitalMinimum(HandleRef electrode);
        [DllImport("EEGFormat", EntryPoint = "GetElectrodeDigitalMaximum", CallingConvention = CallingConvention.Cdecl)]
        static private extern int GetElectrodeDigitalMaximum(HandleRef electrode);
        [DllImport("EEGFormat", EntryPoint = "GetElectrodeDigitalGround", CallingConvention = CallingConvention.Cdecl)]
        static private extern int GetElectrodeDigitalGround(HandleRef electrode);
        [DllImport("EEGFormat", EntryPoint = "GetElectrodeReferenceLabel", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr GetElectrodeReferenceLabel(HandleRef electrode);
        [DllImport("EEGFormat", EntryPoint = "GetElectrodePrefilteringHighPassLimit", CallingConvention = CallingConvention.Cdecl)]
        static private extern int GetElectrodePrefilteringHighPassLimit(HandleRef electrode);
        [DllImport("EEGFormat", EntryPoint = "GetElectrodePrefilteringLowPassLimit", CallingConvention = CallingConvention.Cdecl)]
        static private extern int GetElectrodePrefilteringLowPassLimit(HandleRef electrode);
        [DllImport("EEGFormat", EntryPoint = "GetElectrodeType", CallingConvention = CallingConvention.Cdecl)]
        static private extern int GetElectrodeType(HandleRef electrode);
        #endregion
    }
}