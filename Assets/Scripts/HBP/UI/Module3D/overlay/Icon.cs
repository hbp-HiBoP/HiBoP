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

            scene.SceneInformation.OnUpdateGeneratorState.AddListener((value) =>
            {
                if (column.Type == Data.Enums.ColumnType.iEEG)
                {
                    IsActive = value;
                }
                m_CurrentIcon = null;
            });

            switch (column.Type)
            {
                case Data.Enums.ColumnType.Anatomy:
                    IsActive = false;
                    break;
                case Data.Enums.ColumnType.iEEG:
                    Column3DIEEG col = (Column3DIEEG)column;
                    m_Icons = col.ColumnData.IconicScenario.Icons.OrderByDescending((i) => i.StartPosition).ToList();

                    col.OnUpdateCurrentTimelineID.AddListener(() =>
                    {
                        if (!scene.SceneInformation.IsGeneratorUpToDate) return;

                        Data.Visualization.Icon icon = m_Icons.FirstOrDefault((i) => i.StartPosition <= col.Timeline.CurrentIndex && i.EndPosition >= col.Timeline.CurrentIndex);
                        if (icon == null)
                        {
                            IsActive = false;
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
                    break;
                default:
                    break;
            }
        }
        #endregion
    }
}