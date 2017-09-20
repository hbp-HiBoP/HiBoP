using System;
using System.IO;
using System.Runtime.Serialization;

namespace HBP.Data.Anatomy
{
    [DataContract]
    public class LeftRightMesh : Mesh
    {
        #region Properties
        [DataMember(Order = 1)] public string LeftHemisphere { get; set; }
        [DataMember(Order = 2)] public string RightHemisphere { get; set; }
        [DataMember(Order = 3)] public string LeftMarsAtlasHemisphere { get; set; }
        [DataMember(Order = 4)] public string RightMarsAtlasHemisphere { get; set; }
        public override bool Usable
        {
            get
            {
                return base.Usable && HasMesh;
            }
        }
        public override bool HasMesh
        {
            get
            {
                return !string.IsNullOrEmpty(LeftHemisphere) && !string.IsNullOrEmpty(RightHemisphere) && File.Exists(LeftHemisphere) && File.Exists(RightHemisphere) && new FileInfo(LeftHemisphere).Extension == EXTENSION && new FileInfo(RightHemisphere).Extension == EXTENSION;
            }
        }
        public override bool HasMarsAtlas
        {
            get
            {
                return !string.IsNullOrEmpty(LeftMarsAtlasHemisphere) && !string.IsNullOrEmpty(RightMarsAtlasHemisphere) && File.Exists(LeftMarsAtlasHemisphere) && File.Exists(RightMarsAtlasHemisphere) && new FileInfo(LeftMarsAtlasHemisphere).Extension == EXTENSION && new FileInfo(RightMarsAtlasHemisphere).Extension == EXTENSION;
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
        }
        public LeftRightMesh(string name, string transformation, string leftHemisphere, string rightHemisphere, string leftMarsAtlasHemisphere, string rightMarsAtlasHemisphere) : base(name, transformation)
        {
            LeftHemisphere = leftHemisphere;
            RightHemisphere = rightHemisphere;
            LeftMarsAtlasHemisphere = leftMarsAtlasHemisphere;
            RightMarsAtlasHemisphere = rightMarsAtlasHemisphere;
        }
        public LeftRightMesh():this("New mesh", string.Empty, Guid.NewGuid().ToString(), string.Empty, string.Empty, string.Empty,string.Empty) { }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new LeftRightMesh(Name,Transformation, ID, LeftHemisphere, RightHemisphere,LeftMarsAtlasHemisphere,RightMarsAtlasHemisphere);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);

            LeftRightMesh mesh = copy as LeftRightMesh;
            LeftHemisphere = mesh.LeftHemisphere;
            RightHemisphere = mesh.RightHemisphere;
            LeftMarsAtlasHemisphere = mesh.LeftMarsAtlasHemisphere;
            RightMarsAtlasHemisphere = mesh.RightMarsAtlasHemisphere;
        }
        #endregion
    }
}