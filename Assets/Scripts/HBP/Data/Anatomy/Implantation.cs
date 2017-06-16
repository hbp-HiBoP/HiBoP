using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using UnityEngine;
using System.Linq;

namespace HBP.Data.Anatomy
{
    [DataContract]
    public class Implantation
    {
        #region Properties
        const string HEADER = "ptsfile";
        public const string EXTENSION = ".pts";
        public enum Error { None, PathIsNullOrEmpty, FileNotFound, WrongExtension, CannotReadFile, WrongFormat, CannotReadAllSites};
        [DataMember] public string Name { get; set; }
        [DataMember] public string Path { get; set; }
        [IgnoreDataMember] public Electrode[] Electrodes { get; set; }
        //[IgnoreDataMember] public Brain Brain { get; set; }
        #endregion

        #region Constructor
        public Implantation(string name,string path)
        {
            Name = name;
            Path = path;
            Electrodes = new Site[0];
        }
        public Implantation(): this("New implantation",string.Empty)
        {
            Electrodes = new List<Electrode>();
        }
        #endregion

        #region Public Methods
        public Error Load()
        {
            if(string.IsNullOrEmpty(Path)) return Error.PathIsNullOrEmpty;
            FileInfo fileInfo = new FileInfo(Path);
            if(!fileInfo.Exists) return Error.FileNotFound;
            if(fileInfo.Extension != EXTENSION) return Error.WrongExtension;
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
            Electrodes = sites.ToArray();
            return Error.None;
        }
        public void Unload()
        {
            Electrodes = new Site[0];
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
    }
}