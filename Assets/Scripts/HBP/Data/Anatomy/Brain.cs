using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;
using CielaSpike;

namespace HBP.Data.Anatomy
{
    /**
    * \class Brain
    * \author Adrien Gannerie
    * \version 1.0
    * \date 03 janvier 2017
    * \brief Brain anatomic data.
    * 
    * \details Brain anatomic data which contains:
    *   - \a Left cerebral hemisphere mesh.
    *   - \a Right cerebral hemispheres mesh.
    *   - \a Pre-operation MRI.
    *   - \a Post-operation MRI.
    *   - \a Patient reference frame implantation.
    *   - \a MNI reference frame implantation.
    *   - \a Pre-operation reference frame to scanner reference frame transformation. 
    *   - \a Plots connectivity.
    */
    [DataContract]
    public class Brain : ICloneable
    {
        #region Properties
        public enum Error
        {
            LeftMeshEmpty, LeftMeshNotFound, LeftMeshWrongFile, RightMeshEmpty, RightMeshNotFound, RightMeshWrongFile, PreoperativeMRIEmpty, ImplantationEmpty
        }

        [DataMember] public Mesh[] Meshes { get; set; }
        [DataMember] public MRI[] MRIs { get; set; }
        [DataMember] public Connectivity[] Connectivities { get; set; }
        [DataMember] public Implantation[] Implantations { get; set; }
        [DataMember] public Transformation[] Transformations { get; set; }
        [DataMember] public Epilepsy Epilepsy { get; set; }
        //[IgnoreDataMember] public Patient Patient { get; set; }
        #endregion

        #region Constructors
        public Brain(IEnumerable<Mesh> meshes, IEnumerable<MRI> MRIs, IEnumerable<Connectivity> connectivities, IEnumerable<Implantation> implantations, IEnumerable<Transformation> transformations, Epilepsy epilepsy)
        {
            Meshes = meshes.ToArray();
            this.MRIs = MRIs.ToArray();
            Connectivities = connectivities.ToArray();
            Implantations = implantations.ToArray();
            Transformations = transformations.ToArray();
            Epilepsy = epilepsy;
        }
        public Brain() : this(new Mesh[0], new MRI[0], new Connectivity[0], new Implantation[0], new Transformation[0], new Epilepsy()) { }
        #endregion

        #region Public Methods
     
        //public Error[] GetVisualizableErrors(ReferenceFrameType referenceFrame)
        //{
        //    List<Error> errors = new List<Error>();
        //    switch (referenceFrame)
        //    {
        //        case ReferenceFrameType.Patient:
        //            if (string.IsNullOrEmpty(LeftHemisphereGreyMatter)) errors.Add(Error.LeftMeshEmpty);
        //            if (string.IsNullOrEmpty(RightHemisphereGreyMatter)) errors.Add(Error.RightMeshEmpty);
        //            if (string.IsNullOrEmpty(PreoperativeMRI)) errors.Add(Error.PreoperativeMRIEmpty);
        //            if (string.IsNullOrEmpty(PatientBasedImplantation)) errors.Add(Error.ImplantationEmpty);
        //            if(!errors.Contains(Error.LeftMeshEmpty))
        //            {
                        
        //            }
        //            break;
        //        case ReferenceFrameType.MNI:
        //            if (string.IsNullOrEmpty(PreoperativeMRI)) errors.Add(Error.PreoperativeMRIEmpty);
        //            if (string.IsNullOrEmpty(MNIBasedImplantation)) errors.Add(Error.ImplantationEmpty);
        //            break;
        //        default:
        //            break;
        //    }
        //    return errors.ToArray();
        //}
        //public bool IsVisualizable(ReferenceFrameType referenceFrame)
        //{
        //    if (GetVisualizableErrors(referenceFrame).Length == 0) return true;
        //    else return false;
        //}
        //public void LoadImplantations()
        //{
        //    Implantation.Load(PatientBasedImplantation, ReferenceFrameType.Patient);
        //    Implantation.Load(MNIBasedImplantation, ReferenceFrameType.MNI);
        //}
        //public void LoadImplantationsAsyn()
        //{
        //    ApplicationState.CoroutineManager.Add(LoadAsyn());
        //}
        //public void UnloadImplantations()
        //{
        //    Implantation.Unload();
        //}
        #endregion

        #region Private Methods
        IEnumerator LoadAsyn()
        {
            yield return Ninja.JumpBack;
            Implantation.Load(PatientBasedImplantation, ReferenceFrameType.Patient);
            Implantation.Load(MNIBasedImplantation, ReferenceFrameType.MNI);
            yield return Ninja.JumpToUnity;
        }
        Error[] GetMeshErrors()
        {
            List<Error> errors = new List<Error>();

            //Left Mesh.
            if (string.IsNullOrEmpty(LeftHemisphereGreyMatter))
            {
                errors.Add(Error.LeftMeshEmpty);
            }
            else
            {
                FileInfo leftMeshFileInfo = new FileInfo(LeftHemisphereGreyMatter);
                if (!leftMeshFileInfo.Exists)
                {
                    errors.Add(Error.LeftMeshNotFound);
                }
                else
                {
                    if(leftMeshFileInfo.Extension != MESH_EXTENSION)
                    {
                        errors.Add(Error.LeftMeshWrongFile);
                    }
                }
            }

            //Right Mesh.
            if (string.IsNullOrEmpty(RightHemisphereGreyMatter))
            {
                errors.Add(Error.RightMeshEmpty);
            }
            else
            {
                FileInfo rightMeshFileInfo = new FileInfo(RightHemisphereGreyMatter);
                if (!rightMeshFileInfo.Exists)
                {
                    errors.Add(Error.RightMeshNotFound);
                }
                else
                {
                    if (rightMeshFileInfo.Extension != MESH_EXTENSION)
                    {
                        errors.Add(Error.RightMeshWrongFile);
                    }
                }
            }
            return errors.ToArray();
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone The object.
        /// </summary>
        /// <returns>Object cloned.</returns>
        public object Clone()
        {
            return new Brain(Epilepsy.Clone() as Epilepsy, LeftHemisphereGreyMatter, RightHemisphereGreyMatter, PreoperativeMRI, PostoperativeMRI, PatientBasedImplantation, MNIBasedImplantation, PreoperativeBasedToScannerBasedTransformation, SitesConnectivities);
        }
        #endregion
        
        #region Serialization
        [OnDeserialized]
        void OnDeserialized(StreamingContext context)
        {
            LoadImplantations();
        }
        #endregion
    }
}