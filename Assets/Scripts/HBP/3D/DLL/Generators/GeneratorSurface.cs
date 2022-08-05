using System;
using System.Runtime.InteropServices;

namespace HBP.Core.DLL
{
    public class GeneratorSurface : CppDLLImportBase
    {
        #region Properties
        public Surface Surface { get; private set; }
        public Volume Volume { get; private set; }
        #endregion

        #region Public Methods
        public void Initialize(Surface surface, Volume volume, int dimension)
        {
            Surface = surface;
            Volume = volume;
            initialize_GeneratorSurface(_handle, surface.getHandle(), volume.getHandle(), dimension);
        }
        #endregion

        #region Private Methods
        protected override void create_DLL_class()
        {
            _handle = new HandleRef(this, create_GeneratorSurface());
        }
        protected override void delete_DLL_class()
        {
            delete_GeneratorSurface(_handle);
        }
        #endregion

        #region DLLImport
        [DllImport("hbp_export", EntryPoint = "create_GeneratorSurface", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_GeneratorSurface();
        [DllImport("hbp_export", EntryPoint = "delete_GeneratorSurface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_GeneratorSurface(HandleRef generator);
        [DllImport("hbp_export", EntryPoint = "initialize_GeneratorSurface", CallingConvention = CallingConvention.Cdecl)]
        static private extern void initialize_GeneratorSurface(HandleRef generatorSurface, HandleRef surface, HandleRef volume, int dimension);
        #endregion
    }
}