
/**
 * \file    Texture.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define TextureColors and DLL.Texture classes
 */

// system
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// unity
using UnityEngine;

namespace HBP.Module3D
{
    /// <summary>
    /// Predefined colors
    /// </summary>
    public class PredefinedColors
    {
        public static Color Red { get { return new Color(1, 0, 0); } }
        public static Color Blue { get { return new Color(0, 0, 1); } }
        public static Color DarkRed { get { return new Color(157 / 255, 0, 0); } }
        public static Color VeryDarkBlue { get { return new Color(29 / 255, 35 / 255, 109 / 255); } }
        public static Color DarkBlue { get { return new Color(0, 0, 0.52f); } }
        public static Color CoolBlue { get { return new Color(0, 0.5f, 1); } }
        public static Color CoolLightBlue { get { return new Color(0, 1, 1); } }
        public static Color Green { get { return new Color(0, 1, 0); } }
        public static Color DarkGreen { get { return new Color(0.074f, 0.54f, 0.11f); } }
        public static Color White { get { return new Color(1, 1, 1); } }
        public static Color Black { get { return new Color(0, 0, 0); } }
        public static Color Yellow { get { return new Color(1, 1, 0); } }
        public static Color Orange { get { return new Color(1, 0.64f, 0); } }
        public static Color Pink { get { return new Color(0.78f, 0.41f, 0.41f); } }
        public static Color LightGreen { get { return new Color(0, 1, 0.5f); } }
    }

    /// <summary>
    /// Class for generating textures for UI parts
    /// </summary>
    public class UITextureGenerator
    {
        static public Texture2D GenerateSliderBackgroundTexture(int posMin, int posMax, int posStart, int posEnd, int timelineSize, int positionMainEvent, List<int> secondaryEventsPosition)
        {
            Color noDataColor = Color.gray;
            Color dataColor = Color.white;
            Color interTimeColor = Color.black;

            int size = 1000; // length of the 1D texture
            if (size % (timelineSize - 1) != 0)
            {
                size += (timelineSize - 1) - (size % (timelineSize - 1));
            }

            int offsetT = size / (timelineSize - 1);
            Texture2D texture = new Texture2D(size, 1);
            Color[] cols = texture.GetPixels();

            bool fistBlack = true;
            for (int ii = 0; ii < texture.width; ++ii)
            {
                if ((1f * ii / texture.width) < (1f * posStart / (posMax - posMin)))
                {
                    cols[ii] = noDataColor;
                }
                else if ((1f * ii / texture.width) > (1f * posEnd / (posMax - posMin)))
                {
                    cols[ii] = noDataColor;
                }
                else
                {
                    cols[ii] = dataColor;
                }

                if (ii % offsetT == 0)
                {
                    if (fistBlack)
                    {
                        fistBlack = false;
                        continue;
                    }
                    cols[ii] = interTimeColor;
                }
            }

            texture.SetPixels(cols);
            texture.Apply();

            return texture;
        }
    }

