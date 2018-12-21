﻿using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Anatomy;
using Tools.Unity;

namespace HBP.UI.Anatomy
{
    public class ImplantationModifier : ItemModifier<Implantation>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] FileSelector m_FileSelector;
        [SerializeField] FileSelector m_MarsAtlasSelector;

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
                m_FileSelector.interactable = value;
                m_MarsAtlasSelector.interactable = value;
            }
        }
        #endregion

        #region Private Methods
        protected override void SetFields(Implantation objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_FileSelector.File = objectToDisplay.SavedFile;
            m_MarsAtlasSelector.File = objectToDisplay.SavedMarsAtlas;
        }
        protected override void Initialize()
        {
            base.Initialize();
            m_NameInputField.onValueChanged.RemoveAllListeners();
            m_NameInputField.onValueChanged.AddListener((name) => ItemTemp.Name = name);

            m_FileSelector.onValueChanged.RemoveAllListeners();
            m_FileSelector.onValueChanged.AddListener((file) => ItemTemp.File = file);

            m_MarsAtlasSelector.onValueChanged.RemoveAllListeners();
            m_MarsAtlasSelector.onValueChanged.AddListener((file) => ItemTemp.MarsAtlas = file);
        }
        #endregion
    }
}