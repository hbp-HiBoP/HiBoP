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
        public override void Initialize(Base3DScene scene, Column3D column, Column3DUI columnUI)
        {
            base.Initialize(scene, column, columnUI);
            IsActive = false;

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