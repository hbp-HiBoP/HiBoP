using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class TimeDisplay : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        private Text m_Text;
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
                    m_Text.text = col.CurrentTimeLineID.ToString() + " (" + col.CurrentTimeLine.ToString("N2") + col.Column.TimeLine.Start.Unite + ")";
                });
            }
        }
        #endregion
    }
}