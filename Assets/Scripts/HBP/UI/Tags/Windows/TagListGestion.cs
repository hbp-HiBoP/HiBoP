using System.Collections.Generic;
using System.Collections.ObjectModel;
using Tools.Unity.Components;

namespace HBP.UI.Tags
{
    public class TagListGestion : ListGestion<Data.Tags.Tag>
    {
        #region Properties
        public new TagList List;
        public override List<Data.Tags.Tag> Objects
        {
            get
            {
                return base.Objects;
            }

            set
            {
                List.Initialize();
                base.Objects = value;
                List.SortByName(TagList.Sorting.Descending);
            }
        }

        List<Data.Tags.Tag> m_ModifiedTags = new List<Data.Tags.Tag>();
        public ReadOnlyCollection<Data.Tags.Tag> ModifiedTags
        {
            get
            {
                return new ReadOnlyCollection<Data.Tags.Tag>(m_ModifiedTags);
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.List = List;
            base.Initialize();
        }
        protected override void OnSaveCreator(CreatorWindow creatorWindow)
        {
            Data.Enums.CreationType type = creatorWindow.Type;
            Data.Tags.Tag item = new Data.Tags.EmptyTag();
            switch (type)
            {
                case Data.Enums.CreationType.FromScratch:
                    OpenModifier(item, true);
                    break;
                case Data.Enums.CreationType.FromExistingItem:
                    OpenSelector(Objects.ToArray());
                    break;
                case Data.Enums.CreationType.FromFile:
                    if (LoadFromFile(out item)) OpenModifier(item, true);
                    break;
            }
        }
        protected override void OnSaveModifier(ItemModifier<Data.Tags.Tag> modifier)
        {
            base.OnSaveModifier(modifier);
            m_ModifiedTags.Add(modifier.Item);
        }
        #endregion
    }
}