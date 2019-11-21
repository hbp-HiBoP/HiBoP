using System;
using System.Linq;
using System.Collections.Generic;
using Tools.CSharp;
using System.Runtime.Serialization;
using Tools.Unity;
using System.IO;

namespace HBP.Data.Experience.Protocol
{
    /**
    * \class Protocol
    * \author Adrien Gannerie
    * \version 1.0
    * \date 09 janvier 2017
    * \brief Protocol of a Experience.
    * 
    * \details Class which define a visualization protocol which contains : 
    *     - \a Unique \a ID.
    *     - \a Label.
    *     - \a Blocs.
    */
    [DataContract]
	public class Protocol : BaseData, ILoadable<Protocol>, INameable
    {
        #region Properties
        public const string EXTENSION = ".prov";
        /// <summary>
        /// Name of the protocol.
        /// </summary>
        [DataMember] public string Name { get; set; }
        /// <summary>
        /// Blocs of the protocol.
        /// </summary>
        [DataMember] public List<Bloc> Blocs { get; set; }
        public IOrderedEnumerable<Bloc> OrderedBlocs
        {
            get
            {
                return Blocs.OrderBy(s => s.Order).ThenBy(s => s.Name);
            }
        }
        public bool IsVisualizable
        {
            get
            {
                return Blocs.Count > 0 && Blocs.All(b => b.IsVisualizable);
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new protocol.
        /// </summary>
        /// <param name="name">Name of the protocol.</param>
        /// <param name="blocs">Blocs of the protocol.</param>
        /// <param name="ID">Unique ID of the protocol.</param>
        public Protocol(string name,IEnumerable<Bloc> blocs,string ID) : base(ID)
        {
            Name = name;
            Blocs = blocs.ToList();
        }
        /// <summary>
        /// Create a new protocol.
        /// </summary>
        /// <param name="name">Name of the protocol.</param>
        /// <param name="blocs">Blocs of the protocol.</param>
        public Protocol(string name, IEnumerable<Bloc> blocs) : base()
        {
            Name = name;
            Blocs = blocs.ToList();
        }
        /// <summary>
        /// Create a new protocol instance with default values.
        /// </summary>
        public Protocol() : this("New protocol",new List<Bloc>())
		{
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Generates  ID recursively.
        /// </summary>
        public override void GenerateID()
        {
            base.GenerateID();
            foreach (var bloc in Blocs) bloc.GenerateID();
        }
        #endregion

        #region Public Static Methods
        public static string GetExtension()
        {
            return EXTENSION[0] == '.' ? EXTENSION.Substring(1) : EXTENSION;
        }
        public static bool LoadFromFile(string path, out Protocol result)
        {
            result = null;
            try
            {
                result = ClassLoaderSaver.LoadFromJson<Protocol>(path);
                if (result != null) return true;
                else return false;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                throw new CanNotReadProtocolFileException(Path.GetFileNameWithoutExtension(path));
            }
        }
        #endregion

        #region Operator
        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Instance cloned.</returns>
        public override object Clone()
        {
            return new Protocol(Name, Blocs.DeepClone(), ID);
        }
        /// <summary>
        /// Copy the instance.
        /// </summary>
        /// <param name="obj">instance to copy.</param>
        public override void Copy(object obj)
        {
            base.Copy(obj);
            if(obj is Protocol protocol)
            {
                Name = protocol.Name;
                Blocs = protocol.Blocs;
            }
        }
        #endregion

        #region Interfaces
        string ILoadable<Protocol>.GetExtension()
        {
            return GetExtension();
        }
        bool ILoadable<Protocol>.LoadFromFile(string path, out Protocol result)
        {
            return LoadFromFile(path, out result);
        }
        #endregion
    }
}