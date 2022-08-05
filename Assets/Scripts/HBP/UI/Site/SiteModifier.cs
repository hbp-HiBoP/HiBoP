using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Tools.CSharp;
using HBP.Core.Data;

namespace HBP.UI
{
    /// <summary>
    /// Window to modify site.
    /// </summary>
    public class SiteModifier : ObjectModifier<Core.Data.Site>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] CoordinateListGestion m_CoordinateListGestion;
        [SerializeField] TagValueListGestion m_TagValueListGestion;

        /// <summary>
        /// True if interactable, False otherwise.
        /// </summary>
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
        /// <summary>
        /// Initialize the window.
        /// </summary>
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
        /// <summary>
        /// Set the fields.
        /// </summary>
        /// <param name="objectToDisplay">Site to display</param>
        protected override void SetFields(Core.Data.Site objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_CoordinateListGestion.List.Set(objectToDisplay.Coordinates);
            m_TagValueListGestion.Tags = ApplicationState.ProjectLoaded.Preferences.SitesTags.Concat(ApplicationState.ProjectLoaded.Preferences.GeneralTags).ToArray();
            m_TagValueListGestion.List.Set(objectToDisplay.Tags);
        }
        /// <summary>
        /// Called when the value on the nameInputField changed.
        /// </summary>
        /// <param name="value"></param>
        protected void OnChangeName(string value)
        {
            if(value != "")
            {
                ObjectTemp.Name = value;
            }
            else
            {
                m_NameInputField.text = ObjectTemp.Name;
            }
        }
        /// <summary>
        /// Called when a coordinate is added to the site.
        /// </summary>
        /// <param name="coordinate">Coordinate added</param>
        protected void OnAddCoordinate(Core.Data.Coordinate coordinate)
        {
            ObjectTemp.Coordinates.AddIfAbsent(coordinate);
        }
        /// <summary>
        /// Called when a coordinate is removed from the site.
        /// </summary>
        /// <param name="coordinate">Coordinate removed</param>
        protected void OnRemoveCoordinate(Core.Data.Coordinate coordinate)
        {
            ObjectTemp.Coordinates.Remove(coordinate);
        }
        /// <summary>
        /// Called when a coordinate is updated from the site.
        /// </summary>
        /// <param name="coordinate">Coordiate updated</param>
        protected void OnUpdateCoordinate(Core.Data.Coordinate coordinate)
        {
            int index = ObjectTemp.Coordinates.FindIndex(c => c.Equals(coordinate));
            if (index != -1)
            {
                ObjectTemp.Coordinates[index] = coordinate;
            }
        }
        /// <summary>
        /// Called when a tag is added to the site.
        /// </summary>
        /// <param name="tag">Tag added</param>
        protected void OnAddTag(Core.Data.BaseTagValue tag)
        {
            ObjectTemp.Tags.AddIfAbsent(tag);
        }
        /// <summary>
        /// Called when a tag is removed from the site.
        /// </summary>
        /// <param name="tag">Tag removed</param>
        protected void OnRemoveTag(Core.Data.BaseTagValue tag)
        {
            ObjectTemp.Tags.Remove(tag);
        }
        /// <summary>
        /// Called when a tag is updated from the site.
        /// </summary>
        /// <param name="tag"></param>
        protected void OnUpdateTag(Core.Data.BaseTagValue tag)
        {
            int index = ObjectTemp.Tags.FindIndex(t => t.Equals(tag));
            if(index != -1)
            {
                ObjectTemp.Tags[index] = tag;
            }
        }
        #endregion
    }
}