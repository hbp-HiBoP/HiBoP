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
        /// <summary>Raised synchronously before a cut definition changes.</summary>
        public static event Action<Cut> DefinitionChanging;

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
        private CutOrientation m_Orientation;

        public CutOrientation Orientation
        {
            get => m_Orientation;
            set
            {
                if (m_Orientation != value) NotifyDefinitionChanging();
                m_Orientation = value;
            }
        }

        /// <summary>
        /// Is the cut flipped ?
        /// </summary>
        private bool m_Flip;

        public bool Flip
        {
            get => m_Flip;
            set
            {
                if (m_Flip != value) NotifyDefinitionChanging();
                m_Flip = value;
            }
        }

        /// <summary>
        /// Number of cuts (levels in the MRI)
        /// </summary>
        private int m_NumberOfCuts;

        public int NumberOfCuts
        {
            get => m_NumberOfCuts;
            set
            {
                if (m_NumberOfCuts != value) NotifyDefinitionChanging();
                m_NumberOfCuts = value;
            }
        }

        /// <summary>
        /// Position of the cut (between 0 and 1)
        /// </summary>
        private float m_Position;

        public float Position
        {
            get => m_Position;
            set
            {
                if (m_Position != value) NotifyDefinitionChanging();
                m_Position = value;
            }
        }

        protected override void OnNormalChanging() => NotifyDefinitionChanging();

        private void NotifyDefinitionChanging() => DefinitionChanging?.Invoke(this);

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
