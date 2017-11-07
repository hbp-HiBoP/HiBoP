using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class Icon : OverlayElement
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
        public override void Initialize(Base3DScene scene, Column3D column, Column3DUI columnUI)
        {
            base.Initialize(scene, column, columnUI);
            IsActive = false;
            m_DefaultSprite = m_Image.sprite;

            scene.SceneInformation.OnUpdateGeneratorState.AddListener((value) =>
            {
                IsActive = value;
            });

            switch (column.Type)
            {
                case Column3D.ColumnType.Base:
                    IsActive = false;
                    break;
                case Column3D.ColumnType.FMRI:
                    IsActive = false;
                    break;
                case Column3D.ColumnType.IEEG:
                    Column3DIEEG col = (Column3DIEEG)column;
                    m_Icons = col.ColumnData.IconicScenario.Icons.OrderByDescending((i) => i.StartPosition).ToList();

                    col.OnUpdateCurrentTimelineID.AddListener(() =>
                    {
                        if (!scene.SceneInformation.IsGeneratorUpToDate) return;

                        Data.Visualization.Icon icon = m_Icons.DefaultIfEmpty(null).FirstOrDefault((i) => i.StartPosition <= col.CurrentTimeLineID && i.EndPosition >= col.CurrentTimeLineID);
                        if (icon != m_CurrentIcon)
                        {
                            m_CurrentIcon = icon;
                            if (icon == null || icon.Illustration == null)
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