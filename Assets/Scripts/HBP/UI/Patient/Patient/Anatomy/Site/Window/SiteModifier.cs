using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Anatomy;
using System.Linq;

namespace HBP.UI.Anatomy
{
    public class SiteModifier : ObjectModifier<Site>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] CoordinateListGestion m_CoordinateListGestion;
        [SerializeField] Tags.TagValueListGestion m_TagValueListGestion;

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
        protected override void SetFields(Site objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_CoordinateListGestion.List.Set(objectToDisplay.Coordinates);

            m_TagValueListGestion.Tags = ApplicationState.ProjectLoaded.Settings.SitesTags.Concat(ApplicationState.ProjectLoaded.Settings.GeneralTags).ToArray();
            m_TagValueListGestion.List.Set(objectToDisplay.Tags);
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