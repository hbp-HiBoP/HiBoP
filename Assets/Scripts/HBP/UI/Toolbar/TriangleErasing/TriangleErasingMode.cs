using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Enums;


namespace HBP.UI.Toolbar
{
    public class TriangleErasingMode : Tool
    {
        #region Properties
        /// <summary>
        /// Dropdown to select the erasing mode
        /// </summary>
        [SerializeField] private Dropdown m_Dropdown;
        /// <summary>
        /// Parent of the inputfield to set the degrees of the area
        /// </summary>
        [SerializeField] private RectTransform m_InputFieldParent;
        /// <summary>
        /// Inputfield to set the degrees beyond which the triangles are not erased
        /// </summary>
        [SerializeField] private InputField m_InputField;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Dropdown.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                TriEraserMode mode = (TriEraserMode)value;
                SelectedScene.TriangleEraser.CurrentMode = mode;
                UpdateInteractable();
            });

            m_InputField.onEndEdit.AddListener((value) =>
            {
                if (ListenerLock) return;

                int degrees = 30;
                int.TryParse(value, out degrees);
                SelectedScene.TriangleEraser.Degrees = degrees;
                m_InputField.text = degrees.ToString();
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Dropdown.interactable = false;
            m_Dropdown.value = 0;
            m_InputField.interactable = false;
            m_InputField.text = "30";
            m_InputFieldParent.gameObject.SetActive(false);
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool isZoneModeEnabled = SelectedScene.TriangleEraser.CurrentMode == TriEraserMode.Zone;

            m_InputFieldParent.gameObject.SetActive(isZoneModeEnabled);
            m_InputField.gameObject.SetActive(isZoneModeEnabled);
            m_Dropdown.interactable = true;
            m_InputField.interactable = isZoneModeEnabled;
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            m_Dropdown.value = (int)SelectedScene.TriangleEraser.CurrentMode;
            m_InputField.text = SelectedScene.TriangleEraser.Degrees.ToString();
            if (SelectedScene.TriangleEraser.CurrentMode != TriEraserMode.Zone)
            {
                m_InputField.gameObject.SetActive(false);
                m_InputFieldParent.gameObject.SetActive(false);
            }
            else
            {
                m_InputField.gameObject.SetActive(true);
                m_InputFieldParent.gameObject.SetActive(true);
            }
        }
        #endregion
    }
}