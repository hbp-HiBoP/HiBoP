using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class Colormap : MonoBehaviour
    {
        #region Properties
        public Sprite Colormap0;
        public Sprite Colormap1;
        public Sprite Colormap2;
        public Sprite Colormap3;
        public Sprite Colormap4;
        public Sprite Colormap5;
        public Sprite Colormap6;
        public Sprite Colormap7;
        public Sprite Colormap8;
        public Sprite Colormap9;
        public Sprite Colormap10;
        public Sprite Colormap11;
        public Sprite Colormap12;
        public Sprite Colormap13;
        public Sprite Colormap14;
        public Sprite Colormap15;
        public Sprite Colormap16;
        public Sprite Colormap17;

        public bool IsActive { get; set; }

        /// <summary>
        /// Icon of the colormap display
        /// </summary>
        private Image m_Icon;

        /// <summary>
        /// Minimum value
        /// </summary>
        private Text m_Min;
        /// <summary>
        /// Middle value
        /// </summary>
        private Text m_Mid;
        /// <summary>
        /// Maximum value
        /// </summary>
        private Text m_Max;

        /// <summary>
        /// Links between the type of a color and its sprite
        /// </summary>
        private Dictionary<ColorType, Sprite> m_SpriteByColorType = new Dictionary<ColorType, Sprite>();
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_SpriteByColorType.Add(ColorType.Grayscale, Colormap0);
            m_SpriteByColorType.Add(ColorType.Hot, Colormap1);
            m_SpriteByColorType.Add(ColorType.Winter, Colormap2);
            m_SpriteByColorType.Add(ColorType.Warm, Colormap3);
            m_SpriteByColorType.Add(ColorType.Surface, Colormap4);
            m_SpriteByColorType.Add(ColorType.Cool, Colormap5);
            m_SpriteByColorType.Add(ColorType.RedYellow, Colormap6);
            m_SpriteByColorType.Add(ColorType.BlueGreen, Colormap7);
            m_SpriteByColorType.Add(ColorType.ACTC, Colormap8);
            m_SpriteByColorType.Add(ColorType.Bone, Colormap9);
            m_SpriteByColorType.Add(ColorType.GEColor, Colormap10);
            m_SpriteByColorType.Add(ColorType.Gold, Colormap11);
            m_SpriteByColorType.Add(ColorType.XRain, Colormap12);
            m_SpriteByColorType.Add(ColorType.MatLab, Colormap13);
            m_SpriteByColorType.Add(ColorType.Default, Colormap14);
            m_SpriteByColorType.Add(ColorType.BrainColor, Colormap15);
            m_SpriteByColorType.Add(ColorType.White, Colormap16);
            m_SpriteByColorType.Add(ColorType.SoftGrayscale, Colormap17);

            m_Icon = transform.Find("Color").GetComponent<Image>();
            Transform textTransform = transform.Find("Values");
            m_Min = textTransform.Find("Min").GetComponent<Text>();
            m_Mid = textTransform.Find("Mid").GetComponent<Text>();
            m_Max = textTransform.Find("Max").GetComponent<Text>();
        }
        #endregion

        #region Public Methods
        public void Initialize(Base3DScene scene, Column3D column)
        {
            scene.SceneInformation.OnUpdateGeneratorState.AddListener((value) =>
            {
                gameObject.SetActive(value);
                IsActive = value;
            });

            scene.Events.OnChangeColormap.AddListener((color) => m_Icon.sprite = m_SpriteByColorType[color]);

            scene.Events.OnSendColorMapValues.AddListener((min, mid, max, col) =>
            {
                if (col != column) return;

                m_Min.text = min.ToString("0.0");
                m_Mid.text = mid.ToString("0.0");
                m_Max.text = max.ToString("0.0");
            });
        }
        #endregion
    }
}