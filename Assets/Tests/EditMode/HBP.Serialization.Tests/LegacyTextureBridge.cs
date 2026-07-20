using System;
using System.Runtime.InteropServices;
using HBP.Core.DLL;
using HBP.Core.Enums;
using UnityEngine;

namespace HBP.Tests.Serialization
{
    internal sealed class LegacyTextureBridge : IDisposable
    {
        public IntPtr Handle { get; private set; }

        public LegacyTextureBridge()
        {
            Handle = create_Texture();
        }

        private LegacyTextureBridge(IntPtr handle)
        {
            Handle = handle;
        }

        public static LegacyTextureBridge Generate1D(int colorType)
        {
            return new LegacyTextureBridge(generate_1D_color_Texture(colorType));
        }

        public static LegacyTextureBridge Generate2D(int horizontalColorType, int verticalColorType)
        {
            return new LegacyTextureBridge(generate_2D_color_Texture(horizontalColorType, verticalColorType));
        }

        public static LegacyTextureBridge GenerateHistogram(LegacyNative.NIFTI nifti, int height, int width, bool withGreyArea)
        {
            return new LegacyTextureBridge(generate_distribution_histogram_NIFTI_Texture(
                nifti.getHandle().Handle, height, width, withGreyArea));
        }

        public void ApplyBlur()
        {
            apply_blur_Texture(Handle);
        }

        public LegacyTextureBridge Rotate(CutOrientation orientation, bool flip)
        {
            return new LegacyTextureBridge(rotate_with_cut_plane_Texture(
                Handle, orientation.ToString(), flip ? 1 : 0));
        }

        public void ResizeToSquare(int size)
        {
            resize_to_square_Texture(Handle, size);
        }

        public static LegacyTextureBridge CreateFromPixels(Color32[] pixels, int width, int height)
        {
            LegacyTextureBridge texture = new();
            texture.Reset(width, height);
            byte[] rgb = new byte[pixels.Length * 3];
            for (int i = 0; i < pixels.Length; i++)
            {
                rgb[3 * i] = pixels[i].r;
                rgb[3 * i + 1] = pixels[i].g;
                rgb[3 * i + 2] = pixels[i].b;
            }
            set_colors_Texture(texture.Handle, rgb, pixels.Length);
            return texture;
        }

        public Color32[] GetPixels(out int width, out int height)
        {
            width = get_width_Texture(Handle);
            height = get_height_Texture(Handle);
            Color32[] pixels = new Color32[width * height];
            GCHandle handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
            try
            {
                update_Texture(Handle, handle.AddrOfPinnedObject(), 255);
            }
            finally
            {
                handle.Free();
            }
            return pixels;
        }

        public void Dispose()
        {
            if (Handle == IntPtr.Zero)
            {
                return;
            }

            delete_Texture(Handle);
            Handle = IntPtr.Zero;
        }

        private void Reset(int width, int height)
        {
            reset_Texture(Handle, width, height);
        }

        [DllImport("hbp_export", EntryPoint = "create_Texture", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create_Texture();

        [DllImport("hbp_export", EntryPoint = "delete_Texture", CallingConvention = CallingConvention.Cdecl)]
        private static extern void delete_Texture(IntPtr texture);

        [DllImport("hbp_export", EntryPoint = "get_width_Texture", CallingConvention = CallingConvention.Cdecl)]
        private static extern int get_width_Texture(IntPtr texture);

        [DllImport("hbp_export", EntryPoint = "get_height_Texture", CallingConvention = CallingConvention.Cdecl)]
        private static extern int get_height_Texture(IntPtr texture);

        [DllImport("hbp_export", EntryPoint = "update_Texture", CallingConvention = CallingConvention.Cdecl)]
        private static extern void update_Texture(IntPtr texture, IntPtr colors, int alpha);

        [DllImport("hbp_export", EntryPoint = "reset_Texture", CallingConvention = CallingConvention.Cdecl)]
        private static extern void reset_Texture(IntPtr texture, int width, int height);

        [DllImport("hbp_export", EntryPoint = "set_colors_Texture", CallingConvention = CallingConvention.Cdecl)]
        private static extern void set_colors_Texture(IntPtr texture, byte[] colors, int length);

        [DllImport("hbp_export", EntryPoint = "generate_1D_color_Texture", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr generate_1D_color_Texture(int colorType);

        [DllImport("hbp_export", EntryPoint = "generate_2D_color_Texture", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr generate_2D_color_Texture(int horizontalColorType, int verticalColorType);

        [DllImport("hbp_export", EntryPoint = "generate_distribution_histogram_NIFTI_Texture", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr generate_distribution_histogram_NIFTI_Texture(IntPtr nifti, int height, int width, bool withGreyArea);

        [DllImport("hbp_export", EntryPoint = "apply_blur_Texture", CallingConvention = CallingConvention.Cdecl)]
        private static extern void apply_blur_Texture(IntPtr texture);

        [DllImport("hbp_export", EntryPoint = "rotate_with_cut_plane_Texture", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr rotate_with_cut_plane_Texture(IntPtr texture, string orientation, int flip);

        [DllImport("hbp_export", EntryPoint = "resize_to_square_Texture", CallingConvention = CallingConvention.Cdecl)]
        private static extern void resize_to_square_Texture(IntPtr texture, int size);
    }
}
