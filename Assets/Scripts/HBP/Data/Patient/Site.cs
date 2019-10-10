using HBP.Data.Tags;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Tools.CSharp;

namespace HBP.Data
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