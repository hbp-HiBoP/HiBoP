using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Tools.CSharp.BooleanExpressionParser
{
    public enum TokenType { OpenParenthesis, CloseParenthesis, UnaryOperator, BinaryOperator, Value, EndOfExpression, Space }

    public class Token
    {
        #region Properties
        public const char OPEN_PARENTHESIS_CHAR = '(';
        public const char CLOSE_PARENTHESIS_CHAR = ')';
        public const char NOT_CHAR = '!';
        public const char AND_CHAR = '&';
        public const char OR_CHAR = '|';
        public const string OPEN_PARENTHESIS_STRING = "(";
        public const string CLOSE_PARENTHESIS_STRING = ")";
        public const string NOT_STRING = "NOT";
        public const string AND_STRING = "AND";
        public const string OR_STRING = "OR";
        private static Dictionary<char, Tuple<TokenType, string>> m_SymbolToOperator = new Dictionary<char, Tuple<TokenType, string>>()
        {
            { OPEN_PARENTHESIS_CHAR, new Tuple<TokenType, string>(TokenType.OpenParenthesis, OPEN_PARENTHESIS_STRING) },
            { CLOSE_PARENTHESIS_CHAR, new Tuple<TokenType, string>(TokenType.CloseParenthesis, CLOSE_PARENTHESIS_STRING) },
            { NOT_CHAR, new Tuple<TokenType, string>(TokenType.UnaryOperator, NOT_STRING) },
            { AND_CHAR, new Tuple<TokenType, string>(TokenType.BinaryOperator, AND_STRING) },
            { OR_CHAR, new Tuple<TokenType, string>(TokenType.BinaryOperator, OR_STRING) }
        };
        public TokenType Type;
        public string Value;
        #endregion

        #region Constructors
        public Token(StringReader stringReader)
        {
            int c = stringReader.Read();
            if (c == -1)
            {
                Type = TokenType.EndOfExpression;
                Value = "";
                return;
            }
            else if ((char)c == ' ')
            {
                Type = TokenType.Space;
                Value = "";
                return;
            }
            else
            {
                char character = (char)c;
                Tuple<TokenType, string> value;
                if (m_SymbolToOperator.TryGetValue(character, out value))
                {
                    Type = value.Item1;
                    Value = value.Item2;
                }
                else
                {
                    string stringValue = "";
                    stringValue += character;
                    while (stringReader.Peek() != -1 && !m_SymbolToOperator.ContainsKey((char)stringReader.Peek()))
                    {
                        stringValue += (char)stringReader.Read();
                    }
                    Type = TokenType.Value;
                    Value = stringValue;
                }
            }
        }
        #endregion
    }
}