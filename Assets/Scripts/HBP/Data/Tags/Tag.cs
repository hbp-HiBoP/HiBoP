using System;
using System.IO;
using System.Runtime.Serialization;
using Tools.Unity;

namespace HBP.Data.Tags
{
    [DataContract]
    public class Tag : ICloneable, ICopiable, ILoadable, IIdentifiable
    {
        #region Properties
        public const string EXTENSION = ".tag";

        /// <summary>
        /// Tag Name.
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// Unique ID.
        /// </summary>
        [DataMember]
        public string ID { get; set; }
        #endregion

        #region Constructors
        public Tag() : this("")
        {
        }
        public Tag(string name) : this(name, Guid.NewGuid().ToString())
        {
        }
        public Tag(string name, string ID)
        {
            Name = name;
            this.ID = ID;
        }
        #endregion

        #region Public Methods
        public void Load(string path)
        {
            Tag result;
            try
            {
                result = ClassLoaderSaver.LoadFromJson<Tag>(path);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                throw new CanNotReadTagFileException(Path.GetFileNameWithoutExtension(path));
            }
            Copy(result);
        }
        public string GetExtension()
        {
            return EXTENSION[0] == '.' ? EXTENSION.Substring(1) : EXTENSION;
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
            Tag tag = obj as Tag;
            if (tag != null && tag.ID == ID)
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
            return this.ID.GetHashCode();
        }
        /// <summary>
        /// Operator equals.
        /// </summary>
        /// <param name="a">First tag to compare.</param>
        /// <param name="b">Second tag to compare.</param>
        /// <returns>\a True if equals and \a false otherwise.</returns>
        public static bool operator ==(Tag a, Tag b)
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
        /// <param name="a">First tag to compare.</param>
        /// <param name="b">Second tag to compare.</param>
        /// <returns>\a True if not equals and \a false otherwise.</returns>
        public static bool operator !=(Tag a, Tag b)
        {
            return !(a == b);
        }
        public virtual object Clone()
        {
            return new Tag(Name, ID);
        }
        public virtual void Copy(object copy)
        {
            Tag tag = copy as Tag;
            Name = tag.Name;
            ID = tag.ID;
        }
        #endregion
    }
}