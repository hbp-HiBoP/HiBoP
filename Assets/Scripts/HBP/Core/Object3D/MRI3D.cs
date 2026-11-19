using System;

namespace HBP.Core.Object3D
{
    /// <summary>
    /// This class contains information about a MRI and can load MRIs to DLL objects
    /// </summary>
    public class MRI3D : ICloneable
    {
        #region Properties

        /// <summary>
        /// Name of the MRI
        /// </summary>
        public string Name { get; set; }

        private readonly object m_LoadLock = new();
        private DLL.Volume m_Volume = new();

        /// <summary>
        /// Volume of this MRI
        /// </summary>
        public DLL.Volume Volume
        {
            get
            {
                lock (m_LoadLock)
                {
                    if (!IsLoaded) Load();
                    return m_Volume;
                }
            }
            protected set { m_Volume = value; }
        }

        /// <summary>
        /// Is the MRI completely loaded ?
        /// </summary>
        public bool IsLoaded
        {
            get
            {
                lock (m_LoadLock) return m_Volume != null && m_Volume.IsLoaded;
            }
        }

        /// <summary>
        /// Is the MRI currently loading ?
        /// </summary>
        protected bool m_IsLoading = false;

        /// <summary>
        /// Does the mesh have been loaded outside of a scene and copied to the scene (e.g. MNI objects) ?
        /// </summary>
        public bool HasBeenLoadedOutside { get; protected set; }

        /// <summary>
        /// Data of the MRI (paths etc.)
        /// </summary>
        protected Data.MRI m_MRI;

        #endregion

        #region Constructors

        public MRI3D(Data.MRI mri, bool load)
        {
            Name = mri.Name;
            m_MRI = mri;
            if (load) Load();
            HasBeenLoadedOutside = false;
        }

        public MRI3D(string name, DLL.Volume volume, bool shared = true)
        {
            Name = name;
            Volume = volume;
            HasBeenLoadedOutside = shared;
        }

        public MRI3D()
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Load the mri to DLL objects
        /// </summary>
        public void Load()
        {
            lock (m_LoadLock)
            {
                // Configuration, anatomy preparation and lazy readers can request the
                // same MRI. Never replace a native volume another reader already uses.
                if (IsLoaded) return;
                m_IsLoading = true;
                try
                {
                    if (!m_Volume.LoadNIFTIFile(m_MRI.File)) throw new System.IO.IOException($"Unable to load MRI '{Name}'.");
                }
                finally
                {
                    m_IsLoading = false;
                }
            }
        }

        /// <summary>
        /// Dispose all DLL objects
        /// </summary>
        public void Clean()
        {
            lock (m_LoadLock) m_Volume?.Dispose();
        }

        public object Clone()
        {
            MRI3D mri = new()
            {
                Name = Name,
                Volume = Volume,
                HasBeenLoadedOutside = HasBeenLoadedOutside
            };
            return mri;
        }

        #endregion
    }
}
