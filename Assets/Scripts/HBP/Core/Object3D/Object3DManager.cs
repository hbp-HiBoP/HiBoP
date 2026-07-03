using HBP.Core.DLL;
using UnityEngine;

namespace HBP.Core.Object3D
{
    public static class Object3DManager
    {
        #region Properties
        public static MarsAtlas MarsAtlas { get; set; } = new MarsAtlas();
        public static JuBrainAtlas JuBrain { get; set; } = new JuBrainAtlas();
        public static MNIObjects MNI { get; set; } = new MNIObjects();
        public static DiFuMoObjects DiFuMo { get; set; } = new DiFuMoObjects();
        public static IBCObjects IBC { get; set; } = new IBCObjects();
        public static LocalizersObjects Localizers { get; set; } = new LocalizersObjects();
        #endregion

        #region Public Methods
        public static void Clean()
        {
            MNI?.Clean();
            IBC?.Clean();
            DiFuMo?.Clean();
            Localizers?.Clean();
            MarsAtlas?.Dispose();
            JuBrain?.Dispose();
        }
        public static void Reset()
        {
            try
            {
                Clean();
            }
            finally
            {
                MarsAtlas = new MarsAtlas();
                JuBrain = new JuBrainAtlas();
                MNI = new MNIObjects();
                DiFuMo = new DiFuMoObjects();
                IBC = new IBCObjects();
                Localizers = new LocalizersObjects();
            }
        }
        public static void UnloadMarsAtlas()
        {
            if (MarsAtlas.Loaded)
            {
                MarsAtlas.Dispose();
                MarsAtlas = new MarsAtlas();
            }
        }
        public static void UnloadJuBrain()
        {
            if (JuBrain.Loaded)
            {
                JuBrain.Dispose();
                JuBrain = new JuBrainAtlas();
            }
        }
        public static void UnloadIBC()
        {
            if (IBC.Loaded)
            {
                IBC.Clean();
                IBC = new IBCObjects();
            }
        }
        public static void UnloadDiFuMo(string atlas)
        {
            DiFuMo.Unload(atlas);
        }
        public static void UnloadLocalizer(string protocolName)
        {
            Localizers.Unload(protocolName);
        }
        #endregion
    }
}
