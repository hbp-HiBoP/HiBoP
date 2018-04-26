using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.Module3D
{
    public class MRI3D : ICloneable
    {
        #region Properties
        public string Name { get; set; }

        private DLL.NIFTI m_NII = new DLL.NIFTI();
        public DLL.NIFTI NII
        {
            get
            {
                while (m_IsLoading) System.Threading.Thread.Sleep(10);
                if (!IsLoaded) Load();
                return m_NII;
            }
            protected set
            {
                m_NII = value;
            }
        }

        private DLL.Volume m_Volume = new DLL.Volume();
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
        public bool IsLoaded
        {
            get
            {
                return m_NII.IsLoaded;
            }
        }

        protected bool m_IsLoading = false;

        protected Data.Anatomy.MRI m_MRI;
        #endregion

        #region Constructors
        public MRI3D(Data.Anatomy.MRI mri)
        {
            Name = mri.Name;
            m_MRI = mri;
            if (ApplicationState.UserPreferences.Data.Anatomy.PreloadMRIs)
            {
                Load();
            }
        }
        public MRI3D(string name, DLL.NIFTI nii, DLL.Volume volume)
        {
            Name = name;
            NII = nii;
            Volume = volume;
        }
        public MRI3D() { }
        #endregion

        #region Public Methods
        public object Clone()
        {
            MRI3D mri = new MRI3D();
            mri.Name = Name;
            mri.NII = NII;
            mri.Volume = Volume;
            return mri;
        }
        public void Load()
        {
            m_IsLoading = true;
            if (m_NII.LoadNIIFile(m_MRI.File))
            {
                m_NII.ConvertToVolume(m_Volume);
            }
            m_IsLoading = false;
        }
        #endregion
    }
}