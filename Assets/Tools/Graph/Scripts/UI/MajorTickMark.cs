using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Graph
{
    public class MajorTickMark : TickMark
    {
        #region Parameters
        [SerializeField]
        protected RectTransform labelRectTransform;
        #endregion

        #region Public Methods
        public virtual void Set(string label, float position, Axe.SideEnum side, Color color)
        {
            base.Set(position, side, color);
            SetLabel(label,side);
        }
        public override void SetColor(Color color)
        {
            labelRectTransform.GetComponent<Text>().color = color;
        }
        #endregion

        #region Private Methods
        protected virtual void SetLabel(string label, Axe.SideEnum side)
        {
            labelRectTransform.GetComponent<Text>().text = label;
            switch (side)
            {
                case Axe.SideEnum.absciss: labelRectTransform.offsetMax = new Vector2(0, - TICK_MARK_LENGHT / 2); break;
                case Axe.SideEnum.ordinate: labelRectTransform.offsetMax = new Vector2(- TICK_MARK_LENGHT / 2, 0); break;
            }
        }
        #endregion
    }
}