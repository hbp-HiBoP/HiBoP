using HBP.Core.Tools;
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
    /// Mars atlas index, used to identify sites mars IDs and areas on the brain
    /// </summary>
    public class MarsAtlas : BrainAtlas
    {
        private readonly Dictionary<int, MarsAtlasMetadata> m_MetadataByLabel = new();
        private bool m_MetadataCacheBuilt;

        private readonly struct MarsAtlasMetadata
        {
            public MarsAtlasMetadata(
                string hemisphere,
                string lobe,
                string nameFS,
                string name,
                string fullName,
                string brodmannAreas,
                string information,
                Color color,
                Color highlightedColor)
            {
                Hemisphere = hemisphere;
                Lobe = lobe;
                NameFS = nameFS;
                Name = name;
                FullName = fullName;
                BrodmannAreas = brodmannAreas;
                Information = information;
                Color = color;
                HighlightedColor = highlightedColor;
            }

            public string Hemisphere { get; }
            public string Lobe { get; }
            public string NameFS { get; }
            public string Name { get; }
            public string FullName { get; }
            public string BrodmannAreas { get; }
            public string Information { get; }
            public Color Color { get; }
            public Color HighlightedColor { get; }
        }

        #region Constructors
        public MarsAtlas() : base() { }
        #endregion

        #region Public Methods
        /// <summary>
        /// Get the label of the MarsAtlas region given its shortened name
        /// </summary>
        /// <param name="name">Shortened name of the MarsAtlas region</param>
        /// <returns></returns>
        public int Label(string name)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_mars_atlas_find_label(_handle.Handle, name, out int label));
                return label;
            }

            return get_label_MarsAtlasIndex(_handle, name);
        }
        /// <summary>
        /// Return all the labels of the mars atlas file
        /// </summary>
        /// <returns>Array of all labels</returns>
        public int[] Labels()
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_mars_atlas_get_label_count(_handle.Handle, out int labelCount));
                int[] hbpCoreLabels = new int[labelCount];
                if (labelCount > 0)
                {
                    ThrowIfFailed(hbp_mars_atlas_copy_labels(_handle.Handle, hbpCoreLabels, hbpCoreLabels.Length));
                }
                return hbpCoreLabels;
            }

            int[] labels = new int[get_label_count_MarsAtlasIndex(_handle)];
            get_all_labels_MarsAtlasIndex(_handle, labels);
            return labels;
        }
        /// <summary>
        /// Return the name of the hemisphere given a mars atlas label ID
        /// </summary>
        /// <param name="id">ID of mars atlas label</param>
        /// <returns>Name of the hemipshere</returns>
        public string Hemisphere(int id)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                if (id < 0) return "not found";
                if (TryGetMetadata(id, out MarsAtlasMetadata metadata)) return metadata.Hemisphere;
                return CopyHbpCoreText(id, hbp_mars_atlas_copy_hemisphere);
            }

            lock (typeof(Marshal))
            {
                if (id < 0) return "not found";

                IntPtr result = hemisphere_MarsAtlasIndex(_handle, id);
                return Marshal.PtrToStringAnsi(result);
            }
        }
        /// <summary>
        /// Return the name of the lobe given a mars atlas label ID
        /// </summary>
        /// <param name="id">ID of mars atlas label</param>
        /// <returns>Name of the lobe</returns>
        public string Lobe(int label)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                if (label < 0) return "not found";
                if (TryGetMetadata(label, out MarsAtlasMetadata metadata)) return metadata.Lobe;
                return CopyHbpCoreText(label, hbp_mars_atlas_copy_lobe);
            }

            lock (typeof(Marshal))
            {
                if (label < 0) return "not found";

                IntPtr result = lobe_MarsAtlasIndex(_handle, label);
                return Marshal.PtrToStringAnsi(result);
            }
        }
        /// <summary>
        /// Return the name of the name fs given a mars atlas label ID
        /// </summary>
        /// <param name="id">ID of mars atlas label</param>
        /// <returns>Name of the name fs</returns>
        public string NameFS(int label)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                if (label < 0) return "not found";
                if (TryGetMetadata(label, out MarsAtlasMetadata metadata)) return metadata.NameFS;
                return CopyHbpCoreText(label, hbp_mars_atlas_copy_name_fs);
            }

            lock (typeof(Marshal))
            {
                if (label < 0) return "not found";

                IntPtr result = nameFS_MarsAtlasIndex(_handle, label);
                return Marshal.PtrToStringAnsi(result);
            }
        }
        /// <summary>
        /// Return the name of a mars atlas area given a mars atlas label ID
        /// </summary>
        /// <param name="id">ID of mars atlas label</param>
        /// <returns>Name of the mars atlas area</returns>
        public string Name(int label)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                if (label < 0) return "not found";
                if (TryGetMetadata(label, out MarsAtlasMetadata metadata)) return metadata.Name;
                return CopyHbpCoreText(label, hbp_mars_atlas_copy_name);
            }

            lock (typeof(Marshal))
            {
                if (label < 0) return "not found";

                IntPtr result = name_MarsAtlasIndex(_handle, label);
                return Marshal.PtrToStringAnsi(result);
            }
        }
        /// <summary>
        /// Return the full name of a mars atlas area given a mars atlas label ID
        /// </summary>
        /// <param name="id">ID of mars atlas label</param>
        /// <returns>Full name of the mars atlas area</returns>
        public string FullName(int label)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                if (label < 0) return "not found";
                if (TryGetMetadata(label, out MarsAtlasMetadata metadata)) return metadata.FullName;
                return CopyHbpCoreText(label, hbp_mars_atlas_copy_full_name);
            }

            lock (typeof(Marshal))
            {
                if (label < 0) return "not found";

                IntPtr result = fullName_MarsAtlasIndex(_handle, label);
                return Marshal.PtrToStringAnsi(result);
            }
        }
        /// <summary>
        /// Return the name of the brodmann area given a mars atlas label ID
        /// </summary>
        /// <param name="id">ID of mars atlas label</param>
        /// <returns>Name of the brodmann area</returns>
        public string BrodmannArea(int label)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                if (label < 0) return "not found";
                if (TryGetMetadata(label, out MarsAtlasMetadata metadata)) return metadata.BrodmannAreas;
                return CopyHbpCoreText(label, hbp_mars_atlas_copy_brodmann_areas);
            }

            lock (typeof(Marshal))
            {
                if (label < 0) return "not found";

                IntPtr result = BA_MarsAtlasIndex(_handle, label);
                return Marshal.PtrToStringAnsi(result);
            }
        }
        public override void Load()
        {
            string indexPath = Path.Combine(ApplicationState.DataPath, "Atlases", "MarsAtlas", "mars_atlas_index.csv");
            string brodmannPath = Path.Combine(ApplicationState.DataPath, "Atlases", "MarsAtlas", "brodmann_areas.txt");
            string mriPath = Path.Combine(ApplicationState.DataPath, "Atlases", "MarsAtlas", "colin27_MNI_MarsAtlas.nii");
            Load(indexPath, brodmannPath, mriPath);
        }
        /// <summary>
        /// Load the mars atlas
        /// </summary>
        /// <param name="path">Path to mars atlas csv file</param>
        /// <param name="pathBrodmann">Path to brodmann txt file</param>
        /// <param name="pathNifti">Path to mars atlas nifti file</param>
        /// <returns>True if the mars atlas is correctly loaded</returns>
        public bool Load(string path, string pathBrodmann, string pathNifti)
        {
            Loading = true;
            m_MetadataByLabel.Clear();
            m_MetadataCacheBuilt = false;
            if (m_Backend == NativeBackend.HbpCore)
            {
                Loaded = hbp_mars_atlas_load(_handle.Handle, path, pathBrodmann, pathNifti) == HbpCoreStatus.Ok;
                if (Loaded)
                {
                    ThrowIfFailed(hbp_mars_atlas_apply_offset(_handle.Handle, 1.7f, 0f, 1f));
                }
            }
            else
            {
                Loaded = load_MarsAtlasIndex(_handle, path, pathBrodmann, pathNifti) == 1;
                apply_offset_MarsAtlasIndex(_handle, 1.7f, 0f, 1f);
            }
            Loading = false;
            return Loaded;
        }
        public override string GetAreaName(int index)
        {
            string[] areaInformation = GetInformation(index);
            if (areaInformation.Length == 5)
                return areaInformation[4];
            return string.Empty;
        }
        #endregion

        #region Private Methods
        protected override void GetAreaNames()
        {
            m_AreaNames = new List<string>();

            string indexPath = Path.Combine(ApplicationState.DataPath, "Atlases", "MarsAtlas", "mars_atlas_index.csv");
            if (!File.Exists(indexPath)) return;

            var names = new List<string>();

            using (var reader = new StreamReader(indexPath))
            {
                string headerLine = reader.ReadLine();
                if (headerLine == null) return;

                var headers = headerLine.Split(',');
                int labelIndex = -1, fullNameIndex = -1;
                for (int i = 0; i < headers.Length; i++)
                {
                    if (headers[i].Trim() == "Label") labelIndex = i;
                    if (headers[i].Trim() == "Full name") fullNameIndex = i;
                }
                if (labelIndex == -1 || fullNameIndex == -1) return;

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var columns = line.Split(',');

                    if (columns.Length <= fullNameIndex || columns.Length <= labelIndex) continue;

                    string label = columns[labelIndex].Trim();
                    string fullName = columns[fullNameIndex].Trim();

                    if (label == "0") continue; // Ignore White Matter

                    if (!string.IsNullOrEmpty(fullName))
                    {
                        names.Add(fullName);
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
            GetAreaNames();
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_mars_atlas_create(out IntPtr atlas));
                _handle = new HandleRef(this, atlas);
                return;
            }

            _handle = new HandleRef(this, create_MarsAtlasIndex());
        }
        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
            m_MetadataByLabel.Clear();
            m_MetadataCacheBuilt = false;
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_mars_atlas_destroy(_handle.Handle));
                return;
            }

            delete_MarsAtlasIndex(_handle);
        }
        #endregion

        private delegate HbpCoreStatus CopyMarsAtlasText(IntPtr atlas, int label, StringBuilder text, int textCapacity);

        protected override bool TryGetCachedInformation(int labelIndex, out string[] information)
        {
            if (TryGetMetadata(labelIndex, out MarsAtlasMetadata metadata))
            {
                information = metadata.Information.Split(new char[1] { '?' }, StringSplitOptions.None);
                return true;
            }

            information = null;
            return false;
        }

        protected override bool TryConvertCachedIndicesToColors(int[] indices, int selectedArea, Color[] colors)
        {
            EnsureMetadataCache();
            if (m_Backend != NativeBackend.HbpCore || m_MetadataByLabel.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < indices.Length; ++i)
            {
                if (m_MetadataByLabel.TryGetValue(indices[i], out MarsAtlasMetadata metadata))
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

        private bool TryGetMetadata(int label, out MarsAtlasMetadata metadata)
        {
            EnsureMetadataCache();
            metadata = default;
            return m_Backend == NativeBackend.HbpCore && m_MetadataByLabel.TryGetValue(label, out metadata);
        }

        private void EnsureMetadataCache()
        {
            if (m_Backend == NativeBackend.HbpCore && Loaded && !m_MetadataCacheBuilt)
            {
                BuildMetadataCache();
                m_MetadataCacheBuilt = true;
            }
        }

        private void BuildMetadataCache()
        {
            m_MetadataByLabel.Clear();
            foreach (int label in Labels())
            {
                string hemisphere = CopyHbpCoreText(label, hbp_mars_atlas_copy_hemisphere);
                string lobe = CopyHbpCoreText(label, hbp_mars_atlas_copy_lobe);
                string nameFS = CopyHbpCoreText(label, hbp_mars_atlas_copy_name_fs);
                string name = CopyHbpCoreText(label, hbp_mars_atlas_copy_name);
                string fullName = CopyHbpCoreText(label, hbp_mars_atlas_copy_full_name);
                m_MetadataByLabel[label] = new MarsAtlasMetadata(
                    hemisphere,
                    lobe,
                    nameFS,
                    name,
                    fullName,
                    CopyHbpCoreText(label, hbp_mars_atlas_copy_brodmann_areas),
                    $"{hemisphere}_{name}?{hemisphere}?{lobe}?{nameFS}?{fullName}",
                    CopyHbpCoreColor(label, highlighted: false),
                    CopyHbpCoreColor(label, highlighted: true));
            }

            GetAreaNames();
        }

        private string CopyHbpCoreText(int label, CopyMarsAtlasText copyText)
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

            throw new InvalidOperationException("hbp_core MarsAtlas text is too large.");
        }

        private Color CopyHbpCoreColor(int label, bool highlighted)
        {
            ThrowIfFailed(hbp_mars_atlas_get_color(_handle.Handle, label, highlighted ? 1 : 0, out Color4 color));
            return color.ToColor();
        }

        #region DLLImport
        [DllImport(NativeDll.HbpExport, EntryPoint = "create_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_MarsAtlasIndex();
        [DllImport(NativeDll.HbpExport, EntryPoint = "delete_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_MarsAtlasIndex(HandleRef marsAtlasIndex);
        [DllImport(NativeDll.HbpExport, EntryPoint = "get_label_count_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern int get_label_count_MarsAtlasIndex(HandleRef marsAtlasIndex);
        [DllImport(NativeDll.HbpExport, EntryPoint = "get_all_labels_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern void get_all_labels_MarsAtlasIndex(HandleRef marsAtlasIndex, int[] labels);
        [DllImport(NativeDll.HbpExport, EntryPoint = "load_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern int load_MarsAtlasIndex(HandleRef marsAtlasIndex, string pathFile, string pathBrodmannFile, string pathNiftiFile);
        [DllImport(NativeDll.HbpExport, EntryPoint = "get_label_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern int get_label_MarsAtlasIndex(HandleRef marsAtlasIndex, string name);
        [DllImport(NativeDll.HbpExport, EntryPoint = "hemisphere_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr hemisphere_MarsAtlasIndex(HandleRef marsAtlasIndex, int label);
        [DllImport(NativeDll.HbpExport, EntryPoint = "lobe_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr lobe_MarsAtlasIndex(HandleRef marsAtlasIndex, int label);
        [DllImport(NativeDll.HbpExport, EntryPoint = "nameFS_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr nameFS_MarsAtlasIndex(HandleRef marsAtlasIndex, int label);
        [DllImport(NativeDll.HbpExport, EntryPoint = "name_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr name_MarsAtlasIndex(HandleRef marsAtlasIndex, int label);
        [DllImport(NativeDll.HbpExport, EntryPoint = "fullName_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr fullName_MarsAtlasIndex(HandleRef marsAtlasIndex, int label);
        [DllImport(NativeDll.HbpExport, EntryPoint = "BA_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr BA_MarsAtlasIndex(HandleRef marsAtlasIndex, int label);
        [DllImport(NativeDll.HbpExport, EntryPoint = "apply_offset_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr apply_offset_MarsAtlasIndex(HandleRef marsAtlasIndex, float x, float y, float z);

        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_mars_atlas_create", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_mars_atlas_create(out IntPtr atlas);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_mars_atlas_destroy", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_mars_atlas_destroy(IntPtr atlas);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_mars_atlas_load", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_mars_atlas_load(IntPtr atlas, [MarshalAs(UnmanagedType.LPUTF8Str)] string indexPath, [MarshalAs(UnmanagedType.LPUTF8Str)] string brodmannPath, [MarshalAs(UnmanagedType.LPUTF8Str)] string niftiPath);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_mars_atlas_apply_offset", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_mars_atlas_apply_offset(IntPtr atlas, float x, float y, float z);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_mars_atlas_get_label_count", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_mars_atlas_get_label_count(IntPtr atlas, out int count);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_mars_atlas_copy_labels", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_mars_atlas_copy_labels(IntPtr atlas, [Out] int[] labels, int labelCapacity);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_mars_atlas_find_label", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_mars_atlas_find_label(IntPtr atlas, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, out int label);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_mars_atlas_get_color", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_mars_atlas_get_color(IntPtr atlas, int label, int highlighted, out Color4 color);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_mars_atlas_copy_hemisphere", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_mars_atlas_copy_hemisphere(IntPtr atlas, int label, StringBuilder text, int textCapacity);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_mars_atlas_copy_lobe", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_mars_atlas_copy_lobe(IntPtr atlas, int label, StringBuilder text, int textCapacity);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_mars_atlas_copy_name_fs", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_mars_atlas_copy_name_fs(IntPtr atlas, int label, StringBuilder text, int textCapacity);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_mars_atlas_copy_name", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_mars_atlas_copy_name(IntPtr atlas, int label, StringBuilder text, int textCapacity);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_mars_atlas_copy_full_name", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_mars_atlas_copy_full_name(IntPtr atlas, int label, StringBuilder text, int textCapacity);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_mars_atlas_copy_brodmann_areas", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_mars_atlas_copy_brodmann_areas(IntPtr atlas, int label, StringBuilder text, int textCapacity);
        #endregion

    }
}
