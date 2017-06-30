using System.Runtime.Serialization;

namespace HBP.Data.Anatomy
{
    [DataContract]
    public class LeftRightMesh : Mesh
    {
        #region Properties
        [DataMember] public string LeftHemisphere { get; set; }
        [DataMember] public string RightHemisphere { get; set; }
        #endregion

        #region Constructors
        public LeftRightMesh(string name, string leftHemisphere, string rightHemisphere) : base(name)
        {
            LeftHemisphere = leftHemisphere;
            RightHemisphere = rightHemisphere;
        }
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