using UnityEngine;
using UnityEngine.Events;
using System;
using HBP.Core.Enums;
using HBP.Core.Interfaces;

namespace HBP.Core.Object3D
{
    /// <summary>
    /// Class representing a cut on the scene
    /// </summary>
    public class Cut : HBP.Core.DLL.Plane, IIdentifiable
    {
        #region Properties

        /// <summary>
        /// Stable identity of the cut
        /// </summary>
        public string ID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Index into cut textures and geometry generators; changes when cuts are removed.</summary>
        public int Index { get; set; }

        public void GenerateID() => ID = Guid.NewGuid().ToString();

        /// <summary>
        /// Orientation of the cut
        /// </summary>
        public CutOrientation Orientation { get; set; }

        /// <summary>
        /// Is the cut flipped ?
        /// </summary>
        public bool Flip { get; set; }

        /// <summary>
        /// Number of cuts (levels in the MRI)
        /// </summary>
        public int NumberOfCuts { get; set; }

        /// <summary>
        /// Position of the cut (between 0 and 1)
        /// </summary>
        public float Position { get; set; }

        #endregion

        #region Events

        /// <summary>
        /// Event called when the GUI textures are computed
        /// </summary>
        public UnityEvent OnUpdateGUITextures = new();

        /// <summary>
        /// Event called when a cut is removed
        /// </summary>
        public UnityEvent OnRemoveCut = new();

        #endregion

        #region Constructors

        public Cut() : base()
        {
            Orientation = CutOrientation.Axial;
            Flip = false;
            NumberOfCuts = 500;
            Position = 0.5f;
        }

        public Cut(Vector3 point, Vector3 normal) : base(point, normal)
        {
            Orientation = CutOrientation.Axial;
            Flip = false;
            NumberOfCuts = 500;
            Position = 0.5f;
        }

        #endregion
    }
}
