using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;

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
        [DataMember] public List<Mesh> Meshes { get; set; }
        [DataMember] public List<MRI> MRIs { get; set; }
        [DataMember] public List<Connectivity> Connectivities { get; set; }
        [DataMember] public List<Implantation> Implantations { get; set; }
        [DataMember] public Epilepsy Epilepsy { get; set; }
        [IgnoreDataMember] public Patient Patient { get; set; }
        #endregion

        #region Constructors
        public Brain(IEnumerable<Mesh> meshes, IEnumerable<MRI> MRIs, IEnumerable<Connectivity> connectivities, IEnumerable<Implantation> implantations, Epilepsy epilepsy)
        {
            Meshes = meshes.ToList();
            this.MRIs = MRIs.ToList();
            Connectivities = connectivities.ToList();
            Implantations = implantations.ToList();
            Epilepsy = epilepsy;
        }
        public Brain() : this(new Mesh[0], new MRI[0], new Connectivity[0], new Implantation[0], new Epilepsy()) { }
        public Brain(string path)
        {
            Meshes = Mesh.GetMeshes(path).ToList();
            MRIs = MRI.GetMRIs(path).ToList();
            Implantations = Implantation.GetImplantations(path).ToList();
            Connectivities = new List<Connectivity>();
            Epilepsy = new Epilepsy(Epilepsy.EpilepsyType.Unknown);
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone The object.
        /// </summary>
        /// <returns>Object cloned.</returns>
        public object Clone()
        {
            return new Brain(Meshes,MRIs,Connectivities,Implantations,Epilepsy);
        }
        #endregion
    }
}