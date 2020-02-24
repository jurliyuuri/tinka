using System;
using System.Collections.Generic;
using System.Linq;

namespace Tinka.Translator
{
    public abstract class SyntaxNode { }

    public abstract class ExpressionNode : SyntaxNode
    {
        public TokenType Operator { get; set; }

        protected ExpressionNode() : base() { }
    }

    public class XokNode : SyntaxNode
    {
        public IdentifierNode FunctionName { get; set; }

        public XokNode() : base()
        {
            FunctionName = IdentifierNode.Empty;
        }

        public override string ToString()
        {
            return $"Xok(FunctionName: {FunctionName})";
        }
    }

    public class KueNode : SyntaxNode
    {
        public IdentifierNode FunctionName { get; set; }

        public KueNode() : base()
        {
            FunctionName = IdentifierNode.Empty;
        }

        public override string ToString()
        {
            return $"Kue(FunctionName: {FunctionName})";
        }
    }

    public class CersvaNode : SyntaxNode
    {
        public IdentifierNode Name { get; set; }
        public IList<AnaxNode>? Arguments { get; set; }
        public IList<SyntaxNode> Syntaxes { get; set; }

        public CersvaNode() : base()
        {
            Name = IdentifierNode.Empty;
            Syntaxes = new List<SyntaxNode>();
        }

        public override string ToString()
        {
            string arguments = Arguments is null ? "" : string.Join(", ", Arguments.Select(x => x.ToString()));
            string syntaxes = string.Join(", ", Syntaxes.Select(x => x.ToString()));

            return $"Cersva(Name: {Name}, Arguments: [{arguments}], Syntaxes: [{syntaxes}])";
        }
    }

    public class DosnudNode : SyntaxNode
    {
        public ExpressionNode? Expression { get; set; }

        public DosnudNode() : base() { }

        public override string ToString()
        {
            if (Expression is null)
            {
                return "Dosnud()";
            }
            else
            {
                return $"Dosnud(Expression: {Expression})";
            }
        }
    }

    public class AnaxNode : SyntaxNode
    {
        public IdentifierNode Name { get; set; }
        public ExpressionNode? Expression { get; set; }
        public ConstantNode Length { get; set; }

        public AnaxNode() : base()
        {
            Name = IdentifierNode.Empty;
            Length = new ConstantNode
            {
                Value = "1",
            };
        }

        public override string ToString()
        {
            string expression = Expression?.ToString() ?? "null";
            
            return $"Anax(Name: {Name}, Expression: {expression}, Length: {Length})";
        }
    }

    public abstract class ControlNode : SyntaxNode
    {
        public ExpressionNode CompareExpression { get; set; }
        public IList<SyntaxNode> Syntaxes { get; set; }

        public ControlNode() : base()
        {
            CompareExpression = ConstantNode.Empty;
            Syntaxes = new List<SyntaxNode>();
        }
    }

    public class FiNode : ControlNode
    {
        public FiNode() : base() { }

        public override string ToString()
        {
            string syntaxes = string.Join(", ", Syntaxes.Select(x => x.ToString()));

            return $"Fi(CompareExpression: {CompareExpression}, Syntaxes: [{syntaxes}])";
        }
    }

    public class FalNode : ControlNode
    {
        public FalNode() : base() { }

        public override string ToString()
        {
            string syntaxes = Syntaxes == null ? "" : string.Join(", ", Syntaxes.Select(x => x.ToString()));

            return $"Fal(CompareExpression: {CompareExpression}, Syntaxes: [{syntaxes}])";
        }
    }

    public class ElNode : SyntaxNode
    {
        public ExpressionNode Left { get; set; }
        public ExpressionNode Right { get; set; }

        public ElNode() : base()
        {
            Left = ConstantNode.Empty;
            Right = ConstantNode.Empty;
        }

        public override string ToString()
        {
            return $"El(Left: {Left}, Right: {Right})";
        }
    }

    public class EksaNode : SyntaxNode
    {
        public ExpressionNode Left { get; set; }
        public ExpressionNode Right { get; set; }

        public EksaNode() : base()
        {
            Left = ConstantNode.Empty;
            Right = ConstantNode.Empty;
        }

        public override string ToString()
        {
            return $"Eksa(Left: {Left}, Right: {Right})";
        }
    }

    public class BiOperatorNode : ExpressionNode
    {
        public ExpressionNode Left { get; set; }
        public ExpressionNode Right { get; set; }

        public BiOperatorNode() : base()
        {
            Left = ConstantNode.Empty;
            Right = ConstantNode.Empty;
        }

        public override string ToString()
        {
            return $"BiOperator(Operator: \"{Operator}\", Left: {Left}, Right: {Right})";
        }
    }

    public class MonoOperatorNode : ExpressionNode
    {
        public ExpressionNode Value { get; set; }

        public MonoOperatorNode() : base()
        {
            Value = ConstantNode.Empty;
        }

        public override string ToString()
        {
            return $"MonoOperator(Operator: \"{Operator}\", Value: {Value})";
        }
    }

    public class ConstantNode : ExpressionNode
    {
        public readonly static ConstantNode Empty;

        static ConstantNode()
        {
            Empty = new ConstantNode();
        }

        public string Value { get; set; }

        public ConstantNode() : base()
        {
            Value = string.Empty;
        }

        public override bool Equals(object? obj)
        {
            if (!(obj is ConstantNode ident))
            {
                return false;
            }
            else
            {
                return Value == ident.Value;
            }
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }

        public override string ToString()
        {
            return $"Constant(Value: \"{Value}\")";
        }
    }

    public class FenxeNode : ExpressionNode
    {
        public IdentifierNode Name { get; set; }
        public IList<ExpressionNode>? Arguments { get; set; }

        public FenxeNode() : base()
        {
            Name = IdentifierNode.Empty;
        }

        public override string ToString()
        {
            string arguments = Arguments == null ? "" : string.Join(", ", Arguments.Select(x => x.ToString()));

            return $"Fenxe(Name: {Name}, Arguments[{arguments}])";
        }
    }
    
    public class IdentifierNode : ExpressionNode
    {
        public readonly static IdentifierNode Empty;

        static IdentifierNode()
        {
            Empty = new IdentifierNode();
        }

        public string Value { get; set; }

        public IdentifierNode() : base()
        {
            Value = string.Empty;
        }
        
        public override bool Equals(object? obj)
        {
            if (!(obj is IdentifierNode ident))
            {
                return false;
            }
            else
            {
                return Value == ident.Value;
            }
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }

        public override string ToString()
        {
            return $"Identifier(Value: \"{Value}\")";
        }
    }
}
