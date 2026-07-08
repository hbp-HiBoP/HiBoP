using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Core.DLL
{
    /// <summary>
    /// Temporary legacy texture bridge for hbp_export generator APIs.
    /// </summary>
    /// <remarks>
    /// Rendering responsibilities moved to Unity in step 10. Keep this wrapper
    /// limited to native cut outputs, histogram fallbacks and pixel upload to
    /// legacy hbp_export functions until the remaining generators are migrated.
    /// </remarks>
    public class Texture : CppDLLImportBase
    {
        #region Properties
        /// <summary>
        /// Is the array of pixels pinned ?
        /// </summary>
        private bool m_IsPinned = false;
        /// <summary>
        /// Width of the texture
        /// </summary>
        public int Width
        {
            get
            {
                return get_width_Texture(_handle);
            }
        }
        /// <summary>
        /// Height of the texture
        /// </summary>
        public int Height
        {
            get
            {
                return get_height_Texture(_handle);
            }
        }
        /// <summary>
        /// Array of pixels of the texture
        /// </summary>
        private Color32[] Pixels2 = new Color32[1];
        private byte[] m_RgbUploadBuffer = Array.Empty<byte>();
        private Color32[] m_ManagedPixels = Array.Empty<Color32>();

        /// <summary>
        /// Handle of the pixels array
        /// </summary>
        GCHandle pixelsHandle2;
        #endregion

        #region Public Methods
        /// <summary>
        /// Update the input Unity texture with this DLL texture
        /// </summary>
        /// <param name="texture">Texture to be updated</param>
        /// <param name="forcePinned">Do we force the pixels array to be pinned ?</param>
        public void UpdateTexture2D(Texture2D texture, bool forcePinned = false)
        {
            bool nullDLLTexture = Width == 0 || Height == 0;
            if (nullDLLTexture)
            {
                texture.Reinitialize(10, 10);
                Pixels2 = texture.GetPixels32(0);
                for (int ii = 0; ii < Pixels2.Length; ++ii)
                    Pixels2[ii] = new Color32(0, 0, 0, 255);
                texture.SetPixels32(Pixels2, 0);
                texture.Apply();
                return;
            }

            if (texture.width != Width || texture.height != Height || forcePinned || !m_IsPinned)
            {
                texture.Reinitialize(Width, Height);
                Pixels2 = texture.GetPixels32(0);
                if (pixelsHandle2.IsAllocated) pixelsHandle2.Free();
                pixelsHandle2 = GCHandle.Alloc(Pixels2, GCHandleType.Pinned);
                m_IsPinned = true;
            }

            update_Texture(_handle, pixelsHandle2.AddrOfPinnedObject(), 255);
            texture.SetPixels32(Pixels2, 0);
            texture.Apply();
        }
        /// <summary>
        /// Apply the texture2D to this texture
        /// </summary>
        /// <param name="texture">Texture2D to apply</param>
        public void FromTexture2D(Texture2D texture)
        {
            if (texture == null) throw new ArgumentNullException(nameof(texture));

            FromPixels(texture.GetPixels32(), texture.width, texture.height);
        }

        public void FromPixels(Color32[] colors, int width, int height)
        {
            if (colors == null) throw new ArgumentNullException(nameof(colors));
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (colors.Length < width * height) throw new ArgumentException("Pixel buffer is smaller than width * height.", nameof(colors));

            if (Width != width || Height != height)
            {
                Reset(width, height);
            }

            m_ManagedPixels = new Color32[width * height];
            Array.Copy(colors, m_ManagedPixels, m_ManagedPixels.Length);

            int pixelCount = width * height;
            int byteCount = pixelCount * 3;
            if (m_RgbUploadBuffer.Length != byteCount)
            {
                m_RgbUploadBuffer = new byte[byteCount];
            }

            for (int i = 0; i < pixelCount; i++)
            {
                Color32 col = colors[i];
                m_RgbUploadBuffer[3 * i] = col.r;
                m_RgbUploadBuffer[3 * i + 1] = col.g;
                m_RgbUploadBuffer[3 * i + 2] = col.b;
            }
            set_colors_Texture(_handle, m_RgbUploadBuffer, pixelCount);
        }
        public Color32[] GetManagedPixels()
        {
            if (m_ManagedPixels == null || m_ManagedPixels.Length == 0)
            {
                return Array.Empty<Color32>();
            }

            Color32[] result = new Color32[m_ManagedPixels.Length];
            Array.Copy(m_ManagedPixels, result, result.Length);
            return result;
        }
        public static Texture CreateFromPixels(Color32[] colors, int width, int height)
        {
            Texture texture = new();
            texture.FromPixels(colors, width, height);
            return texture;
        }
        public void Reset(int width, int height)
        {
            reset_Texture(_handle, width, height);
        }
        /// <summary>
        /// Generate a texture representing the values of the voxels of the input volume as a histogram
        /// </summary>
        /// <param name="volume">Volume to get values from</param>
        /// <param name="height">Height of the resulting texture</param>
        /// <param name="width">Width of the resulting texture</param>
        /// <returns>Newly created texture</returns>
        public static Texture GenerateDistributionHistogram(Volume volume, int height, int width, bool withGreyArea = true)
        {
            return new Texture(generate_distribution_histogram_Texture(volume.getHandle(), height, width, withGreyArea));
        }
        /// <summary>
        /// Generate a texture representing the values of the voxels of the input volume as a histogram
        /// </summary>
        /// <param name="nifti">FMRI to get values from</param>
        /// <param name="height">Height of the resulting texture</param>
        /// <param name="width">Width of the resulting texture</param>
        /// <returns>Newly created texture</returns>
        public static Texture GenerateDistributionHistogram(NIFTI nifti, int height, int width, bool withGreyArea = true)
        {
            return new Texture(generate_distribution_histogram_NIFTI_Texture(nifti.getHandle(), height, width, withGreyArea));
        }
        #endregion

        #region Memory Management
        public Texture() : base() { }
        public Texture(IntPtr texturePtr) : base(texturePtr)
        {
        }
        ~Texture()
        {
            if (pixelsHandle2.IsAllocated) pixelsHandle2.Free();
        }
        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void create_DLL_class()
        {
            _handle = new HandleRef(this,create_Texture());
        }
        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
            delete_Texture(_handle);
        }
        #endregion

        #region DLLImport    
        [DllImport("hbp_export", EntryPoint = "create_Texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_Texture();
        [DllImport("hbp_export", EntryPoint = "delete_Texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_Texture(HandleRef handleTexture);
        [DllImport("hbp_export", EntryPoint = "get_width_Texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern int get_width_Texture(HandleRef handleTexture);
        [DllImport("hbp_export", EntryPoint = "get_height_Texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern int get_height_Texture(HandleRef handleTexture);
        [DllImport("hbp_export", EntryPoint = "update_Texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern void update_Texture(HandleRef handleTexture, IntPtr colors, int alpha);
        [DllImport("hbp_export", EntryPoint = "set_colors_Texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern void set_colors_Texture(HandleRef handleTexture, byte[] colors, int length);
        [DllImport("hbp_export", EntryPoint = "reset_Texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern void reset_Texture(HandleRef handleTexture, int width, int height);
        [DllImport("hbp_export", EntryPoint = "generate_distribution_histogram_Texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr generate_distribution_histogram_Texture(HandleRef handleVolume, int height, int width, bool withGreyArea);
        [DllImport("hbp_export", EntryPoint = "generate_distribution_histogram_NIFTI_Texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr generate_distribution_histogram_NIFTI_Texture(HandleRef handleNifti, int height, int width, bool withGreyArea);
        #endregion
    }
}
