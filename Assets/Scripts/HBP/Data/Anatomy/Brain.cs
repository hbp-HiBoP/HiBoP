using System;
using System.Reflection;
using System.Runtime.Serialization;

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
        /// <summary>
        /// Left cerebral hemisphere mesh file(.gii).
        /// </summary>
        [DataMember]
        public string LeftCerebralHemisphereMesh { get; set; }

        /// <summary>
        /// Right cerebral hemisphere mesh file(.gii).
        /// </summary>
        [DataMember]
        public string RightCerebralHemisphereMesh { get; set; }

        /// <summary>
        /// Preoperative MRI file(.nii).
        /// </summary>
        [DataMember]
        public string PreoperativeMRI { get; set; }

        /// <summary>
        /// Postoperative MRI file(.nii).
        /// </summary>
        [DataMember]
        public string PostoperativeMRI { get; set; }

        /// <summary>
        /// Patient based implantation file(.pts).
        /// </summary>
        [DataMember]
        public string PatientBasedImplantation
        {
            get { return m_PatientBasedImplantation; }
            set { m_PatientBasedImplantation = value; Implantation.Load(value, ReferenceFrameType.Patient); }
        }
        string m_PatientBasedImplantation;

        /// <summary>
        /// MNI based implantation file(.pts).
        /// </summary>
        [DataMember]
        public string MNIBasedImplantation
        {
            get { return m_MNIBasedImplantation; }
            set { m_MNIBasedImplantation = value; Implantation.Load(value, ReferenceFrameType.MNI); }
        }
        string m_MNIBasedImplantation;

        /// <summary>
        /// Preoperative based to scanner based transformation file(.trm).
        /// </summary>
        [DataMember]
        public string PreoperativeBasedToScannerBasedTransformation { get; set; }

        /// <summary>
        /// Sites connectivities file.
        /// </summary>
        [DataMember]
        public string SitesConnectivities { get; set; }

        /// <summary>
        /// Patient epilepsy.
        /// </summary>
        [DataMember]
        public Epilepsy Epilepsy { get; set; }

        /// <summary>
        /// Brain implantation.
        /// </summary>
        public Implantation Implantation { get; set; }

        /// <summary>
        /// Patient to whon the brain belongs.
        /// </summary>
        public Patient Patient { get; set; }

        /// <summary>
        /// Number of fields filled.
        /// </summary>
        public int NumberOfFieldsFilled
		{
			get
			{
                int i = 0;
                PropertyInfo[] properties = typeof(Brain).GetProperties();
                foreach(PropertyInfo property in properties)
                {
                    if(property.PropertyType == typeof(string) && property.CanRead && property.GetValue(this, null) as string != string.Empty)
                    {
                        i++;
                    }
                }
                return i;
			}
		}

        /// <summary>
        /// The patient can be used in single patient visualization.
        /// </summary>
        public bool CanBeUsedInSinglePatientVisualization
        {
            get { return LeftCerebralHemisphereMesh != string.Empty && RightCerebralHemisphereMesh != string.Empty && PreoperativeMRI != string.Empty && PatientBasedImplantation != string.Empty; }
        }

        /// <summary>
        /// The patient can be used in multi-patients visualization.
        /// </summary>
        public bool CanBeUsedInMultiPatientsVisualization
        {
            get { return PreoperativeMRI != string.Empty && MNIBasedImplantation != string.Empty;}
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new brain anatomic data.
        /// </summary>
        /// <param name="leftCerebralHemisphereMesh">Path to the \b left cerebral hemispheres mesh (.gii).</param>
        /// <param name="rightCerebralHemisphereMesh">Path to the \b right cerebral hemispheres mesh (.gii).</param>
        /// <param name="preOperationMRI">Path to the \b pre operation IRM (.nii).</param>
        /// <param name="postOperationMRI">Path to the \b post operation IRM (.nii).</param>
        /// <param name="patientReferenceFrameImplantation">Path to the \b patient based implantation (.pts).</param>
        /// <param name="MNIreferenceFrameImplantation">Path to the \b MNI based implantation (.pts).</param>
        /// <param name="preOperationReferenceFrameToScannerReferenceFrameTransformation">Path to the \b pre operation base to scanner base transformation (.trm).</param>
        /// <param name="connectivity">Connectivity.</param>
        /// <param name="epilepsy">Epilepsy.</param>
        public Brain(Epilepsy epilepsy,string leftCerebralHemisphereMesh = "", string rightCerebralHemisphereMesh = "", string preOperationMRI = "", string postOperationMRI = "", string patientReferenceFrameImplantation = "", string MNIreferenceFrameImplantation = "", string preOperationReferenceFrameToScannerReferenceFrameTransformation = "",string connectivity = "")
        {
            LeftCerebralHemisphereMesh = leftCerebralHemisphereMesh;
            RightCerebralHemisphereMesh = rightCerebralHemisphereMesh;
            PreoperativeMRI = preOperationMRI;
            PostoperativeMRI = postOperationMRI;
            PatientBasedImplantation = patientReferenceFrameImplantation;
            MNIBasedImplantation = MNIreferenceFrameImplantation;
            PreoperativeBasedToScannerBasedTransformation = preOperationReferenceFrameToScannerReferenceFrameTransformation;
            SitesConnectivities = connectivity;
            Epilepsy = epilepsy;
        }
        /// <summary>
        /// Create a new brain which contains empty paths.
        /// </summary>
        public Brain() : this(new Epilepsy())
		{
		}
        #endregion

        #region Operators
        /// <summary>
        /// Clone The object.
        /// </summary>
        /// <returns>Object cloned.</returns>
        public object Clone()
        {
            return new Brain(Epilepsy.Clone() as Epilepsy, LeftCerebralHemisphereMesh, RightCerebralHemisphereMesh, PreoperativeMRI, PostoperativeMRI, PatientBasedImplantation, MNIBasedImplantation, PreoperativeBasedToScannerBasedTransformation, SitesConnectivities);
        }
        #endregion

        #region Serialization
        [OnDeserialized]
        void OnDeserialized()
        {
            // TODO
        }
        #endregion
    }
}