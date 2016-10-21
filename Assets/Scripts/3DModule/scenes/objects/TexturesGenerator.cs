

/**
 * \file    TexturesGenerator.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define UITextureGenerator and DLL.BrainTextureGenerator classes
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
    /// 
    /// </summary>
    public class UITextureGenerator
    {
        static public Texture2D getSliderBackgroundTexture(int posMin, int posMax, int posStart, int posEnd, int timelineSize, int positionMainEvent, List<int> secondaryEventsPosition)
        {
            Color noDataColor = Color.gray;//  Color.red;
            Color dataColor = Color.white;// Color.green;
            Color interTimeColor = Color.black;// Color.grey;


            int size = 1000;
            if(size % (timelineSize-1) != 0)
            {
                size += (timelineSize-1) - (size % (timelineSize-1));
            }

            int offsetT = size / (timelineSize-1);
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

                if(ii % offsetT == 0)
                {
                    if(fistBlack)
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


    namespace DLL
    {
        /// <summary>
        /// 
        /// </summary>
        public class BrainTextureGenerator : CppDLLImportBase, ICloneable
        {
            #region members

            private float[] m_uvAmplitudes = new float[0];
            private float[] m_uvAlpha = new float[0];
            Vector2[] m_uvAmplitudesV = new Vector2[0];
            Vector2[] m_uvAlphaV = new Vector2[0];

            #endregion members

            #region functions

            public void reset(DLL.Surface surface, Volume volume)
            {
                reset_BrainSurfaceTextureGenerator(_handle, surface.getHandle(), volume.getHandle());
                DLLDebugManager.checkError();
            }

            public void initOctree(RawPlotList rawPlotList)
            {
                initOctree_BrainSurfaceTextureGenerator(_handle, rawPlotList.getHandle());
                DLLDebugManager.checkError();
            }

            public void computeDistances(float maxDistance, bool multiCPU)
            {
                computeDistances_BrainSurfaceTextureGenerator(_handle, maxDistance, multiCPU);
                DLLDebugManager.checkError();
            }

            public float getMaximumDensity()
            {
                return getMaximumDensity_BrainSurfaceTextureGenerator(_handle);
            }

            public float getMinimumInfluence()
            {
                return getMinInf_BrainSurfaceTextureGenerator(_handle);
            }

            public float getMaximumInfluence()
            {
                return getMaxInf_BrainSurfaceTextureGenerator(_handle);
            }

            public void synchronizeWithOthersGenerators(float sharedMaxDensity, float sharedMinInf, float sharedMaxInf)
            {
                synchronizeWithOthersGenerators_BrainSurfaceTextureGenerator(_handle, sharedMaxDensity, sharedMinInf, sharedMaxInf);
            }

            public void computeInfluences(Column3DViewIEEG iEEGColumn, bool multiCPU, bool addValues = false, bool ratioDistances = false)
            {
                computeInfluences_BrainSurfaceTextureGenerator(_handle, iEEGColumn.getAmplitudes(), iEEGColumn.dimensions(), iEEGColumn.maxDistanceElec, multiCPU, addValues, ratioDistances, 
                    iEEGColumn.middle, iEEGColumn.spanMin, iEEGColumn.spanMax);
                DLLDebugManager.checkError();
            }

            public void ajustInfluencesToColormap()
            {
                ajustInfluencesToColormap_BrainSurfaceTextureGenerator(_handle);
            }

            public void computeSurfaceTextCoordAmplitudes(DLL.Surface surface, Column3DViewIEEG iEEGColumn)
            {
                computeSurfaceTextCoordAmplitudes_BrainSurfaceTextureGenerator(_handle, surface.getHandle(), iEEGColumn.currentTimeLineID, iEEGColumn.alphaMin, iEEGColumn.alphaMax);

                int m_nbVertices = surface.verticesNb();
                int nbFloatUV = m_nbVertices * 2;

                // amplitudes
                if (m_uvAmplitudes.Length != nbFloatUV)
                    m_uvAmplitudes = new float[nbFloatUV];
                getUVAmplitudes_BrainSurfaceTextureGenerator(_handle, m_uvAmplitudes);

                if (m_uvAmplitudesV.Length != m_nbVertices)
                {
                    m_uvAmplitudesV = new Vector2[m_nbVertices];

                    for (int ii = 0; ii < m_uvAmplitudesV.Length; ++ii)
                        m_uvAmplitudesV[ii] = new Vector2(m_uvAmplitudes[2 * ii], m_uvAmplitudes[2 * ii + 1]);
                }
                else
                {
                    for (int ii = 0; ii < m_uvAmplitudesV.Length; ++ii)
                    {
                        m_uvAmplitudesV[ii].x = m_uvAmplitudes[2 * ii];
                        m_uvAmplitudesV[ii].y = m_uvAmplitudes[2 * ii + 1];
                    }
                }

                // alpha
                if (m_uvAlpha.Length != nbFloatUV)
                    m_uvAlpha = new float[nbFloatUV];
                getUVAlpha_BrainSurfaceTextureGenerator(_handle, m_uvAlpha);

                if (m_uvAlphaV.Length != m_nbVertices)
                {
                    m_uvAlphaV = new Vector2[m_nbVertices];

                    for (int ii = 0; ii < m_uvAlphaV.Length; ++ii)
                        m_uvAlphaV[ii] = new Vector2(m_uvAlpha[2 * ii], m_uvAlpha[2 * ii + 1]);
                }
                else
                {
                    for (int ii = 0; ii < m_uvAlphaV.Length; ++ii)
                    {
                        m_uvAlphaV[ii].x = m_uvAlpha[2 * ii];
                        m_uvAlphaV[ii].y = m_uvAlpha[2 * ii + 1];
                    }
                }

                DLLDebugManager.checkError();
            }

            public Vector2[] getAmplitudesUV()
            {
                return m_uvAmplitudesV;
            }
            public Vector2[] getAlphaUV()
            {
                return m_uvAlphaV;
            }


            #endregion functions

            #region memory_management

            /// <summary>
            /// Allocate DLL memory
            /// </summary>
            protected override void createDLLClass()
            {
                _handle = new HandleRef(this, create_BrainSurfaceTextureGenerator());
            }

            /// <summary>
            /// Clean DLL memory
            /// </summary>
            protected override void deleteDLLClass()
            {
                delete_BrainSurfaceTextureGenerator(_handle);
            }

            /// <summary>
            /// BrainTextureGenerator default constructor
            /// </summary>
            public BrainTextureGenerator() : base() { }

            /// <summary>
            /// BrainTextureGenerator copy constructor
            /// </summary>
            /// <param name="other"></param>
            public BrainTextureGenerator(BrainTextureGenerator other) : base(clone_BrainSurfaceTextureGenerator(other.getHandle())) { }

            /// <summary>
            /// Clone the BrainTextureGenerator
            /// </summary>
            /// <returns></returns>
            public object Clone()
            {
                return new BrainTextureGenerator(this);
            }

            #endregion memory_management

            #region DLLImport

            // test
            [DllImport("hbp_export", EntryPoint = "create_BrainSurfaceTextureGeneratorsContainer", CallingConvention = CallingConvention.Cdecl)]
            static public extern IntPtr create_BrainSurfaceTextureGeneratorsContainer();

            [DllImport("hbp_export", EntryPoint = "add_BrainSurfaceTextureGeneratorsContainer", CallingConvention = CallingConvention.Cdecl)]
            static public extern void add_BrainSurfaceTextureGeneratorsContainer(IntPtr container, HandleRef handleBrainSurfaceTextureGenerator);

            [DllImport("hbp_export", EntryPoint = "display_BrainSurfaceTextureGeneratorsContainer", CallingConvention = CallingConvention.Cdecl)]
            static public extern void display_BrainSurfaceTextureGeneratorsContainer(IntPtr container);



            // memory management
            [DllImport("hbp_export", EntryPoint = "create_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr create_BrainSurfaceTextureGenerator();

            [DllImport("hbp_export", EntryPoint = "clone_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr clone_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator);

            [DllImport("hbp_export", EntryPoint = "delete_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void delete_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator);

            // actions
            [DllImport("hbp_export", EntryPoint = "reset_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void reset_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator, HandleRef handleSurface, HandleRef handleVolume);

            [DllImport("hbp_export", EntryPoint = "initOctree_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void initOctree_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator, HandleRef handleRawPlotList);

            [DllImport("hbp_export", EntryPoint = "computeDistances_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void computeDistances_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator, float maxDistance, bool multiCPU);

            [DllImport("hbp_export", EntryPoint = "computeInfluences_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void computeInfluences_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator, float[] timelineAmplitudes, int[] dimensions, float maxDistance, bool multiCPU,
                bool addValues, bool ratioDistances, float middle, float spanMin, float spanMax);

            [DllImport("hbp_export", EntryPoint = "ajustInfluencesToColormap_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void ajustInfluencesToColormap_BrainSurfaceTextureGenerator(HandleRef handleBrainCutTextureGenerator);

            [DllImport("hbp_export", EntryPoint = "synchronizeWithOthersGenerators_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void synchronizeWithOthersGenerators_BrainSurfaceTextureGenerator(HandleRef handleBrainCutTextureGenerator, float sharedMaxDensity, float sharedMinInf, float sharedMaxInf);

            // retrieve data                            
            [DllImport("hbp_export", EntryPoint = "computeSurfaceTextCoordAmplitudes_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void computeSurfaceTextCoordAmplitudes_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator, HandleRef handleSurface, int idTimeline, float alphaMin, float alphaMax);

            [DllImport("hbp_export", EntryPoint = "getUVAmplitudes_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void getUVAmplitudes_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator, float[] textureCoordsArray);

            [DllImport("hbp_export", EntryPoint = "getUVAlpha_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void getUVAlpha_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator, float[] textureCoordsArray);

            [DllImport("hbp_export", EntryPoint = "getMaximumDensity_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern float getMaximumDensity_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator);

            [DllImport("hbp_export", EntryPoint = "getMinInf_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern float getMinInf_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator);

            [DllImport("hbp_export", EntryPoint = "getMaxInf_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern float getMaxInf_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator);

            #endregion DLLImport
        }

        /// <summary>
        /// 
        /// </summary>
        public class CutTextureGenerator : CppDLLImportBase, ICloneable
        {           
            #region functions 

            public void reset(DLL.Volume volume, DLL.Surface surface, Plane plane)
            {
                float[] planeF = new float[6];
                for (int ii = 0; ii < 3; ++ii)
                {
                    planeF[ii] = plane.point[ii];
                    planeF[ii + 3] = plane.normal[ii];
                }

                reset_BrainCutTextureGenerator(_handle, volume.getHandle(), surface.getHandle(), planeF);
                DLLDebugManager.checkError();
            }

            public void initOctree(RawPlotList rawPlotList)
            {                
                initOctree_BrainCutTextureGenerator(_handle, rawPlotList.getHandle());
                DLLDebugManager.checkError();
            }

            public void computeDistances(float maxDistance, bool multiCPU)
            {
                computeDistances_BrainCutTextureGenerator(_handle, maxDistance, multiCPU);
                DLLDebugManager.checkError();
            }

            public float getMaximumDensity()
            {
                return getMaximumDensity_BrainCutTextureGenerator(_handle);
            }

            public float getMinimumInfluence()
            {
                return getMinInf_BrainCutTextureGenerator(_handle);
            }

            public float getMaximumInfluence()
            {
                return getMaxInf_BrainCutTextureGenerator(_handle);
            }

            public void synchronizeWithOthersGenerators(float sharedMaxDensity, float sharedMinInf, float sharedMaxInf)
            {
                synchronizeWithOthersGenerators_BrainCutTextureGenerator(_handle, sharedMaxDensity, sharedMinInf, sharedMaxInf);
            }

            public void computeInfluences(Column3DViewIEEG iEEGColumn, bool multiCPU, bool addValues = false, bool ratioDistances = false)
            {
                computeInfluences_BrainCutTextureGenerator(_handle, iEEGColumn.getAmplitudes(), iEEGColumn.dimensions(), iEEGColumn.maxDistanceElec, multiCPU, addValues, ratioDistances, iEEGColumn.middle, iEEGColumn.spanMin, iEEGColumn.spanMax);
                DLLDebugManager.checkError();
            }

            public void ajustInfluencesToColormap()
            {
                ajustInfluencesToColormap_BrainCutTextureGenerator(_handle);
            }

            public void createTextureAndUpdateMeshUV(Volume volume, Plane planeCut, DLL.Texture colorScheme, DLL.Surface surfaceCut, float calMin, float calMax, Color notInBrainCol)
            {
                float[] planeF = new float[6];
                for (int ii = 0; ii < 3; ++ii)
                {
                    planeF[ii] = planeCut.point[ii];
                    planeF[ii + 3] = planeCut.normal[ii];
                }

                float[] notInBrainColor = new float[3];
                notInBrainColor[0] = notInBrainCol.b;
                notInBrainColor[1] = notInBrainCol.r;
                notInBrainColor[2] = notInBrainCol.r;
                createTextureAndUpdateMeshUV_BrainCutTextureGenerator(_handle, volume.getHandle(), planeF, colorScheme.getHandle(), surfaceCut.getHandle(), calMin, calMax, notInBrainColor);
                DLLDebugManager.checkError();
            }

            public void colorTextureWithAmplitudes(Column3DViewIEEG iEEGColumn, DLL.Texture colorScheme, Color notInBrainCol)
            {
                float[] notInBrainColor = new float[3];
                notInBrainColor[0] = notInBrainCol.b;
                notInBrainColor[1] = notInBrainCol.r;
                notInBrainColor[2] = notInBrainCol.r;
                colorTextureWithAmplitudes_BrainCutTextureGenerator(_handle, iEEGColumn.currentTimeLineID, colorScheme.getHandle(), iEEGColumn.alphaMin, iEEGColumn.alphaMax, notInBrainColor);
                DLLDebugManager.checkError();
            }

            public void colorTextureWithIRMF(Volume IRMFVolume, DLL.Texture colorScheme, float calMin, float calMax, float alpha)
            {
                colorTextureWithIRMF_BrainCutTextureGenerator(_handle, IRMFVolume.getHandle(), colorScheme.getHandle(), calMin, calMax, alpha);
                DLLDebugManager.checkError();
            }

            public DLL.Texture getTexture()
            {
                return new DLL.Texture(getTexture_BrainCutTextureGenerator(_handle));
            }

            public DLL.Texture getTextureWithAmplitudes()
            {
                return new DLL.Texture(getTextureWithAmplitudes_BrainCutTextureGenerator(_handle));
            }

            public DLL.Texture getTextureWithIRMF()
            {
                return new DLL.Texture(getTextureWithIRMF_BrainCutTextureGenerator(_handle));                
            }


            #endregion functions

            #region memory_management

            /// <summary>
            /// Allocate DLL memory
            /// </summary>
            protected override void createDLLClass()
            {
                _handle = new HandleRef(this, create_BrainCutTextureGenerator());
            }

            /// <summary>
            /// Clean DLL memory
            /// </summary>
            protected override void deleteDLLClass()
            {
                delete_BrainCutTextureGenerator(_handle);
            }

            /// <summary>
            /// CutTextureGenerator default constructor
            /// </summary>
            public CutTextureGenerator() : base() { }

            /// <summary>
            /// CutTextureGenerator copy constructor
            /// </summary>
            /// <param name="other"></param>
            public CutTextureGenerator(CutTextureGenerator other) : base(clone_BrainCutTextureGenerator(other.getHandle()))
            {}

            /// <summary>
            /// Clone the CutTextureGenerator
            /// </summary>
            /// <returns></returns>
            public object Clone()
            {
                return new CutTextureGenerator(this);
            }

            #endregion memory_management

            #region DLLImport

            // memory management
            [DllImport("hbp_export", EntryPoint = "create_BrainCutTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr create_BrainCutTextureGenerator();

            [DllImport("hbp_export", EntryPoint = "clone_BrainCutTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr clone_BrainCutTextureGenerator(HandleRef handleBrainCutTextureGenerator);

            [DllImport("hbp_export", EntryPoint = "delete_BrainCutTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void delete_BrainCutTextureGenerator(HandleRef handleBrainCutTextureGenerator);

            // actions
            [DllImport("hbp_export", EntryPoint = "reset_BrainCutTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void reset_BrainCutTextureGenerator(HandleRef handleBrainCutTextureGenerator, HandleRef handleVolume, HandleRef handleSurface, float[] plane);

            [DllImport("hbp_export", EntryPoint = "initOctree_BrainCutTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void initOctree_BrainCutTextureGenerator(HandleRef handleBrainCutTextureGenerator, HandleRef handleRawPlotList);

            [DllImport("hbp_export", EntryPoint = "computeDistances_BrainCutTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void computeDistances_BrainCutTextureGenerator(HandleRef handleBrainCutTextureGenerator, float maxDistance, bool multiCPU);

            [DllImport("hbp_export", EntryPoint = "computeInfluences_BrainCutTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void computeInfluences_BrainCutTextureGenerator(HandleRef handleBrainCutTextureGenerator, float[] timelineAmplitudes, int[] dimensions, float maxDistance, bool multiCPU
                                        , bool addValues, bool ratioDistances, float middle, float spanMin, float spanMax);

            [DllImport("hbp_export", EntryPoint = "ajustInfluencesToColormap_BrainCutTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void ajustInfluencesToColormap_BrainCutTextureGenerator(HandleRef handleBrainCutTextureGenerator);
            
            [DllImport("hbp_export", EntryPoint = "createTextureAndUpdateMeshUV_BrainCutTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void createTextureAndUpdateMeshUV_BrainCutTextureGenerator(HandleRef handleBrainCutTextureGenerator, HandleRef handleVolume, 
                float[] plane, HandleRef handleTexture, HandleRef handleSurface, float calMin, float calMax, float[] notInBrainColor);

            [DllImport("hbp_export", EntryPoint = "colorTextureWithAmplitudes_BrainCutTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void colorTextureWithAmplitudes_BrainCutTextureGenerator(HandleRef handleBrainCutTextureGenerator, int idTimeline, HandleRef handleTexture, 
                float alphaMin, float alphaMax, float[] notInBrainColor);

            [DllImport("hbp_export", EntryPoint = "colorTextureWithIRMF_BrainCutTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void colorTextureWithIRMF_BrainCutTextureGenerator(HandleRef handleBrainCutTextureGenerator, HandleRef handleIRMFVolume,HandleRef handleColorSchemeTexture, float calMin, float calMax, float alpha);

            [DllImport("hbp_export", EntryPoint = "synchronizeWithOthersGenerators_BrainCutTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void synchronizeWithOthersGenerators_BrainCutTextureGenerator(HandleRef handleBrainCutTextureGenerator, float sharedMaxDensity, float sharedMinInf, float sharedMaxInf);

            // retrieve data
            [DllImport("hbp_export", EntryPoint = "getTexture_BrainCutTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr getTexture_BrainCutTextureGenerator(HandleRef handleBrainCutTextureGenerator);

            [DllImport("hbp_export", EntryPoint = "getTextureWithAmplitudes_BrainCutTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr getTextureWithAmplitudes_BrainCutTextureGenerator(HandleRef handleBrainCutTextureGenerator);

            [DllImport("hbp_export", EntryPoint = "getTextureWithIRMF_BrainCutTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr getTextureWithIRMF_BrainCutTextureGenerator(HandleRef handleBrainCutTextureGenerator);

            [DllImport("hbp_export", EntryPoint = "getMaximumDensity_BrainCutTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern float getMaximumDensity_BrainCutTextureGenerator(HandleRef handleBrainCutTextureGenerator);

            [DllImport("hbp_export", EntryPoint = "getMinInf_BrainCutTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern float getMinInf_BrainCutTextureGenerator(HandleRef handleBrainCutTextureGenerator);

            [DllImport("hbp_export", EntryPoint = "getMaxInf_BrainCutTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern float getMaxInf_BrainCutTextureGenerator(HandleRef handleBrainCutTextureGenerator);

            #endregion DLLImport
        }
    }
}