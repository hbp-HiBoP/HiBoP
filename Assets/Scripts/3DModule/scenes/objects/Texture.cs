
/**
 * \file    Texture.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define TextureColors and DLL.Texture classes
 */

// system
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// unity
using UnityEngine;

namespace HBP.VISU3D
{
    /// <summary>
    /// Predefined colors
    /// </summary>
    public class TextureColors
    {
        public static Color red() { return new Color(1, 0, 0); }
        public static Color blue() { return new Color(0, 0, 1); }
        public static Color darkRed() { return new Color(157 / 255, 0, 0); }
        public static Color veryDarkBlue() { return new Color(29 / 255, 35 / 255, 109 / 255); }
        public static Color darkBlue() { return new Color(0, 0, 0.52f); }
        public static Color coolBlue() { return new Color(0, 0.5f, 1); }
        public static Color coolLightBlue() { return new Color(0, 1, 1); }
        public static Color green() { return new Color(0, 1, 0); }
        public static Color darkGreen() { return new Color(0.074f, 0.54f, 0.11f); }
        public static Color white() { return new Color(1, 1, 1); }
        public static Color black() { return new Color(0, 0, 0); }
        public static Color yellow() { return new Color(1, 1, 0); }
        public static Color orange() { return new Color(1, 0.64f, 0); }
        public static Color pink() { return new Color(0.78f, 0.41f, 0.41f); }
        public static Color lightGreen() { return new Color(0, 1, 0.5f); }
    }

    namespace DLL
    {
        /// <summary>
        /// A DLL texture, based on opencv. 
        /// </summary>
        public class Texture : CppDLLImportBase
        {
            #region members

            public int[] m_sizeTexture = new int[2]; /**< size of the texure */
            private float[] rgb = new float[0]; /**< raw rgb values of the texture */

           

            #endregion members

            #region functions

            /// <summary>
            /// Init a texture by loading an image.
            /// </summary>
            /// <param name="path"></param>
            /// <returns></returns>
            public static Texture load(string pathTextureFile)
            {
                return new Texture(load_Texture(pathTextureFile));
            }

            public bool saveToPng(string path)
            {
                return saveToPng_Texture(_handle, path);
            }



            //public void setTexture2D(Texture2D texture, float alpha = 1f)
            //{
            //    if (rgb.Length != (m_sizeTexture[0] * m_sizeTexture[1] * 3))
            //        rgb = new float[m_sizeTexture[0] * m_sizeTexture[1] * 3];

            //    getData_Texture(_handle, rgb);

            //    texture = new Texture2D(m_sizeTexture[1], m_sizeTexture[0]);

            //    //if (texture.width != m_sizeTexture[1] || texture.height != m_sizeTexture[0])
            //    //    texture.Resize(m_sizeTexture[1], m_sizeTexture[0]);

            //    if (texture.width == 0)
            //        return;

            //    Color[] cols = texture.GetPixels();


            //    Profiler.BeginSample("getTexture2D 2.2-------- ");

            //    for (int ii = 0; ii < texture.height; ++ii)
            //    {
            //        int id1 = 3 * ii * texture.width;
            //        int idC = (texture.height - 1 - ii) * texture.width;
            //        for (int jj = 0; jj < texture.width * 3; jj += 3, ++idC)
            //        {
            //            int id2 = id1 + jj;
            //            cols[idC].b = rgb[id2++];
            //            cols[idC].g = rgb[id2++];
            //            cols[idC].r = rgb[id2++];
            //            cols[idC].a = alpha;
            //        }
            //    }

            //    Profiler.EndSample();
            //    texture.SetPixels(cols);
            //    texture.Apply();
            //}


            //private static byte[] Color32ArrayToByteArray(Color32[] colors)
            //{
            //    if (colors == null || colors.Length == 0)
            //        return null;

            //    int lengthOfColor32 = Marshal.SizeOf(typeof(Color32));
            //    int length = lengthOfColor32 * colors.Length;
            //    byte[] bytes = new byte[length];

            //    GCHandle handle = default(GCHandle);
            //    try
            //    {
            //        handle = GCHandle.Alloc(colors, GCHandleType.Pinned);
            //        IntPtr ptr = handle.AddrOfPinnedObject();
            //        Marshal.Copy(ptr, bytes, 0, length);
            //    }
            //    finally
            //    {
            //        if (handle != default(GCHandle))
            //            handle.Free();
            //    }

