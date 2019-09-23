using System.Collections.Generic;
using Tools.Unity.Components;
using UnityEngine;
using HBP.Data.Tags;

namespace HBP.UI.Tags
{
    public class TagValueListGestion : ListGestion<BaseTagValue>
    {
        #region Properties
        [SerializeField] new TagValueList List;
        public override List<BaseTagValue> Objects
        {
            get
            {
                return base.Objects;
            }

            set
            {
                List.Initialize();
                base.Objects = value;
                List.SortByTag(TagValueList.Sorting.Descending);
            }
        }

        public Tag[] Tags { get; set; }
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
            BaseTagValue item = new IntTagValue();
            switch (type)
            {
                case Data.Enums.CreationType.FromScratch:
                    OpenModifier(item, true);
                    break;
                case Data.Enums.CreationType.FromExistingItem:
                    OpenSelector(Objects.ToArray());
                    break;
                case Data.Enums.CreationType.FromFile:
                    if (LoadFromFile(out item))
                    {
                        OpenModifier(item, true);
                    }
                    break;
            }
        }
        #endregion

        #region Protected Methods
        protected override ItemModifier<BaseTagValue> OpenModifier(BaseTagValue item, bool interactable)
        {
            TagValueModifier modifier = (TagValueModifier) base.OpenModifier(item, interactable);
            modifier.Tags = Tags;
            return modifier;
        }
        #endregion
    }
}
