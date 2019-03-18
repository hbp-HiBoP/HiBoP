using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HBP.Module3D
{
    public abstract class Mesh3D : ICloneable
    {
        #region Properties
        public string Name { get; set; }

        protected DLL.Surface m_Both;
        public DLL.Surface Both
        {
            get
            {
                while (m_IsLoading) System.Threading.Thread.Sleep(10);
                if (!IsLoaded) Load();
                return m_Both;
            }
            protected set
            {
                m_Both = value;
            }
        }

        protected DLL.Surface m_SimplifiedBoth;
        public DLL.Surface SimplifiedBoth
        {
            get
            {
                while (m_IsLoading) System.Threading.Thread.Sleep(10);
                if (!IsLoaded) Load();
                return m_SimplifiedBoth;
            }
            protected set
            {
                m_SimplifiedBoth = value;
            }
        }

        public bool IsLoaded
        {
            get
            {
                return m_Both.IsLoaded;
            }
        }
        public bool IsMarsAtlasLoaded
        {
            get
            {
                return m_Both.IsMarsAtlasLoaded;
            }
        }
        protected bool m_IsLoading = false;
        public bool HasBeenLoadedOutside { get; protected set; }

        protected Data.Anatomy.Mesh m_Mesh;
        #endregion

        #region Constructors
        public Mesh3D(Data.Anatomy.Mesh mesh)
        {
            m_Mesh = mesh;
            Name = mesh.Name;
            if (ApplicationState.UserPreferences.Data.Anatomic.MeshPreloading)
            {
                Load();
            }
            HasBeenLoadedOutside = false;
        }
        public Mesh3D() { }
        #endregion

        #region Public Methods
        public abstract object Clone();
        public abstract void Load();
        public virtual void Clean()
        {
            m_Both?.Dispose();
            m_SimplifiedBoth?.Dispose();
        }
        #endregion
    }

    public class SingleMesh3D : Mesh3D
    {
        #region Constructors
        public SingleMesh3D(Data.Anatomy.SingleMesh mesh) : base(mesh) { }
        public SingleMesh3D() { }
        #endregion

        #region Public Methods
        public override object Clone()
        {
            SingleMesh3D mesh = new SingleMesh3D
            {
                Name = Name,
                Both = Both,
                SimplifiedBoth = SimplifiedBoth,
                m_Mesh = m_Mesh,
                HasBeenLoadedOutside = HasBeenLoadedOutside
            };
            return mesh;
        }
        public override void Load()
        {
            m_IsLoading = true;
            Data.Anatomy.SingleMesh mesh = m_Mesh as Data.Anatomy.SingleMesh;

            m_Both = new DLL.Surface();
            if (m_Both.LoadGIIFile(mesh.Path, true, mesh.Transformation))
            {
                m_Both.FlipTriangles();
                m_Both.ComputeNormals();
                m_Both.SearchMarsParcelFileAndUpdateColors(ApplicationState.Module3D.MarsAtlasIndex, mesh.MarsAtlasPath);
                SimplifiedBoth = m_Both.Simplify();
            }
            m_IsLoading = false;
        }
        #endregion
    }

    public class LeftRightMesh3D : Mesh3D
    {
        #region Properties
        protected DLL.Surface m_Left;
        public DLL.Surface Left
        {
            get
            {
                while (m_IsLoading) System.Threading.Thread.Sleep(10);
                if (!IsLoaded) Load();
                return m_Left;
            }
            protected set
            {
                m_Left = value;
            }
        }

        protected DLL.Surface m_Right;
        public DLL.Surface Right
        {
            get
            {
                while (m_IsLoading) System.Threading.Thread.Sleep(10);
                if (!IsLoaded) Load();
                return m_Right;
            }
            protected set
            {
                m_Right = value;
            }
        }

        protected DLL.Surface m_SimplifiedLeft;
        public DLL.Surface SimplifiedLeft
        {
            get
            {
                while (m_IsLoading) System.Threading.Thread.Sleep(10);
                if (!IsLoaded) Load();
                return m_SimplifiedLeft;
            }
            protected set
            {
                m_SimplifiedLeft = value;
            }
        }

        protected DLL.Surface m_SimplifiedRight;
        public DLL.Surface SimplifiedRight
        {
            get
            {
                while (m_IsLoading) System.Threading.Thread.Sleep(10);
                if (!IsLoaded) Load();
                return m_SimplifiedRight;
            }
            protected set
            {
                m_SimplifiedRight = value;
            }
        }
        #endregion

        #region Constructors
        public LeftRightMesh3D(Data.Anatomy.LeftRightMesh mesh) : base(mesh) { }
        public LeftRightMesh3D(string name, DLL.Surface left, DLL.Surface right, DLL.Surface both)
        {
            Name = name;
            Left = left;
            Right = right;
            Both = both;
            SimplifiedLeft = left.Simplify();
            SimplifiedRight = right.Simplify();
            SimplifiedBoth = both.Simplify();
            HasBeenLoadedOutside = true;
        }
        public LeftRightMesh3D() { }
        #endregion

        #region Public Methods
        public override object Clone()
        {
            LeftRightMesh3D mesh = new LeftRightMesh3D
            {
                Name = Name,
                Both = Both,
                SimplifiedBoth = SimplifiedBoth,
                Left = Left,
                Right = Right,
                SimplifiedLeft = SimplifiedLeft,
                SimplifiedRight = SimplifiedRight,
                m_Mesh = m_Mesh,
                HasBeenLoadedOutside = HasBeenLoadedOutside
            };
            return mesh;
        }
        public override void Load()
        {
            m_IsLoading = true;
            Data.Anatomy.LeftRightMesh mesh = m_Mesh as Data.Anatomy.LeftRightMesh;
            m_Left = new DLL.Surface();
            if (m_Left.LoadGIIFile(mesh.LeftHemisphere, true, mesh.Transformation))
            {
                m_Left.FlipTriangles();
                m_Left.ComputeNormals();
                m_Left.SearchMarsParcelFileAndUpdateColors(ApplicationState.Module3D.MarsAtlasIndex, mesh.LeftMarsAtlasHemisphere);
                SimplifiedLeft = m_Left.Simplify();
            }

            m_Right = new DLL.Surface();
            if (m_Right.LoadGIIFile(mesh.RightHemisphere, true, mesh.Transformation))
            {
                m_Right.FlipTriangles();
                m_Right.ComputeNormals();
                m_Right.SearchMarsParcelFileAndUpdateColors(ApplicationState.Module3D.MarsAtlasIndex, mesh.RightMarsAtlasHemisphere);
                SimplifiedRight = m_Right.Simplify();
            }

            if (m_Left.IsLoaded && m_Right.IsLoaded)
            {
                m_Both = (DLL.Surface)m_Left.Clone();
                m_Both.Append(m_Right);
                SimplifiedBoth = m_Both.Simplify();
            }
            m_IsLoading = false;
        }
        public override void Clean()
        {
            base.Clean();
            m_Left?.Dispose();
            m_Right?.Dispose();
            m_SimplifiedLeft?.Dispose();
            m_SimplifiedRight?.Dispose();
        }
        #endregion
    }
}