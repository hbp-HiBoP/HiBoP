using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI.Extensions;
using HBP.Core.Tools;

namespace HBP.UI.Informations.Graphs
{
    public class AxisLines : MonoBehaviour
    {
        #region Properties

        [SerializeField] protected Vector2 m_Range;

        public Vector2 Range
        {
            get { return m_Range; }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_Range, value))
                {
                    SetRange();
                }
            }
        }

        [SerializeField] Vector2Event m_OnChangeRange;

        public Vector2Event OnChangeRange
        {
            get { return m_OnChangeRange; }
        }

        #endregion

        #region Protected Setters

        protected virtual void OnValidate()
        {
            SetRange();
        }

        protected void SetRange()
        {
            m_OnChangeRange.Invoke(m_Range);
        }

        #endregion
    }
}