            //    return bytes;
            //}


            /// <summary>
            /// Create a Texture2D from the DLL texture
            /// </summary>
            /// <returns></returns>
            public Texture2D getTexture2D(float alpha = 1f)
            {


                //if (rgb.Length != (m_sizeTexture[0] * m_sizeTexture[1] * 3))
                //    rgb = new float[m_sizeTexture[0] * m_sizeTexture[1] * 3];

                
                Profiler.BeginSample("getTexture2D 1 ");

                //getData_Texture(_handle, rgb);
                Profiler.EndSample();

                Profiler.BeginSample("getTexture2D 2 ");

                Texture2D texture = new Texture2D(m_sizeTexture[1], m_sizeTexture[0]);

                if (texture.width == 0)
                    return texture;


                // TEST
                Color32[] pixels = texture.GetPixels32(0);
                GCHandle pixelsHandle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
                update_Texture(_handle, pixelsHandle.AddrOfPinnedObject(), 255);
                pixelsHandle.Free();

                texture.SetPixels32(pixels, 0);
                texture.Apply();

                Profiler.EndSample();
                Profiler.BeginSample("getTexture2D 2.1 ");

                //Color[] cols = texture.GetPixels();

                Profiler.EndSample();
                Profiler.BeginSample("getTexture2D 2.2 ");


                //for (int ii = 0; ii < texture.height; ++ii)
                //{
                //    //int id1 = ii * texture.width;
                //    int id1 = 3 * ii * texture.width;
                //    int idC = (texture.height - 1 - ii) * texture.width;
                //    //for (int jj = 0; jj < texture.width; ++jj)
                //    for (int jj = 0; jj < texture.width * 3; jj += 3, ++idC)
                //    {
                //        int id2 = id1 + jj;// 3*jj;
                //        //int idPix = idC + jj;
                //        cols[idC].b = rgb[id2++];
                //        cols[idC].g = rgb[id2++];
                //        cols[idC].r = rgb[id2++];
                //        cols[idC].a = alpha;
                //    }
                //}


                Profiler.EndSample();
                Profiler.BeginSample("getTexture2D 3");
                //texture.SetPixels(cols);
                Profiler.EndSample();

                Profiler.BeginSample("getTexture2D 4 ");
                texture.Apply();

                Profiler.EndSample();

                return texture;
            }

            public void display()
            {
                if (rgb.Length != (m_sizeTexture[0] * m_sizeTexture[1] * 3))
                    rgb = new float[m_sizeTexture[0] * m_sizeTexture[1] * 3];

                getData_Texture(_handle, rgb);
                Debug.Log("rgb ! " + rgb.Length);
                for (int ii = 0; ii < rgb.Length; ++ii)
                {
                    if(ii % 1000 == 0)
                        Debug.Log(rgb[ii]);
                }
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
            public static Texture generateDistributionHistogram(Volume volume, int height, int width, float minCoeff, float maxCoeff, float middle = -1f)
            {
                return new Texture(generateDistributionHistogram_Texture(volume.getHandle(), height, width, minCoeff, maxCoeff, middle));
            }


            public static Texture generateDistributionHistogram(float[] data, int height, int width, float minCoeff, float maxCoeff, float middle = -1f)
            {
                //DLL.QtGUI.plotWidget;

                return new Texture(generateDistributionHistogramWithData_Texture(data, data.Length, height, width, minCoeff, maxCoeff, middle));
            }


            public static Texture generate1DColorScheme(int idColorScheme)
            {
                return new Texture(generate1DColorScheme_Texture(idColorScheme));
            }
            
            public static Texture generate1DColorScheme(List<Color> colors, float[] factors)
            {
                float[] colorsF = new float[colors.Count * 3];
                for (int ii = 0; ii < colors.Count; ++ii)
                {
                    colorsF[ii * 3] = colors[ii].b;
                    colorsF[ii * 3 + 1] = colors[ii].g;
                    colorsF[ii * 3 + 2] = colors[ii].r;
                }

                return new Texture(generate1DColorSchemeNoID_Texture(colors.Count, colorsF, factors));
            }

            public static Texture generateColorScheme(int idColorScheme, List<Color> influenceColors, float[] influenceFactors)
            {
                float[] leftInfluenceColorsF = new float[influenceColors.Count * 3];
                for (int ii = 0; ii < influenceColors.Count; ++ii)
                {
                    leftInfluenceColorsF[ii * 3] = influenceColors[ii].b;
                    leftInfluenceColorsF[ii * 3 + 1] = influenceColors[ii].g;
                    leftInfluenceColorsF[ii * 3 + 2] = influenceColors[ii].r;
                }

                return new Texture(generateColorScheme_Texture(idColorScheme, leftInfluenceColorsF, influenceFactors, influenceColors.Count));
            }

            public void applyBlur()
            {
                blurTexture(_handle);
            }

            public static Texture applyCorrespondingRotationToCut(Texture texture, string orientation, bool flip)
            {
                return new Texture(rotateTextureWithCutPlane(texture.getHandle(), orientation, flip));
            }

            #endregion functions

            #region memory_management

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
                getSize_Texture(_handle, m_sizeTexture);
            }

