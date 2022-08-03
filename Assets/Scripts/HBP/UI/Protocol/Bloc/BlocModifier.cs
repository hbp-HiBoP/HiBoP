using UnityEngine.UI;
using Tools.Unity;
using UnityEngine;
using HBP.Theme.Components;
using Tools.CSharp;

namespace HBP.UI.Experience.Protocol
{
    /// <summary>
    /// Window to modify a bloc.
    /// </summary>
    public class BlocModifier : ObjectModifier<Core.Data.Bloc>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField, m_SortInputField, m_OrderInputField;
        [SerializeField] SubBlocListGestion m_SubBlocListGestion;
        [SerializeField] ImageSelector m_ImageFileSelector;

        [SerializeField] ThemeElement m_SortStateThemeElement;
        [SerializeField] Tooltip m_SortErrorText;
        [SerializeField] HBP.Theme.State m_OKState;
        [SerializeField] HBP.Theme.State m_ErrorState;

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
                m_SortInputField.interactable = value;
                m_OrderInputField.interactable = value;

                m_SubBlocListGestion.Interactable = value;
                m_SubBlocListGestion.Modifiable = value;
                m_ImageFileSelector.interactable = value;
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
            m_ImageFileSelector.onValueChanged.AddListener(ChangeImage);
            m_SortInputField.onEndEdit.AddListener(ChangeSort);
            m_OrderInputField.onEndEdit.AddListener(ChangeOrder);

            m_SubBlocListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_SubBlocListGestion.List.OnAddObject.AddListener(AddSubBloc);
            m_SubBlocListGestion.List.OnRemoveObject.AddListener(RemoveSubBloc);
        }
        /// <summary>
        /// Set the fields.
        /// </summary>
        /// <param name="objectToDisplay">Bloc to display</param>
        protected override void SetFields(Core.Data.Bloc objectToDisplay)
        {
            base.SetFields();

            m_NameInputField.text = objectToDisplay.Name;
            m_ImageFileSelector.Path = objectToDisplay.IllustrationPath;
            m_SortInputField.text = objectToDisplay.Sort;
            ChangeSort(objectToDisplay.Sort);
            m_OrderInputField.text = objectToDisplay.Order.ToString();
            m_SubBlocListGestion.List.Set(objectToDisplay.SubBlocs);
        }
        /// <summary>
        /// Change name.
        /// </summary>
        /// <param name="value">Name</param>
        protected void ChangeName(string value)
        {
            if (value != "")
            {
                ObjectTemp.Name = value;
            }
            else
            {
                m_NameInputField.text = ObjectTemp.Name;
            }
        }
        /// <summary>
        /// Change image.
        /// </summary>
        /// <param name="value">Path to the image file</param>
        protected void ChangeImage(string value)
        {
            ObjectTemp.IllustrationPath = value;
        }
        /// <summary>
        /// Change sort.
        /// </summary>
        /// <param name="value">Sorting</param>
        protected void ChangeSort(string value)
        {
            ObjectTemp.Sort = value;
            Core.Data.Bloc.SortingMethodError error = ObjectTemp.GetSortingMethodError();
            m_SortErrorText.Text = ObjectTemp.GetSortingMethodErrorMessage(error);
            m_SortStateThemeElement.Set(error == Core.Data.Bloc.SortingMethodError.NoError ? m_OKState : m_ErrorState);
        }
        /// <summary>
        /// Change order.
        /// </summary>
        /// <param name="value">Order</param>
        protected void ChangeOrder(string value)
        {
            if (int.TryParse(value, out int order))
            {
                ObjectTemp.Order = order;
            }
            else
            {
                m_OrderInputField.text = ObjectTemp.Order.ToString();
            }
        }
        /// <summary>
        /// Add subBloc to the bloc.
        /// </summary>
        /// <param name="subBloc">SubBloc to add</param>
        protected void AddSubBloc(Core.Data.SubBloc subBloc)
        {
            ObjectTemp.SubBlocs.AddIfAbsent(subBloc);
        }
        /// <summary>
        /// Remove subBloc from the bloc.
        /// </summary>
        /// <param name="subBloc">SubBloc to remove</param>
        protected void RemoveSubBloc(Core.Data.SubBloc subBloc)
        {
            ObjectTemp.SubBlocs.Remove(subBloc);
        }
        #endregion
    }
}