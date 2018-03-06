using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Expressions
{
	public class InfixExpression : Expression
	{
		// tokenizes input expression
		private static IEnumerable<ExpressionToken> Tokenize(string expression)
		{
			// use basic tokenization with parentheses and comma as function argument separator (e.g. min(a,b)) as additional operators
			return BasicTokenize(expression, "(", ")", ",");
		}

		// converts tokenized infix expression into tokenized reverse polish expression using the shunting-yard algorithm
		private static IEnumerable<ExpressionToken> ConvertToRPN(IEnumerable<ExpressionToken> infixTokens)
		{
			var rpnTokens = new List<ExpressionToken>();
			var stack = new Stack<ExpressionToken>();

			foreach (var tok in infixTokens)
			{
				if (tok.type == TokenType.Number || tok.type == TokenType.Variable)
				{
					rpnTokens.Add(tok);
					continue;
				}
				if (tok.type == TokenType.Operator)
				{
					if (tok.token == "(")
					{
						// push left-paren onto stack
						stack.Push(tok);
					}
					else if (tok.token == ")")
					{
						ExpressionToken t;
						// pop an operator from the stack
						// while it isn't a left-paren, push operator to output
						// left-paren itself is not pushed to output
						while ((t = stack.Pop()).token != "(")
						{
							rpnTokens.Add(t);
						}
						// functions like sin(...) must be handled
						if (stack.Count > 0) // stack may be empty now, check for that
						{
							t = stack.Peek(); // check token at top without removing it
							if (Operators.ContainsKey(t.token) && Operators[t.token].Precedence == -1) // precedence -1 is a function
							{
								// it's a function --> remove it now
								stack.Pop();
								rpnTokens.Add(t);
							}
						}
					}
					else if (tok.token == ",")
					{
						ExpressionToken t;
						// pop operators from the stack and push them to the output
						// until left-paren is found, do not pop that from the stack
						while ((t = stack.Peek()).token != "(")
						{
							stack.Pop();
							rpnTokens.Add(t);
						}
					}
					else
					{
						// standard operator (in globally valid list) found
						// get operator data
						var od = Operators[tok.token];
						if (od.Precedence == -1) // function
						{
							// push function to stack
							stack.Push(tok);
						}
						else
						{
							ExpressionToken t;
							// pop operators with higher precedence
							// something must be on the stack to check, peek at it
							// if the globally valid operators contain the current operator, check it
							// otherwise exit the loop, as it could be a left-paren that needs to stay on the stack
							while (stack.Count > 0 && Operators.ContainsKey((t = stack.Peek()).token))
							{
								// get data of operator on top of stack
								var od2 = Operators[t.token];
								// if the operator has a higher or higher or equal precedence (depending on associativity)
								// pop it and push it to output
								// otherwise end the loop
								if ((od.Associativity == OperatorAssociativity.Left && od.Precedence <= od2.Precedence) ||
									(od.Associativity == OperatorAssociativity.Right && od.Precedence < od2.Precedence))
								{
									stack.Pop();
									rpnTokens.Add(t);
								}
								else
								{
									break;
								}
							}
							// push current operator onto stack
							stack.Push(tok);
						}
					}
					continue;
				}
				// didn't handle token
				throw new Exception("");
			}

			// pop the rest of the stack to the output
			while (stack.Count > 0)
			{
				rpnTokens.Add(stack.Pop());
			}

			return rpnTokens;
		}

		// original expression
		private string m_expression;
		// parsed token array (infix notation)
		private ExpressionToken[] m_infixTokens;
		// parsed token array (reverse polish notation)
		private ExpressionToken[] m_reversePolishTokens;
		private ExpressionToken[] InfixTokens { get { return m_infixTokens ?? (m_infixTokens = Tokenize(m_expression).ToArray()); } }
		private ExpressionToken[] ReversePolishTokens { get { return m_reversePolishTokens ?? (m_reversePolishTokens = ConvertToRPN(InfixTokens).ToArray()); } }

		// number of vars used in expression
		private int m_varCount;
		/// <summary>
		/// Gets the number of variables needed for evaluation.
		/// </summary>
		/// <remarks>
		/// This property does not get the number of variable tokens in the expression, but the number of elements the variable-array has to have for evaluation.
		/// <remarks>
		public override int VariableCount { get { return (m_varCount == -1 ? m_varCount = GetVarCount() : m_varCount); } }

		/// <summary>
		/// Initializes a new instance of the <see cref="InfixExpression"/> class.
		/// </summary>
		/// <param name="expression">The expression the new instance should represent.</param>
		/// 
		/// <remarks>
		/// The specified expression is parsed lazily unless forced by calling <see cref="ForceTokenization"/>.
		/// </remarks>
		public InfixExpression(string expression)
		{
			m_expression = expression;
		}

		/// <summary>
		/// Forces tokenization of the expression.
		/// </summary>
		public override void ForceTokenization()
		{
			if (m_reversePolishTokens == null)
				m_reversePolishTokens = ConvertToRPN(InfixTokens).ToArray();
		}

		/// <summary>
		/// Calculates number of variables needed for evaluation.
		/// </summary>
		/// <returns>Number of variables needed for evaluation</returns>
		private int GetVarCount()
		{
			int maxIdx = -1;
			foreach (var tok in ReversePolishTokens)
			{
				if (tok.type != TokenType.Variable)
					continue;
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
			foreach (var tok in InfixTokens)
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
		public string ToDetailedString(bool useInfixNotation = true)
		{
			var str = "";
			foreach (var tok in (useInfixNotation ? InfixTokens : ReversePolishTokens))
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
			var revTokens = ReversePolishTokens.Reverse();
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

		/// <summary>
		/// Evaluates this expression using the given variables.
		/// </summary>
		/// <param name="variables">The variables used in the expression. Can be <c>null</c> if no variables are used.</param>
		/// <returns>The evaluated value.</returns>
		public override double Evaluate(double[] variables)
		{
			return EvaluateReversePolish(ReversePolishTokens, variables);
		}
	}
}
