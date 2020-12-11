using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using HBP.Module3D.DLL;
using HBP.Data.Enums;

namespace HBP.Module3D
{
    /// <summary>
    /// Class containing anatomical data for a column
    /// </summary>
    public class Column3DAnatomy : Column3D
    {
        #region Properties
        #endregion

        #region Events
        #endregion

        #region Private Methods
        #endregion

        #region Public Methods
        /// <summary>
        /// Compute the UVs of the meshes for the brain activity
        /// </summary>
        /// <param name="splittedMeshes">DLL brain splitted meshes</param>
        public override void ComputeSurfaceBrainUVWithActivity(List<Surface> splittedMeshes)
        {
            //TODO
        }
        #endregion
    }
}
