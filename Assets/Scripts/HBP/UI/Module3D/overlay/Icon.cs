using HBP.Module3D;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// Overlay element to display the current icon of the iconic scenario of the bloc used for this column
    /// </summary>
    public class Icon : ColumnOverlayElement
    {
        #region Properties
        /// <summary>
        /// Displays the image of the current icon
        /// </summary>
        [SerializeField] private Image m_Image;
        /// <summary>
        /// Displays the title of the current icon
        /// </summary>
        [SerializeField] private Text m_Text;

        /// <summary>
        /// Default sprite for the icon (used when no icon matches the current point in the timeline)
        /// </summary>
        private Sprite m_DefaultSprite;
        /// <summary>
        /// Currently displayed icon
        /// </summary>
        private Core.Data.Processed.Icon m_CurrentIcon;
        /// <summary>
        /// Sprite of the current icon
        /// </summary>
        public Sprite Sprite { get { return m_CurrentIcon != null ? m_CurrentIcon.Illustration : null; } }
        /// <summary>
        /// List of all icons used for this column
        /// </summary>
        private List<Core.Data.Processed.Icon> m_Icons;
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
            m_DefaultSprite = m_Image.sprite;

            scene.OnUpdateGeneratorState.AddListener((value) =>
            {
                if (column is Column3DDynamic)
                {
                    IsActive = value;
                }
                m_CurrentIcon = null;
            });

            if (column is Column3DDynamic dynamicColumn)
            {
                if (dynamicColumn is Column3DIEEG iEEGColumn)
                {
                    m_Icons = iEEGColumn.ColumnIEEGData.Data.IconicScenario.Icons.OrderByDescending((i) => i.StartPosition).ToList();
                }
                else if (dynamicColumn is Column3DCCEP ccepColumn)
                {
                    m_Icons = ccepColumn.ColumnCCEPData.Data.IconicScenario.Icons.OrderByDescending((i) => i.StartPosition).ToList();
                }
                dynamicColumn.OnUpdateCurrentTimelineID.AddListener(() =>
                {
                    if (!scene.IsGeneratorUpToDate) return;

                    Core.Data.Processed.Icon icon = m_Icons.FirstOrDefault((i) => i.StartPosition <= dynamicColumn.Timeline.CurrentIndex && i.EndPosition >= dynamicColumn.Timeline.CurrentIndex);
                    if (icon == null)
                    {
                        IsActive = false;
                        m_CurrentIcon = null;
                    }
                    if (icon != m_CurrentIcon)
                    {
                        if (!icon.Usable)
                        {
                            IsActive = false;
                            m_Image.sprite = m_DefaultSprite;
                        }
                        else
                        {
                            IsActive = true;
                            m_Image.sprite = icon.Illustration;
                            m_Text.text = icon.Label;
                        }
                        m_CurrentIcon = icon;
                    }
                });
            }
            else
            {
                IsActive = false;
            }
        }
        #endregion
    }
}