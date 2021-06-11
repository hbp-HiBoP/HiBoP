using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Module3D.DLL
{
    public class CutGenerator : Tools.DLL.CppDLLImportBase
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
            initialize_CutGenerator(_handle, activityGenerator.getHandle(), cutGeometryGenerator.getHandle(), blurFactor);
        }
        public void FillTextureWithVolume(Texture colorScheme, float calMin, float calMax)
        {
            fill_texture_with_volume_CutGenerator(_handle, colorScheme.getHandle(), calMin, calMax);
        }
        public void FillTextureWithAtlas(BrainAtlas atlas, float alpha, int selectedArea)
        {
            fill_texture_with_atlas_CutGenerator(_handle, atlas.getHandle(), alpha, selectedArea);
        }
        public void FillTextureWithActivity(Texture colorScheme, int timelineIndex, float alpha)
        {
            fill_texture_with_activity_CutGenerator(_handle, colorScheme.getHandle(), timelineIndex, alpha);
        }
        public void FillTextureWithFMRI(Volume volume, float negativeMin, float negativeMax, float positiveMin, float positiveMax, float alpha)
        {
            fill_texture_with_fmri_CutGenerator(_handle, volume.getHandle(), negativeMin, negativeMax, positiveMin, positiveMax, alpha);
        }
        public void UpdateTextureWithVolume(Texture texture)
        {
            update_texture_with_volume_CutGenerator(_handle, texture.getHandle());
        }
        public void UpdateTextureWithAtlas(Texture texture)
        {
            update_texture_with_atlas_CutGenerator(_handle, texture.getHandle());
        }
        public void UpdateTextureWithActivity(Texture texture)
        {
            update_texture_with_activity_CutGenerator(_handle, texture.getHandle());
        }
        #endregion

        #region Memory Management
        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void create_DLL_class()
        {
            _handle = new HandleRef(this, create_CutGenerator());
        }
        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
            delete_CutGenerator(_handle);
        }
        #endregion

        #region DLLImport
        [DllImport("hbp_export", EntryPoint = "create_CutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_CutGenerator();
        [DllImport("hbp_export", EntryPoint = "delete_CutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_CutGenerator(HandleRef generator);
        [DllImport("hbp_export", EntryPoint = "initialize_CutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void initialize_CutGenerator(HandleRef generator, HandleRef activityGenerator, HandleRef geometryGenerator, int blurFactor);
        [DllImport("hbp_export", EntryPoint = "fill_texture_with_volume_CutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void fill_texture_with_volume_CutGenerator(HandleRef generator, HandleRef colorScheme, float calMin, float calMax);
        [DllImport("hbp_export", EntryPoint = "fill_texture_with_atlas_CutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void fill_texture_with_atlas_CutGenerator(HandleRef generator, HandleRef atlas, float alpha, int highlightedLabelIndex);
        [DllImport("hbp_export", EntryPoint = "fill_texture_with_activity_CutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void fill_texture_with_activity_CutGenerator(HandleRef generator, HandleRef colorScheme, int timelineIndex, float alpha);
        [DllImport("hbp_export", EntryPoint = "fill_texture_with_fmri_CutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void fill_texture_with_fmri_CutGenerator(HandleRef generator, HandleRef volumeHandle, float negativeMin, float negativeMax, float positiveMin, float positiveMax, float alpha);
        [DllImport("hbp_export", EntryPoint = "update_texture_with_volume_CutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void update_texture_with_volume_CutGenerator(HandleRef generator, HandleRef texture);
        [DllImport("hbp_export", EntryPoint = "update_texture_with_atlas_CutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void update_texture_with_atlas_CutGenerator(HandleRef generator, HandleRef texture);
        [DllImport("hbp_export", EntryPoint = "update_texture_with_activity_CutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void update_texture_with_activity_CutGenerator(HandleRef generator, HandleRef texture);
        #endregion
    }
}