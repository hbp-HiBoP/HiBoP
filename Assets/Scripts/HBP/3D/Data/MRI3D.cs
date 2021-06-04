using System;

namespace HBP.Module3D
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

        private DLL.Volume m_Volume = new DLL.Volume();
        /// <summary>
        /// Volume of this MRI
        /// </summary>
        public DLL.Volume Volume
        {
            get
            {
                while (m_IsLoading) System.Threading.Thread.Sleep(10);
                if (!IsLoaded) Load();
                return m_Volume;
            }
            protected set
            {
                m_Volume = value;
            }
        }

        /// <summary>
        /// Is the MRI completely loaded ?
        /// </summary>
        public bool IsLoaded
        {
            get
            {
                return m_Volume != null ? m_Volume.IsLoaded : false;
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
        public MRI3D(string name, DLL.Volume volume)
        {
            Name = name;
            Volume = volume;
            HasBeenLoadedOutside = true;
        }
        public MRI3D() { }
        #endregion

        #region Public Methods
        /// <summary>
        /// Load the mri to DLL objects
        /// </summary>
        public void Load()
        {
            m_IsLoading = true;
            m_Volume.LoadNIFTIFile(m_MRI.File);
            m_IsLoading = false;
        }
        /// <summary>
        /// Dispose all DLL objects
        /// </summary>
        public void Clean()
        {
            m_Volume?.Dispose();
        }
        public object Clone()
        {
            MRI3D mri = new MRI3D
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