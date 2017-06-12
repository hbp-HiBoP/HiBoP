﻿using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class CutParametersController : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Image of the cut
        /// </summary>
        [SerializeField]
        private Image m_Image;
        /// <summary>
        /// Dropdown to change the axis of the cut
        /// </summary>
        [SerializeField]
        private Dropdown m_Orientation;
        /// <summary>
        /// Slider to change the position of the cut
        /// </summary>
        [SerializeField]
        private Slider m_Position;
        /// <summary>
        /// Toggle to change the flip of the cut
        /// </summary>
        [SerializeField]
        private Toggle m_Flip;
        /// <summary>
        /// Button to remove the cut
        /// </summary>
        [SerializeField]
        private Button m_Remove;
        /// <summary>
        /// X value for the custom normal
        /// </summary>
        [SerializeField]
        private InputField m_CustomX;
        /// <summary>
        /// Y value for the custom normal
        /// </summary>
        [SerializeField]
        private InputField m_CustomY;
        /// <summary>
        /// Z value for the custom normal
        /// </summary>
        [SerializeField]
        private InputField m_CustomZ;
        #endregion

        #region Private Methods
        private void AddListeners(Base3DScene scene, Cut cut)
        {
            cut.OnUpdateGUITextures.AddListener((texture) =>
            {
                Destroy(m_Image.sprite);
                m_Image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
            });
            m_Position.onValueChanged.AddListener((value) =>
            {
                cut.Position = value;
                scene.UpdateCutPlane(cut);
            });
            m_Orientation.onValueChanged.AddListener((value) =>
            {
                cut.Orientation = (CutOrientation)value;
                if (cut.Orientation == CutOrientation.Custom)
                {
                    cut.Normal = new Vector3(int.Parse(m_CustomX.text), int.Parse(m_CustomY.text), int.Parse(m_CustomZ.text));
                }
                scene.UpdateCutPlane(cut);
            });
            m_Flip.onValueChanged.AddListener((isOn) =>
            {
                cut.Flip = isOn;
                scene.UpdateCutPlane(cut);
            });
            m_Remove.onClick.AddListener(() =>
            {
                scene.RemoveCutPlane(cut);
                Destroy(gameObject);
            });
            m_CustomX.onValueChanged.AddListener((value) =>
            {
                if (cut.Orientation == CutOrientation.Custom)
                {
                    cut.Normal = new Vector3(int.Parse(m_CustomX.text), int.Parse(m_CustomY.text), int.Parse(m_CustomZ.text));
                    scene.UpdateCutPlane(cut);
                }
            });
            m_CustomY.onValueChanged.AddListener((value) =>
            {
                if (cut.Orientation == CutOrientation.Custom)
                {
                    cut.Normal = new Vector3(int.Parse(m_CustomX.text), int.Parse(m_CustomY.text), int.Parse(m_CustomZ.text));
                    scene.UpdateCutPlane(cut);
                }
            });
            m_CustomZ.onValueChanged.AddListener((value) =>
            {
                if (cut.Orientation == CutOrientation.Custom)
                {
                    cut.Normal = new Vector3(int.Parse(m_CustomX.text), int.Parse(m_CustomY.text), int.Parse(m_CustomZ.text));
                    scene.UpdateCutPlane(cut);
                }
            });
        }
        #endregion

        #region Public Methods
        public void Initialize(Base3DScene scene, Cut cut)
        {
            m_Position.value = cut.Position;
            m_Orientation.value = (int)cut.Orientation;
            m_Flip.isOn = cut.Flip;
            AddListeners(scene, cut);
        }
        #endregion
    }
}