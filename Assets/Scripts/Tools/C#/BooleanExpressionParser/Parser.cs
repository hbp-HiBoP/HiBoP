using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Tools.CSharp.BooleanExpressionParser
{
    public class Parser
    {
        public static BooleanExpression Parse(string expression)
        {
            FixExpression(ref expression);

            List<Token> tokens = new List<Token>();
            StringReader stringReader = new StringReader(expression);

            Token token;
            do
            {
                token = new Token(stringReader);
                if (token.Type != TokenType.Space)
                {
                    tokens.Add(token);
                }
            } while (token.Type != TokenType.EndOfExpression);

            string error;
            if (!CheckExpression(tokens, out error))
            {
                throw new InvalidBooleanExpressionException(error);
            }

            // If there is no value token and the expression is valid, return true
            if (tokens.Where(t => t.Type == TokenType.Value).Count() == 0)
            {
                return BooleanExpression.CreateValue("TRUE");
            }

            List<Token> polishNotationTokens = TransformToPolishNotation(tokens);

            var enumerator = polishNotationTokens.GetEnumerator();
            enumerator.MoveNext();
            return MakeBooleanExpression(ref enumerator);
        }
        private static void FixExpression(ref string expression)
        {
            expression = new Regex(Token.AND_CHAR + "+").Replace(expression, Token.AND_CHAR.ToString());
            expression = new Regex("\\" + Token.OR_CHAR + "+").Replace(expression, Token.OR_CHAR.ToString());
        }
        private static bool CheckExpression(List<Token> tokens, out string error)
        {
            int binaryOperatorCount = 0, booleanValueCount = 0, openParenthesisCount = 0, closeParenthesisCount = 0;
            foreach (var token in tokens)
            {
                switch (token.Type)
                {
                    case TokenType.OpenParenthesis:
                        openParenthesisCount++;
                        break;
                    case TokenType.CloseParenthesis:
                        closeParenthesisCount++;
                        break;
                    case TokenType.BinaryOperator:
                        binaryOperatorCount++;
                        break;
                    case TokenType.Value:
                        booleanValueCount++;
                        break;
                }
            }
            if (binaryOperatorCount >= booleanValueCount && booleanValueCount != 0)
            {
                error = "There are more binary operators (" + binaryOperatorCount + ") than operands (" + booleanValueCount + "), which is not possible.";
                return false;
            }
            if (openParenthesisCount != closeParenthesisCount)
            {
                error = "The number of open parenthesis (" + openParenthesisCount + ") is different from the number of close parenthesis (" + closeParenthesisCount + "), which is not possible";
                return false;
            }
            for (int i = 0; i < tokens.Count; ++i)
            {
                Token token = tokens[i];
                if (token.Type == TokenType.BinaryOperator)
                {
                    if (i == 0 || i == tokens.Count - 2)
                    {
                        error = "A binary operator does not have enough operands.";
                        return false;
                    }
                    else if ((tokens[i - 1].Type != TokenType.Value && tokens[i - 1].Type != TokenType.CloseParenthesis) || (tokens[i + 1].Type != TokenType.Value && tokens[i + 1].Type != TokenType.OpenParenthesis))
                    {
                        error = "A binary operator does not have enough operands.";
                        return false;
                    }
                }
                else if (token.Type == TokenType.UnaryOperator)
                {
                    if (i == tokens.Count - 2)
                    {
                        error = "An unary operator does not have any operand.";
                        return false;
                    }
                    else if (tokens[i + 1].Type != TokenType.Value && tokens[i + 1].Type != TokenType.UnaryOperator && tokens[i + 1].Type != TokenType.OpenParenthesis)
                    {
                        error = "An unary operator does not have any operand.";
                        return false;
                    }
                }
                else if (token.Type == TokenType.OpenParenthesis)
                {
                    if (i != 0)
                    {
                        if (tokens[i-1].Type != TokenType.UnaryOperator && tokens[i-1].Type != TokenType.OpenParenthesis && tokens[i-1].Type != TokenType.BinaryOperator)
                        {
                            error = "There is an open parenthesis which is not at the begining of the expression, after an operator or after another open parenthesis, which is not possible.";
                            return false;
                        }
                    }
                }
                else if (token.Type == TokenType.CloseParenthesis)
                {
                    if (i != tokens.Count - 2)
                    {
                        if (tokens[i+1].Type != TokenType.CloseParenthesis && tokens[i+1].Type != TokenType.BinaryOperator && tokens[i+1].Type != TokenType.EndOfExpression)
                        {
                            error = "There is a close parenthesis which is not at the end of the expression, before a binary operator or before another close parenthesis, which is not possible.";
                            return false;
                        }
                    }
                }
            }
            error = "No information";
            return true;
        }
        private static List<Token> TransformToPolishNotation(List<Token> tokens)
        {
            Queue<Token> outputQueue = new Queue<Token>();
            Stack<Token> stack = new Stack<Token>();

            foreach (var token in tokens)
            {
                switch (token.Type)
                {
                    case TokenType.Value:
                        outputQueue.Enqueue(token);
                        break;
                    case TokenType.BinaryOperator:
                    case TokenType.UnaryOperator:
                    case TokenType.OpenParenthesis:
                        stack.Push(token);
                        break;
                    case TokenType.CloseParenthesis:
                        while(stack.Peek().Type != TokenType.OpenParenthesis)
                        {
                            outputQueue.Enqueue(stack.Pop());
                        }
                        stack.Pop();
                        if (stack.Count > 0 && stack.Peek().Type == TokenType.UnaryOperator)
                        {
                            outputQueue.Enqueue(stack.Pop());
                        }
                        break;
                    default:
                        break;
                }
            }
            while(stack.Count > 0)
            {
                outputQueue.Enqueue(stack.Pop());
            }

            return outputQueue.Reverse().ToList();
        }
        private static BooleanExpression MakeBooleanExpression(ref List<Token>.Enumerator enumerator)
        {
            if (enumerator.Current.Type == TokenType.Value)
            {
                BooleanExpression expression = BooleanExpression.CreateValue(enumerator.Current.Value);
                enumerator.MoveNext();
                return expression;
            }
            else
            {
                if (enumerator.Current.Value == Token.NOT_STRING)
                {
                    enumerator.MoveNext();
                    BooleanExpression operand = MakeBooleanExpression(ref enumerator);
                    return BooleanExpression.CreateNot(operand);
                }
                else if (enumerator.Current.Value == Token.AND_STRING)
                {
                    enumerator.MoveNext();
                    BooleanExpression left = MakeBooleanExpression(ref enumerator);
                    BooleanExpression right = MakeBooleanExpression(ref enumerator);
                    return BooleanExpression.CreateAnd(left, right);
                }
                else if (enumerator.Current.Value == Token.OR_STRING)
                {
                    enumerator.MoveNext();
                    BooleanExpression left = MakeBooleanExpression(ref enumerator);
                    BooleanExpression right = MakeBooleanExpression(ref enumerator);
                    return BooleanExpression.CreateOr(left, right);
                }
            }
            return null;
        }
    }
}