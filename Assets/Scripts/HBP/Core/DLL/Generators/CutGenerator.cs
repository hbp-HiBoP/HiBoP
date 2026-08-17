using System;
using System.Runtime.InteropServices;
using HBP.Core.DLL.HbpCore;
using HBP.Core.Tools;
using UnityEngine;

namespace HBP.Core.DLL
{
    public class CutGenerator : CppDLLImportBase
    {
        #region Properties

        public ActivityGenerator ActivityGenerator { get; private set; }
        public CutGeometryGenerator CutGeometryGenerator { get; private set; }

        #endregion

        #region Public Methods

        public void Initialize(ActivityGenerator activityGenerator, CutGeometryGenerator cutGeometryGenerator, int blurFactor)
        {
            ActivityGenerator = activityGenerator;
            CutGeometryGenerator = cutGeometryGenerator;

            if (cutGeometryGenerator == null) throw new ArgumentNullException(nameof(cutGeometryGenerator));
            IntPtr activityHandle = activityGenerator == null ? IntPtr.Zero : activityGenerator.getHandle().Handle;
            ThrowIfFailed(hbp_cut_generator_initialize(_handle.Handle, activityHandle, cutGeometryGenerator.getHandle().Handle, blurFactor));
        }

        public void FillTextureWithVolume(Color32[] colorScheme, float calMin, float calMax)
        {
            if (colorScheme == null) throw new ArgumentNullException(nameof(colorScheme));
            HbpCoreStatus status;
            GCHandle colorSchemeHandle = GCHandle.Alloc(colorScheme, GCHandleType.Pinned);
            try
            {
                status = hbp_cut_generator_fill_volume_rgba8(_handle.Handle, colorSchemeHandle.AddrOfPinnedObject(), colorScheme.Length, calMin, calMax);
            }
            finally
            {
                colorSchemeHandle.Free();
            }

            if (status != HbpCoreStatus.Ok)
            {
                Vector2Int size = CutGeometryGenerator != null ? CutGeometryGenerator.TextureSize : Vector2Int.zero;
                throw new InvalidOperationException($"hbp_core CutGenerator.FillTextureWithVolume failed with status {status}: {HbpCoreRuntime.LastError} TextureSize={size.x}x{size.y} ColorCount={colorScheme.Length}");
            }
        }

        public void SetMaskActivityOnMRIBackground(bool enabled)
        {
            ThrowIfFailed(hbp_cut_generator_set_mask_activity_on_mri_background(_handle.Handle, enabled ? 1 : 0));
        }

        public void FillTextureWithAtlas(BrainAtlas atlas, float alpha, int selectedArea)
        {
            if (atlas == null) throw new ArgumentNullException(nameof(atlas));
            ThrowIfFailed(hbp_cut_generator_fill_atlas_rgba(_handle.Handle, atlas.getHandle().Handle, alpha, selectedArea));
        }

        public void FillTextureWithActivity(Color32[] colorScheme, int timelineIndex, float alpha)
        {
            if (colorScheme == null) throw new ArgumentNullException(nameof(colorScheme));
            GCHandle colorSchemeHandle = GCHandle.Alloc(colorScheme, GCHandleType.Pinned);
            try
            {
                ThrowIfFailed(hbp_cut_generator_fill_activity_rgba8(_handle.Handle, colorSchemeHandle.AddrOfPinnedObject(), colorScheme.Length, timelineIndex, alpha));
            }
            finally
            {
                colorSchemeHandle.Free();
            }
        }

        public void FillTextureWithFMRI(Volume volume, float negativeMin, float negativeMax, float positiveMin, float positiveMax, float alpha)
        {
            if (volume == null) throw new ArgumentNullException(nameof(volume));
            ThrowIfFailed(hbp_cut_generator_fill_fmri_rgba(_handle.Handle, volume.getHandle().Handle, negativeMin, negativeMax, positiveMin, positiveMax, alpha));
        }

        public void FillTextureWithLocalizer(Volume volume, float min, float middle, float max, Volume mask, Color32[] colorScheme)
        {
            if (volume == null) throw new ArgumentNullException(nameof(volume));
            if (colorScheme == null) throw new ArgumentNullException(nameof(colorScheme));

            Color4[] nativeColorScheme = colorScheme.ToNativeColor4Array();
            IntPtr maskHandle = mask == null ? IntPtr.Zero : mask.getHandle().Handle;
            ThrowIfFailed(hbp_cut_generator_fill_localizer_rgba(_handle.Handle, volume.getHandle().Handle, maskHandle, min, middle, max, nativeColorScheme, nativeColorScheme.Length));
        }

