using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tinka.Translator
{
    public abstract class SyntaxNode
    {
    }

    public abstract class ExpressionNode : SyntaxNode
    {
        protected ExpressionNode() : base()
        {
        }
    }

    public class XokNode : SyntaxNode
    {
        public IdentifierNode FunctionName { get; set; }

        public XokNode() : base()
        {
        }

        public override string ToString()
        {
            return $"Xok(FunctionName: {this.FunctionName})";
        }
    }

    public class KueNode : SyntaxNode
    {
        public IdentifierNode FunctionName { get; set; }

        public KueNode() : base()
        {
        }

        public override string ToString()
        {
            return $"Kue(FunctionName: {this.FunctionName})";
        }
    }

    public class CersvaNode : SyntaxNode
    {
        public IdentifierNode Name { get; set; }
        public IList<AnaxNode> Arguments { get; set; }
        public IList<SyntaxNode> Syntaxes { get; set; }

        public CersvaNode() : base()
        {
        }

        public override string ToString()
        {
            string syntaxes = this.Syntaxes == null ? "" : string.Join(", ", this.Syntaxes.Select(x => x.ToString()));
            string arguments = this.Arguments == null ? "" : string.Join(", ", this.Arguments.Select(x => x.ToString()));

            return $"Cersva(Name: {this.Name}, Arguments: [{arguments}], Syntaxes: [{syntaxes}])";
        }
    }

    public class DosnudNode : SyntaxNode
    {
        public ExpressionNode Expression { get; set; }

        public DosnudNode() : base()
        {
        }

        public override string ToString()
        {
            if (this.Expression == null)
            {
                return "Dosnud(Expression: null)";
            }
            else
            {
                return $"Dosnud(Expression: {this.Expression})";
            }
        }
    }

    public class AnaxNode : SyntaxNode
    {
        public IdentifierNode Name { get; set; }
        public ExpressionNode Expression { get; set; }

        public AnaxNode() : base()
        {
        }

        public override string ToString()
        {
            if (this.Expression == null)
            {
                return $"Anax(Name: {this.Name}, Code: null)";
            }
            else {
                return $"Anax(Name: {this.Name}, Code: {this.Expression})";
            }
        }
    }

    public class ElNode : SyntaxNode
    {
        public IdentifierNode Left { get; set; }
        public ExpressionNode Right { get; set; }

        public ElNode() : base()
        {
        }

        public override string ToString()
        {
            return $"El(Left: {this.Left}, Right: {this.Right})";
        }
    }

    public class EksaNode : SyntaxNode
    {
        public ExpressionNode Left { get; set; }
        public IdentifierNode Right { get; set; }

        public EksaNode() : base()
        {
        }

        public override string ToString()
        {
            return $"Eksa(Left: {this.Left}, Right: {this.Right})";
        }
    }

    public class BiOperatorNode : ExpressionNode
    {
        public TokenType Operator { get; set; }
        public ExpressionNode Left { get; set; }
        public ExpressionNode Right { get; set; }

        public BiOperatorNode() : base()
        {
        }

        public override string ToString()
        {
            return $"BiOperator(Operator: \"{this.Operator}\", Left: {this.Left}, Right: {this.Right})";
        }
    }

    public class MonoOperatorNode : ExpressionNode
    {
        public TokenType Operator { get; set; }
        public ExpressionNode Value { get; set; }

        public MonoOperatorNode() : base()
        {
        }

        public override string ToString()
        {
            return $"MonoOperator(Operator: \"{this.Operator}\", Value: {this.Value})";
        }
    }

    public class ConstantNode : ExpressionNode
    {
        public string Value { get; set; }

        public ConstantNode() : base()
        {
        }

        public override string ToString()
        {
            return $"Constant(Value: \"{this.Value}\")";
        }
    }

    public class FenxeNode : ExpressionNode
    {
        public IdentifierNode Name { get; set; }
        public IList<ExpressionNode> Arguments { get; set; }

        public FenxeNode() : base()
        {
        }

        public override string ToString()
        {
            string arguments = this.Arguments == null ? "" : string.Join(", ", this.Arguments.Select(x => x.ToString()));

            return $"Fenxe(Name: {this.Name}, Arguments[{arguments}])";
        }
    }
    
    public class IdentifierNode : ExpressionNode
    {
        public string Value { get; set; }

        public IdentifierNode() : base()
        {
        }
        
        public override bool Equals(object obj)
        {
            var ident = obj as IdentifierNode;

            if(ident is null)
            {
                return false;
            }
            else
            {
                return this.Value == ident.Value;
            }
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return $"Identifier(Value: \"{this.Value}\")";
        }
    }
}
