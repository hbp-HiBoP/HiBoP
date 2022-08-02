using HBP.Module3D;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Data.Enums;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// Overlay element to display the used colormap and the corresponding minimum, middle and maximum values
    /// </summary>
    public class Colormap : ColumnOverlayElement
    {
        #region Properties
        /// <summary>
        /// Image containing the colormap sprite
        /// </summary>
        [SerializeField] private Image m_ColormapImage;

        /// <summary>
        /// Displays the minimum value for the colormap
        /// </summary>
        [SerializeField] private Text m_Min;
        /// <summary>
        /// Displays the middle value for the colormap
        /// </summary>
        [SerializeField] private Text m_Mid;
        /// <summary>
        /// Displays the maximum value for the colormap
        /// </summary>
        [SerializeField] private Text m_Max;

        /// <summary>
        /// Links between the type of a color and its sprite
        /// </summary>
        private Dictionary<ColorType, Sprite> m_SpriteByColorType = new Dictionary<ColorType, Sprite>();
        #endregion

        #region Private Methods
        private void Awake()
        {
            foreach (var colormap in System.Enum.GetValues(typeof(ColorType)).Cast<ColorType>())
            {
                m_SpriteByColorType.Add(colormap, Resources.Load<Sprite>(System.IO.Path.Combine("Colormaps", string.Format("colormap_{0}", ((int)colormap).ToString()))) as Sprite);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Setup the overlay element
        /// </summary>
        /// <param name="scene">Associated 3D scene</param>
        /// <param name="column">Associated 3D column</param>
        /// <param name="columnUI">Parent UI column</param>
        public override void Setup(Base3DScene scene, Column3D column, Column3DUI columnUI)
        {
            base.Setup(scene, column, columnUI);
            IsActive = false;

            scene.OnUpdateGeneratorState.AddListener((value) =>
            {
                IsActive = value;
                if (IsActive && column is Column3DAnatomy anatomyColumn)
                {
                    int density = Mathf.FloorToInt((anatomyColumn.ActivityGenerator as Core.DLL.DensityGenerator).MaxDensity);
                    m_Min.text = "0";
                    m_Mid.text = (density / 2).ToString();
                    m_Max.text = density.ToString();
                }
            });

            scene.OnChangeColormap.AddListener((color) => m_ColormapImage.sprite = m_SpriteByColorType[color]);

            if (column is Column3DDynamic dynamicColumn)
            {
                dynamicColumn.DynamicParameters.OnUpdateSpanValues.AddListener(() =>
                {
                    m_Min.text = dynamicColumn.DynamicParameters.SpanMin.ToString("0.00");
                    m_Mid.text = dynamicColumn.DynamicParameters.Middle.ToString("0.00");
                    m_Max.text = dynamicColumn.DynamicParameters.SpanMax.ToString("0.00");
                });
            }
            else if (column is Column3DFMRI fmriColumn)
            {
                fmriColumn.FMRIParameters.OnUpdateCalValues.AddListener(() =>
                {
                    UpdateTextFMRI(fmriColumn);
                });
                fmriColumn.OnChangeSelectedFMRI.AddListener(() =>
                {
                    UpdateTextFMRI(fmriColumn);
                });
            }
            else if (column is Column3DMEG megColumn)
            {
                megColumn.MEGParameters.OnUpdateCalValues.AddListener(() =>
                {
                    UpdateTextMEG(megColumn);
                });
                megColumn.OnChangeSelectedMEG.AddListener(() =>
                {
                    UpdateTextMEG(megColumn);
                });
            }
        }
        #endregion

        #region Private Methods
        private void UpdateTextFMRI(Column3DFMRI fmriColumn)
        {
            Core.Tools.MRICalValues values = fmriColumn.SelectedFMRI.NIFTI.ExtremeValues;
            float min = values.Min;
            float max = values.Max;
            float negativeMin = fmriColumn.FMRIParameters.FMRINegativeCalMinFactor * min;
            float negativeMax = fmriColumn.FMRIParameters.FMRINegativeCalMaxFactor * min;
            float positiveMin = fmriColumn.FMRIParameters.FMRIPositiveCalMinFactor * max;
            float positiveMax = fmriColumn.FMRIParameters.FMRIPositiveCalMaxFactor * max;
            if (min > 0)
            {
                m_Min.text = "";
                m_Mid.text = positiveMin.ToString("0.0");
                m_Max.text = positiveMax.ToString("0.0");
            }
            else if (max < 0)
            {
                m_Min.text = negativeMax.ToString("0.0");
                m_Mid.text = negativeMin.ToString("0.0");
                m_Max.text = "";
            }
            else
            {
                m_Min.text = negativeMax.ToString("0.0");
                m_Mid.text = string.Format("{0}|{1}", negativeMin.ToString("0.0"), positiveMin.ToString("0.0"));
                m_Max.text = positiveMax.ToString("0.0");
            }
        }
        private void UpdateTextMEG(Column3DMEG megColumn)
        {
            Core.Tools.MRICalValues values = megColumn.SelectedFMRI.NIFTI.ExtremeValues;
            float min = values.Min;
            float max = values.Max;
            float negativeMin = megColumn.MEGParameters.FMRINegativeCalMinFactor * min;
            float negativeMax = megColumn.MEGParameters.FMRINegativeCalMaxFactor * min;
            float positiveMin = megColumn.MEGParameters.FMRIPositiveCalMinFactor * max;
            float positiveMax = megColumn.MEGParameters.FMRIPositiveCalMaxFactor * max;
            if (min > 0)
            {
                m_Min.text = "";
                m_Mid.text = positiveMin.ToString("0.0");
                m_Max.text = positiveMax.ToString("0.0");
            }
            else if (max < 0)
            {
                m_Min.text = negativeMax.ToString("0.0");
                m_Mid.text = negativeMin.ToString("0.0");
                m_Max.text = "";
            }
            else
            {
                m_Min.text = negativeMax.ToString("0.0");
                m_Mid.text = string.Format("{0}|{1}", negativeMin.ToString("0.0"), positiveMin.ToString("0.0"));
                m_Max.text = positiveMax.ToString("0.0");
            }
        }
        #endregion
    }
}