        public Color32[] CopyBasePixels()
        {
            return CopyPixels(overlay: false);
        }

        public Color32[] CopyOverlayPixels()
        {
            return CopyPixels(overlay: true);
        }

        #endregion

        #region Memory Management

        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void create_DLL_class()
        {
            ThrowIfFailed(hbp_cut_generator_create(out IntPtr generator));
            _handle = new HandleRef(this, generator);
        }

        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
            ThrowIfFailed(hbp_cut_generator_destroy(_handle.Handle));
        }

        #endregion

        private Color32[] CopyPixels(bool overlay)
        {
            Vector2Int size = CutGeometryGenerator.TextureSize;
            int pixelCount = size.x * size.y;
            Color32[] pixels = new Color32[pixelCount];
            HbpCoreStatus status;
            GCHandle pixelsHandle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
            try
            {
                status = overlay ? hbp_cut_generator_copy_overlay_rgba8(_handle.Handle, pixelsHandle.AddrOfPinnedObject(), pixels.Length) : hbp_cut_generator_copy_base_rgba8(_handle.Handle, pixelsHandle.AddrOfPinnedObject(), pixels.Length);
            }
            finally
            {
                pixelsHandle.Free();
            }

            ThrowIfFailed(status);
            return pixels;
        }

        private static void ThrowIfFailed(HbpCoreStatus status)
        {
            if (status != HbpCoreStatus.Ok)
            {
                throw new InvalidOperationException($"hbp_core CutGenerator call failed with status {status}: {HbpCoreRuntime.LastError}");
            }
        }

        #region DLLImport

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_cut_generator_create", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_generator_create(out IntPtr generator);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_cut_generator_destroy", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_generator_destroy(IntPtr generator);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_cut_generator_initialize", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_generator_initialize(IntPtr generator, IntPtr activityGenerator, IntPtr geometryGenerator, int blurFactor);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_cut_generator_set_mask_activity_on_mri_background", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_generator_set_mask_activity_on_mri_background(IntPtr generator, int enabled);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_cut_generator_fill_volume_rgba", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_generator_fill_volume_rgba(IntPtr generator, [In] Color4[] colorScheme, int colorCount, float calMin, float calMax);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_cut_generator_fill_volume_rgba8", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_generator_fill_volume_rgba8(IntPtr generator, IntPtr colorScheme, int colorCount, float calMin, float calMax);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_cut_generator_fill_atlas_rgba", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_generator_fill_atlas_rgba(IntPtr generator, IntPtr atlas, float alpha, int selectedArea);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_cut_generator_fill_activity_rgba", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_generator_fill_activity_rgba(IntPtr generator, [In] Color4[] colorScheme, int colorCount, int timelineIndex, float alpha);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_cut_generator_fill_activity_rgba8", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_generator_fill_activity_rgba8(IntPtr generator, IntPtr colorScheme, int colorCount, int timelineIndex, float alpha);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_cut_generator_fill_fmri_rgba", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_generator_fill_fmri_rgba(IntPtr generator, IntPtr volume, float negativeMin, float negativeMax, float positiveMin, float positiveMax, float alpha);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_cut_generator_fill_localizer_rgba", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_generator_fill_localizer_rgba(IntPtr generator, IntPtr volume, IntPtr mask, float minValue, float middleValue, float maxValue, [In] Color4[] colorScheme, int colorCount);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_cut_generator_copy_base_rgba", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_generator_copy_base_rgba(IntPtr generator, [Out] Color4[] colors, int colorCapacity);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_cut_generator_copy_overlay_rgba", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_generator_copy_overlay_rgba(IntPtr generator, [Out] Color4[] colors, int colorCapacity);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_cut_generator_copy_base_rgba8", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_generator_copy_base_rgba8(IntPtr generator, IntPtr colors, int pixelCapacity);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_cut_generator_copy_overlay_rgba8", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_generator_copy_overlay_rgba8(IntPtr generator, IntPtr colors, int pixelCapacity);

        #endregion
    }
}
