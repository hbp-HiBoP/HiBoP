using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using UnityEngine.Events;

namespace HBP.UI.Module3D
{
    public class CutParametersController : MonoBehaviour
    {
        #region Properties
        private Cut m_Cut;
        private bool m_IsUIUpdating = false;
        public bool AreControlsOpen { get; set; }
        public Texture2D Texture { get { return m_Image.sprite.texture; } }

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
        /// Button to slightly change the position of the cut
        /// </summary>
        [SerializeField]
        private Button m_PlusPosition;
        /// <summary>
        /// Button to slightly change the position of the cut
        /// </summary>
        [SerializeField]
        private Button m_MinusPosition;
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

        #region Events
        public UnityEvent OnOpenControls = new UnityEvent();
        public UnityEvent OnCloseControls = new UnityEvent();
        #endregion

        #region Private Methods
        private void AddListeners(Base3DScene scene, Cut cut)
        {
            cut.OnUpdateGUITextures.AddListener((texture) =>
            {
                // FIXME : maybe memory leak
                Destroy(m_Image.sprite);
                m_Image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
                m_Image.sprite.texture.filterMode = FilterMode.Trilinear;
                m_Image.sprite.texture.anisoLevel = 9;
            });
            cut.OnUpdateCut.AddListener(() =>
            {
                UpdateUI();
            });
            cut.OnRemoveCut.AddListener(() =>
            {
                Destroy(gameObject);
            });

            m_Position.onValueChanged.AddListener((value) =>
            {
                if (m_IsUIUpdating) return;

                cut.Position = value;
                scene.UpdateCutPlane(cut);
            });
            m_MinusPosition.onClick.AddListener(() =>
            {
                if (m_IsUIUpdating) return;

                m_Position.value -= 1.0f / cut.NumberOfCuts;
            });
            m_PlusPosition.onClick.AddListener(() =>
            {
                if (m_IsUIUpdating) return;

                m_Position.value += 1.0f / cut.NumberOfCuts;
            });
            m_Orientation.onValueChanged.AddListener((value) =>
            {
                if (m_IsUIUpdating) return;

                cut.Orientation = (Data.Enums.CutOrientation)value;
                if (cut.Orientation == Data.Enums.CutOrientation.Custom)
                {
                    int x = 1, y = 0, z = 0;
                    int.TryParse(m_CustomX.text, out x);
                    int.TryParse(m_CustomY.text, out y);
                    int.TryParse(m_CustomZ.text, out z);
                    cut.Normal = new Vector3(x, y, z);
                }
                scene.UpdateCutPlane(cut);
            });
            m_Flip.onValueChanged.AddListener((isOn) =>
            {
                if (m_IsUIUpdating) return;

                cut.Flip = isOn;
                cut.Position = 1.0f - cut.Position;
                scene.UpdateCutPlane(cut);
            });
            m_Remove.onClick.AddListener(() =>
            {
                if (m_IsUIUpdating) return;

                scene.RemoveCutPlane(cut);
            });
            m_CustomX.onEndEdit.AddListener((value) =>
            {
                if (m_IsUIUpdating) return;

                if (cut.Orientation == Data.Enums.CutOrientation.Custom)
                {
                    int x = 1, y = 0, z = 0;
                    int.TryParse(m_CustomX.text, out x);
                    int.TryParse(m_CustomY.text, out y);
                    int.TryParse(m_CustomZ.text, out z);
                    cut.Normal = new Vector3(x, y, z);
                }
                scene.UpdateCutPlane(cut);
            });
            m_CustomY.onEndEdit.AddListener((value) =>
            {
                if (m_IsUIUpdating) return;

                if (cut.Orientation == Data.Enums.CutOrientation.Custom)
                {
                    int x = 1, y = 0, z = 0;
                    int.TryParse(m_CustomX.text, out x);
                    int.TryParse(m_CustomY.text, out y);
                    int.TryParse(m_CustomZ.text, out z);
                    cut.Normal = new Vector3(x, y, z);
                }
                scene.UpdateCutPlane(cut);
            });
            m_CustomZ.onEndEdit.AddListener((value) =>
            {
                if (m_IsUIUpdating) return;

                if (cut.Orientation == Data.Enums.CutOrientation.Custom)
                {
                    int x = 1, y = 0, z = 0;
                    int.TryParse(m_CustomX.text, out x);
                    int.TryParse(m_CustomY.text, out y);
                    int.TryParse(m_CustomZ.text, out z);
                    cut.Normal = new Vector3(x, y, z);
                }
                scene.UpdateCutPlane(cut);
            });
            m_Image.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (AreControlsOpen)
                {
                    CloseControls();
                }
                else
                {
                    OpenControls();
                }
            });
        }
        private void UpdateUI()
        {
            m_IsUIUpdating = true;
            if (AreControlsOpen)
            {
                m_Position.value = m_Cut.Position;
                m_Orientation.value = (int)m_Cut.Orientation;
                m_Orientation.RefreshShownValue();
                m_Flip.isOn = m_Cut.Flip;
                m_CustomX.text = m_Cut.Normal.x.ToString();
                m_CustomY.text = m_Cut.Normal.y.ToString();
                m_CustomZ.text = m_Cut.Normal.z.ToString();
                m_Remove.gameObject.SetActive(true);
                m_Orientation.gameObject.SetActive(true);
                m_Position.transform.parent.gameObject.SetActive(true);
                m_Flip.gameObject.SetActive(m_Cut.Orientation != Data.Enums.CutOrientation.Custom);
                m_CustomValues.gameObject.SetActive(m_Cut.Orientation == Data.Enums.CutOrientation.Custom);
            }
            else
            {
                m_Remove.gameObject.SetActive(false);
                m_Orientation.gameObject.SetActive(false);
                m_Position.transform.parent.gameObject.SetActive(false);
                m_Flip.gameObject.SetActive(false);
                m_CustomValues.gameObject.SetActive(false);
            }
            m_IsUIUpdating = false;
        }
        #endregion

        #region Public Methods
        public void Initialize(Base3DScene scene, Cut cut)
        {
            m_Cut = cut;
            m_Image.GetComponent<ImageRatio>().Type = ImageRatio.RatioType.FixedWidth;
            m_Orientation.options = new List<Dropdown.OptionData>();
            foreach (var orientation in Enum.GetNames(typeof(Data.Enums.CutOrientation)))
            {
                m_Orientation.options.Add(new Dropdown.OptionData(orientation));
            }
            UpdateUI();
            AddListeners(scene, cut);
        }
        public void OpenControls()
        {
            AreControlsOpen = true;
            UpdateUI();
            OnOpenControls.Invoke();
        }
        public void CloseControls()
        {
            AreControlsOpen = false;
            UpdateUI();
            OnCloseControls.Invoke();
        }
        #endregion
    }
}