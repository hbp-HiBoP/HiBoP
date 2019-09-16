using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class Icon : ColumnOverlayElement
    {
        #region Properties
        [SerializeField]
        private Image m_Image;
        [SerializeField]
        private Text m_Text;

        private Sprite m_DefaultSprite;
        private Data.Visualization.Icon m_CurrentIcon;
        private List<Data.Visualization.Icon> m_Icons;
        #endregion

        #region Public Methods
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

                    Data.Visualization.Icon icon = m_Icons.FirstOrDefault((i) => i.StartPosition <= dynamicColumn.Timeline.CurrentIndex && i.EndPosition >= dynamicColumn.Timeline.CurrentIndex);
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