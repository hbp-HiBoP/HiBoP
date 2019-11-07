using UnityEngine;
using UnityEngine.UI;
using System.Linq;

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

        #region Public Methods
        public override void Save()
        {
            foreach (var window in WindowsReferencer.Windows.OfType<SavableWindow>()) window.Save();
            itemTemp.Coordinates = m_CoordinateListGestion.List.Objects.ToList();
            itemTemp.Tags = m_TagValueListGestion.List.Objects.ToList();
            base.Save();
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(Data.Site objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;

            m_CoordinateListGestion.List.Set(objectToDisplay.Coordinates);
            m_CoordinateListGestion.List.OnAddObject.AddListener(coordinate => ItemTemp.Coordinates.Add(coordinate));
            m_CoordinateListGestion.List.OnRemoveObject.AddListener(coordinate => ItemTemp.Coordinates.Remove(coordinate));
            m_CoordinateListGestion.List.OnUpdateObject.AddListener(coordinate => { ItemTemp.Coordinates[ItemTemp.Coordinates.FindIndex(t => t.Equals(coordinate))] = coordinate; });

            m_TagValueListGestion.Tags = ApplicationState.ProjectLoaded.Preferences.SitesTags.Concat(ApplicationState.ProjectLoaded.Preferences.GeneralTags).ToArray();
            m_TagValueListGestion.List.Set(objectToDisplay.Tags);
            m_TagValueListGestion.List.OnAddObject.AddListener(tag => ItemTemp.Tags.Add(tag));
            m_TagValueListGestion.List.OnRemoveObject.AddListener(tag => ItemTemp.Tags.Remove(tag));
            m_TagValueListGestion.List.OnUpdateObject.AddListener(tag => { ItemTemp.Tags[ItemTemp.Tags.FindIndex(t => t.Equals(tag))] = tag; });
        }
        protected override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onValueChanged.AddListener((name) => ItemTemp.Name = name);

            m_CoordinateListGestion.WindowsReferencer.OnOpenWindow.AddListener(window => WindowsReferencer.Add(window));
            m_TagValueListGestion.WindowsReferencer.OnOpenWindow.AddListener(window => WindowsReferencer.Add(window));
        }
        #endregion
    }
}