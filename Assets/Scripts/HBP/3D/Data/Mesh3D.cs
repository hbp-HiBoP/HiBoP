using System;

namespace HBP.Core.Object3D
{
    /// <summary>
    /// This class contains information about a mesh and can load meshes to DLL objects
    /// </summary>
    public abstract class Mesh3D : ICloneable
    {
        #region Properties
        /// <summary>
        /// Name of the mesh
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Type of the mesh (Patient or MNI)
        /// </summary>
        public Data.Enums.MeshType Type { get; protected set; }

        protected DLL.Surface m_Both;
        /// <summary>
        /// DLL surface containing data for the whole brain mesh
        /// </summary>
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
        /// <summary>
        /// DLL surface containing data for the whole simplified brain mesh
        /// </summary>
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

        /// <summary>
        /// Is the 3D mesh completely loaded ?
        /// </summary>
        public bool IsLoaded
        {
            get
            {
                return m_Both != null ? m_Both.IsLoaded : false;
            }
        }
        /// <summary>
        /// Is mars atlas loaded for this mesh ?
        /// </summary>
        public bool IsMarsAtlasLoaded
        {
            get
            {
                return m_Both != null ? m_Both.IsMarsAtlasLoaded : false;
            }
        }
        /// <summary>
        /// Is the mesh currently loading ?
        /// </summary>
        protected bool m_IsLoading = false;
        /// <summary>
        /// Does the mesh have been loaded outside of a scene and copied to the scene (e.g. MNI objects) ?
        /// </summary>
        public bool HasBeenLoadedOutside { get; protected set; }
        /// <summary>
        /// Data of the mesh (paths etc.)
        /// </summary>
        protected Data.BaseMesh m_Mesh;
        #endregion

        #region Constructors
        public Mesh3D(Data.BaseMesh mesh, Data.Enums.MeshType type, bool load)
        {
            m_Mesh = mesh;
            Name = mesh.Name;
            Type = type;
            if (load) Load();
            HasBeenLoadedOutside = false;
        }
        public Mesh3D() { }
        #endregion

        #region Public Methods
        /// <summary>
        /// Load the mesh to DLL objects
        /// </summary>
        public abstract void Load();
        /// <summary>
        /// Dispose all DLL objects
        /// </summary>
        public virtual void Clean()
        {
            m_Both?.Dispose();
            m_SimplifiedBoth?.Dispose();
        }
        public abstract object Clone();
        #endregion
    }

    /// <summary>
    /// Subclass of <see cref="Mesh3D"/> that contains data for a mesh in one piece
    /// </summary>
    public class SingleMesh3D : Mesh3D
    {
        #region Constructors
        public SingleMesh3D(Data.SingleMesh mesh, Data.Enums.MeshType type, bool load) : base(mesh, type, load) { }
        public SingleMesh3D() { }
        #endregion

        #region Public Methods
        /// <summary>
        /// Load the mesh to DLL objects
        /// </summary>
        public override void Load()
        {
            m_IsLoading = true;
            Data.SingleMesh mesh = m_Mesh as Data.SingleMesh;

            m_Both = new DLL.Surface();
            if (m_Both.LoadGIIFile(mesh.Path, mesh.Transformation))
            {
                m_Both.FlipTriangles();
                m_Both.ComputeNormals();
                if (ApplicationState.Module3D.MarsAtlas.Loaded)
                    m_Both.SearchMarsParcelFileAndUpdateColors(ApplicationState.Module3D.MarsAtlas, mesh.MarsAtlasPath);
                SimplifiedBoth = m_Both.Simplify();
            }
            m_IsLoading = false;
        }
        public override object Clone()
        {
            SingleMesh3D mesh = new SingleMesh3D
            {
                Name = Name,
                Type = Type,
                Both = Both,
                SimplifiedBoth = SimplifiedBoth,
                m_Mesh = m_Mesh,
                HasBeenLoadedOutside = HasBeenLoadedOutside
            };
            return mesh;
        }
        #endregion
    }

    /// <summary>
    /// Subclass of <see cref="Mesh3D"/> that contains data for a mesh divided in two hemispheres
    /// </summary>
    public class LeftRightMesh3D : Mesh3D
    {
        #region Properties
        protected DLL.Surface m_Left;
        /// <summary>
        /// DLL surface containing data for the left part of the mesh
        /// </summary>
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
        /// <summary>
        /// DLL surface containing data for the right part of the mesh
        /// </summary>
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
        /// <summary>
        /// DLL surface containing data for the left simplified part of the mesh
        /// </summary>
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
        /// <summary>
        /// DLL surface containing data for the right simplified part of the mesh
        /// </summary>
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
        public LeftRightMesh3D(Data.LeftRightMesh mesh, Data.Enums.MeshType type, bool load) : base(mesh, type, load) { }
        public LeftRightMesh3D(string name, DLL.Surface left, DLL.Surface right, DLL.Surface both, Data.Enums.MeshType type)
        {
            Name = name;
            Type = type;
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
        /// <summary>
        /// Load the mesh to DLL objects
        /// </summary>
        public override void Load()
        {
            m_IsLoading = true;
            Data.LeftRightMesh mesh = m_Mesh as Data.LeftRightMesh;
            m_Left = new DLL.Surface();
            if (m_Left.LoadGIIFile(mesh.LeftHemisphere, mesh.Transformation))
            {
                m_Left.FlipTriangles();
                m_Left.ComputeNormals();
                if (ApplicationState.Module3D.MarsAtlas.Loaded)
                    m_Left.SearchMarsParcelFileAndUpdateColors(ApplicationState.Module3D.MarsAtlas, mesh.LeftMarsAtlasHemisphere);
                SimplifiedLeft = m_Left.Simplify();
            }

            m_Right = new DLL.Surface();
            if (m_Right.LoadGIIFile(mesh.RightHemisphere, mesh.Transformation))
            {
                m_Right.FlipTriangles();
                m_Right.ComputeNormals();
                if (ApplicationState.Module3D.MarsAtlas.Loaded)
                    m_Right.SearchMarsParcelFileAndUpdateColors(ApplicationState.Module3D.MarsAtlas, mesh.RightMarsAtlasHemisphere);
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
        /// <summary>
        /// Dispose all DLL objects
        /// </summary>
        public override void Clean()
        {
            base.Clean();
            m_Left?.Dispose();
            m_Right?.Dispose();
            m_SimplifiedLeft?.Dispose();
            m_SimplifiedRight?.Dispose();
        }
        public override object Clone()
        {
            LeftRightMesh3D mesh = new LeftRightMesh3D
            {
                Name = Name,
                Type = Type,
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
        #endregion
    }
}