using HBP.Module3D;
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
        /// Is the controller initialized ? (prevents the call to listeners)
        /// </summary>
        private bool m_IsInitialized;
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

        #region Public Methods
        public void Initialize(Base3DScene scene, Cut cut)
        {
            switch (scene.Cuts.Count)
            {
                case 1:
                    scene.UpdateCutPlane(cut, CutOrientation.Axial, false, false, Vector3.zero, 0.5f);
                    m_Position.value = 0.5f;
                    m_Orientation.value = 0;
                    m_Flip.isOn = false;
                    break;
                case 2:
                    scene.UpdateCutPlane(cut, CutOrientation.Coronal, false, false, Vector3.zero, 0.5f);
                    m_Position.value = 0.5f;
                    m_Orientation.value = 1;
                    m_Flip.isOn = false;
                    break;
                case 3:
                    scene.UpdateCutPlane(cut, CutOrientation.Sagital, false, false, Vector3.zero, 0.5f);
                    m_Position.value = 0.5f;
                    m_Orientation.value = 2;
                    m_Flip.isOn = false;
                    break;
                default:
                    scene.UpdateCutPlane(cut, CutOrientation.Axial, false, false, Vector3.zero, 0.5f);
                    m_Position.value = 0.5f;
                    m_Orientation.value = 0;
                    m_Flip.isOn = false;
                    break;
            }
            cut.OnUpdateGUITextures.AddListener((texture) =>
            {
                Destroy(m_Image.sprite);
                m_Image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
            });
            m_Position.onValueChanged.AddListener((value) =>
            {
                if (!m_IsInitialized) return;

                scene.UpdateCutPlane(cut, cut.Orientation, cut.Flip, cut.RemoveFrontPlane == 1, cut.Normal, value);
            });
            m_Orientation.onValueChanged.AddListener((value) =>
            {
                if (!m_IsInitialized) return;

                if (value == 4) // Custom orientation
                {
                    Vector3 normal = new Vector3(int.Parse(m_CustomX.text), int.Parse(m_CustomY.text), int.Parse(m_CustomZ.text));
                    scene.UpdateCutPlane(cut, (CutOrientation)value, cut.Flip, cut.RemoveFrontPlane == 1, normal, cut.Position);
                }
                else
                {
                    scene.UpdateCutPlane(cut, (CutOrientation)value, cut.Flip, cut.RemoveFrontPlane == 1, cut.Normal, cut.Position);
                }
            });
            m_Flip.onValueChanged.AddListener((isOn) =>
            {
                if (!m_IsInitialized) return;

                scene.UpdateCutPlane(cut, cut.Orientation, isOn, cut.RemoveFrontPlane == 1, cut.Normal, cut.Position);
            });
            m_Remove.onClick.AddListener(() =>
            {
                if (!m_IsInitialized) return;

                scene.RemoveCutPlane(cut);
                Destroy(gameObject);
            });
            m_CustomX.onValueChanged.AddListener((value) =>
            {
                if (!m_IsInitialized) return;

                Vector3 normal = new Vector3(int.Parse(m_CustomX.text), int.Parse(m_CustomY.text), int.Parse(m_CustomZ.text));
                scene.UpdateCutPlane(cut, cut.Orientation, cut.Flip, cut.RemoveFrontPlane == 1, normal, cut.Position);
            });
            m_CustomY.onValueChanged.AddListener((value) =>
            {
                if (!m_IsInitialized) return;

                Vector3 normal = new Vector3(int.Parse(m_CustomX.text), int.Parse(m_CustomY.text), int.Parse(m_CustomZ.text));
                scene.UpdateCutPlane(cut, cut.Orientation, cut.Flip, cut.RemoveFrontPlane == 1, normal, cut.Position);
            });
            m_CustomZ.onValueChanged.AddListener((value) =>
            {
                if (!m_IsInitialized) return;

                Vector3 normal = new Vector3(int.Parse(m_CustomX.text), int.Parse(m_CustomY.text), int.Parse(m_CustomZ.text));
                scene.UpdateCutPlane(cut, cut.Orientation, cut.Flip, cut.RemoveFrontPlane == 1, normal, cut.Position);
            });
            m_IsInitialized = true;
        }
        #endregion
    }
}