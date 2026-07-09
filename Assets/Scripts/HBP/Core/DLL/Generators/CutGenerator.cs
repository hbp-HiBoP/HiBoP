using System;
using System.Runtime.InteropServices;
using HBP.Core.DLL.HbpCore;
using HBP.Core.Tools;
using UnityEngine;

namespace HBP.Core.DLL
{
    public class CutGenerator : CppDLLImportBase
    {
        private NativeBackend m_Backend = NativeBackendOptions.ExperimentalBackend;

        #region Properties
        public ActivityGenerator ActivityGenerator { get; private set; }
        public CutGeometryGenerator CutGeometryGenerator { get; private set; }
        internal NativeBackend Backend => m_Backend;
        public bool UsesHbpCore => m_Backend == NativeBackend.HbpCore;
        #endregion

        #region Public Methods
        public void Initialize(ActivityGenerator activityGenerator, CutGeometryGenerator cutGeometryGenerator, int blurFactor)
        {
            ActivityGenerator = activityGenerator;
            CutGeometryGenerator = cutGeometryGenerator;

            if (m_Backend == NativeBackend.HbpCore)
            {
                EnsureHbpCoreGeometry(cutGeometryGenerator, nameof(Initialize));
                IntPtr activityHandle = IntPtr.Zero;
                if (activityGenerator != null)
                {
                    EnsureHbpCoreActivity(activityGenerator, nameof(Initialize));
                    activityHandle = activityGenerator.getHandle().Handle;
                }
                ThrowIfFailed(hbp_cut_generator_initialize(_handle.Handle, activityHandle, cutGeometryGenerator.getHandle().Handle, blurFactor));
                return;
            }

            initialize_CutGenerator(_handle, activityGenerator.getHandle(), cutGeometryGenerator.getHandle(), blurFactor);
        }

        public void FillTextureWithVolume(Color32[] colorScheme, float calMin, float calMax)
        {
            EnsureHbpCore(nameof(FillTextureWithVolume));
            Color4[] nativeColorScheme = colorScheme.ToNativeColor4Array();
            HbpCoreStatus status = hbp_cut_generator_fill_volume_rgba(_handle.Handle, nativeColorScheme, nativeColorScheme.Length, calMin, calMax);
            if (status != HbpCoreStatus.Ok)
            {
                Vector2Int size = CutGeometryGenerator != null ? CutGeometryGenerator.TextureSize : Vector2Int.zero;
                throw new InvalidOperationException($"hbp_core CutGenerator.FillTextureWithVolume failed with status {status}: {HbpCoreRuntime.LastError} TextureSize={size.x}x{size.y} ColorCount={nativeColorScheme.Length}");
            }
        }

        public void FillTextureWithAtlas(BrainAtlas atlas, float alpha, int selectedArea)
        {
            EnsureHbpCore(nameof(FillTextureWithAtlas));
            EnsureHbpCoreAtlas(atlas, nameof(FillTextureWithAtlas));
            ThrowIfFailed(hbp_cut_generator_fill_atlas_rgba(_handle.Handle, atlas.getHandle().Handle, alpha, selectedArea));
        }

        public void FillTextureWithActivity(Color32[] colorScheme, int timelineIndex, float alpha)
        {
            EnsureHbpCore(nameof(FillTextureWithActivity));
            Color4[] nativeColorScheme = colorScheme.ToNativeColor4Array();
            ThrowIfFailed(hbp_cut_generator_fill_activity_rgba(_handle.Handle, nativeColorScheme, nativeColorScheme.Length, timelineIndex, alpha));
        }

        public void FillTextureWithFMRI(Volume volume, float negativeMin, float negativeMax, float positiveMin, float positiveMax, float alpha)
        {
            EnsureHbpCore(nameof(FillTextureWithFMRI));
            EnsureHbpCoreVolume(volume, nameof(FillTextureWithFMRI));
            ThrowIfFailed(hbp_cut_generator_fill_fmri_rgba(_handle.Handle, volume.getHandle().Handle, negativeMin, negativeMax, positiveMin, positiveMax, alpha));
        }

        public void FillTextureWithLocalizer(Volume volume, float min, float middle, float max, Volume mask, Color32[] colorScheme)
        {
            EnsureHbpCore(nameof(FillTextureWithLocalizer));
            EnsureHbpCoreVolume(volume, nameof(FillTextureWithLocalizer));
            if (mask != null)
            {
                EnsureHbpCoreVolume(mask, nameof(FillTextureWithLocalizer));
            }

            Color4[] nativeColorScheme = colorScheme.ToNativeColor4Array();
            IntPtr maskHandle = mask == null ? IntPtr.Zero : mask.getHandle().Handle;
            ThrowIfFailed(hbp_cut_generator_fill_localizer_rgba(_handle.Handle, volume.getHandle().Handle, maskHandle, min, middle, max, nativeColorScheme, nativeColorScheme.Length));
        }

        public Color32[] CopyBasePixels()
        {
            EnsureHbpCore(nameof(CopyBasePixels));
            return CopyPixels(overlay: false);
        }

        public Color32[] CopyOverlayPixels()
        {
            EnsureHbpCore(nameof(CopyOverlayPixels));
            return CopyPixels(overlay: true);
        }
        #endregion

