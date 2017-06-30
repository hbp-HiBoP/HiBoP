using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;

namespace HBP.Data.Anatomy
{
    [DataContract]
    public class Implantation : ICloneable, ICopiable
    {
        #region Properties
        const string HEADER = "ptsfile";
        public const string EXTENSION = ".pts";
        public enum Error { None, PathIsNullOrEmpty, FileNotFound, WrongExtension, CannotReadFile, WrongFormat, CannotReadAllSites };
        [DataMember] public string Name { get; set; }
        [DataMember] public string Path { get; set; }
        [IgnoreDataMember] public List<Electrode> Electrodes { get; set; }
        [IgnoreDataMember] public Brain Brain { get; set; }
        #endregion

        #region Constructor
        public Implantation(string name, string path)
        {
            Name = name;
            Path = path;
            Electrodes = new List<Electrode>();
        }
        public Implantation() : this("New implantation", string.Empty)
        {
        }
        #endregion

        #region Public Methods
        public Error Load()
        {
            if (string.IsNullOrEmpty(Path)) return Error.PathIsNullOrEmpty;
            FileInfo fileInfo = new FileInfo(Path);
            if (!fileInfo.Exists) return Error.FileNotFound;
            if (fileInfo.Extension != EXTENSION) return Error.WrongExtension;
            string[] lines = new string[0];
            try
            {
                lines = File.ReadAllLines(fileInfo.FullName);
            }
            catch
            {
                return Error.CannotReadFile;
            }
            if (!IsCorrect(lines)) return Error.WrongFormat;
            IEnumerable<Site> sites;
            if (!ReadSites(lines, out sites)) return Error.CannotReadAllSites;
            Electrodes = Electrode.GetElectrodes(sites).ToList();
            return Error.None;
        }
        public void Unload()
        {
            Electrodes = new List<Electrode>();
        }
        public static Implantation[] GetImplantations(string path)
        {
            //UnityEngine.Profiling.Profiler.BeginSample("GetImplantations");
            List<Implantation> implantations = new List<Implantation>();
            DirectoryInfo patientDirectory = new DirectoryInfo(path);
            DirectoryInfo implantationDirectory = new DirectoryInfo(path + System.IO.Path.DirectorySeparatorChar + "implantation");
            if (implantationDirectory.Exists)
            {
                FileInfo patientImplantation = new FileInfo(implantationDirectory.FullName + System.IO.Path.DirectorySeparatorChar + patientDirectory.Name + EXTENSION);
                if(patientImplantation.Exists) implantations.Add(new Implantation("Patient", patientImplantation.FullName));
                FileInfo MNIImplantation = new FileInfo(implantationDirectory.FullName + System.IO.Path.DirectorySeparatorChar + patientDirectory.Name + "_MNI" + EXTENSION);
                if(MNIImplantation.Exists) implantations.Add(new Implantation("MNI", MNIImplantation.FullName));
                FileInfo ACPCImplantation = new FileInfo(implantationDirectory.FullName + System.IO.Path.DirectorySeparatorChar + patientDirectory.Name + "_ACPC" + EXTENSION);
                if (ACPCImplantation != null) implantations.Add(new Implantation("ACPC", ACPCImplantation.FullName));
            }
            //UnityEngine.Profiling.Profiler.EndSample();
            return implantations.ToArray();
        }
        #endregion

        #region Private Methods
        bool IsCorrect(string[] lines)
        {
            int sites = 0;
            return lines[0] == HEADER && int.TryParse(lines[2],out sites) && sites == (lines.Length - 3);
        }
        bool ReadSites(IEnumerable<string> lines, out IEnumerable<Site> sites)
        {
            sites = new List<Site>();
            bool ok = true;
            foreach (var line in lines)
            {
                Site site;
                if (Site.ReadLine(line, out site))
                {
                    ok &= true;
                    (sites as List<Site>).Add(site);
                }
                else
                {
                    ok = false;
                }
            }
            return ok;
        }
        #endregion

        #region Operators
        public object Clone()
        {
            return new Implantation(Name, Path);
        }
        public void Copy(object copy)
        {
            Implantation implantation = copy as Implantation;
            Name = implantation.Name;
            Path = implantation.Path;
        }
        #endregion
    }
}