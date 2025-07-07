using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.Data.Preferences;
using HBP.UI.Tools;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class BasicProtocolSubModifier : SubModifier<Protocol>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] RangeSlider m_WindowSlider;
        [SerializeField] RangeSlider m_BaselineSlider;

        [SerializeField] BlocListGestion m_BlocListGestion;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;

                m_NameInputField.interactable = value;
                m_WindowSlider.interactable = value;
                m_BaselineSlider.interactable = value;

                m_BlocListGestion.Interactable = value;
                m_BlocListGestion.Modifiable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onEndEdit.AddListener(ChangeName);
            m_WindowSlider.onValueChanged.AddListener(ChangeWindow);
            m_BaselineSlider.onValueChanged.AddListener(ChangeBaseline);

            m_BlocListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_BlocListGestion.List.OnAddObject.AddListener(AddBloc);
            m_BlocListGestion.List.OnRemoveObject.AddListener(RemoveBloc);
        }
        #endregion

        #region Private Methods
        protected override void SetFields(Protocol objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            m_NameInputField.text = objectToDisplay.Name;

            ProtocolPreferences preferences = PersistentDataManager.UserPreferences.Data.Protocol;
            SubBloc firstSubBloc = objectToDisplay.Blocs.Count > 0 ? objectToDisplay.Blocs[0].SubBlocs.Count > 0 ? objectToDisplay.Blocs[0].SubBlocs[0] : null : null;

            m_WindowSlider.minLimit = preferences.MinLimit;
            m_WindowSlider.maxLimit = preferences.MaxLimit;
            m_WindowSlider.step = preferences.Step;
            m_WindowSlider.Values = firstSubBloc != null ? firstSubBloc.Window.ToVector2() : new Vector2(-1000, 1000);

            m_BaselineSlider.minLimit = preferences.MinLimit;
            m_BaselineSlider.maxLimit = preferences.MaxLimit;
            m_BaselineSlider.step = preferences.Step;
            m_BaselineSlider.Values = firstSubBloc != null ? firstSubBloc.Baseline.ToVector2() : new Vector2(-300, 0);

            m_BlocListGestion.List.Set(objectToDisplay.Blocs);
        }
        protected void ChangeName(string value)
        {
            if (value != "")
            {
                Object.Name = value;
            }
            else
            {
                m_NameInputField.text = Object.Name;
            }
        }
        protected void ChangeWindow(float min, float max)
        {
            foreach (var subBloc in Object.Blocs.SelectMany(b => b.SubBlocs))
            {
                subBloc.Window = new TimeWindow(Mathf.RoundToInt(min), Mathf.RoundToInt(max));
            }
        }
        protected void ChangeBaseline(float min, float max)
        {
            foreach (var subBloc in Object.Blocs.SelectMany(b => b.SubBlocs))
            {
                subBloc.Baseline = new TimeWindow(Mathf.RoundToInt(min), Mathf.RoundToInt(max));
            }
        }
        protected void AddBloc(Bloc bloc)
        {
            Object.Blocs.AddIfAbsent(bloc);
        }
        protected void RemoveBloc(Bloc bloc)
        {
            Object.Blocs.Remove(bloc);
        }
        #endregion
    }
}