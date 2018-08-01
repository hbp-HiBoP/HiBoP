using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class TimeDisplay : ColumnOverlayElement
    {
        #region Properties
        [SerializeField]
        private Text m_Text;
        #endregion

        #region Public Methods
        public override void Setup(Base3DScene scene, Column3D column, Column3DUI columnUI)
        {
            base.Setup(scene, column, columnUI);
            IsActive = false;

            scene.SceneInformation.OnUpdateGeneratorState.AddListener((value) =>
            {
                if (column.Type == Data.Enums.ColumnType.iEEG)
                {
                    IsActive = value;
                }
            });

            switch (column.Type)
            {
                case Data.Enums.ColumnType.Anatomy:
                    IsActive = false;
                    break;
                case Data.Enums.ColumnType.iEEG:
                    Column3DIEEG col = (Column3DIEEG)column;
                    col.OnUpdateCurrentTimelineID.AddListener(() =>
                    {
                        m_Text.text = col.CurrentTimeLineID.ToString() + " (" + col.CurrentTimeLine.ToString("N2") + col.TimeLineUnite + ")";
                    });
                    break;
                default:
                    break;
            }
        }
        #endregion
    }
}