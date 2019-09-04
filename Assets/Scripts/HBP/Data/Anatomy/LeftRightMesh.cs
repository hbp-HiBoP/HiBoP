using System;
using System.IO;
using System.Runtime.Serialization;
using Tools.Unity;

namespace HBP.Data.Anatomy
{
    [DataContract]
    public class LeftRightMesh : Mesh
    {
        #region Properties
        [DataMember(Order = 1, Name = "LeftHemisphere")] public string SavedLeftHemisphere { get; protected set; }
        public string LeftHemisphere
        {
            get
            {
                return SavedLeftHemisphere.ConvertToFullPath();
            }
            set
            {
                SavedLeftHemisphere = value.ConvertToShortPath();
            }
        }
        [DataMember(Order = 2, Name = "RightHemisphere")] public string SavedRightHemisphere { get; protected set; }
        public string RightHemisphere
        {
            get
            {
                return SavedRightHemisphere.ConvertToFullPath();
            }
            set
            {
                SavedRightHemisphere = value.ConvertToShortPath();
            }
        }
        [DataMember(Order = 3, Name = "LeftMarsAtlasHemisphere")] public string SavedLeftMarsAtlasHemisphere { get; protected set; }
        public string LeftMarsAtlasHemisphere
        {
            get
            {
                return SavedLeftMarsAtlasHemisphere.ConvertToFullPath();
            }
            set
            {
                SavedLeftMarsAtlasHemisphere = value.ConvertToShortPath();
            }
        }
        [DataMember(Order = 4, Name = "RightMarsAtlasHemisphere")] public string SavedRightMarsAtlasHemisphere { get; protected set; }
        public string RightMarsAtlasHemisphere
        {
            get
            {
                return SavedRightMarsAtlasHemisphere.ConvertToFullPath();
            }
            set
            {
                SavedRightMarsAtlasHemisphere = value.ConvertToShortPath();
            }
        }
        public override bool HasMesh
        {
            get
            {
                return !string.IsNullOrEmpty(LeftHemisphere) && !string.IsNullOrEmpty(RightHemisphere) && File.Exists(LeftHemisphere) && File.Exists(RightHemisphere) && new FileInfo(LeftHemisphere).Extension == MESH_EXTENSION && new FileInfo(RightHemisphere).Extension == MESH_EXTENSION;
            }
        }
        public override bool HasMarsAtlas
        {
            get
            {
                return !string.IsNullOrEmpty(LeftMarsAtlasHemisphere) && !string.IsNullOrEmpty(RightMarsAtlasHemisphere) && File.Exists(LeftMarsAtlasHemisphere) && File.Exists(RightMarsAtlasHemisphere) && new FileInfo(LeftMarsAtlasHemisphere).Extension == MESH_EXTENSION && new FileInfo(RightMarsAtlasHemisphere).Extension == MESH_EXTENSION;
            }
        }
        #endregion

        #region Constructors
        public LeftRightMesh(string name, string transformation, string ID, string leftHemisphere, string rightHemisphere, string leftMarsAtlasHemisphere, string rightMarsAtlasHemisphere) : base(name, transformation, ID)
        {
            LeftHemisphere = leftHemisphere;
            RightHemisphere = rightHemisphere;
            LeftMarsAtlasHemisphere = leftMarsAtlasHemisphere;
            RightMarsAtlasHemisphere = rightMarsAtlasHemisphere;
            RecalculateUsable();
        }
        public LeftRightMesh(string name, string transformation, string leftHemisphere, string rightHemisphere, string leftMarsAtlasHemisphere, string rightMarsAtlasHemisphere) : base(name, transformation)
        {
            LeftHemisphere = leftHemisphere;
            RightHemisphere = rightHemisphere;
            LeftMarsAtlasHemisphere = leftMarsAtlasHemisphere;
            RightMarsAtlasHemisphere = rightMarsAtlasHemisphere;
            RecalculateUsable();
        }
        public LeftRightMesh():this("New mesh", string.Empty, Guid.NewGuid().ToString(), string.Empty, string.Empty, string.Empty,string.Empty) { }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new LeftRightMesh(Name, Transformation, ID, LeftHemisphere, RightHemisphere, LeftMarsAtlasHemisphere, RightMarsAtlasHemisphere);
        }
        public override void Copy(object obj)
        {
            base.Copy(obj);
            if(obj is LeftRightMesh leftRightMesh)
            {
                LeftHemisphere = leftRightMesh.LeftHemisphere;
                RightHemisphere = leftRightMesh.RightHemisphere;
                LeftMarsAtlasHemisphere = leftRightMesh.LeftMarsAtlasHemisphere;
                RightMarsAtlasHemisphere = leftRightMesh.RightMarsAtlasHemisphere;
            }
        }
        #endregion

        #region Serialization
        protected override void OnDeserializedOperation(StreamingContext context)
        {
            SavedLeftHemisphere = SavedLeftHemisphere.ToPath();
            SavedRightHemisphere = SavedRightHemisphere.ToPath();
            SavedLeftMarsAtlasHemisphere = SavedLeftMarsAtlasHemisphere.ToPath();
            SavedRightMarsAtlasHemisphere = SavedRightMarsAtlasHemisphere.ToPath();
            base.OnDeserializedOperation(context);
        }
        #endregion
    }
}