            /// <summary>
            /// Allocate DLL memory
            /// </summary>
            protected override void createDLLClass()
            {
                _handle = new HandleRef(this, IntPtr.Zero);
            }

            /// <summary>
            /// Clean DLL memory
            /// </summary>
            protected override void deleteDLLClass()
            {
                delete_Texture(_handle);
            }

            #endregion memory_management

            #region DLLImport

            // memory management
            [DllImport("hbp_export", EntryPoint = "load_Texture", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr load_Texture(string path);

            [DllImport("hbp_export", EntryPoint = "delete_Texture", CallingConvention = CallingConvention.Cdecl)]
            static private extern void delete_Texture(HandleRef handleTexture);

            [DllImport("hbp_export", EntryPoint = "saveToPng_Texture", CallingConvention = CallingConvention.Cdecl)]
            static private extern bool saveToPng_Texture(HandleRef handleTexture, string path);

            // retrieve data
            [DllImport("hbp_export", EntryPoint = "getData_Texture", CallingConvention = CallingConvention.Cdecl)]
            static private extern void getData_Texture(HandleRef handleTexture, float[] rgb);

            [DllImport("hbp_export", EntryPoint = "getSize_Texture", CallingConvention = CallingConvention.Cdecl)]
            static private extern void getSize_Texture(HandleRef handleTexture, int[] size);

            [DllImport("hbp_export", EntryPoint = "update_Texture", CallingConvention = CallingConvention.Cdecl)]
            private static extern void update_Texture(HandleRef handleTexture, IntPtr colors, int alpha);

            // actions
            [DllImport("hbp_export", EntryPoint = "generateDistributionHistogram_Texture", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr generateDistributionHistogram_Texture(HandleRef handleVolume, int height, int width, float minCoeff, float maxCoeff, float middle);

            [DllImport("hbp_export", EntryPoint = "generateDistributionHistogramWithData_Texture", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr generateDistributionHistogramWithData_Texture(float[] data,  int dataSize, int height, int width, float minCoeff, float maxCoeff, float middle);
            
            [DllImport("hbp_export", EntryPoint = "generateColorScheme_Texture", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr generateColorScheme_Texture(int idColorScheme, float[] leftInfluenceColors, float[] leftInfluenceFactors, int nbLeftInfluenceColors);

            [DllImport("hbp_export", EntryPoint = "generate1DColorScheme_Texture", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr generate1DColorScheme_Texture(int idColorScheme);

            [DllImport("hbp_export", EntryPoint = "generate1DColorSchemeNoID_Texture", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr generate1DColorSchemeNoID_Texture(int colorsNb, float[] colors, float[] factors);

            [DllImport("hbp_export", EntryPoint = "blurTexture", CallingConvention = CallingConvention.Cdecl)]
            static private extern void blurTexture(HandleRef handleTexture);

            [DllImport("hbp_export", EntryPoint = "rotateTextureWithCutPlane", CallingConvention = CallingConvention.Cdecl)]
            static private extern void rotateTextureWithCutPlane(HandleRef handleTexture, float[] plane);

            [DllImport("hbp_export", EntryPoint = "rotateTextureWithCutPlane", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr rotateTextureWithCutPlane(HandleRef handleTexture, string orientationStr, bool flip);


            #endregion DLLImport
        }
    }
}