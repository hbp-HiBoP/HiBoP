using System;
using System.IO;
using System.Runtime.Serialization;
using Tools.Unity;

namespace HBP.Data
{
    [DataContract]
    public class BaseTag : BaseData, ILoadable<BaseTag>, INameable
    {
        #region Properties
        public const string EXTENSION = ".tag";
        /// <summary>
        /// BaseTag Name.
        /// </summary>
        [DataMember(Order = 0)] public string Name { get; set; }
        #endregion

        #region Constructors
        public BaseTag() : this("New BaseTag")
        {
        }
        public BaseTag(string name) : base()
        {
            Name = name;
        }
        public BaseTag(string name, string ID) : base(ID)
        {
            Name = name;
        }
        #endregion

        #region Public static Methods
        public static bool LoadFromFile(string path, out BaseTag result)
        {
            try
            {
                result = ClassLoaderSaver.LoadFromJson<BaseTag>(path);
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
            return new BaseTag(Name, ID);
        }
        /// <summary>
        /// Copy the instance.
        /// </summary>
        /// <param name="obj">instance to copy.</param>
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if(copy is BaseTag tag)
            {
                Name = tag.Name;
            }
        }
        #endregion

        #region Interfaces
        string ILoadable<BaseTag>.GetExtension()
        {
            return GetExtension();
        }
        bool ILoadable<BaseTag>.LoadFromFile(string path, out BaseTag result)
        {
            return LoadFromFile(path, out result);
        }
        #endregion
    }
}