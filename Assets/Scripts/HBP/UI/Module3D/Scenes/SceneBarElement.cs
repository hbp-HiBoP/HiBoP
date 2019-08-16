﻿using HBP.Module3D;
using NewTheme.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class SceneBarElement : MonoBehaviour
    {
        #region Properties
        private Base3DScene m_Scene;

        [SerializeField]
        private Text m_Text;
        [SerializeField]
        private Toggle m_Toggle;
        [SerializeField]
        private Button m_Button;

        [SerializeField] private State m_SelectedState;
        [SerializeField] private ThemeElement m_ThemeElement;

        private UnityEngine.Events.UnityAction<Base3DScene> OnSelectSceneCallback;
        #endregion

        #region Public Methods
        public void Initialize(Base3DScene scene)
        {
            m_Scene = scene;
            OnSelectSceneCallback = (s) => SetSelectedState(s == m_Scene);
            ApplicationState.Module3D.OnSelectScene.AddListener(OnSelectSceneCallback);
            SetSelectedState(m_Scene.IsSelected);

            m_Text.text = scene.Name;

            m_Button.onClick.AddListener(() =>
            {
                ApplicationState.Module3D.RemoveScene(scene);
            });

            ApplicationState.Module3D.OnRemoveScene.AddListener((s) =>
            {
                if (s == scene)
                {
                    Destroy(gameObject);
                }
            });

            m_Toggle.isOn = true;
            m_Toggle.onValueChanged.AddListener((isOn) =>
            {
                scene.UpdateVisibleState(isOn);
            });
        }
        #endregion

        #region Private Methods
        private void OnDestroy()
        {
            ApplicationState.Module3D.OnSelectScene.RemoveListener(OnSelectSceneCallback);
        }
        private void SetSelectedState(bool selected)
        {
            if (selected)
            {
                m_ThemeElement.Set(m_SelectedState);
            }
            else
            {
                m_ThemeElement.Set();
            }
        }
        #endregion
    }
}