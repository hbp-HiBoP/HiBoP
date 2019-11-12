using HBP.Data.Tags;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Tools.CSharp;

namespace HBP.Data.Anatomy
{
    [DataContract]
    public class Site : BaseData
    {
        #region Properties
        /// <summary>
        /// Name of the site.
        /// </summary>
        [DataMember] public string Name { get; set; }
        /// <summary>
        /// Coordinates of the site.
        /// </summary>
        [DataMember] public List<Coordinate> Coordinates { get; set; }
        /// <summary>
        /// Tags of the site.
        /// </summary>
        [DataMember] public List<BaseTagValue> Tags { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the site class.
        /// </summary>
        /// <param name="name">Name of the site.</param>
        /// <param name="tags">Tags of the site.</param>
        /// <param name="ID">Unique identifier to identify the patient.</param>
        public Site(string name, IEnumerable<Coordinate> coordinates, IEnumerable<BaseTagValue> tags, string ID) : base(ID)
        {
            Name = name;
            Coordinates = coordinates.ToList();
            Tags = tags.ToList();
        }
        /// <summary>
        /// Initializes a new instance of the site class.
        /// </summary>
        /// <param name="name">Name of the site.</param>
        /// <param name="tags">Tags of the site.</param>
        public Site(string name, IEnumerable<Coordinate> coordinates, IEnumerable<BaseTagValue> tags) : base()
        {
            Name = name;
            Coordinates = coordinates.ToList();
            Tags = tags.ToList();
        }
        /// <summary>
        /// Initializes a new instance of the site class.
        /// </summary>
        public Site() : this("Unknown", new Coordinate[0], new BaseTagValue[0])
        {
        }
        #endregion

        #region Public Methods
        public static List<Site> LoadImplantationFromBIDSFile(string referenceSystem, string tsvFile)
        {
            List<Site> result = new List<Site>();
            if (!string.IsNullOrEmpty(tsvFile))
            {
                using (StreamReader sr = new StreamReader(tsvFile))
                {
                    // Find which column of the tsv corresponds to which mandatory argument
                    string firstLine = sr.ReadLine();
                    string[] firstLineSplits = firstLine.Split('\t');
                    int[] indices = new int[4]
                    {
                        System.Array.IndexOf(firstLineSplits, "name"),
                        System.Array.IndexOf(firstLineSplits, "x"),
                        System.Array.IndexOf(firstLineSplits, "y"),
                        System.Array.IndexOf(firstLineSplits, "z")
                    };
                    Dictionary<int, Tag> tagByColumnIndex = new Dictionary<int, Tag>();
                    for (int i = 0; i < firstLineSplits.Length; i++)
                    {
                        if (indices.Contains(i)) continue;
                        Tag associatedTag = ApplicationState.ProjectLoaded.Settings.SitesTags.FirstOrDefault(t => t.Name == firstLineSplits[i]);
                        if (associatedTag == null) associatedTag = ApplicationState.ProjectLoaded.Settings.GeneralTags.FirstOrDefault(t => t.Name == firstLineSplits[i]);
                        tagByColumnIndex.Add(i, associatedTag);
                    }
                    // Create sites
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        Site site = new Site();
                        string[] args = line.Split('\t');
                        site.Name = ApplicationState.UserPreferences.Data.Anatomic.SiteNameCorrection ? FixName(args[indices[0]]) : args[indices[0]];
                        if (!NumberExtension.TryParseFloat(args[indices[1]], out float x)) continue;
                        if (!NumberExtension.TryParseFloat(args[indices[2]], out float y)) continue;
                        if (!NumberExtension.TryParseFloat(args[indices[3]], out float z)) continue;
                        site.Coordinates.Add(new Coordinate(referenceSystem, new UnityEngine.Vector3(x, y, z)));
                        foreach (var kvTag in tagByColumnIndex)
                        {
                            site.Tags.Add(new BaseTagValue(kvTag.Value, args[kvTag.Key]));
                        }
                        result.Add(site);
                    }
                }
            }
            return result;
        }
        public static List<Site> LoadImplantationFromIntrAnatFile(string referenceSystem, string ptsFile, string csvFile)
        {
            List<Site> result = new List<Site>();
            if (!string.IsNullOrEmpty(ptsFile))
            {
                using (StreamReader ptssr = new StreamReader(ptsFile))
                {
                    string line;
                    line = ptssr.ReadLine();
                    if (!line.Contains("ptsfile")) throw new System.Exception("Invalid PTS file");
                    while ((line = ptssr.ReadLine()) != null)
                    {
                        Site site = new Site();
                        string[] splits = Regex.Split(line, "[\\s\t]+");
                        if (splits.Length < 4) continue;
                        site.Name = ApplicationState.UserPreferences.Data.Anatomic.SiteNameCorrection ? FixName(splits[0]) : splits[0];
                        if (!NumberExtension.TryParseFloat(splits[1], out float x)) continue;
                        if (!NumberExtension.TryParseFloat(splits[2], out float y)) continue;
                        if (!NumberExtension.TryParseFloat(splits[3], out float z)) continue;
                        site.Coordinates.Add(new Coordinate(referenceSystem, new UnityEngine.Vector3(x, y, z)));
                        result.Add(site);
                    }
                }
                if (!string.IsNullOrEmpty(csvFile))
                {
                    using (StreamReader csvsr = new StreamReader(csvFile))
                    {
                        csvsr.ReadLine();
                        csvsr.ReadLine();
                        // Find which column of the tsv corresponds to which mandatory argument
                        string firstLine = csvsr.ReadLine();
                        string[] firstLineSplits = firstLine.Split('\t');
                        int[] indices = new int[2]
                        {
                            System.Array.IndexOf(firstLineSplits, "contact"),
                            System.Array.IndexOf(firstLineSplits, "MNI")
                        };
                        Dictionary<int, Tag> tagByColumnIndex = new Dictionary<int, Tag>();
                        for (int i = 0; i < firstLineSplits.Length; i++)
                        {
                            if (indices.Contains(i)) continue;
                            Tag associatedTag = ApplicationState.ProjectLoaded.Settings.SitesTags.FirstOrDefault(t => t.Name == firstLineSplits[i]);
                            if (associatedTag == null) associatedTag = ApplicationState.ProjectLoaded.Settings.GeneralTags.FirstOrDefault(t => t.Name == firstLineSplits[i]);
                            tagByColumnIndex.Add(i, associatedTag);
                        }
                        // Fill tags
                        string line;
                        while ((line = csvsr.ReadLine()) != null)
                        {
                            string[] args = line.Split('\t');
                            string siteName = ApplicationState.UserPreferences.Data.Anatomic.SiteNameCorrection ? FixName(args[indices[0]]) : args[indices[0]];
                            Site site = result.FirstOrDefault(s => s.Name == siteName);
                            if (site != null)
                            {
                                foreach (var kvTag in tagByColumnIndex)
                                {
                                    site.Tags.Add(new BaseTagValue(kvTag.Value, args[kvTag.Key]));
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }
        public static string FixName(string name)
        {
            string siteName = name.ToUpper();
            int prime = siteName.LastIndexOf('P');
            if (prime > 0)
            {
                siteName = siteName.Remove(prime, 1).Insert(prime, "\'");
            }
            for (int i = siteName.Length - 1; i > 0; --i)
            {
                if (siteName[i] == '0' && !char.IsDigit(siteName[i - 1]))
                {
                    siteName = siteName.Remove(i, 1);
                }
            }
            return siteName;
        }
        /// <summary>
        /// Generates  ID recursively.
        /// </summary>
        public override void GenerateID()
        {
            base.GenerateID();
            foreach (var tag in Tags) tag.GenerateID();
            foreach (var coordinate in Coordinates) coordinate.GenerateID();
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone the instance.
        /// </summary>
        /// <returns>object cloned.</returns>
        public override object Clone()
        {
            return new Site(Name, Coordinates.DeepClone(), Tags.DeepClone(), ID);
        }
        /// <summary>
        /// Copy the instance.
        /// </summary>
        /// <param name="obj">instance to copy.</param>
        public override void Copy(object obj)
        {
            base.Copy(obj);
            if (obj is Site site)
            {
                Name = site.Name;
                Coordinates = site.Coordinates;
                Tags = site.Tags;
            }
        }
        #endregion
    }
}