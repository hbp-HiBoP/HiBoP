using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.Module3D
{
    public abstract class Mesh3D
    {
        #region Properties
        public string Name { get; set; }
        public DLL.Surface Both { get; set; }
        public DLL.Surface SimplifiedBoth { get; set; }
        public List<DLL.Surface> SplittedMeshes { get; set; }
        public bool IsLoaded
        {
            get
            {
                return Both.IsLoaded;
            }
        }
        public bool IsMarsAtlasLoaded
        {
            get
            {
                return Both.IsMarsAtlasLoaded;
            }
        }
        #endregion

        #region Constructors
        public Mesh3D(Data.Anatomy.Mesh mesh)
        {
            Name = mesh.Name;
        }
        public Mesh3D() { }
        #endregion
    }

    public class SingleMesh3D : Mesh3D
    {
        #region Properties

        #endregion

        #region Constructors
        public SingleMesh3D(Data.Anatomy.SingleMesh mesh) : base(mesh)
        {
            Both = new DLL.Surface();
            if (Both.LoadGIIFile(mesh.Path, true, mesh.Transformation))
            {
                Both.FlipTriangles();
                Both.ComputeNormals();
                Both.SearchMarsParcelFileAndUpdateColors(ApplicationState.Module3D.MarsAtlasIndex, mesh.MarsAtlasPath);
                SimplifiedBoth = Both.Simplify();
            }
        }
        #endregion
    }

    public class LeftRightMesh3D : Mesh3D
    {
        #region Properties
        public DLL.Surface Left { get; set; }
        public DLL.Surface Right { get; set; }
        #endregion

        #region Constructors
        public LeftRightMesh3D(Data.Anatomy.LeftRightMesh mesh) : base(mesh)
        {
            Left = new DLL.Surface();
            if (Left.LoadGIIFile(mesh.LeftHemisphere, true, mesh.Transformation))
            {
                Left.FlipTriangles();
                Left.ComputeNormals();
                Left.SearchMarsParcelFileAndUpdateColors(ApplicationState.Module3D.MarsAtlasIndex, mesh.LeftMarsAtlasHemisphere);
            }

            Right = new DLL.Surface();
            if (Right.LoadGIIFile(mesh.RightHemisphere, true, mesh.Transformation))
            {
                Right.FlipTriangles();
                Right.ComputeNormals();
                Right.SearchMarsParcelFileAndUpdateColors(ApplicationState.Module3D.MarsAtlasIndex, mesh.RightMarsAtlasHemisphere);
            }
            
            if (Left.IsLoaded && Right.IsLoaded)
            {
                Both = (DLL.Surface)Left.Clone();
                Both.Append(Right);
                SimplifiedBoth = Both.Simplify();
            }
            else
            {
                Both = new DLL.Surface();
            }
        }
        public LeftRightMesh3D(string name, DLL.Surface left, DLL.Surface right, DLL.Surface both)
        {
            Name = name;
            Left = left;
            Right = right;
            Both = both;
            SimplifiedBoth = both.Simplify();
        }
        #endregion
    }
}