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
        /// Rect Transform of the custom vector editor
        /// </summary>
        [SerializeField]
        private RectTransform m_CustomValues;
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
                    m_CustomValues.gameObject.SetActive(true);
                    int x = 1, y = 0, z = 0;
                    if (!int.TryParse(m_CustomX.text, out x))
                    {
                        x = 1;
                        m_CustomX.text = "0";
                    }
                    if (!int.TryParse(m_CustomY.text, out y))
                    {
                        y = 0;
                        m_CustomY.text = "0";
                    }
                    if (!int.TryParse(m_CustomZ.text, out z))
                    {
                        z = 0;
                        m_CustomZ.text = "0";
                    }
                    if (x == 0 && y == 0 && z == 0)
                    {
                        x = 1;
                        m_CustomX.text = "1";
                    }
                    cut.Normal = new Vector3(x, y, z);
                }
                else
                {
                    m_CustomValues.gameObject.SetActive(false);
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
            m_CustomX.onEndEdit.AddListener((value) =>
            {
                if (cut.Orientation == CutOrientation.Custom)
                {
                    int x = 0, y = 0, z = 0;
                    if (!int.TryParse(m_CustomX.text, out x))
                    {
                        x = 0;
                        m_CustomX.text = "0";
                    }
                    if (!int.TryParse(m_CustomY.text, out y))
                    {
                        y = 0;
                        m_CustomY.text = "0";
                    }
                    if (!int.TryParse(m_CustomZ.text, out z))
                    {
                        z = 0;
                        m_CustomZ.text = "0";
                    }
                    if (x == 0 && y == 0 && z == 0)
                    {
                        x = 1;
                        m_CustomX.text = "1";
                    }
                    cut.Normal = new Vector3(x, y, z);
                    scene.UpdateCutPlane(cut);
                }
            });
            m_CustomY.onEndEdit.AddListener((value) =>
            {
                if (cut.Orientation == CutOrientation.Custom)
                {
                    int x = 0, y = 0, z = 0;
                    if (!int.TryParse(m_CustomX.text, out x))
                    {
                        x = 0;
                        m_CustomX.text = "0";
                    }
                    if (!int.TryParse(m_CustomY.text, out y))
                    {
                        y = 0;
                        m_CustomY.text = "0";
                    }
                    if (!int.TryParse(m_CustomZ.text, out z))
                    {
                        z = 0;
                        m_CustomZ.text = "0";
                    }
                    if (x == 0 && y == 0 && z == 0)
                    {
                        x = 1;
                        m_CustomX.text = "1";
                    }
                    cut.Normal = new Vector3(x, y, z);
                    scene.UpdateCutPlane(cut);
                }
            });
            m_CustomZ.onEndEdit.AddListener((value) =>
            {
                if (cut.Orientation == CutOrientation.Custom)
                {
                    int x = 0, y = 0, z = 0;
                    if (!int.TryParse(m_CustomX.text, out x))
                    {
                        x = 0;
                        m_CustomX.text = "0";
                    }
                    if (!int.TryParse(m_CustomY.text, out y))
                    {
                        y = 0;
                        m_CustomY.text = "0";
                    }
                    if (!int.TryParse(m_CustomZ.text, out z))
                    {
                        z = 0;
                        m_CustomZ.text = "0";
                    }
                    if (x == 0 && y == 0 && z == 0)
                    {
                        x = 1;
                        m_CustomX.text = "1";
                    }
                    cut.Normal = new Vector3(x, y, z);
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