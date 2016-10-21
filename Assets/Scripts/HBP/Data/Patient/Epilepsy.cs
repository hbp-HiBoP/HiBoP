using System;
using UnityEngine;

namespace HBP.Data.Patient
{
    /// <summary>
    /// Class which define the type of a epilepsy.
    /// </summary>
    [Serializable]
	public class Epilepsy : ICloneable
	{
		#region Properties
		public enum epilepsyTypeEnum { IGE,IPE,SGE,SPE,Unknown}

        [SerializeField]
		private epilepsyTypeEnum epilepsyType;
		public epilepsyTypeEnum EpilepsyType
		{
			get
			{
				return epilepsyType;
			}
			set
			{
				epilepsyType=value;
			}
		}
		#endregion

		#region Set/Get Methods
        /// <summary>
        /// Get long string epilepsy type.
        /// </summary>
        /// <returns></returns>
		public string GetLongEpilepsyType()
		{
			string l_toReturn="";
			if(epilepsyType == epilepsyTypeEnum.IGE)
			{
				l_toReturn="Idiopathic generalized epilepsy";
			}
			else if(epilepsyType == epilepsyTypeEnum.IPE)
			{
				l_toReturn="Idiotpathic partial epilepsy";
			}
			else if(epilepsyType == epilepsyTypeEnum.SGE)
			{
				l_toReturn="Symptomatic generalized epilepsy";
			}
			else if(epilepsyType == epilepsyTypeEnum.SPE)
			{
				l_toReturn="Symptomatic partial epilepsy";
			}
			else if(epilepsyType == epilepsyTypeEnum.Unknown)
			{
				l_toReturn="Unknown";
			}
			return l_toReturn;
		}

        /// <summary>
        /// Get short string epilepsy type.
        /// </summary>
        /// <returns></returns>
		public string GetEpilepsyType()
		{
			string l_toReturn="";
			if(epilepsyType == epilepsyTypeEnum.IGE)
			{
				l_toReturn="IGE";
			}
			else if(epilepsyType == epilepsyTypeEnum.IPE)
			{
				l_toReturn="IPE";
			}
			else if(epilepsyType == epilepsyTypeEnum.SGE)
			{
				l_toReturn="SGE";
			}
			else if(epilepsyType == epilepsyTypeEnum.SPE)
			{
				l_toReturn="SPE";
			}
			else if(epilepsyType == epilepsyTypeEnum.Unknown)
			{
				l_toReturn="Unknown";
			}
			return l_toReturn;
		}

        /// <summary>
        /// Set the epilespy type.
        /// </summary>
        /// <param name="epilepsyType">Short or long string epilepsy type</param>
		public void SetEpilepsyType(string epilepsyType)
		{
			if(epilepsyType == "IGE" || epilepsyType == "Idiopathic generalized epilepsy")
			{
				this.epilepsyType = epilepsyTypeEnum.IGE;
			}
			else if(epilepsyType == "IPE" || epilepsyType == "Idiotpathic partial epilepsy")
			{
				this.epilepsyType = epilepsyTypeEnum.IPE;
			}
			else if(epilepsyType == "SGE" || epilepsyType == "Symptomatic generalized epilepsy")
			{
				this.epilepsyType = epilepsyTypeEnum.SGE;
			}
			else if(epilepsyType == "SPE" || epilepsyType == "Symptomatic partial epilepsy")
			{
				this.epilepsyType = epilepsyTypeEnum.SPE;
			}
			else
			{
				this.epilepsyType = epilepsyTypeEnum.Unknown;
			}
		}
		#endregion

		#region Constructor
        /// <summary>
        /// Create a new epilepsy type.
        /// </summary>
		public Epilepsy() : this(epilepsyTypeEnum.Unknown)
		{
		}

        /// <summary>
        /// Create a new epilepsy type.
        /// </summary>
        /// <param name="epilepsyType"></param>
        public Epilepsy(epilepsyTypeEnum epilepsyType)
        {
            this.epilepsyType = epilepsyType;
        }

        /// <summary>
        /// Create new epilepsy type.
        /// </summary>
        /// <param name="epilepsyType">String epilepsy type.</param>
		public Epilepsy(string epilepsyType)
		{
			if(epilepsyType == "IGE" || epilepsyType == "Idiopathic generalized epilepsy")
			{
				this.epilepsyType = epilepsyTypeEnum.IGE;
			}
			else if(epilepsyType == "IPE" || epilepsyType == "Idiotpathic partial epilepsy")
			{
				this.epilepsyType = epilepsyTypeEnum.IPE;
			}
			else if(epilepsyType == "SGE" || epilepsyType == "Symptomatic generalized epilepsy")
			{
				this.epilepsyType = epilepsyTypeEnum.SGE;
			}
			else if(epilepsyType == "SPE" || epilepsyType == "Symptomatic partial epilepsy")
			{
				this.epilepsyType = epilepsyTypeEnum.SPE;
			}
			else
			{
				this.epilepsyType = epilepsyTypeEnum.Unknown;
			}
		}

        public object Clone()
        {
            return new Epilepsy(EpilepsyType);
        }
		#endregion
	}
}