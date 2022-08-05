using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HBP.Core.DLL;

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
        #endregion

        #region Public Methods
        public static void Clean()
        {
            MNI.Clean();
            IBC.Clean();
            DiFuMo.Clean();
            MarsAtlas.Dispose();
            JuBrain.Dispose();
        }
        #endregion
    }
}