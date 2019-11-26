using UnityEngine;
using UnityEngine.Events;

namespace HBP.Module3D
{
    /// <summary>
    /// Class representing a cut on the scene
    /// </summary>
    public class Cut : Plane
    {
        #region Properties
        /// <summary>
        /// ID of the cut
        /// </summary>
        public int ID { get; set; }
        /// <summary>
        /// Orientation of the cut
        /// </summary>
        public Data.Enums.CutOrientation Orientation { get; set; }
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
        public GenericEvent<Column3D> OnUpdateGUITextures = new GenericEvent<Column3D>();
        /// <summary>
        /// Event called when a cut is removed
        /// </summary>
        public UnityEvent OnRemoveCut = new UnityEvent();
        #endregion

        #region Constructors
        public Cut() : base()
        {
            Orientation = Data.Enums.CutOrientation.Axial;
            Flip = false;
            NumberOfCuts = 500;
            Position = 0.5f;
        }
        public Cut(Vector3 point, Vector3 normal) : base(point, normal)
        {
            Orientation = Data.Enums.CutOrientation.Axial;
            Flip = false;
            NumberOfCuts = 500;
            Position = 0.5f;
        }
        #endregion
    }
}