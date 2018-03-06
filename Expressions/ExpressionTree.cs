using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Expressions
{
	/// <summary>
	/// Represents an expression tree.
	/// </summary>
	public sealed class ExpressionTree
	{
		/// <summary>
		/// The root node of this tree.
		/// </summary>
		public ExpressionTreeNode RootNode { get; private set; }

		/// <summary>
		/// Creates a new instance of <see cref="ExpressionTree"/> using the specified node as root node.
		/// </summary>
		/// <param name="rootNode">The node to use as root node.</param>
		public ExpressionTree(ExpressionTreeNode rootNode)
		{
			RootNode = rootNode;
		}

		/// <summary>
		/// Creates a new instance of <see cref="ExpressionTree"/> using a new node as root node.
		/// </summary>
		public ExpressionTree() : this(new ExpressionTreeNode()) { }

		/// <summary>
		/// Evaluates the expression represented by this tree using the specified variables.
		/// </summary>
		/// <param name="variables">The variables to use.</param>
		/// <returns>The evaluated value.</returns>
		public double Evaluate(double[] variables)
		{
			return RootNode.Evaluate(variables);
		}
	}

	/// <summary>
	/// Represents a single node in an expression tree.
	/// </summary>
	public sealed class ExpressionTreeNode
	{
		/// <summary>
		/// The parent of this node. Null if this node doesn't have a parent.
		/// </summary>
		/// 
		/// <remarks>
		/// Automatically set by <see cref="AddChild(ExpressionTreeNode, int)"/>, <see cref="AddChild(ExpressionTreeNode)"/> and <see cref="RemoveChild(ExpressionTreeNode)"/>.
		/// </remarks>
		public ExpressionTreeNode Parent { get; private set; }

		private List<ExpressionTreeNode> m_children;
		/// <summary>
		/// The children of this node.
		/// </summary>
		public IReadOnlyList<ExpressionTreeNode> Children { get { return m_children.AsReadOnly(); } }

		/// <summary>
		/// The token represented by this node.
		/// </summary>
		public ExpressionToken Token { get; set; }

		/// <summary>
		/// Creates a new instance of the <see cref="ExpressionTreeNode"/>-class.
		/// </summary>
		public ExpressionTreeNode()
		{
			m_children = new List<ExpressionTreeNode>();
			Parent = null;
			Token = new ExpressionToken { type = TokenType.Invalid };
		}

		/// <summary>
		/// Adds a new child to this node's children.
		/// </summary>
		/// <param name="node">The node to add</param>
		/// <returns>Returns whether the addition was successful.</returns>
		/// 
		/// <remarks>
		/// The addition fails if the new node already has a parent node.
		/// The new node is added at the end of the children.
		/// </remarks>
		public bool AddChild(ExpressionTreeNode node)
		{
			return AddChild(node, m_children.Count);
		}

		/// <summary>
		/// Adds a new child to this node's children at the specified index.
		/// </summary>
		/// <param name="node">The node to add.</param>
		/// <param name="index">Specifies where to add the new node.</param>
		/// <returns>Whether the addition was successful.</returns>
		/// 
		/// <remarks>
		/// The addition fails if the new node already has a parent node.
		/// </remarks>
		public bool AddChild(ExpressionTreeNode node, int index)
		{
			if (m_children.Contains(node) || node.Parent != null)
				return false;
			m_children.Insert(index, node);
			node.Parent = this;
			return true;
		}

		/// <summary>
		/// Removes a node from this node's children.
		/// </summary>
		/// <param name="node">The node to remove.</param>
		/// <returns>Whether the node was removed successfully.</returns>
		/// 
		/// <remarks>
		/// Removing fails if the node belongs to another node or to no node at all.
		/// </remarks>
		public bool RemoveChild(ExpressionTreeNode node)
		{
			if (node.Parent == this)
			{
				m_children.Remove(node);
				node.Parent = null;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Gets a string representation of this node.
		/// </summary>
		/// <returns>A string representation of this node.</returns>
		public override string ToString()
		{
			return $"({ Token }), child count: { m_children.Count }";
		}

		/// <summary>
		/// Evaluates this node's value using the specified variables. Evaluates subtrees if needed.
		/// </summary>
		/// <param name="variables">The variables for evaluation.</param>
		/// <returns>The value of this node.</returns>
		public double Evaluate(double[] variables)
		{
			if (Token.type == TokenType.Invalid)
				throw new InvalidOperationException("Can't evaluate an invalid token.");

			if (Token.type == TokenType.Number)
			{
				// just return the token value
				return (double)Token.value;
			}
			else if (Token.type == TokenType.Variable)
			{
				// check index for validity and return the correct variable value
				int idx = (int)Token.value;
				if (variables == null)
				{
					throw new ArgumentNullException("variables", $"Expression tried to access variable with index { idx }, but no variables are given.");
				}
				if (idx < 0 || idx >= variables.Length)
				{
					throw new ArgumentOutOfRangeException("", $"Expression tried to access variable with index { idx }, but only { variables.Length } variable{ (variables.Length == 1 ? " is" : "s are") } given.");
				}
				return variables[idx];
			}
			else if (Token.type == TokenType.Operator)
			{
				// values of child nodes
				// needs to be reversed because CalculateOperation(string, double[]) calculates args[1] op args[0]
				// but children are stored as child[0] op child[1]
				var args = m_children.Reverse<ExpressionTreeNode>();
				// only take as many nodes as needed
				args = args.Take(Expression.GetOperatorArity(Token.token));
				// evaluate them
				var values = args.Select(node => node.Evaluate(variables)).ToArray();
				// calculate the operation
				return Expression.CalculateOperation(Token.token, values);
			}
			throw new InvalidOperationException($"Invalid token { Token } was found!");
		}
	}
}
