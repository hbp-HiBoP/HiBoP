using UnityEngine.UI;
using d = HBP.Data.Experience.Protocol;
using Tools.Unity;

namespace HBP.UI.Experience.Protocol
{
    public class IconModifier : ItemModifier<d.Icon>
    {
        #region Properties
        InputField m_NameInputField;
        InputField m_MinInputField;
        InputField m_MaxInputField;
        ImageSelector m_ImageSelector;
        #endregion

        protected override void SetFields(d.Icon objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_NameInputField.onValueChanged.AddListener((name) => ItemTemp.Name = name);

            m_MinInputField.text = objectToDisplay.Window.Start.ToString();
            m_MinInputField.onValueChanged.AddListener((min) => ItemTemp.Window = new Tools.CSharp.Window(int.Parse(min), ItemTemp.Window.End));
            m_MaxInputField.text = objectToDisplay.Window.End.ToString();
            m_MaxInputField.onValueChanged.AddListener((max) => ItemTemp.Window = new Tools.CSharp.Window(ItemTemp.Window.Start, int.Parse(max)));
            m_ImageSelector.Path = objectToDisplay.IllustrationPath;
            m_ImageSelector.onValueChanged.AddListener(() => ItemTemp.IllustrationPath = m_ImageSelector.Path);
        }

        protected override void SetInteractableFields(bool interactable)
        {
            m_NameInputField.interactable = interactable;
            m_MinInputField.interactable = interactable;
            m_MaxInputField.interactable = interactable;
            m_ImageSelector.interactable = interactable;
        }

        protected override void Initialize()
        {
            m_NameInputField = transform.Find("Content").Find("General").Find("Fields").Find("Left").Find("Name").GetComponentInChildren<InputField>();
            m_MinInputField = transform.Find("Content").Find("General").Find("Fields").Find("Left").Find("Window").Find("Panel").Find("Min").GetComponentInChildren<InputField>();
            m_MaxInputField = transform.Find("Content").Find("General").Find("Fields").Find("Left").Find("Window").Find("Panel").Find("Max").GetComponentInChildren<InputField>();
            m_ImageSelector = transform.Find("Content").Find("General").Find("Fields").Find("Right").GetComponentInChildren<ImageSelector>();
        }
    }
}