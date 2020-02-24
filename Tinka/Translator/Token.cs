using System;
using System.Collections.Generic;
using System.Text;

namespace Tinka.Translator
{
    public class Token
    {
        public TokenType Type { get; set; }
        
        public override string ToString()
        {
            return $"{{ Type: {this.Type} }}";
        }
    }
    
    public class Identifier : Token
    {
        public string Name { get; set; }

        public Identifier() : base()
        {
            Name = string.Empty;
            Type = TokenType.IDENTIFIER;
        }

        public override string ToString()
        {
            return $"{{ Type: {this.Type}, Name: {this.Name} }}";
        }
    }

    public class Integer : Token
    {
        public string Value { get; set; }

        public Integer() : base()
        {
            Value = string.Empty;
            Type = TokenType.INTEGER;
        }
        
        public override string ToString()
        {
            return $"{{ Type: {this.Type}, Value: {this.Value} }}";
        }
    }
}
