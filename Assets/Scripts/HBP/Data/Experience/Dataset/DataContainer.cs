using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine.Events;

namespace HBP.Data.Experience.Dataset
{
    [DataContract]
    public class DataContainer : ICloneable, ICopiable, IIdentifiable
    {
        #region Properties
        [DataMember] public string ID { get; set; }

        public virtual string DataTypeString
        {
            get
            {
                return "None";
            }
        }
        public virtual string[] DataFilesPaths
        {
            get
            {
                return new string[0];
            }
        }
        public virtual Tools.CSharp.EEG.File.FileType Type
        {
            get
            {
                throw new Exception("Invalid file type");
            }
        }

        protected DataInfo.ErrorType[] m_Errors = new DataInfo.ErrorType[0];
        public virtual DataInfo.ErrorType[] Errors
        {
            get
            {
                List<DataInfo.ErrorType> errors = new List<DataInfo.ErrorType>();
                errors.AddRange(m_Errors);
                return errors.Distinct().ToArray();
            }
        }
        public bool IsOk
        {
            get
            {
                return Errors.Length == 0;
            }
        }

        public UnityEvent OnRequestErrorCheck { get; set; } = new UnityEvent();
        #endregion

        #region Constructors
        public DataContainer(string id)
        {
            ID = id;
        }
        public DataContainer() : this(Guid.NewGuid().ToString())
        {

        }
        #endregion

        #region Public Methods
        public virtual void CopyDataToDirectory(DirectoryInfo dataInfoDirectory, string projectDirectory, string oldProjectDirectory)
        {
        }
        public virtual DataInfo.ErrorType[] GetErrors()
        {
            return new DataInfo.ErrorType[0];
        }
        #endregion

        #region Operators
        /// <summary>
        /// Operator Equals.
        /// </summary>
        /// <param name="obj">Object to test.</param>
        /// <returns>\a True if equals and \a false otherwise.</returns>
        public override bool Equals(object obj)
        {
            DataContainer dataContainer = obj as DataContainer;
            if (dataContainer != null && dataContainer.ID == ID)
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
        /// <param name="a">First mesh to compare.</param>
        /// <param name="b">Second mesh to compare.</param>
        /// <returns>\a True if equals and \a false otherwise.</returns>
        public static bool operator ==(DataContainer a, DataContainer b)
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
        /// <param name="a">First mesh to compare.</param>
        /// <param name="b">Second mesh to compare.</param>
        /// <returns>\a True if not equals and \a false otherwise.</returns>
        public static bool operator !=(DataContainer a, DataContainer b)
        {
            return !(a == b);
        }
        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Clone of this instance.</returns>
        public virtual object Clone()
        {
            return new DataContainer(ID);
        }
        public virtual void Copy(object copy)
        {
            DataContainer dataInfo = copy as DataContainer;
            ID = dataInfo.ID;
        }
        #endregion

        #region Serialization
        [OnDeserialized()]
        public void OnDeserialized(StreamingContext context)
        {
            OnDeserializedOperation(context);
        }
        public virtual void OnDeserializedOperation(StreamingContext context)
        {
        }
        #endregion
    }
}