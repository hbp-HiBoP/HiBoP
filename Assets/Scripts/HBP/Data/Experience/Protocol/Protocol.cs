using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Tools.CSharp;
using HBP.Data.Settings;

namespace HBP.Data.Experience.Protocol
{
    /// <summary>
    /// Class which define a visualisation protocol.
    ///     - Label of the protocol.
    ///     - Blocs of the protocol.
    /// </summary>
    [Serializable]
	public class Protocol : ICloneable, ICopiable
    {
        #region Properties
        [SerializeField]
        private string id;
        public string ID
        {
            private set { id = value; }
            get { return id; }
        }

        [SerializeField]
        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        [SerializeField]
        private List<Bloc> blocs;
        public ReadOnlyCollection<Bloc> Blocs
        {
            get { return new ReadOnlyCollection<Bloc>(blocs); }
        }
        #endregion

        #region Constructors
        public Protocol(string name,List<Bloc> blocs,string id)
        {
            Name = name;
            this.blocs = blocs;
            ID = id;
        }
        public Protocol(string name, List<Bloc> blocs) : this(name,blocs, Guid.NewGuid().ToString())
        {
        }
        public Protocol() : this(string.Empty,new List<Bloc>())
		{
        }
        #endregion

        #region Public Methods
        public void Add(Bloc bloc)
        {
            if(!blocs.Contains(bloc))
            {
                blocs.Add(bloc);
            }
        }
        public void Add(Bloc[] blocs)
        {
            foreach (Bloc bloc in Blocs) Add(bloc);
        }
        public void Remove(Bloc bloc)
        {
            blocs.Remove(bloc);
        }
        public void Remove(Bloc[] blocs)
        {
            foreach (Bloc bloc in Blocs) Remove(bloc);
        }
        public void Clear()
        {
            blocs = new List<Bloc>();
        }
        public void Set(Bloc[] blocs)
        {
            this.blocs = new List<Bloc>(blocs);
        }
        public void SaveXML(string path)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Protocol));
            TextWriter textWriter = new StreamWriter(GenerateSavePath(path));
            xmlSerializer.Serialize(textWriter, this);
            textWriter.Close();
        }
        public void SaveJSon(string path)
        {
            string l_json = JsonUtility.ToJson(this, true);
            using (StreamWriter outPutFile = new StreamWriter(GenerateSavePath(path)))
            {
                outPutFile.Write(l_json);
            }
        }
        public static Protocol LoadXML(string path)
        {
            Protocol l_protocol = new Protocol();
            if (File.Exists(path) && Path.GetExtension(path) == FileExtension.Protocol)
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(Protocol));
                TextReader streamReader = new StreamReader(path);
                l_protocol = xmlSerializer.Deserialize(streamReader) as Protocol;
                streamReader.Close();
            }
            return l_protocol;
        }
        public static Protocol LoadJSon(string path)
        {
            string l_json = string.Empty;
            using (StreamReader inPutFile = new StreamReader(path))
            {
                return JsonUtility.FromJson<Protocol>(inPutFile.ReadToEnd());
            }
        }
        public void Copy(object copy)
        {
            Protocol protocol = copy as Protocol;
            Name = protocol.Name;
            Set(protocol.blocs.ToArray());
            ID = protocol.ID;
        }
        #endregion

        #region Private Methods
        string GenerateSavePath(string path)
        {
            string l_path = path + Path.DirectorySeparatorChar + Name;
            string l_finalPath = l_path + FileExtension.Protocol;
            int count = 1;
            while (File.Exists(l_finalPath))
            {
                string tempFileName = string.Format("{0}({1})", l_path, count++);
                l_finalPath = Path.Combine(path, tempFileName + FileExtension.Protocol);
            }
            return l_finalPath;
        }
        #endregion

        #region Operator
        public object Clone()
        {
            return new Protocol(Name.Clone() as string, new List<Bloc>(Blocs.ToArray().DeepClone()), ID.Clone() as string);
        }
        public override bool Equals(object obj)
        {
            Protocol p = obj as Protocol;
            if (p == null)
            {
                return false;
            }
            else
            {
                return Name == p.Name && System.Linq.Enumerable.SequenceEqual(Blocs, p.Blocs);
            }
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public static bool operator ==(Protocol a, Protocol b)
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
        public static bool operator !=(Protocol a, Protocol b)
        {
            return !(a == b);
        }
        #endregion
    }
}
