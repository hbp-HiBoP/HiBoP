using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.Data.TrialMatrix.Grid
{
    public class TrialMatrixGrid
    {
        #region Properties
        public ChannelStruct[] Channels { get; set; }
        public DataStruct[] Data { get; set; }
        public Texture2D Colormap { get; set; }
        #endregion

        #region Constructor
        public TrialMatrixGrid(ChannelStruct[] channels, DataStruct[] data)
        {
            Channels = channels;
            Data = data;
        }
        #endregion
    }
}