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
                case Data.Enums.ColumnType.Anatomic:
                    IsActive = false;
                    break;
                case Data.Enums.ColumnType.iEEG:
                    Column3DDynamic col = (Column3DDynamic)column;
                    col.OnUpdateCurrentTimelineID.AddListener(() =>
                    {
                        m_Text.text = col.Timeline.CurrentSubtimeline.GetLocalIndex(col.Timeline.CurrentIndex).ToString() + " (" + col.Timeline.CurrentSubtimeline.GetLocalTime(col.Timeline.CurrentIndex).ToString("N2") + col.Timeline.Unit + ")";
                    });
                    break;
                default:
                    break;
            }
        }
        #endregion
    }
}