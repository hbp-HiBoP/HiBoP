using System;
using System.Runtime.Serialization;

namespace HBP.Data.Anatomy
{
    /**
    * \class Epilepsy
    * \author Adrien Gannerie
    * \version 1.0
    * \date 04 janvier 2017
    * \brief Class which represent a brain epilepsy.
    * 
    * \details Brain epilepsy which contains:
    *   - \a Type.
    */
    [DataContract]
	public class Epilepsy : ICloneable
	{
		#region Properties
        /// <summary>
        /// Type of epilepsy.
        /// </summary>
		public enum EpilepsyType { None,IGE,IPE,SGE,SPE,Unknown}
        /// <summary>
        /// Type of this epilepsy.
        /// </summary>
        [DataMember]
		public EpilepsyType Type { get; set; }
		#endregion

		#region Constructor
        /// <summary>
        /// Create a new epilepsy type.
        /// </summary>
        /// <param name="type"></param>
        public Epilepsy(EpilepsyType type = EpilepsyType.Unknown)
        {
            Type = type;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Object cloned.</returns>
        public object Clone()
        {
            return new Epilepsy(Type);
        }
        /// <summary>
        /// Get the full name of a epilepsy type.
        /// </summary>
        /// <param name="type">Epilepsy type.</param>
        /// <returns>Full name of the epilepsy type.</returns>
        public static string GetFullEpilepsyName(EpilepsyType type)
        {
            switch (type)
            {
                case EpilepsyType.None: return "None";
                case EpilepsyType.IGE: return "Idiopathic generalized";
                case EpilepsyType.IPE: return "Idiotpathic partial";
                case EpilepsyType.SGE: return "Symptomatic generalized";
                case EpilepsyType.SPE: return "Symptomatic partial";
                default: return "Unknown";
            }
        }
        #endregion
    }
}