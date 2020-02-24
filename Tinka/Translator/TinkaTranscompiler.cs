using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Tinka.Translator
{
    public abstract class TinkaTranscompiler
    {
        protected IList<IdentifierNode> FunctionIdentifiers { get; }
        protected List<SyntaxNode> Nodes { get; }
        protected IDictionary<IdentifierNode, uint> GlobalVariables { get; }

        public TinkaTranscompiler()
        {
            FunctionIdentifiers = new List<IdentifierNode>();
            Nodes = new List<SyntaxNode>();
            GlobalVariables = new Dictionary<IdentifierNode, uint>();
        }

        public void Input(IList<string> fileNames)
        {
            foreach (var fileName in fileNames)
            {
                if (!File.Exists(fileName))
                {
                    throw new ApplicationException($"Not found '{fileName}'");
                }

                using StreamReader reader = File.OpenText(fileName);
                Input(reader);
            }
        }

        public void Input(TextReader reader)
        {
            IList<string> words = Splitter(reader);
            Console.Write("words: ");
            ListingValues(words);
            Console.WriteLine();

            IList<Token> tokens = Tokenizer(words);
            Console.Write("tokens: ");
            ListingValues(tokens, ", ");
            Console.WriteLine();

            Nodes.AddRange(Parser(tokens));
            Console.Write("nodes: ");
            ListingValues(Nodes, ", ");
            Console.WriteLine();
        }

        #region Output
        
        public void Output(string fileName)
        {
            using var stream = File.Create(fileName);
            Output(stream);
        }

        public void Output(Stream stream)
        {
            Preprocess(stream);

            // AnaxNodeはPreprocess内で領域の確保等を行うため不要
            foreach (var node in Nodes.Where(x => !(x is AnaxNode)))
            {
                if (node is XokNode xokNode)
                {
                    ToXok(xokNode);
                }
                else if (node is KueNode kueNode)
                {
                    ToKue(kueNode);
                }
                else if (node is CersvaNode cersvaNode)
                {
                    ToCersva(cersvaNode);
                }
                else
                {
                    throw new NotSupportedException($"Unknown node: {node}");
                }
            }

            Postprocess();
        }

        protected void OutputSyntax(IList<SyntaxNode> expressions, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            // AnaxNodeはToCersva内で領域の確保等を行うため不要
            foreach (var node in expressions.Where(x => !(x is AnaxNode)))
            {
                if (node is DosnudNode dosnudNode)
                {
                    ToDosnud(dosnudNode, anaxDictionary);
                }
                else if (node is FiNode fiNode)
                {
                    // TODO: スコープ内変数の領域を確保する
                    ToFi(fiNode, anaxDictionary);
                    // TODO: スコープ内変数を領域を解放する
                }
                else if (node is FalNode falNode)
                {
                    // TODO: スコープ内変数の領域を確保する
                    ToFal(falNode, anaxDictionary);
                    // TODO: スコープ内変数を領域を解放する
                }
                else if (node is ElNode elNode)
                {
                    ToEl(elNode.Left, elNode.Right, anaxDictionary);
                }
                else if (node is EksaNode eksaNode)
                {
                    ToEksa(eksaNode.Left, eksaNode.Right, anaxDictionary);
                }
                else
                {
                    throw new NotSupportedException($"Unknown node: {node}");
                }
            }
        }

        protected void OutputExpression(ExpressionNode expression, string register, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            if (expression is MonoOperatorNode monoNode)
            {
                switch (monoNode.Operator)
                {
                    case TokenType.SNA:
                        ToSna(monoNode.Value, anaxDictionary);
                        break;
                    case TokenType.NAC:
                        ToNac(monoNode.Value, anaxDictionary);
                        break;
                    default:
                        throw new ApplicationException($"Unknown MonoOperandNode: {monoNode.Operator}");
                }
            }
            else if (expression is BiOperatorNode biNode)
            {
                switch (biNode.Operator)
                {
                    case TokenType.ARRAY_SIGN:
                        ToArrayPos(biNode.Left, biNode.Right, anaxDictionary);
                        break;
                    case TokenType.ATA:
                        ToAta(biNode.Left, biNode.Right, anaxDictionary);
                        break;
                    case TokenType.NTA:
                        ToNta(biNode.Left, biNode.Right, anaxDictionary);
                        break;
                    case TokenType.LAT:
                        ToLat(biNode.Left, biNode.Right, anaxDictionary);
                        break;
                    case TokenType.LATSNA:
                        ToLatsna(biNode.Left, biNode.Right, anaxDictionary);
                        break;
                    case TokenType.KAK:
                    case TokenType.KAKSNA:
                        break;
                    case TokenType.ADA:
                        ToAda(biNode.Left, biNode.Right, anaxDictionary);
                        break;
                    case TokenType.EKC:
                        ToEkc(biNode.Left, biNode.Right, anaxDictionary);
                        break;
                    case TokenType.DAL:
                        ToDal(biNode.Left, biNode.Right, anaxDictionary);
                        break;
                    case TokenType.XTLO:
                    case TokenType.XYLO:
                    case TokenType.CLO:
                    case TokenType.NIV:
                    case TokenType.LLO:
                    case TokenType.XOLO:
                    case TokenType.XTLONYS:
                    case TokenType.XYLONYS:
                    case TokenType.LLONYS:
                    case TokenType.XOLONYS:
                        ToCompare(biNode.Operator, biNode.Left, biNode.Right, anaxDictionary);
                        break;
                    case TokenType.DTO:
                        ToDto(biNode.Left, biNode.Right, anaxDictionary);
                        break;
                    case TokenType.DRO:
                        ToDro(biNode.Left, biNode.Right, anaxDictionary);
                        break;
                    case TokenType.DTOSNA:
                        ToDtosna(biNode.Left, biNode.Right, anaxDictionary);
                        break;
                    default:
                        throw new ApplicationException($"Unknown BiOperandNode: {biNode.Operator}");
                }
            }
            else if (expression is IdentifierNode identifierNode)
            {
                ToVariable(identifierNode, register, anaxDictionary);
            }
            else if (expression is ConstantNode constantNode)
            {
                ToConstant(constantNode, register);
            }
            else if (expression is FenxeNode fenxeNode)
            {
                ToFenxe(fenxeNode, register, anaxDictionary);
            }
            else
            {
                throw new ApplicationException($"Unknown ExpressionNode: {expression.GetType()?.Name}");
            }
        }
        
        protected void OutputValue(ExpressionNode expression, string register, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            if (expression is IdentifierNode identifierNode)
            {
                ToVariable(identifierNode, register, anaxDictionary);
            }
            else if (expression is ConstantNode constantNode)
            {
                ToConstant(constantNode, register);
            }
        }

#endregion

        protected abstract void Preprocess(Stream stream);

        protected abstract void Postprocess();

        protected abstract void ToXok(XokNode node);

        protected abstract void ToKue(KueNode node);

        protected abstract void ToCersva(CersvaNode node);

        protected abstract void ToDosnud(DosnudNode node, IDictionary<IdentifierNode, uint> anaxDictionary);

        protected abstract void ToAnax(AnaxNode node, IDictionary<IdentifierNode, uint> anaxDictionary);

        protected abstract void ToFi(FiNode node, IDictionary<IdentifierNode, uint> anaxDictionary);

        protected abstract void ToFal(FalNode node, IDictionary<IdentifierNode, uint> anaxDictionary);

        protected abstract void ToEl(ExpressionNode left, ExpressionNode right, IDictionary<IdentifierNode, uint> anaxDictionary);

        protected abstract void ToEksa(ExpressionNode left, ExpressionNode right, IDictionary<IdentifierNode, uint> anaxDictionary);

        protected abstract void ToAta(ExpressionNode left, ExpressionNode right, IDictionary<IdentifierNode, uint> anaxDictionary);

        protected abstract void ToNta(ExpressionNode left, ExpressionNode right, IDictionary<IdentifierNode, uint> anaxDictionary);

        protected abstract void ToLat(ExpressionNode left, ExpressionNode right, IDictionary<IdentifierNode, uint> anaxDictionary);

        protected abstract void ToLatsna(ExpressionNode left, ExpressionNode right, IDictionary<IdentifierNode, uint> anaxDictionary);

        protected abstract void ToAda(ExpressionNode left, ExpressionNode right, IDictionary<IdentifierNode, uint> anaxDictionary);

        protected abstract void ToEkc(ExpressionNode left, ExpressionNode right, IDictionary<IdentifierNode, uint> anaxDictionary);

        protected abstract void ToDal(ExpressionNode left, ExpressionNode right, IDictionary<IdentifierNode, uint> anaxDictionary);

        protected abstract void ToSna(ExpressionNode value, IDictionary<IdentifierNode, uint> anaxDictionary);

        protected abstract void ToNac(ExpressionNode value, IDictionary<IdentifierNode, uint> anaxDictionary);

        protected abstract void ToDto(ExpressionNode left, ExpressionNode right, IDictionary<IdentifierNode, uint> anaxDictionary);

        protected abstract void ToDro(ExpressionNode left, ExpressionNode right, IDictionary<IdentifierNode, uint> anaxDictionary);

        protected abstract void ToDtosna(ExpressionNode left, ExpressionNode right, IDictionary<IdentifierNode, uint> anaxDictionary);

        protected abstract void ToCompare(TokenType type, ExpressionNode left, ExpressionNode right, IDictionary<IdentifierNode, uint> anaxDictionary);

        protected abstract void ToArrayPos(ExpressionNode left, ExpressionNode right, IDictionary<IdentifierNode, uint> anaxDictionary);

        protected abstract void ToConstant(ConstantNode constant, string register);

        protected abstract void ToVariable(IdentifierNode identifier, string register, IDictionary<IdentifierNode, uint> anaxDictionary);

        protected abstract void ToFenxe(FenxeNode fenxe, string register, IDictionary<IdentifierNode, uint> anaxDictionary);

        #region Splitter

        private IList<string> Splitter(TextReader reader)
        {
            var words = new List<string>();
            var buffer = new StringBuilder();

            void AppendToken()
            {
                if (buffer.Length > 0)
                {
                    words.Add(buffer.ToString());
                    buffer.Clear();
                }
            }

            var token = 0;
            while ((token = reader.Read()) != -1)
            {
                if(token == '-')
                {
                    token = reader.Read();
                    if (token == '-')
                    {
                        AppendToken();
                        do
                        {
                            token = reader.Read();
                            if(token == -1)
                            {
                                throw new ApplicationException("End of file in comment");
                            }
                        } while (token != '\r' && token != '\n');
                    }
                    else
                    {
                        buffer.Append('-');
                    }
                }

                if(char.IsWhiteSpace((char)token))
                {
                    AppendToken();
                }
                else
                {
                    switch (token)
                    {
                        case ':':
                        case '(':
                        case ')':
                        case ',':
                        case '+':
                        case '|':
                        case '#':
                            AppendToken();
                            words.Add(((char)token).ToString());
                            break;
                        default:
                            buffer.Append((char)token);
                            break;
                    }
                }
            }

            AppendToken();

            return words;
        }

        #endregion

        #region Tokenizer

        private IList<Token> Tokenizer(IList<string> words)
        {
            var tokens = new List<Token>();

            foreach (var word in words)
            {
                switch (word)
                {
                    case "xok":
                    case "kue":
                    case "cersva":
                    case "dosnud":
                    case "anax":
                    case "el":
                    case "eksa":
                    case "fi":
                    case "fal":
                    case "rinyv":
                    case "situv":
                        tokens.Add(new Token
                        {
                            Type = Enum.Parse<TokenType>(word, true),
                        });
                        break;
                    case "(":
                        tokens.Add(new Token
                        {
                            Type = TokenType.L_CIRC,
                        });
                        break;
                    case ")":
                        tokens.Add(new Token
                        {
                            Type = TokenType.R_CIRC,
                        });
                        break;
                    case "#":
                        tokens.Add(new Token
                        {
                            Type = TokenType.VARIABLE_SIGN,
                        });
                        break;
                    case ",":
                        tokens.Add(new Token
                        {
                            Type = TokenType.COMMA,
                        });
                        break;
                    case ":":
                        tokens.Add(new Token
                        {
                            Type = TokenType.ARRAY_SIGN,
                        });
                        break;
                    case "+":
                        tokens.Add(new Token
                        {
                            Type = TokenType.ATA,
                        });
                        break;
                    case "|":
                        tokens.Add(new Token
                        {
                            Type = TokenType.NTA,
                        });
                        break;
                    case "lat":
                    case "latsna":
                    case "kak":
                    case "kaksna":
                    case "ada":
                    case "ekc":
                    case "dal":
                    case "nac":
                    case "sna":
                    case "dto":
                    case "dro":
                    case "dRo":
                    case "dtosna":
                    case "xtlo":
                    case "xylo":
                    case "clo":
                    case "niv":
                    case "llo":
                    case "xolo":
                    case "xtlonys":
                    case "xylonys":
                    case "llonys":
                    case "xolonys":
                        tokens.Add(new Token
                        {
                            Type = Enum.Parse<TokenType>(word, true),
                        });
                        break;
                    default:
                        if(word.All(char.IsDigit))
                        {
                            tokens.Add(new Integer
                            {
                                Value = word,
                            }); 
                        }
                        else
                        {
                            tokens.Add(new Identifier
                            {
                                Name = word,
                            });
                        }
                        break;
                }
            }

            return tokens;
        }

        #endregion

        #region Parser

        private IList<SyntaxNode> Parser(IList<Token> tokens)
        {
            var nodes = new List<SyntaxNode>();

            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];

                switch (token.Type)
                {
                    case TokenType.XOK:
                        {
                            token = tokens[++i];
                            if (token is Identifier identifier)
                            {
                                if (identifier.Name[0] == '#')
                                {
                                    throw new ApplicationException($"Invalid cersva name: {identifier.Name}");
                                }

                                nodes.Add(new XokNode
                                {
                                    FunctionName = new IdentifierNode
                                    {
                                        Value = identifier.Name,
                                    },
                                });
                            }
                            else
                            {
                                throw new ApplicationException($"Invalid value: xok {token}");
                            }
                        }
                        break;
                    case TokenType.KUE:
                        {
                            token = tokens[++i];
                            if (token is Identifier identifier)
                            {
                                if (identifier.Name[0] == '#')
                                {
                                    throw new ApplicationException($"Invalid cersva name: {identifier.Name}");
                                }

                                nodes.Add(new KueNode
                                {
                                    FunctionName = new IdentifierNode
                                    {
                                        Value = identifier.Name,
                                    },
                                });
                            }
                            else
                            {
                                throw new ApplicationException($"Invalid value: kue {token}");
                            }
                        }
                        break;
                    case TokenType.CERSVA:
                        nodes.Add(GetCersvaNode(tokens, ref i));
                        break;
                    case TokenType.ANAX:
                        i++;
                        nodes.Add(GetAnaxNode(tokens, ref i));
                        i--;
                        break;
                    default:
                        break;
                }
            }

            return nodes;
        }

        private CersvaNode GetCersvaNode(IList<Token> tokens, ref int index)
        {
            var cersvaNode = new CersvaNode();
            Token token = tokens[++index];

            if(token is Identifier identifier)
            {
                string name = identifier.Name;

                if (name.All(char.IsDigit) && char.IsDigit((name ?? "0").FirstOrDefault()))
                {
                    throw new ApplicationException($"Invalid identifier: cersva {name}");
                }
                else
                {
                    cersvaNode.Name = new IdentifierNode
                    {
                        Value = name!
                    };

                    FunctionIdentifiers.Add(cersvaNode.Name);
                }
            }
            else
            {
                throw new ApplicationException($"Invalid value: cersva {token}");
            }

            token = tokens[++index];
            if(token.Type != TokenType.L_CIRC)
            {
                throw new ApplicationException($"Not found '(': cersva {cersvaNode.Name.Value}");
            }

            index++;
            cersvaNode.Arguments = GetArguments(tokens, ref index);

            token = tokens[index];
            if (token.Type != TokenType.R_CIRC)
            {
                throw new ApplicationException($"Not found ')': cersva {cersvaNode.Name.Value}");
            }

            token = tokens[++index];
            if (token.Type != TokenType.RINYV)
            {
                throw new ApplicationException($"Not found 'rinyv': cersva {cersvaNode.Name.Value}");
            }

            index++;
            cersvaNode.Syntaxes = GetSyntaxNode(tokens, ref index);
            
            token = tokens[index];
            if (token.Type != TokenType.SITUV)
            {
                throw new ApplicationException($"Not found 'situv': cersva {cersvaNode.Name.Value}");
            }

            return cersvaNode;
        }

        private List<AnaxNode> GetArguments(IList<Token> tokens, ref int index)
        {
            List<AnaxNode> anaxNodeList = new List<AnaxNode>();
            int count = tokens.Count;

            if (tokens[index].Type == TokenType.R_CIRC)
            {
                return anaxNodeList;
            }

            while (index < count)
            {
                AnaxNode anaxNode = new AnaxNode();
                Token token = tokens[index];
                if (token is Identifier identifier)
                {
                    string name = identifier.Name;

                    if (name.All(char.IsDigit) && char.IsDigit((name ?? "0").FirstOrDefault()))
                    {
                        throw new ApplicationException($"Invalid identifier: anax {name}");
                    }
                    else
                    {
                        anaxNode.Name = new IdentifierNode
                        {
                            Value = name!
                        };

                        anaxNodeList.Add(anaxNode);
                    }
                }
                else
                {
                    throw new ApplicationException($"Invalid value: anax {token}");
                }

                index++;
                if (index < count)
                {
                    token = tokens[index];

                    if(token.Type == TokenType.COMMA)
                    {
                        index++;
                    }
                    else if(token.Type == TokenType.R_CIRC) {
                        break;
                    }
                    else
                    {
                        throw new ApplicationException($"Invalid value: {token}");
                    }
                }
                else
                {
                    throw new ApplicationException($"End of file in {anaxNode}");
                }
            }

            return anaxNodeList;
        }

        private AnaxNode GetAnaxNode(IList<Token> tokens, ref int index)
        {
            AnaxNode anaxNode = new AnaxNode();
            int count = tokens.Count;

            Token token = tokens[index];
            if(token is Identifier identifier)
            {
                string name = identifier.Name;

                if (name.All(char.IsDigit) && char.IsDigit((name ?? "0").FirstOrDefault()))
                {
                    throw new ApplicationException($"Invalid identifier: anax {name}");
                }
                else
                {
                    anaxNode.Name = new IdentifierNode
                    {
                        Value = name!
                    };
                }
            }
            else
            {
                throw new ApplicationException($"Invalid value: anax {token}");
            }

            index++;
            if(index < count && tokens[index].Type == TokenType.ARRAY_SIGN)
            {
                index++;

                ExpressionNode? node = GetValueNode(tokens, ref index);

                if(node is ConstantNode constantNode)
                {
                    anaxNode.Length = constantNode;
                }
                else
                {
                    throw new ApplicationException("Array length is only constant value");
                }
            }

            if (index < count && tokens[index].Type == TokenType.EL)
            {
                index++;
                anaxNode.Expression = GetCompareNode(tokens, ref index);
            }

            return anaxNode;
        }

        ControlNode GetControlNode(IList<Token> tokens, ref int index)
        {
            ControlNode controlNode;
            Token token = tokens[index++];
            int count = tokens.Count;

            switch(token.Type)
            {
                case TokenType.FI:
                    controlNode = new FiNode();
                    break;
                case TokenType.FAL:
                    controlNode = new FalNode();
                    break;
                default:
                    throw new ApplicationException($"Invalid token: {tokens}");
            }

            if (index >= count)
            {
                throw new ApplicationException("End of code in fi syntax");
            }

            var compare = GetCompareNode(tokens, ref index);
            if (compare == null)
            {
                throw new ApplicationException("Not found compare expression");
            }
            controlNode.CompareExpression = compare;

            token = tokens[index];
            if (token.Type != TokenType.RINYV)
            {
                throw new ApplicationException($"Not found 'rinyv': fi {controlNode.CompareExpression}");
            }

            index++;
            controlNode.Syntaxes = GetSyntaxNode(tokens, ref index);

            token = tokens[index];
            if (token.Type != TokenType.SITUV)
            {
                throw new ApplicationException($"Not found 'situv': fi {controlNode.CompareExpression}");
            }

            index++;

            return controlNode;
        }

        private IList<SyntaxNode> GetSyntaxNode(IList<Token> tokens, ref int index)
        {
            var nodes = new List<SyntaxNode>();
            int count = tokens.Count;

            while (index < count)
            {
                Token token = tokens[index];
                SyntaxNode? node;

                switch (token.Type)
                {
                    case TokenType.DOSNUD:
                        index++;
                        node = GetCompareNode(tokens, ref index);

                        node = new DosnudNode
                        {
                            Expression = node as ExpressionNode,
                        };
                        break;
                    case TokenType.ANAX:
                        index++;
                        node = GetAnaxNode(tokens, ref index);
                        break;
                    case TokenType.FI:
                    case TokenType.FAL:
                        node = GetControlNode(tokens, ref index);
                        break;
                    case TokenType.RINYV:
                    case TokenType.SITUV:
                        return nodes;
                    default:
                        node = GetEksaNode(tokens, ref index);
                        break;
                }

                if(node != null)
                {
                    nodes.Add(node);
                };
            }

            return nodes;
        }

        private SyntaxNode? GetEksaNode(IList<Token> tokens, ref int index)
        {
            ExpressionNode? node = GetCompareNode(tokens, ref index);
            Token token = tokens[index];

            switch (token.Type)
            {
                case TokenType.EL:
                    index++;
                    {
                        var right = GetCompareNode(tokens, ref index);

                        if (node == null || right == null)
                        {
                            throw new ApplicationException($"Invalid arguments: {token.Type}(Left: {node}, Right: {right})");
                        }
                        else
                        {
                            if (!(node is IdentifierNode || node.Operator == TokenType.ARRAY_SIGN))
                            {
                                throw new ApplicationException($"Invalid arguments: {token.Type}(Left: {node}, Right: {right})");
                            }

                            return new ElNode
                            {
                                Left = node,
                                Right = right,
                            };
                        }
                    }
                case TokenType.EKSA:
                    index++;
                    {
                        var right = GetCompareNode(tokens, ref index);

                        if (node == null || right == null)
                        {
                            throw new ApplicationException($"Invalid arguments: {token.Type}(Left: {node}, Right: {right})");
                        }
                        else
                        {
                            if (!(node is IdentifierNode || node.Operator == TokenType.ARRAY_SIGN))
                            {
                                throw new ApplicationException($"Invalid arguments: {token.Type}(Left: {node}, Right: {right})");
                            }

                            return new EksaNode
                            {
                                Left = node,
                                Right = right,
                            };
                        }
                    }
                default:
                    return node;
            }
        }

        ExpressionNode? GetCompareNode(IList<Token> tokens, ref int index)
        {
            ExpressionNode? node = GetBitOperatorNode(tokens, ref index);
            int count = tokens.Count;

            while (index < count)
            {
                Token token = tokens[index];

                switch (token.Type)
                {
                    case TokenType.XTLO:
                    case TokenType.XYLO:
                    case TokenType.CLO:
                    case TokenType.NIV:
                    case TokenType.LLO:
                    case TokenType.XOLO:
                    case TokenType.XTLONYS:
                    case TokenType.XYLONYS:
                    case TokenType.LLONYS:
                    case TokenType.XOLONYS:
                        index++;
                        {
                            var right = GetBitOperatorNode(tokens, ref index);

                            if (right == null || node == null)
                            {
                                throw new ApplicationException($"Invalid arguments: {token.Type}(Left: {node}, Right: {right})");
                            }
                            else
                            {
                                node = new BiOperatorNode
                                {
                                    Operator = token.Type,
                                    Left = node,
                                    Right = right,
                                };
                            }
                        }
                        break;
                    default:
                        return node;
                }
            }

            return node;
        }

        private ExpressionNode? GetBitOperatorNode(IList<Token> tokens, ref int index)
        {
            ExpressionNode? node = GetShiftNode(tokens, ref index);
            int count = tokens.Count;

            while (index < count)
            {
                Token token = tokens[index];

                switch (token.Type)
                {
                    case TokenType.ADA:
                    case TokenType.EKC:
                    case TokenType.DAL:
                        index++;
                        {
                            var right = GetShiftNode(tokens, ref index);

                            if (right == null || node == null)
                            {
                                throw new ApplicationException($"Invalid arguments: {token.Type}(Left: {node}, Right: {right})");
                            }
                            else
                            {
                                node = new BiOperatorNode
                                {
                                    Operator = token.Type,
                                    Left = node,
                                    Right = right,
                                };
                            }
                        }
                        break;
                    default:
                        return node;
                }
            }

            return node;
        }


        private ExpressionNode? GetShiftNode(IList<Token> tokens, ref int index)
        {
            ExpressionNode? node = GetExpressionNode(tokens, ref index);
            int count = tokens.Count;

            while (index < count)
            {
                Token token = tokens[index];

                switch (token.Type)
                {
                    case TokenType.DTO:
                    case TokenType.DTOSNA:
                    case TokenType.DRO:
                        index++;
                        {
                            var right = GetExpressionNode(tokens, ref index);

                            if (right == null || node == null)
                            {
                                throw new ApplicationException($"Invalid arguments: {token.Type}(Left: {node}, Right: {right})");
                            }
                            else
                            {
                                node = new BiOperatorNode
                                {
                                    Operator = token.Type,
                                    Left = node,
                                    Right = right,
                                };
                            }
                        }
                        break;
                    default:
                        return node;
                }
            }

            return node;
        }

        private ExpressionNode? GetExpressionNode(IList<Token> tokens, ref int index)
        {
            ExpressionNode? node = GetTermNode(tokens, ref index);
            int count = tokens.Count;

            while (index < count)
            {
                Token token = tokens[index];

                switch(token.Type)
                {
                    case TokenType.ATA:
                    case TokenType.NTA:
                        index++;
                        {
                            var right = GetTermNode(tokens, ref index);
                            if(right == null || node == null)
                            {
                                throw new ApplicationException($"Invalid arguments: {token.Type}(Left: {node}, Right: {right})");
                            }
                            else
                            {
                                node = new BiOperatorNode
                                {
                                    Operator = token.Type,
                                    Left = node,
                                    Right = right,
                                };
                            }
                        }
                        break;
                    default:
                        return node;
                }
            }

            return node;
        }

        private ExpressionNode? GetTermNode(IList<Token> tokens, ref int index)
        {
            ExpressionNode? node = GetMonoNode(tokens, ref index);
            int count = tokens.Count;

            while (index < count)
            {
                Token token = tokens[index];

                switch (token.Type)
                {
                    case TokenType.LAT:
                    case TokenType.LATSNA:
                        index++;
                        {
                            var right = GetMonoNode(tokens, ref index);

                            if (right == null || node == null)
                            {
                                throw new ApplicationException($"Invalid arguments: {token.Type}(Left: {node}, Right: {right})");
                            }
                            else
                            {
                                node = new BiOperatorNode
                                {
                                    Operator = token.Type,
                                    Left = node,
                                    Right = right,
                                };
                            }
                        }
                        break;
                    default:
                        return node;
                }
            }

            return node;
        }

        private ExpressionNode? GetMonoNode(IList<Token> tokens, ref int index)
        {
            ExpressionNode? node = null;
            int count = tokens.Count;

            if (index < count)
            {
                Token token = tokens[index];

                switch (token.Type)
                {
                    case TokenType.SNA:
                    case TokenType.NAC:
                        {
                            index++;

                            var mono = GetMonoNode(tokens, ref index);

                            if (mono is null)
                            {
                                throw new ApplicationException($"Invalid arguments: {token.Type}(Value: {mono})");
                            }
                            else
                            {
                                node = new MonoOperatorNode
                                {
                                    Operator = token.Type,
                                    Value = mono
                                };
                            }
                        }
                        break;
                    case TokenType.INTEGER:
                    case TokenType.VARIABLE_SIGN:
                    case TokenType.IDENTIFIER:
                        return GetArrayPosNode(tokens, ref index);
                    default:
                        return node;
                }
            }

            return node;
        }

        private ExpressionNode? GetArrayPosNode(IList<Token> tokens, ref int index)
        {
            ExpressionNode? node = GetValueNode(tokens, ref index);
            int count = tokens.Count;

            while (index < count)
            {
                Token token = tokens[index];

                switch (token.Type)
                {
                    case TokenType.ARRAY_SIGN:
                        index++;
                        {
                            var right = GetArrayPosNode(tokens, ref index);

                            if (right is null || node is null)
                            {
                                throw new ApplicationException($"Invalid arguments: {token.Type}(Left: {node}, Right: {right})");
                            }
                            else if (node is ConstantNode)
                            {
                                throw new ApplicationException($"Invalid arguments: {token.Type}(Left: {node}, Right: {right})");
                            }
                            else
                            {
                                node = new BiOperatorNode
                                {
                                    Operator = token.Type,
                                    Left = node,
                                    Right = right,
                                };
                            }
                        }
                        break;
                    default:
                        return node;
                }
            }

            return node;
        }

        private ExpressionNode? GetValueNode(IList<Token> tokens, ref int index)
        {
            ExpressionNode? node = null;
            Token token = tokens[index];

            switch (token.Type)
            {
                case TokenType.INTEGER:
                    node = new ConstantNode
                    {
                        Value = (token as Integer)!.Value,
                    };
                    break;
                case TokenType.VARIABLE_SIGN:
                    token = tokens[++index];
                    if(token is Identifier identifier)
                    {
                        node = new IdentifierNode
                        {
                            Value = identifier.Name
                        };
                    }
                    else
                    {
                        throw new ApplicationException($"Invalid token: {token}");
                    }
                    break;
                case TokenType.IDENTIFIER:
                    node = GetFenxe((token as Identifier)!, tokens, ref index);
                    break;
                default:
                    break;
            }

            index++;
            return node;
        }

        private ExpressionNode GetFenxe(Identifier identifier, IList<Token> tokens, ref int index)
        {
            var fenxe = new FenxeNode
            {
                Name = new IdentifierNode
                {
                    Value = (identifier as Identifier).Name,
                },
                Arguments = new List<ExpressionNode>(),
            };

            Token token = tokens[++index];
            if (token.Type != TokenType.L_CIRC)
            {
                throw new ApplicationException($"Not found '(': fenxe {fenxe.Name.Value}");
            }
            
            int count = tokens.Count;
            token = tokens[++index];
            while (index < count && token.Type != TokenType.R_CIRC)
            {
                ExpressionNode? expression = GetCompareNode(tokens, ref index);

                if(expression is null)
                {
                    throw new ApplicationException($"Invalid token: {token}");
                }
                else
                {
                    fenxe.Arguments.Add(expression);
                }

                token = tokens[index];

                if (token.Type == TokenType.R_CIRC)
                {
                    break;
                }
                else if (token.Type == TokenType.COMMA)
                {
                    index++;
                }
                else
                {
                    throw new ApplicationException($"Invalid token: {token}");
                }
            }

            if (token.Type != TokenType.R_CIRC)
            {
                throw new ApplicationException($"Not found ')': fenxe {fenxe.Name.Value}");
            }

            return fenxe;
        }

        #endregion

        #region Others

        private void ListingValues(IEnumerable<object> tokens, string separator = " ")
        {
            Console.WriteLine("[{0}]", string.Join(separator, tokens.Select(x => x.ToString())));
        }

        #endregion
    }
}
