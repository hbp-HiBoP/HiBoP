using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.Data
{
    [DisplayName("Integer")]
    public class IntTag : BaseTag
    {
        #region Properties
        [DataMember(Name = "Clamped")] bool m_Clamped;
        public bool Clamped
        {
            get => m_Clamped;
            set
            {
                if (m_Clamped != value)
                {
                    m_Clamped = value;
                    OnNeedToRecalculateValue.Invoke();
                }
            }
        }
        [DataMember(Name = "Min")] int m_Min;
        public int Min
        {
            get => m_Min;
            set
            {
                if (m_Min != value)
                {
                    m_Min = value;
                    OnNeedToRecalculateValue.Invoke();
                }
            }

        }
        [DataMember(Name = "Max")] int m_Max;
        public int Max
        {
            get => m_Max;
            set
            {
                if (m_Max != value)
                {
                    m_Max = value;
                    OnNeedToRecalculateValue.Invoke();
                }
            }
        }

        public UnityEvent OnNeedToRecalculateValue { get; set; } = new UnityEvent();
        #endregion

        #region Constructors
        public IntTag() : this("", false, 0, 0)
        {
        }
        public IntTag(string name) : this (name, false, 0, 0)
        {

        }
        public IntTag(string name, string ID) : base(name, ID)
        {

        }
        public IntTag(string name, bool clamped, int min, int max) : base(name)
        {
            Clamped = clamped;
            Min = min;
            Max = max;
        }
        public IntTag(string name, bool clamped, int min, int max, string ID) : base(name, ID)
        {
            Clamped = clamped;
            Min = min;
            Max = max;
        }
        #endregion

        #region Public Methods
        public int Clamp(int value)
        {
            return Clamped ? Mathf.Clamp(value, Min, Max) : value;
        }
        public int Convert(object value)
        {
            if (value != null && value is int)
            {
                return (int)value;
            }
            else
            {
                throw new Exception("Wrong value type");
            }
        }
        public override object Clone()
        {
            return new IntTag(Name, Clamped, Min, Max, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if(copy is IntTag intTag)
            {
                Clamped = intTag.Clamped;
                Min = intTag.Min;
                Max = intTag.Max;
            }
            if(copy is FloatTag floatTag)
            {
                Clamped = floatTag.Clamped;
                Min = (int) floatTag.Min;
                Max = (int) floatTag.Max;
            }
        }
        #endregion
    }
}