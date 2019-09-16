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

            scene.OnUpdateGeneratorState.AddListener((value) =>
            {
                if (column is Column3DDynamic)
                {
                    IsActive = value;
                }
            });

            if (column is Column3DDynamic dynamicColumn)
            {
                dynamicColumn.OnUpdateCurrentTimelineID.AddListener(() =>
                {
                    m_Text.text = string.Format("{0} ({1}{2})",
                        dynamicColumn.Timeline.CurrentSubtimeline.GetLocalIndex(dynamicColumn.Timeline.CurrentIndex).ToString(),
                        dynamicColumn.Timeline.CurrentSubtimeline.GetLocalTime(dynamicColumn.Timeline.CurrentIndex).ToString("N2"),
                        dynamicColumn.Timeline.Unit);
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