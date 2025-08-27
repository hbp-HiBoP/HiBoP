using System.Runtime.InteropServices;

namespace HBP.Core.DLL
{
    public abstract class ActivityGenerator : CppDLLImportBase
    {
        #region Properties
        public float Progress { get { return get_progress_ActivityGenerator(_handle); } }
        public GeneratorSurface GeneratorSurface { get; private set; }
        #endregion

        #region Public Methods
        public void Initialize(GeneratorSurface generatorSurface)
        {
            GeneratorSurface = generatorSurface;
            initialize_ActivityGenerator(_handle, generatorSurface.getHandle());
        }
        public bool SaveActivityAsNifti(string path, int timelineLength)
        {
            return save_activity_as_nifti_ActivityGenerator(_handle, path, timelineLength);
        }
        #endregion

        #region DLLImport
        [DllImport("hbp_export", EntryPoint = "initialize_ActivityGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void initialize_ActivityGenerator(HandleRef generator, HandleRef generatorSurface);
        [DllImport("hbp_export", EntryPoint = "get_progress_ActivityGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern float get_progress_ActivityGenerator(HandleRef generator);
        [DllImport("hbp_export", EntryPoint = "save_activity_as_nifti_ActivityGenerator", CallingConvention = CallingConvention.Cdecl)]
        static public extern bool save_activity_as_nifti_ActivityGenerator(HandleRef generator, string path, int timelineLength);
        #endregion
    }
}