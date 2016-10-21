using System;
using UnityEngine;
using Tools.CSharp;
using System.Collections.Generic;

namespace HBP.Data.Patient
{
    /// <summary>
    /// Class which define a brain and contains some informations:
    ///     - Meshes.
    ///     - IRMs.
    ///     - Electrodes implantation.
    ///     - Base transformations.
    /// </summary>
    [Serializable]
	public class Brain : ICloneable
	{
        #region Properties
        [SerializeField]
        private string leftMesh;
        public string LeftMesh
        {
            get { return leftMesh; }
            set { leftMesh = value; }
        }

        [SerializeField]
        private string rightMesh;
        public string RightMesh
        {
            get { return rightMesh; }
            set { rightMesh = value; }
        }

        [SerializeField]
        private string preIRM;
		public string PreIRM
		{
            get { return preIRM; }
            set { preIRM = value; }
        }

        [SerializeField]
        private string postIRM;
        public string PostIRM
        {
            get { return postIRM; }
            set { postIRM = value; }
        }

        [SerializeField]
        private string patientBasedImplantation;
		public string PatientBasedImplantation
		{
			get{ return patientBasedImplantation; }
			set{ patientBasedImplantation=value; }
		}

        [SerializeField]
        private string MNIbasedImplantation;
		public string MNIBasedImplantation
		{
            get { return MNIbasedImplantation; }
            set { MNIbasedImplantation = value; }
        }

        [SerializeField]
        private string preToScannerBasedTransformation;
		public string PreToScannerBasedTransformation
		{
            get { return preToScannerBasedTransformation; }
            set { preToScannerBasedTransformation = value; }
        }

        [SerializeField]
        private Epilepsy epilepsy;
        public Epilepsy Epilepsy
        {
            get { return epilepsy; }
            set { epilepsy = value; }
        }

        [SerializeField]
        private List<Connectivity> connectivities;
        public List<Connectivity> Connectivities
        {
            get { return connectivities; }
            set { connectivities = value; }
        }

        /// <summary>
        /// Number of path not empty.
        /// </summary>
		public int NotEmptyPaths
		{
			get
			{
				int i=0;
				if(leftMesh != string.Empty) i++;
                if(rightMesh != string.Empty) i++;
				if(preIRM != string.Empty) i++;
                if(postIRM != string.Empty) i++;
                if(patientBasedImplantation != string.Empty) i++;
                if (MNIbasedImplantation != string.Empty) i++;
                if (preToScannerBasedTransformation != string.Empty) i++;
                foreach(Connectivity path in connectivities)
                {
                    if (path.Path != string.Empty) i++;
                }
				return i;
			}
		}

        /// <summary>
        /// Can be used in single patient visualisation.
        /// </summary>
        public bool CanBeUsedInSP
        {
            get
            {
                if(LeftMesh != string.Empty && RightMesh != string.Empty && PreIRM != string.Empty && PatientBasedImplantation != string.Empty)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Can be used in multi-patients visualisation.
        /// </summary>
        public bool CanBeUsedInMP
        {
            get
            {
                if (PreIRM != string.Empty && MNIBasedImplantation != string.Empty)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Return true if all the paths are not empty else return false.
        /// </summary>
        public bool IsFull
        {
            get
            {
                if(NotEmptyPaths >= 7)
                {
                    if (leftMesh != string.Empty && rightMesh != string.Empty && preIRM != string.Empty && postIRM != string.Empty && patientBasedImplantation != string.Empty && MNIbasedImplantation != string.Empty && preToScannerBasedTransformation != string.Empty)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Read the electrodes implantation files.
        /// </summary>
        /// <param name="MNI">if false read patient base electrodes implantation. if true read MNI.</param>
        /// <returns>Electrodes.</returns>
        public Electrode[] ReadImplantation(bool MNI)
        {
            if (MNI)
            {
                return Electrode.readImplantationFile(MNIbasedImplantation);
            }
            else
            {
                return Electrode.readImplantationFile(patientBasedImplantation);
            }
        }
        #endregion

        #region Operators
        public object Clone()
        {
            Connectivity[] connectivitiesClone = Connectivities.ToArray().DeepClone() as Connectivity[];
            Brain brainClone = new Brain(LeftMesh, RightMesh, PreIRM, PostIRM, PatientBasedImplantation, MNIBasedImplantation, PreToScannerBasedTransformation, epilepsy.Clone() as Epilepsy, new List<Connectivity>(connectivitiesClone));
            return brainClone;
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new brain which contains informations.
        /// </summary>
        /// <param name="LmeshPath">Path of the left mesh.</param>
        /// <param name="RmeshPath">Path of the right mesh.</param>
        /// <param name="PreIRM">Path of the pre operation IRMs.</param>
        /// <param name="IRMPostPath">Path of the post operation IRMs.</param>
        /// <param name="implantationPath">Path of the patient based implantation.</param>
        /// <param name="MNIBasedImplantation">Path of the MNI based implantation.</param>
        /// <param name="baseTransformationPath">Path of the Patient/MNI bases transformation.</param>
        public Brain(string leftMesh, string rightMesh, string preIRM, string postIRM, string patientBasedImplantation, string MNIbasedImplantation, string postToScannerBasedTransformation,Epilepsy epilepsy, List<Connectivity> connectivities)
        {
            LeftMesh = leftMesh;
            RightMesh = rightMesh;
            PreIRM = preIRM;
            PostIRM = postIRM;
            PatientBasedImplantation = patientBasedImplantation;
            MNIBasedImplantation = MNIbasedImplantation;
            PreToScannerBasedTransformation = postToScannerBasedTransformation;
            Epilepsy = epilepsy;
            Connectivities = connectivities;
        }

        /// <summary>
        /// Create a new brain which contains empty paths.
        /// </summary>
        public Brain() : this(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,new Epilepsy(), new List<Connectivity>())
		{
		}
		#endregion
	}
}