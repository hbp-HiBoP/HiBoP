using UnityEngine;
using System;
using System.Linq;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace HBP.Data.Experience.Dataset
{
    /// <summary>
    /// Class which define a dataset.
    ///     - Name.
    ///     - Data.
    /// </summary>
    [Serializable]
	public class Dataset : ICloneable, ICopiable
	{
        #region Attributs
        [SerializeField]
        string id;
        public string ID
        {
            get { return id; }
            private set { id = value; }
        }

        [SerializeField]
        private string name;
		public string Name
        {
            get {return name; }
            set {name = value; }
        }

        [SerializeField]
		private List<DataInfo> data;
		public ReadOnlyCollection<DataInfo> Data
        {
            get { return new ReadOnlyCollection<DataInfo>(data); }
        }
        #endregion

        #region Constructor
        public Dataset(string name, DataInfo[] data,string id)
        {
            Name = name;
            this.data = new List<DataInfo>(data);
            ID = id;
        }
        public Dataset() : this(string.Empty,new DataInfo[0], Guid.NewGuid().ToString())
		{
        }
        #endregion

        #region Public Methods
        public void Add(DataInfo data)
        {
            if(!this.data.Contains(data))
            {
                this.data.Add(data);
            }
        }
        public void Add(DataInfo[] data)
        {
            foreach(DataInfo d in data) Add(d);
        }
        public void Remove(DataInfo data)
        {
            this.data.Remove(data);
        }
        public void Remove(DataInfo[] data)
        {
            foreach (DataInfo d in data) Remove(d);
        }
        public void Clear()
        {
            data = new List<DataInfo>();
        }
        public void Set(DataInfo[] data)
        {
            this.data = new List<DataInfo>(data);
        }
        public void SaveXML(string path)
		{
            XmlSerializer serializer = new XmlSerializer(typeof(Dataset));
            TextWriter textWriter = new StreamWriter(GenerateSavePath(path));
            serializer.Serialize(textWriter, this);
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
		public static Dataset LoadXML(string path)
		{
            Dataset result = new Dataset();
            if (File.Exists(path) && Path.GetExtension(path) == Settings.FileExtension.Dataset)
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Dataset));
                TextReader textReader = new StreamReader(path);
                result = serializer.Deserialize(textReader) as Dataset;
                textReader.Close();
            }
            result.UpdateDataStates();
            return result;
        }
        public static Dataset LoadJSon(string path)
        {
            Dataset result = new Dataset();
            try
            {
                using (StreamReader inPutFile = new StreamReader(path))
                {
                    result = JsonUtility.FromJson<Dataset>(inPutFile.ReadToEnd());
                }
            }
            catch
            {
                Debug.LogWarning("Can't read the dataset file.");
            }
            result.UpdateDataStates();
            return result;
        }
        #endregion

        #region Private Methods
        string GenerateSavePath(string path)
        {
            string l_path = path + Path.DirectorySeparatorChar + Name;
            string l_finalPath = l_path + Settings.FileExtension.Dataset;
            int count = 1;
            while (File.Exists(l_finalPath))
            {
                string tempFileName = string.Format("{0}({1})", l_path, count++);
                l_finalPath = Path.Combine(path, tempFileName + Settings.FileExtension.Dataset);
            }
            return l_finalPath;
        }
        void UpdateDataStates()
        {
            foreach(DataInfo dataInfo in data)
            {
                dataInfo.UpdateStates();
            }
        }
        #endregion

        #region Operrators
        public object Clone()
        {
            return new Dataset(Name,Data.ToArray().Clone() as DataInfo[], ID);
        }
        public void Copy(object copy)
        {
            Dataset dataset = copy as Dataset;
            Name = dataset.Name;
            ID = dataset.ID;
            data = dataset.Data.ToList();
        }
        public override bool Equals(object obj)
        {
            Dataset l_dataset = obj as Dataset;
            if (l_dataset != null && l_dataset.ID == ID)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
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
        public static bool operator !=(Dataset a, Dataset b)
        {
            return !(a == b);
        }
        #endregion
    }
}
