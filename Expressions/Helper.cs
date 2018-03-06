using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Expressions
{
	public enum TokenType
	{
		Invalid,
		Number,
		Variable,
		Operator
	}

	/// <summary>
	/// Represents a single token in an expression.
	/// </summary>
	/// 
	/// <remarks>
	/// Can't represent parentheses because they aren't needed in expression trees.
	/// </remarks>
	public struct ExpressionToken
	{
		public string token;
		// stores parsed value (double for number, int for variable, null for operator)
		public object value;
		public TokenType type;

		public override string ToString()
		{
			return $"token: { token }, type: { type }";
		}
	}
}
