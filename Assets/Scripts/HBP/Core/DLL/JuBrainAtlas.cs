using HBP.Core.Tools;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using HBP.Core.DLL.HbpCore;
using UnityEngine;

namespace HBP.Core.DLL
{
    /// <summary>
    /// Class containing information about the different areas of the JuBrain Cytoarchitectonic Atlas
    /// </summary>
    public class JuBrainAtlas : BrainAtlas
    {
        private readonly Dictionary<int, JuBrainAtlasMetadata> m_MetadataByLabel = new();

        private readonly struct JuBrainAtlasMetadata
        {
            public JuBrainAtlasMetadata(string name, Color color, Color highlightedColor)
            {
                Name = name;
                Color = color;
                HighlightedColor = highlightedColor;
            }

            public string Name { get; }
            public Color Color { get; }
            public Color HighlightedColor { get; }
        }

        #region Properties
        private static string LeftNIIPath => Path.Combine(ApplicationState.DataPath, "Atlases", "JuBrain", "JulichBrainAtlas_3.1_207areas_MPM_lh_Colin27.nii.gz");
        private static string RightNIIPath => Path.Combine(ApplicationState.DataPath, "Atlases", "JuBrain", "JulichBrainAtlas_3.1_207areas_MPM_rh_Colin27.nii.gz");
        private static string JsonPath => Path.Combine(ApplicationState.DataPath, "Atlases", "JuBrain", "jubrain_labels_3.1.json");
        #endregion

        #region Constructors
        public JuBrainAtlas() : base() { }
        #endregion

        #region Public Methods
        /// <summary>
        /// Load the JuBrain atlas DLL object
        /// </summary>
        /// <param name="leftNIIPath">Path of the NIFTI file for the left side of the atlas</param>
        /// <param name="rightNIIPath">Path of the NIFTI file for the right side of the atlas</param>
        /// <param name="jsonPath">Path to the json containing information about the areas of the atlas</param>
        /// <returns></returns>
        public override void Load()
        {
            GetAreaNames();
            Loading = true;
            m_MetadataByLabel.Clear();
            if (m_Backend == NativeBackend.HbpCore)
            {
                Loaded = hbp_jubrain_atlas_load(_handle.Handle, LeftNIIPath, RightNIIPath, JsonPath) == HbpCoreStatus.Ok;
                if (Loaded)
                {
                    BuildMetadataCache();
                }
            }
            else
            {
                Loaded = load_JuBrainAtlas(_handle, LeftNIIPath, RightNIIPath, JsonPath) == 1;
            }
            Loading = false;
        }
        public override string GetAreaName(int index)
        {
            if (TryGetMetadata(index, out JuBrainAtlasMetadata metadata))
            {
                return metadata.Name;
            }

            string[] areaInformation = GetInformation(index);
            if (areaInformation.Length == 1)
                return areaInformation[0];
            return string.Empty;
        }
        #endregion

        #region Private Methods
        protected override void GetAreaNames()
        {
            if (m_Backend == NativeBackend.HbpCore && m_MetadataByLabel != null && m_MetadataByLabel.Count > 0)
            {
                m_AreaNames = m_MetadataByLabel.Values
                    .Select(metadata => metadata.Name)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .OrderBy(name => name)
                    .Distinct()
                    .ToList();
                return;
            }

            m_AreaNames = new List<string>();
            var names = new List<string>();

            if (!File.Exists(JsonPath)) return;

            string json = File.ReadAllText(JsonPath);
            JObject root = JObject.Parse(json);

            var structures = root["JulichBrainAtlas"]?["Structures"]?["Structure"];
            if (structures != null)
            {
                foreach (var structure in structures)
                {
                    var name = structure["name"]?.ToString();
                    if (!string.IsNullOrEmpty(name))
                    {
                        names.Add(name);
                    }
                }
            }

            m_AreaNames.AddRange(names.OrderBy(n => n).Distinct());
        }
        #endregion

        #region Memory Management
        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void create_DLL_class()
        {
            m_Backend = NativeBackendOptions.ExperimentalBackend;
            GetAreaNames();
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_jubrain_atlas_create(out IntPtr atlas));
                _handle = new HandleRef(this, atlas);
                return;
            }

