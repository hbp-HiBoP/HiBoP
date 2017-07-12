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
        public override bool isUsable
        {
            get
            {
                return base.isUsable && !string.IsNullOrEmpty(LeftHemisphere) && !string.IsNullOrEmpty(RightHemisphere) && File.Exists(LeftHemisphere) && File.Exists(RightHemisphere) && new FileInfo(LeftHemisphere).Extension == EXTENSION && new FileInfo(RightHemisphere).Extension == EXTENSION;
            }
        }
        #endregion

        #region Constructors
        public LeftRightMesh(string name, string leftHemisphere, string rightHemisphere) : base(name)
        {
            LeftHemisphere = leftHemisphere;
            RightHemisphere = rightHemisphere;
        }
        public LeftRightMesh():this("New mesh", string.Empty, string.Empty) { }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new LeftRightMesh(Name, LeftHemisphere, RightHemisphere);

        }
        public override void Copy(object copy)
        {
            LeftRightMesh mesh = copy as LeftRightMesh;
            Name = mesh.Name;
            LeftHemisphere = mesh.LeftHemisphere;
            RightHemisphere = mesh.RightHemisphere;
        }
        #endregion
    }
}