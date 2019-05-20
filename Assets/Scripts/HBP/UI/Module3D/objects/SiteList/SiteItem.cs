﻿using HBP.Module3D;
using Tools.Unity;
using Tools.Unity.Lists;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class SiteItem : Item<Site>
    {
        #region Properties
        [SerializeField] private Button m_Site;
        [SerializeField] private Image m_SelectedImage;
        [SerializeField] private Button m_LabelsButton;
        [SerializeField] private Text m_LabelsText;
        [SerializeField] private Text m_Patient;
        [SerializeField] private Toggle m_Blacklisted;
        [SerializeField] private Toggle m_Highlighted;
        [SerializeField] private Button m_Color;
        [SerializeField] private Image m_ColorImage;
        [SerializeField] private GameObject m_ColorPickerPrefab;

        public override Site Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                UpdateFields();
                value.State.OnChangeState.AddListener(() => m_UpdateRequired = true);
            }
        }
        private bool m_UpdateRequired;
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_Site.onClick.AddListener(() =>
            {
                Object.IsSelected = true;
            });

            m_Blacklisted.onValueChanged.AddListener((isOn) =>
            {
                Object.State.IsBlackListed = isOn;
            });

            m_Highlighted.onValueChanged.AddListener((isOn) =>
            {
                Object.State.IsHighlighted = isOn;
            });

            m_Color.onClick.AddListener(() =>
            {
                ApplicationState.Module3DUI.ColorPicker.Open(Object.State.Color, (c) => Object.State.Color = c);
            });
        }
        private void Update()
        {
            if (m_UpdateRequired)
            {
                UpdateFields();
            }
        }
        private void UpdateFields()
        {
            m_Site.GetComponentInChildren<Text>().text = Object.Information.ChannelName;
            m_Site.interactable = Object.IsActive;
            m_SelectedImage.gameObject.SetActive(Object.IsSelected);
            m_LabelsText.text = Object.State.Labels.Count.ToString();
            m_Patient.text = Object.Information.Patient.Name;
            m_Blacklisted.isOn = Object.State.IsBlackListed;
            m_Highlighted.isOn = Object.State.IsHighlighted;
            m_ColorImage.color = Object.State.Color;
            m_UpdateRequired = false;
        }
        #endregion
    }
}