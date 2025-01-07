using HBP.Core.Tools;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.Core.Data
{
    public class TagCollection : BaseData
    {
        #region Properties
        public static string PATH = Path.Combine(Application.persistentDataPath, "Tags.json");
        public ReadOnlyCollection<BaseTag> AllTags
        {
            get
            {
                List<BaseTag> tags = new List<BaseTag>();
                tags.AddRange(GeneralTags);
                tags.AddRange(PatientsTags);
                tags.AddRange(SitesTags);
                return new ReadOnlyCollection<BaseTag>(tags);
            }
        }
        [DataMember] public List<BaseTag> GeneralTags { get; set; }
        [DataMember] public List<BaseTag> PatientsTags { get; set; }
        [DataMember] public List<BaseTag> SitesTags { get; set; }
        #endregion

        #region Constructors
        public TagCollection(IEnumerable<BaseTag> generalTags, IEnumerable<BaseTag> patientsTags, IEnumerable<BaseTag> sitesTags, string ID) : base(ID)
        {
            GeneralTags = generalTags.ToList();
            PatientsTags = patientsTags.ToList();
            SitesTags = sitesTags.ToList();
        }
        public TagCollection(IEnumerable<BaseTag> generalTags, IEnumerable<BaseTag> patientsTags, IEnumerable<BaseTag> sitesTags) : base()
        {
            GeneralTags = generalTags.ToList();
            PatientsTags = patientsTags.ToList();
            SitesTags = sitesTags.ToList();
        }
        public TagCollection() : this(new List<BaseTag>(), new List<BaseTag>(), new List<BaseTag>())
        {
        }
        #endregion

        #region Events
        public UnityEvent OnSaveTags = new UnityEvent();
        #endregion

        #region Public Methods
        public override void GenerateID()
        {
            base.GenerateID();
            foreach (var tag in GeneralTags) tag.GenerateID();
            foreach (var tag in PatientsTags) tag.GenerateID();
            foreach (var tag in SitesTags) tag.GenerateID();
        }
        public override List<BaseData> GetAllIdentifiable()
        {
            List<BaseData> IDs = base.GetAllIdentifiable();
            foreach (var tag in GeneralTags) IDs.AddRange(tag.GetAllIdentifiable());
            foreach (var tag in PatientsTags) IDs.AddRange(tag.GetAllIdentifiable());
            foreach (var tag in SitesTags) IDs.AddRange(tag.GetAllIdentifiable());
            return IDs;
        }
        public void Save()
        {
            ClassLoaderSaver.SaveToJSon(this, PATH, true);
            OnSaveTags.Invoke();
        }
        public override object Clone()
        {
            return new TagCollection(GeneralTags.DeepClone(), PatientsTags.DeepClone(), SitesTags.DeepClone(), ID);
        }
        public override void Copy(object copy)
        {
            if (copy is TagCollection tagsCollection)
            {
                GeneralTags = tagsCollection.GeneralTags;
                PatientsTags = tagsCollection.PatientsTags;
                SitesTags = tagsCollection.SitesTags;
            }
        }
        #endregion
    }
}