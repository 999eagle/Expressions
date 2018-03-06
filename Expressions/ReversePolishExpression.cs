using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Expressions
{
	/// <summary>
	/// Represents an expression in reverse polish notation.
	/// </summary>
	/// <remarks>
	/// The operators that may be used in expressions are:
	/// <list type="bullet">
	/// <item>Arithmetic operators - + (addition), - (subtraction), * (multiplication), / (division), ^ (exponentation)</item>
	/// <item>sin - Sine</item>
	/// <item>cos - Cosine</item>
	/// <item>exp - Exponential</item>
	/// <item>ln - Natural logarithm</item>
	/// <item>sqrt - Square root</item>
	/// <item>tan - Tangent</item>
	/// <item>max, min - Bigger/Smaller value of two values</item>
	/// </list>
	/// Furthermore the following constants may be used in expressions:
	/// <list type="bullet">
	/// <item>tau - The circle constant 6.28318530717...</item>
	/// </list>
	/// To use variables in expressions use the format "$&lt;index&gt;" where &lt;index&gt; is an index to the variables-array given when evaluating an expression.
	/// Whitespace is not important except where needed to distinguish between two tokens. Consecutive operators may be written without whitespace. Numbers may start with a decimal point.
	/// Examples for valid expressions are:
	/// <list type="bullet">
	/// <item>2 $0 tau * +</item>
	/// <item>3 5 - sin</item>
	/// <item>2.65$0-sin$1 .3/*</item>
	/// </list>
	/// </remarks>
	public sealed class ReversePolishExpression : Expression
	{
		/// <summary>
		/// Tokenize an expression in reverse polish notation.
		/// </summary>
		/// <param name="expression">The expression to tokenize in string format.</param>
		/// <returns>A list of tokens.</returns>
		private static IEnumerable<ExpressionToken> Tokenize(string expression)
		{
			// use base classes' basic tokenization without any additional operators
			return BasicTokenize(expression);
		}

		// parsed token array
		private ExpressionToken[] m_tokens = null;
		// original expression
		private string m_expression = "";
		// number of vars used in expression
		private int m_varCount = -1;

		/// <summary>
		/// The tokens that represent this expression.
		/// </summary>
		private ExpressionToken[] Tokens { get { return m_tokens ?? (m_tokens = Tokenize(m_expression).ToArray()); } }

		/// <summary>
		/// Gets the number of variables needed for evaluation.
		/// </summary>
		/// <remarks>
		/// This property does not get the number of variable tokens in the expression, but the number of elements the variable-array has to have for evaluation.
		/// <remarks>
		public override int VariableCount { get { return (m_varCount == -1 ? m_varCount = GetVarCount() : m_varCount); } }


		/// <summary>
		/// Initializes a new instance of the <see cref="ReversePolishExpression"/> class.
		/// </summary>
		/// <param name="expression">The expression the new instance should represent.</param>
		/// 
		/// <remarks>
		/// The specified expression is parsed lazily unless forced by calling <see cref="ForceTokenization"/>.
		/// </remarks>
		public ReversePolishExpression(string expression)
		{
			// store expression, tokenization happens lazily unless forced
			m_expression = expression;
		}

		/// <summary>
		/// Forces tokenization of the expression.
		/// </summary>
		public override void ForceTokenization()
		{
			if (m_tokens == null)
				m_tokens = Tokenize(m_expression).ToArray();
		}

		/// <summary>
		/// Evaluates this expression using the given variables.
		/// </summary>
		/// <param name="variables">The variables used in the expression. Can be <c>null</c> if no variables are used.</param>
		/// <returns>The evaluated value.</returns>
		public override double Evaluate(double[] variables)
		{
			// evaluating reverse polish is implemented in base class
			return EvaluateReversePolish(Tokens, variables);
		}

		/// <summary>
		/// Evaluates a new expression using the given variables.
		/// </summary>
		/// <param name="expression">The expression to evaluate.</param>
		/// <param name="variables">The variables used in the expression. Can be <c>null</c> if no variables are used.</param>
		/// <returns>The evaluated value.</returns>
		public static double Evaluate(string expression, double[] variables)
		{
			return new ReversePolishExpression(expression).Evaluate(variables);
		}

		/// <summary>
		/// Calculates number of variables needed for evaluation.
		/// </summary>
		/// <returns>Number of variables needed for evaluation</returns>
		private int GetVarCount()
		{
			int maxIdx = -1;
			foreach (var tok in Tokens)
			{
				if (tok.type != TokenType.Variable) continue;
				var idx = (int)tok.value;
				if (idx > maxIdx)
					maxIdx = idx;
			}
			return maxIdx + 1;
		}

		/// <summary>
		/// Gets a string representation of this expression.
		/// </summary>
		/// <returns>A string representation of this expression</returns>
		/// 
		/// <remarks>
		/// The return value can be parsed again by this class, but it is not guaranteed to be the shortest possible representation.
		/// </remarks>
		public override string ToString()
		{
			var str = "";
			foreach (var tok in Tokens)
			{
				switch (tok.type)
				{
					case TokenType.Number:
					case TokenType.Operator:
						str += tok.token + " ";
						break;
					case TokenType.Variable:
						str += "$" + tok.token + " ";
						break;
				}
			}
			return str.Trim();
		}

		/// <summary>
		/// Gets a more detailed string representation of this expression.
		/// </summary>
		/// <returns>A string representation of this expression</returns>
		/// 
		/// <remarks>
		/// The return value can not be parsed by this class as it is meant as a debug output.
		/// </remarks>
		public string ToDetailedString()
		{
			var str = "";
			foreach (var tok in Tokens)
			{
				switch (tok.type)
				{
					case TokenType.Number:
						str += $"(Number \"{ tok.token }\") ";
						break;
					case TokenType.Variable:
						str += $"(Variable \"{ tok.token }\") ";
						break;
					case TokenType.Operator:
						str += $"(Operator \"{ tok.token }\") ";
						break;
				}
			}
			return str.Trim();
		}

		/// <summary>
		/// Gets an expression tree that represents this epxression.
		/// </summary>
		/// <returns>An expression tree that represents this expression.</returns>
		public override ExpressionTree GetExpressionTree()
		{
			var revTokens = Tokens.Reverse();
			var node = CreateSubTree(revTokens.GetEnumerator());
			return new ExpressionTree(node);
		}

		// recursive helper for creating an expression tree
		private ExpressionTreeNode CreateSubTree(IEnumerator<ExpressionToken> tokens)
		{
			var rootNode = new ExpressionTreeNode();
			if (!tokens.MoveNext())
				throw new Exception("No token on the stack!");
			var token = tokens.Current;
			rootNode.Token = token;
			if (token.type == TokenType.Operator)
			{
				// parse subtrees
				int arity = GetOperatorArity(token.token);
				for (int i = 0; i < arity; i++)
				{
					rootNode.AddChild(CreateSubTree(tokens), 0);
				}
			}
			return rootNode;
		}
	}
}
