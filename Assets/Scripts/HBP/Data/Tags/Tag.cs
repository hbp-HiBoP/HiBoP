using System;
using System.IO;
using System.Runtime.Serialization;
using Tools.Unity;

namespace HBP.Data.Tags
{
    [DataContract]
    public class Tag : BaseData, ILoadable<Tag>
    {
        #region Properties
        public const string EXTENSION = ".tag";
        /// <summary>
        /// Tag Name.
        /// </summary>
        [DataMember(Order = 0)] public string Name { get; set; }
        #endregion

        #region Constructors
        public Tag() : this("New Tag")
        {
        }
        public Tag(string name) : base()
        {
            Name = name;
        }
        public Tag(string name, string ID) : base(ID)
        {
            Name = name;
        }
        #endregion

        #region Public static Methods
        public static bool LoadFromFile(string path, out Tag result)
        {
            try
            {
                result = ClassLoaderSaver.LoadFromJson<Tag>(path);
                return result != null;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                throw new CanNotReadTagFileException(Path.GetFileNameWithoutExtension(path));
            }
        }
        public static string GetExtension()
        {
            return EXTENSION[0] == '.' ? EXTENSION.Substring(1) : EXTENSION;
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone the instance.
        /// </summary>
        /// <returns>object cloned.</returns>
        public override object Clone()
        {
            return new Tag(Name, ID);
        }
        /// <summary>
        /// Copy the instance.
        /// </summary>
        /// <param name="obj">instance to copy.</param>
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if(copy is Tag tag)
            {
                Name = tag.Name;
            }
        }
        #endregion

        #region Interfaces
        string ILoadable<Tag>.GetExtension()
        {
            return GetExtension();
        }
        bool ILoadable<Tag>.LoadFromFile(string path, out Tag result)
        {
            return LoadFromFile(path, out result);
        }
        #endregion
    }
}