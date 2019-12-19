using Tools.CSharp;
using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Protocol;

namespace HBP.UI.Experience.Protocol
{
	public class ProtocolModifier : ObjectModifier<d.Protocol> 
	{
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] BlocListGestion m_BlocListGestion;
        [SerializeField] Button m_CreateBlocButton;
        [SerializeField] Button m_RemoveBlocButton;

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

                m_BlocListGestion.Interactable = value;
                m_CreateBlocButton.interactable = value;
                m_RemoveBlocButton.interactable = value;
            }
        }
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onEndEdit.AddListener(OnChangeName);

            m_BlocListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_BlocListGestion.List.OnAddObject.AddListener(OnAddBloc);
            m_BlocListGestion.List.OnRemoveObject.AddListener(OnRemoveBloc);
        }
        protected override void SetFields(d.Protocol objectToDisplay)
        {
            base.SetFields();

            m_NameInputField.text = objectToDisplay.Name;
            m_BlocListGestion.List.Set(objectToDisplay.Blocs);
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
        protected void OnAddBloc(d.Bloc bloc)
        {
            ItemTemp.Blocs.AddIfAbsent(bloc);
        }
        protected void OnRemoveBloc(d.Bloc bloc)
        {
            ItemTemp.Blocs.Remove(bloc);
        }
        #endregion
    }
}