        #region Memory Management
        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void create_DLL_class()
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_cut_generator_create(out IntPtr generator));
                _handle = new HandleRef(this, generator);
                return;
            }

            _handle = new HandleRef(this, create_CutGenerator());
        }
        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_cut_generator_destroy(_handle.Handle));
                return;
            }

            delete_CutGenerator(_handle);
        }
        #endregion

        private Color32[] CopyPixels(bool overlay)
        {
            Vector2Int size = CutGeometryGenerator.TextureSize;
            int pixelCount = size.x * size.y;
            Color4[] nativeColors = new Color4[pixelCount];
            HbpCoreStatus status = overlay
                ? hbp_cut_generator_copy_overlay_rgba(_handle.Handle, nativeColors, nativeColors.Length)
                : hbp_cut_generator_copy_base_rgba(_handle.Handle, nativeColors, nativeColors.Length);
            ThrowIfFailed(status);

            Color32[] pixels = new Color32[pixelCount];
            for (int i = 0; i < nativeColors.Length; ++i)
            {
                pixels[i] = ToColor32(nativeColors[i]);
            }
            return pixels;
        }

        private static Color32 ToColor32(Color4 color)
        {
            return new Color32(ToByte(color.r), ToByte(color.g), ToByte(color.b), ToByte(color.a));
        }

        private static byte ToByte(float value)
        {
            return (byte)Mathf.RoundToInt(Mathf.Clamp01(value) * 255.0f);
        }

        private void EnsureHbpCore(string methodName)
        {
            if (m_Backend != NativeBackend.HbpCore)
            {
                throw new NotSupportedException($"CutGenerator.{methodName} is only available with hbp_core.");
            }
        }

        private static void EnsureHbpCoreGeometry(CutGeometryGenerator geometry, string methodName)
        {
            if (geometry == null)
            {
                throw new ArgumentNullException(nameof(geometry), $"CutGenerator.{methodName} requires a cut geometry generator.");
            }
            if (geometry.Backend != NativeBackend.HbpCore)
            {
                throw new InvalidOperationException($"CutGenerator.{methodName} cannot use a {geometry.Backend} geometry with hbp_core.");
            }
        }

        private static void EnsureHbpCoreActivity(ActivityGenerator activityGenerator, string methodName)
        {
            if (activityGenerator.Backend != NativeBackend.HbpCore)
            {
                throw new InvalidOperationException($"CutGenerator.{methodName} cannot use a {activityGenerator.Backend} activity generator with hbp_core.");
            }
        }

        private static void EnsureHbpCoreVolume(Volume volume, string methodName)
        {
            if (volume.Backend != NativeBackend.HbpCore)
            {
                throw new InvalidOperationException($"CutGenerator.{methodName} cannot use a {volume.Backend} volume with hbp_core.");
            }
        }

        private static void EnsureHbpCoreAtlas(BrainAtlas atlas, string methodName)
        {
            if (atlas.Backend != NativeBackend.HbpCore)
            {
                throw new InvalidOperationException($"CutGenerator.{methodName} cannot use a {atlas.Backend} atlas with hbp_core.");
            }
        }

        private static void ThrowIfFailed(HbpCoreStatus status)
        {
            if (status != HbpCoreStatus.Ok)
            {
                throw new InvalidOperationException($"hbp_core CutGenerator call failed with status {status}: {HbpCoreRuntime.LastError}");
            }
        }

        #region DLLImport
        [DllImport(NativeDll.HbpExport, EntryPoint = "create_CutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_CutGenerator();
        [DllImport(NativeDll.HbpExport, EntryPoint = "delete_CutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_CutGenerator(HandleRef generator);
        [DllImport(NativeDll.HbpExport, EntryPoint = "initialize_CutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void initialize_CutGenerator(HandleRef generator, HandleRef activityGenerator, HandleRef geometryGenerator, int blurFactor);

        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_cut_generator_create", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_generator_create(out IntPtr generator);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_cut_generator_destroy", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_generator_destroy(IntPtr generator);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_cut_generator_initialize", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_generator_initialize(IntPtr generator, IntPtr activityGenerator, IntPtr geometryGenerator, int blurFactor);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_cut_generator_fill_volume_rgba", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_generator_fill_volume_rgba(IntPtr generator, [In] Color4[] colorScheme, int colorCount, float calMin, float calMax);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_cut_generator_fill_atlas_rgba", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_generator_fill_atlas_rgba(IntPtr generator, IntPtr atlas, float alpha, int selectedArea);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_cut_generator_fill_activity_rgba", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_generator_fill_activity_rgba(IntPtr generator, [In] Color4[] colorScheme, int colorCount, int timelineIndex, float alpha);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_cut_generator_fill_fmri_rgba", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_generator_fill_fmri_rgba(IntPtr generator, IntPtr volume, float negativeMin, float negativeMax, float positiveMin, float positiveMax, float alpha);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_cut_generator_fill_localizer_rgba", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_generator_fill_localizer_rgba(IntPtr generator, IntPtr volume, IntPtr mask, float minValue, float middleValue, float maxValue, [In] Color4[] colorScheme, int colorCount);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_cut_generator_copy_base_rgba", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_generator_copy_base_rgba(IntPtr generator, [Out] Color4[] colors, int colorCapacity);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_cut_generator_copy_overlay_rgba", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_generator_copy_overlay_rgba(IntPtr generator, [Out] Color4[] colors, int colorCapacity);
        #endregion
    }
}
