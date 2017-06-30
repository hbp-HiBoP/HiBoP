using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HBP.Data.Anatomy
{
    [DataContract]
    public class Transformation : ICloneable, ICopiable
    {
        #region Properties
        public const string EXTENSION = ".trm";
        [DataMember] public string Name { get; set; }
        [DataMember] public string Path { get; set; }
        #endregion

        #region Constructors
        public Transformation(string name, string path)
        {
            Name = name;
            Path = path;
        }
        #endregion

        #region Public Methods
        public static Transformation[] GetTransformations(string path)
        {
            //UnityEngine.Profiling.Profiler.BeginSample("GetTransformations");
            List<Transformation> transformations = new List<Transformation>();
            DirectoryInfo directory = new DirectoryInfo(path);
            FileInfo[] transformationFileInfo = directory.GetFiles("*" + EXTENSION, SearchOption.AllDirectories);
            foreach (var file in transformationFileInfo)
            {
                transformations.Add(new Transformation(file.Name, file.FullName));
            }
            //UnityEngine.Profiling.Profiler.EndSample();
            return transformations.ToArray();
        }
        #endregion

        #region Operators
        public object Clone()
        {
            return new Transformation(Name, Path);
        }
        public void Copy(object copy)
        {
            Transformation transformation = copy as Transformation;
            Name = transformation.Name;
            Path = transformation.Path;
        }
        #endregion
    }
}