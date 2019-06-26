using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine.Events;
using HBP.Errors;

namespace HBP.Data.Container
{
    [DataContract]
    public class DataContainer : ICloneable, ICopiable, IIdentifiable
    {
        #region Properties
        [DataMember] public string ID { get; set; }

        protected Error[] m_Errors = new Error[0];
        public virtual Error[] Errors
        {
            get
            {
                List<Error> errors = new List<Error>();
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

        public UnityEvent OnRequestErrorCheck { get; } = new UnityEvent();
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
        public virtual Error[] GetErrors()
        {
            return new Error[0];
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

        public class RequieredFieldEmptyError : Error
        {
            #region Constructors
            public RequieredFieldEmptyError() : this("")
            {

            }
            public RequieredFieldEmptyError(string informations) : base("One of the required fields is empty", informations)
            {

            }
            #endregion
        }
        public class FileDoesNotExistError : Error
        {
            #region Constructors
            public FileDoesNotExistError() : this("")
            {
            }
            public FileDoesNotExistError(string informations) : base("One of the files does not exist", informations)
            {

            }
            #endregion
        }
        public class WrongExtensionError : Error
        {
            #region Constructors
            public WrongExtensionError() : this("")
            {
            }
            public WrongExtensionError(string informations) : base("One of the files has a wrong extension", informations)
            {

            }
            #endregion
        }
        public class NotEnoughInformationError : Error
        {
            #region Constructors
            public NotEnoughInformationError() : this("")
            {
            }
            public NotEnoughInformationError(string informations) : base("One of the files does not contain enough information", informations)
            {

            }
            #endregion
        }
    }
}