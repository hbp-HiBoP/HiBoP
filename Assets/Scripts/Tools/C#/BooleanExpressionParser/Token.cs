using System;
using System.Collections.Generic;
using System.IO;

namespace HBP.Display.Tools
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
        public TokenType Type { get; private set; }
        public string Value { get; private set; }
        #endregion

        #region Default Values
        public static Token Not { get { return new Token(TokenType.UnaryOperator, NOT_STRING); } }
        public static Token And { get { return new Token(TokenType.BinaryOperator, AND_STRING); } }
        public static Token Or { get { return new Token(TokenType.BinaryOperator, OR_STRING); } }
        public static Token OpenParenthesis { get { return new Token(TokenType.OpenParenthesis, OPEN_PARENTHESIS_STRING); } }
        public static Token CloseParenthesis { get { return new Token(TokenType.CloseParenthesis, CLOSE_PARENTHESIS_STRING); } }
        #endregion

        #region Constructors
        public Token(TokenType type, string value = "")
        {
            Type = type;
            Value = value;
        }
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
                if (m_SymbolToOperator.TryGetValue(character, out Tuple<TokenType, string> value))
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