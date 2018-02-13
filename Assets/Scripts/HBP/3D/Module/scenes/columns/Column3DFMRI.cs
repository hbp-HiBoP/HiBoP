

/**
 * \file    Column3DViewFMRI.cs
 * \author  Lance Florian
 * \date    29/02/2016
 * \brief   Define Column3DViewFMRI class
 */

// system
using System.Collections.Generic;
using System.Linq;

// unity
using UnityEngine;

namespace HBP.Module3D
{
    /// <summary>
    /// A 3D column view class, containing all necessary data concerning an FMRI column
    /// </summary>
    public class Column3DFMRI : Column3D
    {
        #region Properties
        public override ColumnType Type
        {
            get
            {
                return ColumnType.FMRI;
            }
        }

        public float CalMin = 0.4f;
        public float CalMax = 0.6f;
        public float Alpha = 0.5f;
        #endregion
    }
}