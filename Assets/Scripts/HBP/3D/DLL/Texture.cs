using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Core.DLL
{
    /// <summary>
    /// This class represents a texture as stored within the DLL
    /// </summary>
    /// <remarks>
    /// Usually, these textures are generated using OpenCV
    /// </remarks>
    public class Texture : CppDLLImportBase, ICloneable
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

        /// <summary>
        /// Handle of the pixels array
        /// </summary>
        GCHandle pixelsHandle2;
        #endregion

        #region Public Methods
        /// <summary>
        /// Create a texture using a image file
        /// </summary>
        /// <param name="pathTextureFile">Path to the file to load</param>
        /// <returns>Newly created texture from the file</returns>
        public static Texture Load(string pathTextureFile)
        {
            return new Texture(load_Texture(pathTextureFile));
        }
        /// <summary>
        /// Clone the input texture and rotate it depending on parameters (used to rotate the cut textures in a more convenient way)
        /// </summary>
        /// <param name="texture">Texture to be cloned</param>
        /// <param name="orientation">Orientation of the cut</param>
        /// <param name="flip">Is the cut flipped ?</param>
        /// <param name="displayCutLines">Do we display the other cuts as lines on the texture ?</param>
        /// <param name="indexCut">Index of the cut of this texture</param>
        /// <param name="cutPlanes">List of all cuts in the scene</param>
        /// <param name="generator">Texture generator of the considered cut</param>
        public void CloneAndRotate(Texture texture, string orientation, bool flip, bool displayCutLines, int indexCut, List<Object3D.Cut> cutPlanes, CutGenerator generator)
        {
            // init plane
            int nbPlanes = cutPlanes.Count - 1;
            float[] planes = new float[nbPlanes * 6];

            int currCut = 0;
            for (int ii = 0; ii < cutPlanes.Count; ++ii)
            {
                if (ii == indexCut)
                    continue;

                for (int jj = 0; jj < 3; ++jj)
                {
                    planes[currCut * 6 + jj] = cutPlanes[ii].Point[jj];
                    planes[currCut * 6 + jj + 3] = cutPlanes[ii].Normal[jj];

                }

                ++currCut;
            }
            copy_from_and_rotate_Texture(_handle, texture.getHandle(), orientation, flip ? 1 : 0, displayCutLines ? 1 : 0, nbPlanes, planes, generator.getHandle());
        }
        /// <summary>
        /// Resize the texture to a square using black borders
        /// </summary>
        /// <param name="size">Maximum length of a side of the square</param>
        public void ResizeToSquare(int size)
        {
            resize_to_square_Texture(_handle, size);
        }
        /// <summary>
        /// Display sites on the texture as red pixels
        /// </summary>
        public void DrawSites(Object3D.Cut cut, RawSiteList rawList, float precision, CutGenerator generator)
        {
            float[] plane = new float[6];
            plane[0] = cut.Point.x;
            plane[1] = cut.Point.y;
            plane[2] = cut.Point.z;
            plane[3] = cut.Normal.x;
            plane[4] = cut.Normal.y;
            plane[5] = cut.Normal.z;
            draw_sites_Texture(_handle, plane, rawList.getHandle(), precision, generator.getHandle());
        }
        /// <summary>
        /// Save the texture to a PNG file
        /// </summary>
        /// <param name="path">Path to the saved file</param>
        /// <returns>True if the file could be saved</returns>
        public bool SaveToPNG(string path)
        {
            return (save_to_png_Texture(_handle, path) == 1);
        }
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
                texture.Resize(10, 10);
                Pixels2 = texture.GetPixels32(0);
                for (int ii = 0; ii < Pixels2.Length; ++ii)
                    Pixels2[ii] = new Color32(0, 0, 0, 255);
                texture.SetPixels32(Pixels2, 0);
                texture.Apply();
                return;
            }

            if (texture.width != Width || texture.height != Height || forcePinned || !m_IsPinned)
            {
                texture.Resize(Width, Height);
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
            Color32[] colors = texture.GetPixels32();
            byte[] cols = new byte[colors.Length * 3];
            for (int i = 0; i < colors.Length; i++)
            {
                Color32 col = colors[i];
                cols[3 * i] = col.r;
                cols[3 * i + 1] = col.g;
                cols[3 * i + 2] = col.b;
            }
            set_colors_Texture(_handle, cols, colors.Length);
        }
        /// <summary>
        /// Write text to this texture
        /// </summary>
        /// <param name="text">Text to be written</param>
        /// <param name="x">X position of the text</param>
        /// <param name="y">Y position of the text</param>
        public void WriteText(string text, int x, int y)
        {
            write_text_Texture(_handle, text, x, y);
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
        /// <param name="fmri">FMRI to get values from</param>
        /// <param name="height">Height of the resulting texture</param>
        /// <param name="width">Width of the resulting texture</param>
        /// <returns>Newly created texture</returns>
        public static Texture GenerateDistributionHistogram(Object3D.FMRI fmri, int height, int width, bool withGreyArea = true)
        {
            return new Texture(generate_distribution_histogram_NIFTI_Texture(fmri.NIFTI.getHandle(), height, width, withGreyArea));
        }
        /// <summary>
        /// Generate a texture representing the input set of values as a histogram
        /// </summary>
        /// <param name="data">Input data array to be represented in a histogram</param>
        /// <param name="height">Height of the resulting texture</param>
        /// <param name="width">Width of the resulting texture</param>
        /// <param name="min">Minimum value of the histogram</param>
        /// <param name="max">Maximum value of the histogram</param>
        /// <returns>Newly created texture</returns>
        public static Texture GenerateDistributionHistogram(float[] data, int height, int width, float min = 0, float max = 0)
        {
            return new Texture(generate_distribution_histogram_with_data_Texture(data, data.Length, height, width, min, max));
        }
        /// <summary>
        /// Generate a 1D texture depending of the chosen type of colormap
        /// </summary>
        /// <param name="color">Chosen type of colormap</param>
        /// <returns>Newly created texture</returns>
        public static Texture Generate1DColorTexture(Data.Enums.ColorType color)
        {
            return new Texture(generate_1D_color_Texture((int)color));
        }
        /// <summary>
        /// Generate a 2D texture depending of the chosen types of colormap
        /// </summary>
        /// <param name="color1">Chosen type of colormap for the columns</param>
        /// <param name="color2">Chosen type of colormap for the rows</param>
        /// <returns>Newly created texture</returns>
        public static Texture Generate2DColorTexture(Data.Enums.ColorType color1, Data.Enums.ColorType color2)
        {
            return new Texture(generate_2D_color_Texture((int)color1, (int)color2));
        }
        #endregion

        #region Memory Management
        public Texture() : base() { }
        public Texture(IntPtr texturePtr) : base(texturePtr)
        {
        }
        public Texture(Texture other) : base(clone_Texture(other.getHandle()))
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
        public object Clone()
        {
            Texture clone = new Texture(this);
            return clone;
        }
        #endregion

        #region DLLImport    
        [DllImport("hbp_export", EntryPoint = "create_Texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_Texture();
        [DllImport("hbp_export", EntryPoint = "clone_Texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr clone_Texture(HandleRef handleTexture);
        [DllImport("hbp_export", EntryPoint = "load_Texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr load_Texture(string path);
        [DllImport("hbp_export", EntryPoint = "delete_Texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_Texture(HandleRef handleTexture);
        [DllImport("hbp_export", EntryPoint = "save_to_png_Texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern int save_to_png_Texture(HandleRef handleTexture, string path);
        [DllImport("hbp_export", EntryPoint = "get_data_Texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern void get_data_Texture(HandleRef handleTexture, float[] rgb);
        [DllImport("hbp_export", EntryPoint = "get_width_Texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern int get_width_Texture(HandleRef handleTexture);
        [DllImport("hbp_export", EntryPoint = "get_height_Texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern int get_height_Texture(HandleRef handleTexture);
        [DllImport("hbp_export", EntryPoint = "update_Texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern void update_Texture(HandleRef handleTexture, IntPtr colors, int alpha);
        [DllImport("hbp_export", EntryPoint = "set_colors_Texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern void set_colors_Texture(HandleRef handleTexture, byte[] colors, int length);
        [DllImport("hbp_export", EntryPoint = "write_text_Texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern void write_text_Texture(HandleRef handleTexture, string text, int x, int y);
        [DllImport("hbp_export", EntryPoint = "reset_Texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern void reset_Texture(HandleRef handleTexture, int width, int height);
        [DllImport("hbp_export", EntryPoint = "generate_distribution_histogram_Texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr generate_distribution_histogram_Texture(HandleRef handleVolume, int height, int width, bool withGreyArea);
        [DllImport("hbp_export", EntryPoint = "generate_distribution_histogram_NIFTI_Texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr generate_distribution_histogram_NIFTI_Texture(HandleRef handleNifti, int height, int width, bool withGreyArea);
        [DllImport("hbp_export", EntryPoint = "generate_distribution_histogram_with_data_Texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr generate_distribution_histogram_with_data_Texture(float[] data, int dataSize, int height, int width, float min, float max);
        [DllImport("hbp_export", EntryPoint = "apply_blur_Texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern void apply_blur_Texture(HandleRef handleTexture);
        [DllImport("hbp_export", EntryPoint = "rotate_with_cut_plane_Texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr rotate_with_cut_plane_Texture(HandleRef handleTexture, string orientationStr, int flip);
        [DllImport("hbp_export", EntryPoint = "copy_from_and_rotate_Texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern void copy_from_and_rotate_Texture(HandleRef handleTexture, HandleRef handleTextureToCopyAndRotate, string orientationStr, int flip, int displayLines, int nbPlanes, float[] planes, HandleRef MRIGenerator);
        [DllImport("hbp_export", EntryPoint = "resize_to_square_Texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern void resize_to_square_Texture(HandleRef handleTexture, int size);
        [DllImport("hbp_export", EntryPoint = "draw_sites_Texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern void draw_sites_Texture(HandleRef handleTexture, float[] planeArray, HandleRef rawListHandle, float precision, HandleRef generatorHandle);
        [DllImport("hbp_export", EntryPoint = "generate_1D_color_Texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr generate_1D_color_Texture(int idColor);
        [DllImport("hbp_export", EntryPoint = "generate_2D_color_Texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr generate_2D_color_Texture(int idColor1, int idColor2);
        #endregion
    }
}