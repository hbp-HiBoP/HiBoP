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
        [DataMember]
        /** Path to the \b left cerebral hemispheres mesh (.gii).*/
        public string LeftCerebralHemisphereMesh { get; set; }
        [DataMember]
        /** Path to the \b right cerebral hemispheres mesh (.gii).*/
        public string RightCerebralHemisphereMesh { get; set; }
        [DataMember]
        /** Path to the \b pre operation MRI (.nii).*/
        public string PreOperationMRI { get; set; }
        [DataMember]
        /** Path to the \b post operation MRI (.nii).*/
        public string PostOperationMRI { get; set; }
        [DataMember]
        /** Path to the \b patient based implantation (.pts).*/
        public string PatientReferenceFrameImplantation { get; set; }
        [DataMember]
        /** Path to the \b MNI based implantation (.pts).*/
        public string MNIReferenceFrameImplantation { get; set; }   
        [DataMember]
        /** Path to the \b pre operation base to scanner base transformation (.trm).*/
        public string PreOperationReferenceFrameToScannerReferenceFrameTransformation { get; set; }
        [DataMember]
        /** Connectivity between plots. */
        public string PlotsConnectivity { get; set; }
        [DataMember]
        /** Epilepsy type.*/
        public Epilepsy Epilepsy { get; set; }

        /** Patient witch contains the brain. */
        public Patient Patient { get; set; }

        /** Number of fields filled.*/
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
        /** Can be used in \b single \b patient visualisation.*/
        public bool CanBeUsedInSinglePatientVisualisation
        {
            get { return LeftCerebralHemisphereMesh != string.Empty && RightCerebralHemisphereMesh != string.Empty && PreOperationMRI != string.Empty && PatientReferenceFrameImplantation != string.Empty; }
        }
        /** Can be used in \b multi-patients visualisation.*/
        public bool CanBeUsedInMultiPatientsVisualisation
        {
            get { return PreOperationMRI != string.Empty && MNIReferenceFrameImplantation != string.Empty;}
        }

        Implantation m_Implantation;
        /** Patient reference frame based implantation. */
        public Implantation Implantation
        {
            get
            {
                if (m_Implantation == null) UnityEngine.Debug.LogError("Implantation not loaded.");
                return m_Implantation;
            }
        }
        Implantation m_MNIImplantation;
        /** MNI reference frame based implantation. */
        public Implantation MNIImplantation
        {
            get
            {
                if (m_MNIImplantation == null) UnityEngine.Debug.LogError("Implantation not loaded.");
                return m_MNIImplantation;
            }
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
            PreOperationMRI = preOperationMRI;
            PostOperationMRI = postOperationMRI;
            PatientReferenceFrameImplantation = patientReferenceFrameImplantation;
            MNIReferenceFrameImplantation = MNIreferenceFrameImplantation;
            PreOperationReferenceFrameToScannerReferenceFrameTransformation = preOperationReferenceFrameToScannerReferenceFrameTransformation;
            PlotsConnectivity = connectivity;
            Epilepsy = epilepsy;
        }
        /// <summary>
        /// Create a new brain which contains empty paths.
        /// </summary>
        public Brain() : this(new Epilepsy())
		{
		}
        #endregion

        #region Public Methods
        /// <summary>
        /// Load implantation.
        /// </summary>
        /// <param name="referenceFrame">Reference frame of the implantation.</param>
        /// <param name="plotNameCorrection">Plot name correction.</param>
        public void LoadImplantation(Implantation.ReferenceFrameType referenceFrame, bool plotNameCorrection)
        {
            switch (referenceFrame)
            {
                case Implantation.ReferenceFrameType.Patient:
                    m_Implantation = new Implantation(PatientReferenceFrameImplantation, plotNameCorrection);
                    m_Implantation.Brain = this;
                    break;
                case Implantation.ReferenceFrameType.MNI:
                    m_MNIImplantation = new Implantation(MNIReferenceFrameImplantation, plotNameCorrection);
                    m_MNIImplantation.Brain = this;
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// Dispose implantation.
        /// </summary>
        /// <param name="referenceFrame">Reference frame of the implantation.</param>
        public void DisposeImplantation(Implantation.ReferenceFrameType referenceFrame)
        {
            switch (referenceFrame)
            {
                case Implantation.ReferenceFrameType.Patient:
                    m_Implantation = null;
                    break;
                case Implantation.ReferenceFrameType.MNI:
                    m_MNIImplantation = null;
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// Clone The object.
        /// </summary>
        /// <returns>Object cloned.</returns>
        public object Clone()
        {
            return new Brain(Epilepsy.Clone() as Epilepsy,LeftCerebralHemisphereMesh, RightCerebralHemisphereMesh, PreOperationMRI, PostOperationMRI, PatientReferenceFrameImplantation, MNIReferenceFrameImplantation, PreOperationReferenceFrameToScannerReferenceFrameTransformation,PlotsConnectivity);
        }
        #endregion
    }
}