            _handle = new HandleRef(this, create_JuBrainAtlas());
        }
        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
            m_MetadataByLabel.Clear();
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_jubrain_atlas_destroy(_handle.Handle));
            }
            else
            {
                delete_JuBrainAtlas(_handle);
            }
        }
        #endregion

        private delegate HbpCoreStatus CopyJuBrainAtlasText(IntPtr atlas, int label, StringBuilder text, int textCapacity);

        protected override bool TryGetCachedInformation(int labelIndex, out string[] information)
        {
            if (TryGetMetadata(labelIndex, out JuBrainAtlasMetadata metadata))
            {
                information = new[] { metadata.Name };
                return true;
            }

            information = null;
            return false;
        }

        protected override bool TryConvertCachedIndicesToColors(int[] indices, int selectedArea, Color[] colors)
        {
            if (m_Backend != NativeBackend.HbpCore || m_MetadataByLabel.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < indices.Length; ++i)
            {
                if (m_MetadataByLabel.TryGetValue(indices[i], out JuBrainAtlasMetadata metadata))
                {
                    colors[i] = indices[i] == selectedArea ? metadata.HighlightedColor : metadata.Color;
                }
                else
                {
                    colors[i] = new Color(0f, 0f, 0f, 0f);
                }
            }

            return true;
        }

        private bool TryGetMetadata(int label, out JuBrainAtlasMetadata metadata)
        {
            metadata = default;
            return m_Backend == NativeBackend.HbpCore && m_MetadataByLabel.TryGetValue(label, out metadata);
        }

        private void BuildMetadataCache()
        {
            m_MetadataByLabel.Clear();
            ThrowIfFailed(hbp_jubrain_atlas_get_label_count(_handle.Handle, out int labelCount));
            int[] labels = new int[labelCount];
            if (labelCount > 0)
            {
                ThrowIfFailed(hbp_jubrain_atlas_copy_labels(_handle.Handle, labels, labels.Length));
            }

            foreach (int label in labels)
            {
                m_MetadataByLabel[label] = new JuBrainAtlasMetadata(
                    CopyHbpCoreText(label, hbp_jubrain_atlas_copy_name),
                    CopyHbpCoreColor(label, highlighted: false),
                    CopyHbpCoreColor(label, highlighted: true));
            }

            GetAreaNames();
        }

        private string CopyHbpCoreText(int label, CopyJuBrainAtlasText copyText)
        {
            int capacity = 256;
            while (capacity <= 4096)
            {
                StringBuilder builder = new(capacity);
                HbpCoreStatus status = copyText(_handle.Handle, label, builder, capacity);
                if (status == HbpCoreStatus.Ok)
                {
                    return builder.ToString();
                }
                if (status != HbpCoreStatus.BufferTooSmall)
                {
                    ThrowIfFailed(status);
                }
                capacity *= 2;
            }

            throw new InvalidOperationException("hbp_core JuBrainAtlas text is too large.");
        }

        private Color CopyHbpCoreColor(int label, bool highlighted)
        {
            ThrowIfFailed(hbp_jubrain_atlas_get_color(_handle.Handle, label, highlighted ? 1 : 0, out Color4 color));
            return color.ToColor();
        }

        #region DLLImport
        [DllImport(NativeDll.HbpExport, EntryPoint = "create_JuBrainAtlas", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_JuBrainAtlas();
        [DllImport(NativeDll.HbpExport, EntryPoint = "delete_JuBrainAtlas", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_JuBrainAtlas(HandleRef juBrainAtlas);
        [DllImport(NativeDll.HbpExport, EntryPoint = "load_JuBrainAtlas", CallingConvention = CallingConvention.Cdecl)]
        static private extern int load_JuBrainAtlas(HandleRef juBrainAtlas, string leftNIIPath, string rightNIIPath, string jsonPath);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_jubrain_atlas_create", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_jubrain_atlas_create(out IntPtr atlas);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_jubrain_atlas_destroy", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_jubrain_atlas_destroy(IntPtr atlas);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_jubrain_atlas_load", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_jubrain_atlas_load(
            IntPtr atlas,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string leftNIIPath,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string rightNIIPath,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string jsonPath);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_jubrain_atlas_get_label_count", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_jubrain_atlas_get_label_count(IntPtr atlas, out int count);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_jubrain_atlas_copy_labels", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_jubrain_atlas_copy_labels(IntPtr atlas, [Out] int[] labels, int labelCapacity);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_jubrain_atlas_copy_name", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_jubrain_atlas_copy_name(IntPtr atlas, int label, StringBuilder text, int textCapacity);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_jubrain_atlas_get_color", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_jubrain_atlas_get_color(IntPtr atlas, int label, int highlighted, out Color4 color);
        #endregion
    }
}
