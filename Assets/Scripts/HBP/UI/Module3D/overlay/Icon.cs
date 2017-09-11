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
        private RawImage m_RawImage;
        [SerializeField]
        private Text m_Text;

        private Data.Visualization.Icon m_CurrentIcon;
        private List<Data.Visualization.Icon> m_Icons;
        #endregion

        #region Public Methods
        public void Initialize(Base3DScene scene, Column3D column, Column3DUI columnUI)
        {
            m_ColumnUI = columnUI;
            IsActive = false;

            scene.SceneInformation.OnUpdateGeneratorState.AddListener((value) =>
            {
                IsActive = value;
            });

            if (column is Column3DFMRI)
            {
                IsActive = false;
            }
            else
            {
                Column3DIEEG col = (Column3DIEEG)column;
                m_Icons = col.ColumnData.IconicScenario.Icons.OrderByDescending((i) => i.StartPosition).ToList();

                col.OnUpdateCurrentTimelineID.AddListener(() =>
                {
                    if (!scene.SceneInformation.IsGeneratorUpToDate) return;

                    Data.Visualization.Icon icon = m_Icons.DefaultIfEmpty(null).FirstOrDefault((i) => i.StartPosition <= col.CurrentTimeLineID && i.EndPosition >= col.CurrentTimeLineID);
                    if (icon != m_CurrentIcon)
                    {
                        m_CurrentIcon = icon;
                        if (icon == null)
                        {
                            IsActive = false;
                        }
                        else
                        {
                            IsActive = true;
                            m_RawImage.texture = icon.Texture;
                            m_Text.text = icon.Label;
                        }
                    }
                });
            }
        }
        #endregion
    }
}