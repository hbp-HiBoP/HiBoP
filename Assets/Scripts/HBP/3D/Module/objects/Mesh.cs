﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.Module3D
{
    public abstract class Mesh3D
    {
        #region Properties
        public DLL.Surface Both { get; set; }
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
    }

    public class SingleMesh3D : Mesh3D
    {
        #region Properties

        #endregion

        #region Constructors
        public SingleMesh3D(Data.Anatomy.SingleMesh mesh, Data.Anatomy.Transformation transformation)
        {
            Both = new DLL.Surface();
            if (Both.LoadGIIFile(mesh.Path, transformation.Path))
            {
                Both.FlipTriangles();
                Both.ComputeNormals();
                Both.SearchMarsParcelFileAndUpdateColors(ApplicationState.Module3D.MarsAtlasIndex, mesh.MarsAtlasPath);
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
        public LeftRightMesh3D(Data.Anatomy.LeftRightMesh mesh, Data.Anatomy.Transformation transformation)
        {
            Left = new DLL.Surface();
            if (Left.LoadGIIFile(mesh.LeftHemisphere, transformation.Path))
            {
                Left.FlipTriangles();
                Left.ComputeNormals();
                Left.SearchMarsParcelFileAndUpdateColors(ApplicationState.Module3D.MarsAtlasIndex, mesh.LeftMarsAtlasHemisphere);
            }

            Right = new DLL.Surface();
            if (Right.LoadGIIFile(mesh.RightHemisphere, transformation.Path))
            {
                Right.FlipTriangles();
                Right.ComputeNormals();
                Right.SearchMarsParcelFileAndUpdateColors(ApplicationState.Module3D.MarsAtlasIndex, mesh.RightMarsAtlasHemisphere);
            }
            
            if (Left.IsLoaded && Right.IsLoaded)
            {
                Both = (DLL.Surface)Left.Clone();
                Both.Append(Right);
            }
            else
            {
                Both = new DLL.Surface();
            }
        }
        #endregion
    }
}