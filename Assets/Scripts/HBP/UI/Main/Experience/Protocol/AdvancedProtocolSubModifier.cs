using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.UI.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class AdvancedProtocolSubModifier : SubModifier<Protocol>
    {
        #region Properties

        [SerializeField] InputField m_NameInputField;
        [SerializeField] BlocListGestion m_BlocListGestion;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;

                m_NameInputField.interactable = value;
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
