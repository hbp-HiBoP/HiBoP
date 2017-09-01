using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HBP.Data.Experience.Dataset
{
    /**
    * \class Dataset
    * \author Adrien Gannerie
    * \version 1.0
    * \date 09 janvier 2017
    * \brief Dataset which contains data of the experiment.
    * 
    * \details Class which contains data of the experiment :
    *       - \a Name.
    *       - \a DataInfo of the experiment.
    *       - \a Unique ID.
    */
    [DataContract]
	public class Dataset : ICloneable, ICopiable
	{
        #region Attributs
        public const string EXTENSION = ".dataset";

        [DataMember]
        /// <summary>
        /// Unique ID of the dataset.
        /// </summary>
        public string ID { get; private set; }

        [DataMember]
        /// <summary>
        /// Name of the dataset.
        /// </summary>
		public string Name { get; set; }

        [DataMember(Order = 3)]
        /// <summary>
        /// DataInfo of the dataset.
        /// </summary>
        public List<DataInfo> Data { get; set; }
        #endregion

        #region Constructor
        public Dataset(string name, DataInfo[] data,string id)
        {
            Name = name;
            Data = new List<DataInfo>(data);
            ID = id;
        }
        public Dataset() : this(string.Empty,new DataInfo[0], Guid.NewGuid().ToString())
		{
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// UpdateDataStates.
        /// </summary>
        public void UpdateDataStates()
        {
            foreach (DataInfo dataInfo in Data) dataInfo.GetErrors();
        }
        #endregion

        #region Operrators
        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Clone of this instance</returns>
        public object Clone()
        {
            return new Dataset(Name,Data.ToArray().Clone() as DataInfo[], ID);
        }
        /// <summary>
        /// Copy this a instance to this instance.
        /// </summary>
        /// <param name="copy">Instance to copy.</param>
        public void Copy(object copy)
        {
            Dataset dataset = copy as Dataset;
            Name = dataset.Name;
            ID = dataset.ID;
            Data = dataset.Data.ToList();
        }
        /// <summary>
        /// Operator Equals.
        /// </summary>
        /// <param name="obj">Object to test.</param>
        /// <returns>\a True if equals and \a false otherwise.</returns>
        public override bool Equals(object obj)
        {
            Dataset dataset = obj as Dataset;
            if (dataset != null && dataset.ID == ID)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Get hash code.
        /// </summary>
        /// <returns>HashCode.</returns>
        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }
        /// <summary>
        /// Operator equals.
        /// </summary>
        /// <param name="a">Dataset to compare.</param>
        /// <param name="b">Dataset to compare.</param>
        /// <returns>\a True if equals and \a false otherwise.</returns>
        public static bool operator ==(Dataset a, Dataset b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            return a.Equals(b);
        }
        /// <summary>
        /// Operator not equals.
        /// </summary>
        /// <param name="a">Dataset to compare.</param>
        /// <param name="b">Dataset to compare.</param>
        /// <returns>\a True if not equals and \a false otherwise.</returns>
        public static bool operator !=(Dataset a, Dataset b)
        {
            return !(a == b);
        }
        #endregion
    }
}
