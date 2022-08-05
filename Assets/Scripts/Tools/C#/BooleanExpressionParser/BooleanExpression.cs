using System;
using System.Collections.Generic;

namespace Tools.CSharp.BooleanExpressionParser
{
    public enum BooleanExpressionType { Value, And, Or, Not };

    public class BooleanExpression
    {
        #region Properties
        public BooleanExpressionType Type { get; set; }
        public BooleanExpression Left { get; set; }
        public BooleanExpression Right { get; set; }
        public string Value { get; set; }
        private bool m_BooleanValue;
        public bool IsValue { get { return Type == BooleanExpressionType.Value; } }
        public bool IsAtomic { get { return (IsValue || (Type == BooleanExpressionType.Not && Right.IsValue)); } }
        #endregion

        #region Constructors
        private BooleanExpression(BooleanExpressionType type, BooleanExpression left, BooleanExpression right)
        {
            Type = type;
            Left = left;
            Right = right;
            Value = "";
        }
        private BooleanExpression(string value)
        {
            Type = BooleanExpressionType.Value;
            Left = null;
            Right = null;
            Value = value;
        }
        public static BooleanExpression CreateAnd(BooleanExpression left, BooleanExpression right)
        {
            return new BooleanExpression(BooleanExpressionType.And, left, right);
        }
        public static BooleanExpression CreateOr(BooleanExpression left, BooleanExpression right)
        {
            return new BooleanExpression(BooleanExpressionType.Or, left, right);
        }
        public static BooleanExpression CreateNot(BooleanExpression child)
        {
            return new BooleanExpression(BooleanExpressionType.Not, null, child);
        }
        public static BooleanExpression CreateValue(string value)
        {
            return new BooleanExpression(value);
        }
        #endregion

        #region Public Methods
        public bool Evaluate()
        {
            if (Type == BooleanExpressionType.Not)
            {
                return !Right.Evaluate();
            }
            else if (Type == BooleanExpressionType.Or)
            {
                return Left.Evaluate() || Right.Evaluate();
            }
            else if (Type == BooleanExpressionType.And)
            {
                return Left.Evaluate() && Right.Evaluate();
            }
            return m_BooleanValue;
        }
        public List<BooleanExpression> GetAllBooleanValuesUnderThisOne()
        {
            List<BooleanExpression> list = new List<BooleanExpression>();
            switch (Type)
            {
                case BooleanExpressionType.Value:
                    list.Add(this);
                    break;
                case BooleanExpressionType.And:
                case BooleanExpressionType.Or:
                    list.AddRange(Left.GetAllBooleanValuesUnderThisOne());
                    list.AddRange(Right.GetAllBooleanValuesUnderThisOne());
                    break;
                case BooleanExpressionType.Not:
                    list.AddRange(Right.GetAllBooleanValuesUnderThisOne());
                    break;
            }
            return list;
        }
        public void SetBooleanValue(Predicate<string> predicate)
        {
            m_BooleanValue = predicate(Value);
        }
        #endregion
    }
}
