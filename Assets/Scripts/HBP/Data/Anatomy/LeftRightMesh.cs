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
        [DataMember(Order = 1, Name = "LeftHemisphere")] string m_LeftHemisphere;
        public string LeftHemisphere
        {
            get
            {
                return m_LeftHemisphere.ConvertToFullPath();
            }
            set
            {
                m_LeftHemisphere = value.ConvertToShortPath();
            }
        }
        public string SavedLeftHemisphere { get { return m_LeftHemisphere; } }
        [DataMember(Order = 2, Name = "RightHemisphere")] string m_RightHemisphere;
        public string RightHemisphere
        {
            get
            {
                return m_RightHemisphere.ConvertToFullPath();
            }
            set
            {
                m_RightHemisphere = value.ConvertToShortPath();
            }
        }
        public string SavedRightHemisphere { get { return m_RightHemisphere; } }
        [DataMember(Order = 3, Name = "LeftMarsAtlasHemisphere")] string m_LeftMarsAtlasHemisphere;
        public string LeftMarsAtlasHemisphere
        {
            get
            {
                return m_LeftMarsAtlasHemisphere.ConvertToFullPath();
            }
            set
            {
                m_LeftMarsAtlasHemisphere = value.ConvertToShortPath();
            }
        }
        public string SavedLeftMarsAtlasHemisphere { get { return m_LeftMarsAtlasHemisphere; } }
        [DataMember(Order = 4, Name = "RightMarsAtlasHemisphere")] string m_RightMarsAtlasHemisphere;
        public string RightMarsAtlasHemisphere
        {
            get
            {
                return m_RightMarsAtlasHemisphere.ConvertToFullPath();
            }
            set
            {
                m_RightMarsAtlasHemisphere = value.ConvertToShortPath();
            }
        }
        public string SavedRightMarsAtlasHemisphere { get { return m_RightMarsAtlasHemisphere; } }
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

        #region Serialization
        protected override void OnDeserializedOperation(StreamingContext context)
        {
            m_LeftHemisphere = m_LeftHemisphere.ToPath();
            m_RightHemisphere = m_RightHemisphere.ToPath();
            m_LeftMarsAtlasHemisphere = m_LeftMarsAtlasHemisphere.ToPath();
            m_RightMarsAtlasHemisphere = m_RightMarsAtlasHemisphere.ToPath();
            base.OnDeserializedOperation(context);
        }
        #endregion
    }
}