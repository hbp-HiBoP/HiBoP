using HBP.Core.Data;
using HBP.Core.Errors;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    [RequireComponent(typeof(Toggle))]
    public class BasicBlocItem : MonoBehaviour
    {
        #region Properties
        [SerializeField] private Text m_IndexText;
        [SerializeField] private InputField m_NameInputField;
        [SerializeField] private InputField m_MainCodesInputField;
        [SerializeField] private InputField m_SecondaryCodesInputField;

        private Bloc m_Bloc;
        public Bloc Bloc
        {
            get => m_Bloc;
            set
            {
                m_Bloc = value;
                if (m_Bloc != null)
                {
                    m_IndexText.text = m_Bloc.Order.ToString();
                    m_NameInputField.text = m_Bloc.Name;

                    if (m_Bloc.MainSubBloc == null || m_Bloc.MainSubBloc.MainEvent == null)
                        return;

                    m_MainCodesInputField.text = m_Bloc.MainSubBloc.MainEvent.CodesString;

                    if (m_Bloc.MainSubBloc.SecondaryEvents.Count == 0)
                        return;

                    m_SecondaryCodesInputField.text = m_Bloc.MainSubBloc.SecondaryEvents[0].CodesString;
                }
            }
        }

        private Toggle m_Toggle;
        public bool Selected => m_Toggle != null ? m_Toggle.isOn : false;
        #endregion

        #region Events
        public Toggle.ToggleEvent OnValueChanged => m_Toggle?.onValueChanged ?? new Toggle.ToggleEvent();
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_Toggle = GetComponent<Toggle>();
        }
        #endregion

        #region Public Methods
        public void OnChangeName(string name)
        {
            if (m_Bloc == null)
                return;

            m_Bloc.Name = name;

            if (m_Bloc.MainSubBloc != null && m_Bloc.MainSubBloc.MainEvent != null)
                m_Bloc.MainSubBloc.MainEvent.Name = name;
        }
        public void OnChangeMainCodes(string codes)
        {
            if (m_Bloc == null)
                return;

            if (m_Bloc.MainSubBloc == null)
                m_Bloc.SubBlocs.Add(new SubBloc() { Name = "Main" });

            if (m_Bloc.MainSubBloc.MainEvent == null)
                m_Bloc.MainSubBloc.Events.Add(new Core.Data.Event(Core.Enums.MainSecondaryEnum.Main) { Name = m_Bloc.Name });

            m_Bloc.MainSubBloc.MainEvent.CodesString = codes;

            if (m_MainCodesInputField.text != codes)
                m_MainCodesInputField.text = m_Bloc.MainSubBloc.MainEvent.CodesString;
        }
        public void OnChangeSecondaryCodes(string codes)
        {
            if (m_Bloc == null)
                return;

            if (m_Bloc.MainSubBloc == null)
                m_Bloc.SubBlocs.Add(new SubBloc() { Name = "Main" });

            if (string.IsNullOrEmpty(codes))
            {
                m_Bloc.MainSubBloc.Events.RemoveAll(e => e.Type == Core.Enums.MainSecondaryEnum.Secondary);
                return;
            }

            if (m_Bloc.MainSubBloc.SecondaryEvents.Count == 0)
                m_Bloc.MainSubBloc.Events.Add(new Core.Data.Event(Core.Enums.MainSecondaryEnum.Secondary) { Name = "RESPONSE" });

            m_Bloc.MainSubBloc.SecondaryEvents[0].CodesString = codes;

            if (m_SecondaryCodesInputField.text != codes)
                m_SecondaryCodesInputField.text = m_Bloc.MainSubBloc.SecondaryEvents[0].CodesString;
        }
        public void Refresh()
        {
            Bloc = m_Bloc;
        }
        #endregion
    }
}