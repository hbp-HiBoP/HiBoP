using UnityEngine;
using UnityEngine.Events;

namespace HBP.UI.Components
{
    public class FloatComparator : MonoBehaviour
    {
        #region Properties
        public enum OperatorType { Equal, NotEqual, Less, LessOrEqual, Greater, GreaterOrEqual }
        OperatorType m_Operator;
        public OperatorType Operator
        {
            get
            {
                return m_Operator;
            }
            set
            {
                if(m_Operator != value)
                {
                    m_Operator = value;
                    Compare();
                }
            }
        }

        float m_Treshhold;
        public float Treshhold
        {
            get
            {
                return m_Treshhold;
            }
            set
            {
                if(m_Treshhold != value)
                {
                    m_Treshhold = value;
                    Compare();
                }
            }
        }

        float m_Value;
        public float Value
        {
            get
            {
                return m_Value;
            }
            set
            {
                if(m_Value != value)
                {
                    m_Value = value;
                    Compare();
                }
            }
        }

        bool m_Result;
        public bool Result
        {
            get
            {
                return m_Result;
            }
            private set
            {
                if(m_Result != value)
                {
                    m_Result = value;
                    OnChangeResult.Invoke(value);
                }

            }
        }
        public BoolEvent OnChangeResult = new BoolEvent();
        #endregion

        #region Public Methods
        public void Compare()
        {
            switch (Operator)
            {
                case OperatorType.Equal:
                    Result = Value == Treshhold;
                    break;
                case OperatorType.NotEqual:
                    Result = Value != Treshhold;
                    break;
                case OperatorType.Less:
                    Result = Value < Treshhold;
                    break;
                case OperatorType.LessOrEqual:
                    Result = Value <= Treshhold;
                    break;
                case OperatorType.Greater:
                    Result = Value > Treshhold;
                    break;
                case OperatorType.GreaterOrEqual:
                    Result = Value >= Treshhold;
                    break;
            }
        }
        #endregion
    }
}

