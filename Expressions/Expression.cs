using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Expressions
{
	/// <summary>
	/// Provides an abstract base class for all expressions.
	/// </summary>
	public abstract class Expression
	{
		protected static Dictionary<string, OperatorData> Operators = new Dictionary<string, OperatorData>
		{
			// binary
			{ "+", new OperatorData { Arity = 2, Precedence = 1, Associativity = OperatorAssociativity.Left } },
			{ "-", new OperatorData { Arity = 2, Precedence = 1, Associativity = OperatorAssociativity.Left } },
			{ "*", new OperatorData { Arity = 2, Precedence = 2, Associativity = OperatorAssociativity.Left } },
			{ "/", new OperatorData { Arity = 2, Precedence = 2, Associativity = OperatorAssociativity.Left } },
			{ "^", new OperatorData { Arity = 2, Precedence = 3, Associativity = OperatorAssociativity.Right } },
			// binary functions
			{ "max", new OperatorData { Arity = 2, Precedence = -1 } },
			{ "min", new OperatorData { Arity = 2, Precedence = -1 } },
			// unary functions
			{ "exp", new OperatorData { Arity = 1, Precedence = -1 } },
			{ "sin", new OperatorData { Arity = 1, Precedence = -1 } },
			{ "cos", new OperatorData { Arity = 1, Precedence = -1 } },
			{ "ln", new OperatorData { Arity = 1, Precedence = -1 } },
			{ "sqrt", new OperatorData { Arity = 1, Precedence = -1 } },
			{ "tan", new OperatorData { Arity = 1, Precedence = -1 } },
			{ "abs", new OperatorData { Arity = 1, Precedence = -1 } },
			// constants
			{ "tau", new OperatorData { Arity = 0, ConstantValue = 6.2831853071795864769252867 } }
		};
		protected struct OperatorData
		{
			public int Arity { get; set; }
			public int Precedence { get; set; }
			public OperatorAssociativity Associativity { get; set; }
			public double ConstantValue { get; set; }
		}
		// left: a+b+c = (a+b)+c
		// right: a+b+c = a+(b+c)
		// +, * can be both, but left is assumed for simpler rpn expression from infix notation
		// -, / must be left
		// ^ must be right
		protected enum OperatorAssociativity
		{
			Left,
			Right
		}

		/// <summary>
		/// Helper for calculating a single operation.
		/// </summary>
		/// <param name="operatorToken">The operator to calculate.</param>
		/// <param name="args">The operands to use.</param>
		/// <returns>The result of the operation.</returns>
		internal static double CalculateOperation(string operatorToken, params double[] args)
		{
			if (!Operators.ContainsKey(operatorToken))
				throw new InvalidOperationException($"Operator \"{operatorToken}\" unknown!");
			var data = Operators[operatorToken];

			// check for enough operands
			if (args.Length < data.Arity)
				throw new InvalidOperationException($"Not enough operands for operator \"{operatorToken}\"! (got {args.Length}, expected {data.Arity})");

			// handle constants
			if (data.Arity == 0)
				return data.ConstantValue;

			// get first operand
			var a = args[0];
			if (data.Arity == 1)
			{
				// handle unary operators
				switch (operatorToken)
				{
					case "exp":
						return Math.Exp(a);
					case "sin":
						return Math.Sin(a);
					case "cos":
						return Math.Cos(a);
					case "ln":
						return Math.Log(a);
					case "sqrt":
						return Math.Sqrt(a);
					case "tan":
						return Math.Tan(a);
					case "abs":
						return Math.Abs(a);
				}
				// operator is unary, but wasn't handled in above switch
				throw new InvalidOperationException($"Unary operator \"{ operatorToken }\" wasn't handled!");
			}

			// get second operand
			var b = args[1];
			if (data.Arity == 2)
			{
				// handle binary operators
				switch (operatorToken)
				{
					case "+":
						return b + a;
					case "-":
						return b - a;
					case "*":
						return b * a;
					case "/":
						return b / a;
					case "^":
						return Math.Pow(b, a);
					case "max":
						return Math.Max(b, a);
					case "min":
						return Math.Min(b, a);
				}
				// operator is binary, but wasn't handled in above switch
				throw new InvalidOperationException($"Binary operator \"{ operatorToken }\" wasn't handled!");
			}

			throw new InvalidOperationException($"Operator \"{operatorToken}\" has invalid arity {data.Arity}!");
		}

		/// <summary>
		/// Gets the arity of an operator.
		/// </summary>
		/// <param name="operatorToken">The operator to get the arity for.</param>
		/// <returns>The specified operator's arity.</returns>
		public static int GetOperatorArity(string operatorToken)
		{
			return Operators[operatorToken].Arity;
		}

		/// <summary>
		/// Gets whether the expression uses any variables.
		/// </summary>
		public bool HasVariables { get { return VariableCount > 0; } }

		/// <summary>
		/// Gets the number of variables needed for evaluation.
		/// </summary>
		/// <remarks>
		/// This property does not get the number of variable tokens in the expression, but the number of elements the variable-array has to have for evaluation.
		/// <remarks>
		public abstract int VariableCount { get; }

		/// <summary>
		/// Forces tokenization of the expression.
		/// </summary>
		public abstract void ForceTokenization();

		/// <summary>
		/// Gets an expression tree that represents this expression.
		/// </summary>
		/// <returns>An expression tree that represents this expression.</returns>
		public abstract ExpressionTree GetExpressionTree();

		/// <summary>
		/// Evaluates this expression using the given variables.
		/// </summary>
		/// <param name="variables">The variables used in the expression. Can be <c>null</c> if no variables are used.</param>
		/// <returns>The evaluated value.</returns>
		public virtual double Evaluate(double[] variables)
		{
			return GetExpressionTree().Evaluate(variables);
		}

		/// <summary>
		/// Helper function for all derived types. Evaluates a specified expression in reverse polish notation using the specified variables.
		/// </summary>
		/// <param name="tokens">The tokens that make up the expression to evaluate.</param>
		/// <param name="variables">The variables to use for evaluation.</param>
		/// <returns>The evaluated value.</returns>
		protected double EvaluateReversePolish(IEnumerable<ExpressionToken> tokens, double[] variables)
		{
			// initialize calculation stack
			var stack = new Stack<double>();
			foreach (var tok in tokens)
			{
				if (tok.type == TokenType.Number)
				{
					// just push numbers onto the stack
					stack.Push((double)tok.value);
				}
				else if (tok.type == TokenType.Variable)
				{
					// check variable index and push value to stack
					int idx = (int)tok.value;
					if (variables == null)
					{
						throw new ArgumentNullException("variables", $"Expression tried to access variable with index { idx }, but no variables are given.");
					}
					if (idx < 0 || idx >= variables.Length)
					{
						throw new ArgumentOutOfRangeException("", $"Expression tried to access variable with index { idx }, but only { variables.Length } variable{ (variables.Length == 1 ? " is" : "s are") } given.");
					}
					stack.Push(variables[idx]);
				}
				else if (tok.type == TokenType.Operator)
				{
					// get operator arity and pop that many values from the stack
					int arity = GetOperatorArity(tok.token);
					var args = new List<double>();
					for (int i = 0; i < arity; i++)
						args.Add(stack.Pop());

					// calculating operations happens in expression-class
					stack.Push(CalculateOperation(tok.token, args.ToArray()));
				}
			}
			if (stack.Count != 1)
				throw new InvalidOperationException($"Malformed expression \"{ this.ToString() }\"! Execution didn't finish with exactly one value on the stack.");

			return stack.Pop();
		}

		/// <summary>
		/// Tokenize an expression.
		/// </summary>
		/// <param name="expression">The expression to tokenize in string format.</param>
		/// <param name="additionalOperators">Operators that aren't included in the global list of operators,
		/// but are needed for the expression format (e.g. parentheses in infix expressions).</param>
		/// <returns>A list of tokens.</returns>
		protected static IEnumerable<ExpressionToken> BasicTokenize(string expression, params string[] additionalOperators)
		{
			expression = expression.ToLowerInvariant();
			var tokens = new List<ExpressionToken>();
			var tok = new ExpressionToken { token = "", type = TokenType.Invalid };
			foreach (var c in expression) // enumerate each char of the input
			{
				if (c == '$') // $ starts variable index
				{
					// end current token, start new variable token, but omit $ in token
					if (tok.type != TokenType.Invalid)
						tokens.Add(tok);
					tok.type = TokenType.Variable;
					tok.token = "";
				}
				else if (c == ' ') // space splits tokens
				{
					// end current token, start invalid empty token as anything can come after a space
					if (tok.type != TokenType.Invalid)
						tokens.Add(tok);
					tok.type = TokenType.Invalid;
					tok.token = "";
				}
				else if (Char.IsDigit(c))
				{
					// digits can either be numbers or variable indices, for both token types append the digit
					if (tok.type == TokenType.Number || tok.type == TokenType.Variable)
					{
						tok.token += c;
					}
					else
					{
						// end current token and start new number token
						if (tok.type != TokenType.Invalid)
							tokens.Add(tok);
						tok.type = TokenType.Number;
						tok.token = "" + c;
					}
				}
				else if (c == '.')
				{
					// dot can only appear in numbers
					if (tok.type == TokenType.Number)
					{
						tok.token += c;
					}
					else
					{
						// end current token and start new number token
						// allows leading zero to be omitted in numbers like 0.5
						if (tok.type != TokenType.Invalid)
							tokens.Add(tok);
						tok.type = TokenType.Number;
						tok.token = "0.";
					}
				}
				else
				{
					// everything else is assumed to be an operator
					// actual checking of operators happens in next step
					if (tok.type == TokenType.Operator)
					{
						tok.token += c;
					}
					else
					{
						// end current token and start new operator token
						if (tok.type != TokenType.Invalid)
							tokens.Add(tok);
						tok.type = TokenType.Operator;
						tok.token = "" + c;
					}
				}
			}
			if (tok.type != TokenType.Invalid)
				tokens.Add(tok); // add leftover token if valid

			// create list of valid operators by concatenating globally valid operators with additional operators
			var validOperators = Operators.Keys.Concat(additionalOperators);

			// check and split operators
			for (int i = 0; i < tokens.Count; i++)
			{
				tok = tokens[i];
				if (tok.type != TokenType.Operator)
					continue;
				var t = tok.token;
				int s = 0; // start index of current operator
						   // remove current token, valid operators will be reinserted
						   // decrease index so that outer loop can continue
				tokens.RemoveAt(i--);
				for (int j = 0; j < t.Length; j++)
				{
					// j is end index of current operator
					string op = t.Substring(s, j - s + 1);
					if (!validOperators.Any(o => o.StartsWith(op)))
					{
						// no valid operator starts with current operator
						// advance start index and reset end index to start index
						// --> randomly inserted char could be fixed by this
						s++;
						j = s - 1;
						continue;
					}
					if (validOperators.Any(o => o == op))
					{
						// op is a valid operator --> insert into list, increase index
						tokens.Insert(++i, new ExpressionToken { token = op, type = TokenType.Operator });
						// next operator start after end of current one
						s = j + 1;
					}
				}
			}
			// check numbers
			for (int i = 0; i < tokens.Count; i++)
			{
				tok = tokens[i];
				if (tok.type != TokenType.Number)
					continue;
				if (tok.token.Count(c => c == '.') > 1)
				{
					// multiple decimal points in this token --> for a.b.c assume a.b 0.c
					// remove original token, split tokens will be added
					tokens.RemoveAt(i);
					var s = tok.token.Split('.');
					// add "a.b" token
					tokens.Insert(i, new ExpressionToken { token = s[0] + "." + s[1], type = TokenType.Number });
					for (int j = 2; j < s.Length; j++)
					{
						// add "0.c" tokens
						tokens.Insert(++i, new ExpressionToken { token = "0." + s[j], type = TokenType.Number });
					}
				}
			}
			// parse values
			// parsing here requires more memory, but improves performance when evaluating an expression multiple times and parser errors are thrown here already
			// (although no parsing errors should happen as invalid characters should have been discarded during tokenization)
			for (int i = 0; i < tokens.Count; i++)
			{
				tok = tokens[i];
				if (tok.type == TokenType.Operator)
				{
					// check for contains needed, as additional operators throw an exception otherwise
					if (Operators.ContainsKey(tok.token) && Operators[tok.token].Arity == 0)
					{
						// change token type to number --> value will be used in evaluation
						tok.type = TokenType.Number;
						tok.value = Operators[tok.token].ConstantValue;
						// do not change tok.token to the value, this way ToString() will output the name instead of the value
					}
				}
				else if (tok.type == TokenType.Number)
				{
					double d;
					if (!Double.TryParse(tok.token, out d))
						throw new ArgumentException("expression", $"Couldn't parse \"{ tok.token }\" as a number!");
					tok.value = d;
				}
				else if (tok.type == TokenType.Variable)
				{
					int idx;
					if (!Int32.TryParse(tok.token, out idx))
						throw new ArgumentException("expression", $"Couldn't parse \"{ tok.token }\" as a variable index!");
					tok.value = idx;
				}
				tokens[i] = tok;
			}

			return tokens;
		}
	}
}
