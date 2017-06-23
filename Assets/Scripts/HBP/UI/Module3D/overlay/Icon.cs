using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class Icon : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        private Image m_Image;
        [SerializeField]
        private Text m_Text;

        private Data.Visualization.Icon m_CurrentIcon;
        #endregion

        #region Public Methods
        public void Initialize(Base3DScene scene, Column3D column)
        {
            scene.SceneInformation.OnUpdateGeneratorState.AddListener((value) =>
            {
                gameObject.SetActive(value);
            });
            if (column is Column3DFMRI)
            {
                gameObject.SetActive(false);
            }
            else
            {
                Column3DIEEG col = (Column3DIEEG)column;
                col.OnUpdateCurrentTimelineID.AddListener(() =>
                {
                    Data.Visualization.Icon icon = col.Column.IconicScenario.Icons.DefaultIfEmpty(null).FirstOrDefault((i) => i.StartPosition <= col.CurrentTimeLineID && i.EndPosition >= col.CurrentTimeLineID);
                    if (icon != m_CurrentIcon)
                    {
                        if (icon == null)
                        {
                            gameObject.SetActive(false);
                        }
                        else
                        {
                            gameObject.SetActive(true);
                            Texture2D iconTexture = Texture2Dutility.GenerateIcon();
                            HBP.Module3D.DLL.Texture.Load(icon.IllustrationPath).UpdateTexture2D(iconTexture);
                            m_Image.sprite = Sprite.Create(iconTexture, new Rect(0, 0, iconTexture.width, iconTexture.height), new Vector2(0.5f, 0.5f));
                            m_Text.text = icon.Label;
                        }
                    }
                });
            }
        }
        #endregion
    }
}