    /// <summary>
    /// Some utility funcctions Texture2D
    /// </summary>
    public class Texture2Dutility
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="mipMapBias"></param>
        /// <param name="anisoLvl"></param>
        /// <param name="filter"></param>
        /// <param name="wrap"></param>
        /// <returns></returns>
        private static Texture2D Generate(int width = 1, int height = 1, float mipMapBias = -10f, int anisoLvl = 9, FilterMode filter = FilterMode.Trilinear, TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            Texture2D tex = new Texture2D(width, height);
            tex.wrapMode = wrap;
            tex.filterMode = filter;

            if (mipMapBias > -10)
                tex.mipMapBias = mipMapBias;

            tex.anisoLevel = anisoLvl;
            return tex;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Texture2D GenerateGUI(int width = 1, int height = 1)
        {
            return Generate(width, height, -10, 9, FilterMode.Point); // ... specify custome parameters for gui texture
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Texture2D GenerateCut(int width = 1, int height = 1)
        {
            return Generate(width, height); // ... specify custome parameters for cut texture
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Texture2D GenerateColorScheme(int width = 1, int height = 1)
        {
            return Generate(width, height); // ... specify custome parameters for color scheme texture
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Texture2D GenerateHistogram(int width = 1, int height = 1)
        {
            return Generate(width, height, -10, 9, FilterMode.Point); // ... specify custome parameters for histogram texture
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Texture2D GenerateIcon(int width = 1, int height = 1)
        {
            return Generate(width, height, -10, 0, FilterMode.Point, TextureWrapMode.Repeat); // ... specify custome parameters for icone texture
        }        
    }


    namespace DLL
    {
        /// <summary>
        /// A DLL texture, based on opencv. 
        /// </summary>
        public class Texture : CppDLLImportBase, ICloneable
        {
            #region Properties
            private bool m_IsPinned = false;

            public int[] TextureSize = new int[2]; /**< size of the texure */

            private Color32[] Pixels2 = new Color32[1];
            GCHandle pixelsHandle2;
            #endregion

            #region Public Methods
            /// <summary>
            /// 
            /// </summary>
            /// <param name="other"></param>
            public Texture(Texture other) : base(clone_Texture(other.getHandle()))
            {
                UpdateSizes();
            }
            /// <summary>
            /// 
            /// </summary>
            ~Texture()
            {
                pixelsHandle2.Free();
            }
            /// <summary>
            /// Init a texture by loading an image.
            /// </summary>
            /// <param name="path"></param>
            /// <returns></returns>
            public static Texture Load(string pathTextureFile)
            {
                return new Texture(load_Texture(pathTextureFile));
            }
            /// <summary>
            /// Copy the input texture, apply a rotation on it and update the current texture
            /// </summary>
            /// <param name="texture"></param>
            /// <param name="orientation"></param>
            /// <param name="flip"></param>
            public void CopyAndRotate(Texture texture, string orientation, bool flip, bool displayCutLines, int indexCut, List<Cut> cutPlanes, MRITextureCutGenerator generator)
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
                UpdateSizes();
            }
            /// <summary>
            /// Resize a texture to a square of size * size
            /// </summary>
            /// <param name="size"></param>
            public void ResizeToSquare(int size)
            {
                resize_to_square_Texture(_handle, size);
                UpdateSizes();
            }
            /// <summary>
            /// Save the texture to a PNG file
            /// </summary>
            /// <param name="path"></param>
            /// <returns></returns>
            public bool SaveToPNG(string path)
            {
                return (save_to_png_Texture(_handle, path) == 1);
            }
            /// <summary>
            /// Update the input Texture2D with the DLL Texture
            /// </summary>
            /// <param name="texture"></param>
            public void UpdateTexture2D(Texture2D texture, bool forcePinned = false)
            {
                bool nullDLLTexture = TextureSize[1] == 0 || TextureSize[0] == 0;
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

                if (texture.width != TextureSize[1] || texture.height != TextureSize[0] || forcePinned || !m_IsPinned)
                {
                    texture.Resize(TextureSize[1], TextureSize[0]);
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
            /// Generate a texture of the values distribution histogram of the volume
            /// </summary>
            /// <param name="volume"></param>
            /// <param name="height"></param>
            /// <param name="width"></param>
            /// <param name="minCoeff"></param>
            /// <param name="maxCoeff"></param>
            /// <returns></returns>
            public static Texture GenerateDistributionHistogram(Volume volume, int height, int width, float minCoeff, float maxCoeff, float middle = -1f)
            {
                return new Texture(generate_distribution_histogram_Texture(volume.getHandle(), height, width, minCoeff, maxCoeff, middle));
            }
            /// <summary>
            /// Generate a texture of the values distribution histogram of the input data array
            /// </summary>
            /// <param name="data"></param>
            /// <param name="height"></param>
            /// <param name="width"></param>
            /// <param name="minCoeff"></param>
            /// <param name="maxCoeff"></param>
            /// <param name="middle"></param>
            /// <returns></returns>
            public static Texture GenerateDistributionHistogram(float[] data, int height, int width, float min = 0, float max = 0)
            {
                return new Texture(generate_distribution_histogram_with_data_Texture(data, data.Length, height, width, min, max));
            }
            /// <summary>
            /// Update the size array with the DLL data
            /// </summary>
            public void UpdateSizes()
            {
                get_size_Texture(_handle, TextureSize);
            }
            /// <summary>
            /// Generate a one dimension texture corresponding to the input id, see DLL for details
            /// </summary>
            /// <param name="idColor"></param>
            /// <returns></returns>
            public static Texture Generate1DColorTexture(Data.Enums.ColorType color)
            {
                return new Texture(generate_1D_color_Texture((int)color));
            }
            /// <summary>
            ///  Generate a two dimensions texture corresponding to the input IDs, see DLL for details
            /// </summary>
            /// <param name="idColor1"></param>
            /// <param name="idColor2"></param>
            /// <returns></returns>
            public static Texture Generate2DColorTexture(Data.Enums.ColorType color1, Data.Enums.ColorType color2)
            {
                return new Texture(generate_2D_color_Texture((int)color1, (int)color2));
            }
            #endregion

            #region Memory Management
            /// <summary>
            /// Texture default constructor
            /// </summary>
            public Texture() : base() { }
            /// <summary>
            /// Texture constructor with an already allocated dll texture
            /// </summary>
            /// <param name="surfaceHandle"></param>
            public Texture(IntPtr texturePtr) : base(texturePtr)
            {
                get_size_Texture( _handle, TextureSize);
            }
            /// <summary>
            /// Clone the surface
            /// </summary>
            /// <returns></returns>
            public object Clone()
            {
                Texture clone = new Texture(this);
                clone.UpdateSizes();
                return clone;
            }
            /// <summary>
            /// Allocate DLL memory
            /// </summary>
            protected override void create_DLL_class()
            {
                _handle = new HandleRef(this,create_Texture());
                get_size_Texture(_handle, TextureSize);
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

            // memory management          
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

            // retrieve data
            [DllImport("hbp_export", EntryPoint = "get_data_Texture", CallingConvention = CallingConvention.Cdecl)]
            static private extern void get_data_Texture(HandleRef handleTexture, float[] rgb);
            [DllImport("hbp_export", EntryPoint = "get_size_Texture", CallingConvention = CallingConvention.Cdecl)]
            static private extern void get_size_Texture(HandleRef handleTexture, int[] size);
            [DllImport("hbp_export", EntryPoint = "update_Texture", CallingConvention = CallingConvention.Cdecl)]
            static private extern void update_Texture(HandleRef handleTexture, IntPtr colors, int alpha);

            // actions
            [DllImport("hbp_export", EntryPoint = "generate_distribution_histogram_Texture", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr generate_distribution_histogram_Texture(HandleRef handleVolume, int height, int width, float minCoeff, float maxCoeff, float middle);
            [DllImport("hbp_export", EntryPoint = "generate_distribution_histogram_with_data_Texture", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr generate_distribution_histogram_with_data_Texture(float[] data, int dataSize, int height, int width, float min, float max);

            [DllImport("hbp_export", EntryPoint = "apply_blur_Texture", CallingConvention = CallingConvention.Cdecl)]
            static private extern void apply_blur_Texture(HandleRef handleTexture);
            [DllImport("hbp_export", EntryPoint = "rotate_with_cut_plane_Texture", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr rotate_with_cut_plane_Texture(HandleRef handleTexture, string orientationStr, int flip);
            [DllImport("hbp_export", EntryPoint = "copy_from_and_rotate_Texture", CallingConvention = CallingConvention.Cdecl)]
            static private extern void copy_from_and_rotate_Texture(HandleRef handleTexture, HandleRef handleTextureToCopyAndRotate, string orientationStr, int flip,
                int displayLines, int nbPlanes, float[] planes, HandleRef MRIGenerator);
            [DllImport("hbp_export", EntryPoint = "resize_to_square_Texture", CallingConvention = CallingConvention.Cdecl)]
            static private extern void resize_to_square_Texture(HandleRef handleTexture, int size);

            [DllImport("hbp_export", EntryPoint = "generate_1D_color_Texture", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr generate_1D_color_Texture(int idColor);

            [DllImport("hbp_export", EntryPoint = "generate_2D_color_Texture", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr generate_2D_color_Texture(int idColor1, int idColor2);


            #endregion
        }
    }
}