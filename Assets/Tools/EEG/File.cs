using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Tools.CSharp.EEG
{
    public class File : DLL.CppDLLImportBase
    {
        #region Properties
        public enum FileType { ELAN, EDF, Micromed, BrainVision }

        /// <summary>
        /// Size of the data
        /// </summary>
        public int NumberOfSamples
        {
            get
            {
                return GetNumberOfSamples(_handle);
            }
        }
        /// <summary>
        /// Number of electrodes in this file
        /// </summary>
        public int ElectrodeCount
        {
            get
            {
                return GetElectrodeCount(_handle);
            }
        }
        /// <summary>
        /// List of electrodes of this file
        /// </summary>
        public List<Electrode> Electrodes
        {
            get
            {
                List<Electrode> electrodes = new List<Electrode>(ElectrodeCount);
                for (int i = 0; i < ElectrodeCount; i++)
                {
                    float[] data = null;
                    if (NumberOfSamples != 0)
                    {
                        data = new float[NumberOfSamples];
                        GetElectrodeData(_handle, i, 0, data, data.Length);
                    }
                    electrodes.Add(new Electrode(GetElectrode(_handle, i), data));
                }
                return electrodes;
            }
        }
        /// <summary>
        /// Number of triggers in this file
        /// </summary>
        public int TriggerCount
        {
            get
            {
                return GetTriggerCount(_handle);
            }
        }
        /// <summary>
        /// List of triggers of this file
        /// </summary>
        public List<Trigger> Triggers
        {
            get
            {
                List<Trigger> triggers = new List<Trigger>(TriggerCount);
                for (int i = 0; i < TriggerCount; i++)
                {
                    triggers.Add(new Trigger(GetTrigger(_handle, i)));
                }
                return triggers;
            }
        }
        /// <summary>
        /// Number of notes in this file
        /// </summary>
        public int NoteCount
        {
            get
            {
                return GetNoteCount(_handle);
            }
        }
        /// <summary>
        /// List of notes of this file
        /// </summary>
        public List<Note> Notes
        {
            get
            {
                List<Note> notes = new List<Note>(NoteCount);
                for (int i = 0; i < NoteCount; i++)
                {
                    notes.Add(new Note(GetNote(_handle, i)));
                }
                return notes;
            }
        }
        /// <summary>
        /// Sampling frequency of this file
        /// </summary>
        public Frequency SamplingFrequency
        {
            get
            {
                return new Frequency(GetSamplingFrequency(_handle));
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Fix the name of the electrodes using the same pattern as Site Name Correction
        /// </summary>
        public void FixElectrodeName()
        {
            FixElectrodeName(_handle);
        }
        /// <summary>
        /// Delete some electrodes and their data
        /// </summary>
        /// <param name="electrodes"></param>
        public void DeleteElectrodesAndData(int[] electrodes)
        {
            DeleteElectrodesAndData(_handle, electrodes, electrodes.Length);
        }
        /// <summary>
        /// Load the data of the file
        /// </summary>
        public void Load()
        {
            Load(_handle);
        }
        /// <summary>
        /// Save the file
        /// </summary>
        public void Save()
        {
            Save(_handle);
        }
        /// <summary>
        /// Save the file in a specific directory with a specific base name (without extension)
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <param name="baseFileName"></param>
        public void SaveAs(string directoryPath, string baseFileName)
        {
            SaveAs(_handle, directoryPath, baseFileName);
        }
        #endregion

        #region Memory Management
        /// <summary>
        /// File constructor with an already allocated dll File
        /// </summary>
        /// <param name="filePtr"></param>
        public File(IntPtr filePtr) : base(filePtr) { }
        /// <summary>
        /// File constructor
        /// </summary>
        public File(FileType type, bool loadData, params string[] paths)
        {
            string dataPath, eventsPath, notesPath;
            switch (type)
            {
                case FileType.ELAN:
                    dataPath = paths.Length > 0 ? paths[0] : "";
                    eventsPath = paths.Length > 1 ? paths[1] : "";
                    notesPath = paths.Length > 2 ? paths[2] : "";
                    _handle = new HandleRef(this, CreateElanFile(dataPath, eventsPath, notesPath, loadData));
                    break;
                case FileType.EDF:
                    dataPath = paths.Length > 0 ? paths[0] : "";
                    _handle = new HandleRef(this, CreateEDFFile(dataPath, loadData));
                    break;
                case FileType.Micromed:
                    dataPath = paths.Length > 0 ? paths[0] : "";
                    _handle = new HandleRef(this, CreateMicromedFile(dataPath, loadData));
                    break;
                case FileType.BrainVision:
                    dataPath = paths.Length > 0 ? paths[0] : "";
                    _handle = new HandleRef(this, CreateBrainVisionFile(dataPath, loadData));
                    break;
            }
        }
        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void create_DLL_class()
        {
            throw new Exception("EEG File can not be created without specifying a path");
        }
        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
            DeleteFile(_handle);
        }
        #endregion

        #region DLLImport
        [DllImport("EEGFormat", EntryPoint = "CreateMicromedFile", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr CreateMicromedFile(string filePath, bool loadData);
        [DllImport("EEGFormat", EntryPoint = "CreateElanFile", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr CreateElanFile(string dataPath, string eventsPath, string notesPath, bool loadData);
        [DllImport("EEGFormat", EntryPoint = "CreateEDFFile", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr CreateEDFFile(string filePath, bool loadData);
        [DllImport("EEGFormat", EntryPoint = "CreateBrainVisionFile", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr CreateBrainVisionFile(string filePath, bool loadData);
        [DllImport("EEGFormat", EntryPoint = "DeleteFile", CallingConvention = CallingConvention.Cdecl)]
        static private extern void DeleteFile(HandleRef fileToDelete);

        [DllImport("EEGFormat", EntryPoint = "GetNumberOfSamples", CallingConvention = CallingConvention.Cdecl)]
        static private extern int GetNumberOfSamples(HandleRef file);
        [DllImport("EEGFormat", EntryPoint = "GetElectrodeCount", CallingConvention = CallingConvention.Cdecl)]
        static private extern int GetElectrodeCount(HandleRef file);
        [DllImport("EEGFormat", EntryPoint = "GetTriggerCount", CallingConvention = CallingConvention.Cdecl)]
        static private extern int GetTriggerCount(HandleRef file);
        [DllImport("EEGFormat", EntryPoint = "GetNoteCount", CallingConvention = CallingConvention.Cdecl)]
        static private extern int GetNoteCount(HandleRef file);
        [DllImport("EEGFormat", EntryPoint = "GetSamplingFrequency", CallingConvention = CallingConvention.Cdecl)]
        static private extern int GetSamplingFrequency(HandleRef file);

        [DllImport("EEGFormat", EntryPoint = "GetData", CallingConvention = CallingConvention.Cdecl)]
        static private extern float GetData(HandleRef file, int electrodeID, int sample, int dataConverterType);
        [DllImport("EEGFormat", EntryPoint = "GetElectrodeData", CallingConvention = CallingConvention.Cdecl)]
        static private extern void GetElectrodeData(HandleRef file, int electrodeID, int dataConverterType, float[] values, int size);
        [DllImport("EEGFormat", EntryPoint = "GetElectrode", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr GetElectrode(HandleRef file, int electrodeID);
        [DllImport("EEGFormat", EntryPoint = "GetTrigger", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr GetTrigger(HandleRef file, int triggerID);
        [DllImport("EEGFormat", EntryPoint = "GetNote", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr GetNote(HandleRef file, int noteID);

        [DllImport("EEGFormat", EntryPoint = "FixElectrodeName", CallingConvention = CallingConvention.Cdecl)]
        static private extern void FixElectrodeName(HandleRef file);
        [DllImport("EEGFormat", EntryPoint = "DeleteElectrodesAndData", CallingConvention = CallingConvention.Cdecl)]
        static private extern void DeleteElectrodesAndData(HandleRef file, int[] electrodesToDelete, int numberOfElectrodesToDelete);
        [DllImport("EEGFormat", EntryPoint = "Load", CallingConvention = CallingConvention.Cdecl)]
        static private extern void Load(HandleRef file);
        [DllImport("EEGFormat", EntryPoint = "Save", CallingConvention = CallingConvention.Cdecl)]
        static private extern void Save(HandleRef file);
        [DllImport("EEGFormat", EntryPoint = "SaveAs", CallingConvention = CallingConvention.Cdecl)]
        static private extern void SaveAs(HandleRef file, string directoryPath, string baseFileName);
        #endregion
    }
}