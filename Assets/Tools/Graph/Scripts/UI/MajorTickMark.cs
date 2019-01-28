using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace Tools.Unity.Graph
{
    public class MajorTickMark : TickMark
    {
        #region Properties
        [SerializeField] protected RectTransform m_LabelRectTransform;

        [SerializeField] protected Text m_Text;
        [SerializeField] protected string m_Label;
        public string Label
        {
            get
            {
                return m_Label;
            }
            set
            {
                if(SetPropertyUtility.SetClass(ref m_Label, value))
                {
                    SetLabel();
                }
            }
        }
        #endregion

        #region Protected Setters
        protected override void OnValidate()
        {
            base.OnValidate();
            SetLabel();
        }
        protected override void SetLenghtThicknessDirection()
        {
            base.SetLenghtThicknessDirection();
            switch (m_Direction)
            {
                case Axe.DirectionEnum.LeftToRight:
                case Axe.DirectionEnum.RightToLeft:
                    m_Text.rectTransform.offsetMax = new Vector2(0, -m_Lenght / 2);
                    break;
                case Axe.DirectionEnum.BottomToTop:
                case Axe.DirectionEnum.TopToBottom:
                    m_Text.rectTransform.offsetMax = new Vector2(-m_Lenght / 2, 0);
                    break;
            }
        }
        protected void SetLabel()
        {
            m_Text.text = m_Label;
        }
        protected override void SetColor()
        {
            base.SetColor();
            m_Text.color = m_Color;
        }
        #endregion
    }
}