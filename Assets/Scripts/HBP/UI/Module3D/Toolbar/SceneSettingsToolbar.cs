using HBP.Module3D;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;

namespace HBP.UI.Module3D
{
    public class SceneSettingsToolbar : Toolbar
    {
        #region Properties
        /// <summary>
        /// Hide / show the left / right parts of the brain mesh
        /// </summary>
        [SerializeField]
        private Tools.BrainMeshes m_BrainMeshes;
        /// <summary>
        /// Change brain type (grey, white, inflated)
        /// </summary>
        [SerializeField]
        private Tools.BrainTypes m_BrainTypes;
        /// <summary>
        /// Show / hide Mars Atlas
        /// </summary>
        [SerializeField]
        private Tools.MarsAtlas m_MarsAtlas;
        /// <summary>
        /// Add / remove views from the selected scene
        /// </summary>
        [SerializeField]
        private Tools.Views m_Views;
        /// <summary>
        /// Add / remove FMRI columns from the selected scene
        /// </summary>
        [SerializeField]
        private Tools.FMRI m_FMRI;
        #endregion

        #region Private Methods
        /// <summary>
        /// Link elements to the toolbar
        /// </summary>
        /// <param name="parent">Transform of the toolbar</param>
        protected override void AddTools()
        {
            m_Tools.Add(m_BrainMeshes);
            m_Tools.Add(m_BrainTypes);
            m_Tools.Add(m_MarsAtlas);
            m_Tools.Add(m_Views);
            m_Tools.Add(m_FMRI);
        }
        /// <summary>
        /// Add the listeners to the elements of the toolbar
        /// </summary>
        protected override void AddListeners()
        {
            base.AddListeners();

            m_BrainTypes.OnChangeValue.AddListener((type) =>
            {
                m_MarsAtlas.ChangeBrainTypeCallback(type);
            });

            m_MarsAtlas.OnChangeValue.AddListener((isOn) =>
            {
                m_BrainTypes.ChangeMarsAtlasCallback(isOn);
                UpdateInteractableButtons();
            });

            m_Views.OnClick.AddListener(() =>
            {
                UpdateInteractableButtons();
            });

            m_FMRI.OnClick.AddListener(() =>
            {
                UpdateInteractableButtons();
            });
        }
        #endregion
    }
}