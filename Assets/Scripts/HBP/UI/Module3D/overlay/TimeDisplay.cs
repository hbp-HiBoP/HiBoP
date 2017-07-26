using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class TimeDisplay : OverlayElement
    {
        #region Properties
        [SerializeField]
        private Text m_Text;
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
                col.OnUpdateCurrentTimelineID.AddListener(() =>
                {
                    m_Text.text = col.CurrentTimeLineID.ToString() + " (" + col.CurrentTimeLine.ToString("N2") + col.TimeLineUnite + ")";
                });
            }
        }
        #endregion
    }
}