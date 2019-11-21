using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Tools.CSharp;

namespace HBP.UI
{
    public class SiteModifier : ObjectModifier<Data.Site>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] CoordinateListGestion m_CoordinateListGestion;
        [SerializeField] TagValueListGestion m_TagValueListGestion;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;

                m_NameInputField.interactable = value;
                m_CoordinateListGestion.Interactable = value;
                m_TagValueListGestion.Interactable = value;
            }
        }
        #endregion

        #region Protected Methods
        protected override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onEndEdit.AddListener(OnChangeName);

            m_CoordinateListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_CoordinateListGestion.List.OnAddObject.AddListener(OnAddCoordinate);
            m_CoordinateListGestion.List.OnRemoveObject.AddListener(OnRemoveCoordinate);
            m_CoordinateListGestion.List.OnUpdateObject.AddListener(OnUpdateCoordinate);

            m_TagValueListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_TagValueListGestion.List.OnAddObject.AddListener(OnAddTag);
            m_TagValueListGestion.List.OnRemoveObject.AddListener(OnRemoveTag);
            m_TagValueListGestion.List.OnUpdateObject.AddListener(OnUpdateTag);
        }

        protected override void SetFields(Data.Site objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_CoordinateListGestion.List.Set(objectToDisplay.Coordinates);
            m_TagValueListGestion.Tags = ApplicationState.ProjectLoaded.Preferences.SitesTags.Concat(ApplicationState.ProjectLoaded.Preferences.GeneralTags).ToArray();
            m_TagValueListGestion.List.Set(objectToDisplay.Tags);
        }

        protected void OnChangeName(string value)
        {
            if(value != "")
            {
                ItemTemp.Name = value;
            }
            else
            {
                m_NameInputField.text = ItemTemp.Name;
            }
        }

        protected void OnAddCoordinate(Data.Coordinate coordinate)
        {
            ItemTemp.Coordinates.AddIfAbsent(coordinate);
        }
        protected void OnRemoveCoordinate(Data.Coordinate coordinate)
        {
            ItemTemp.Coordinates.Remove(coordinate);
        }
        protected void OnUpdateCoordinate(Data.Coordinate coordinate)
        {
            int index = ItemTemp.Coordinates.FindIndex(c => c.Equals(coordinate));
            if (index != -1)
            {
                ItemTemp.Coordinates[index] = coordinate;
            }
        }

        protected void OnAddTag(Data.BaseTagValue tag)
        {
            ItemTemp.Tags.AddIfAbsent(tag);
        }
        protected void OnRemoveTag(Data.BaseTagValue tag)
        {
            ItemTemp.Tags.Remove(tag);
        }
        protected void OnUpdateTag(Data.BaseTagValue tag)
        {
            int index = ItemTemp.Tags.FindIndex(t => t.Equals(tag));
            if(index != -1)
            {
                ItemTemp.Tags[index] = tag;
            }
        }
        #endregion
    }
}