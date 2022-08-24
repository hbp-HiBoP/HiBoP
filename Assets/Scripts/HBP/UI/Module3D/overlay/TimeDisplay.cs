using HBP.Data.Module3D;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// Overlay element to display the current point on the timeline of the column
    /// </summary>
    public class TimeDisplay : ColumnOverlayElement
    {
        #region Properties
        /// <summary>
        /// Displays the current point on the timeline (index and time)
        /// </summary>
        [SerializeField] private Text m_Text;
        #endregion

        #region Public Methods
        /// <summary>
        /// Setup the overlay element
        /// </summary>
        /// <param name="scene">Associated 3D scene</param>
        /// <param name="column">Associated 3D column</param>
        /// <param name="columnUI">Parent UI column</param>
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