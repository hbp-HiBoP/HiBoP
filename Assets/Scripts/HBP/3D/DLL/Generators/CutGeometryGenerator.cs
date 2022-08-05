using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Core.DLL
{
    public class CutGeometryGenerator : CppDLLImportBase
    {
        #region Properties
        public BBox BoundingBox
        {
            get
            {
                return new BBox(bounding_box_CutGeometryGenerator(_handle));
            }
        }
        #endregion

        #region Public Methods
        public void Initialize(Volume volume, Object3D.Cut cut, float sizeFactor)
        {
            float[] planeCut = new float[6];
            for (int ii = 0; ii < 3; ++ii)
            {
                planeCut[ii] = cut.Point[ii];
                planeCut[ii + 3] = cut.Normal[ii];
            }
            initialize_CutGeometryGenerator(_handle, volume.getHandle(), planeCut, (int)cut.Orientation, cut.Flip, sizeFactor);
        }
        public void UpdateSurfaceUV(Surface cutSurface)
        {
            update_mesh_UV_CutGeometryGenerator(_handle, cutSurface.getHandle());
        }
        public Vector2 GetPositionRatioOnTexture(Vector3 point)
        {
            float[] pointArray = new float[3] { -point.x, point.y, point.z };
            float[] resultArray = new float[2];
            get_position_ratio_on_texture_CutGeometryGenerator(_handle, pointArray, resultArray);
            return new Vector2(resultArray[0], resultArray[1]);
        }
        #endregion

        #region Memory Management
        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void create_DLL_class()
        {
            _handle = new HandleRef(this, create_CutGeometryGenerator());
        }
        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
            delete_CutGeometryGenerator(_handle);
        }
        #endregion

        #region DLLImport
        [DllImport("hbp_export", EntryPoint = "create_CutGeometryGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_CutGeometryGenerator();
        [DllImport("hbp_export", EntryPoint = "delete_CutGeometryGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_CutGeometryGenerator(HandleRef generator);
        [DllImport("hbp_export", EntryPoint = "initialize_CutGeometryGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void initialize_CutGeometryGenerator(HandleRef generator, HandleRef volume, float[] planeCut, int orientation, bool flip, float sizeFactor);
        [DllImport("hbp_export", EntryPoint = "update_mesh_UV_CutGeometryGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void update_mesh_UV_CutGeometryGenerator(HandleRef generator, HandleRef surface);
        [DllImport("hbp_export", EntryPoint = "get_position_ratio_on_texture_CutGeometryGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void get_position_ratio_on_texture_CutGeometryGenerator(HandleRef generator, float[] point, float[] result);
        [DllImport("hbp_export", EntryPoint = "bounding_box_CutGeometryGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr bounding_box_CutGeometryGenerator(HandleRef generator);
        #endregion
    }
}