using Tools.CSharp;
using UnityEngine;
using UnityEngine.UI;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    /// <summary>
    /// Window to modify a protocol.
    /// </summary>
	public class ProtocolModifier : ObjectModifier<Core.Data.Protocol> 
	{
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] BlocListGestion m_BlocListGestion;
        [SerializeField] Button m_CreateBlocButton;
        [SerializeField] Button m_RemoveBlocButton;

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

                m_BlocListGestion.Interactable = value;
                m_CreateBlocButton.interactable = value;
                m_RemoveBlocButton.interactable = value;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Initialize the window.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onEndEdit.AddListener(ChangeName);

            m_BlocListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_BlocListGestion.List.OnAddObject.AddListener(AddBloc);
            m_BlocListGestion.List.OnRemoveObject.AddListener(RemoveBloc);
        }
        /// <summary>
        /// Set the fields
        /// </summary>
        /// <param name="objectToDisplay">Protocol to display</param>
        protected override void SetFields(Core.Data.Protocol objectToDisplay)
        {
            base.SetFields();

            m_NameInputField.text = objectToDisplay.Name;
            m_BlocListGestion.List.Set(objectToDisplay.Blocs);
        }
        /// <summary>
        /// Change the name.
        /// </summary>
        /// <param name="value">Name of the protocol</param>
        protected void ChangeName(string value)
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
        /// Add bloc to the protocol.
        /// </summary>
        /// <param name="bloc">Bloc to add</param>
        protected void AddBloc(Core.Data.Bloc bloc)
        {
            ObjectTemp.Blocs.AddIfAbsent(bloc);
        }
        /// <summary>
        /// Remove bloc to the protocol.
        /// </summary>
        /// <param name="bloc">Bloc to remove</param>
        protected void RemoveBloc(Core.Data.Bloc bloc)
        {
            ObjectTemp.Blocs.Remove(bloc);
        }
        #endregion
    }
}