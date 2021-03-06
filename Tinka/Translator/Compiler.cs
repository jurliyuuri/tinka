﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Tinka.Translator
{
    public abstract class Compiler
    {
        protected readonly List<IdentifierNode> functionIdentifiers;
        protected readonly List<SyntaxNode> nodes;
        protected readonly Dictionary<IdentifierNode, uint> globalVariables;

        public Compiler()
        {
            this.functionIdentifiers = new List<IdentifierNode>();
            this.nodes = new List<SyntaxNode>();
            this.globalVariables = new Dictionary<IdentifierNode, uint>();
        }

        public void Input(IList<string> fileNames)
        {
            foreach (var fileName in fileNames)
            {
                if (!File.Exists(fileName))
                {
                    throw new ApplicationException($"Not found '{fileName}'");
                }

                using (StreamReader reader = File.OpenText(fileName))
                {
                    Input(reader);
                }
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

            this.nodes.AddRange(Parser(tokens));
            Console.Write("nodes: ");
            ListingValues(this.nodes, ", ");
            Console.WriteLine();
        }

        #region Output
        
        public void Output(string fileName)
        {
            using (var stream = File.Create(fileName))
            {
                Output(stream);
            }
        }

        public void Output(Stream stream)
        {
            Preprocess(stream);

            // AnaxNodeはPreprocess内で領域の確保等を行うため不要
            foreach (var node in this.nodes.Where(x => !(x is AnaxNode)))
            {
                if (node is XokNode)
                {
                    ToXok(node as XokNode);
                }
                else if (node is KueNode)
                {
                    ToKue(node as KueNode);
                }
                else if (node is CersvaNode)
                {
                    ToCersva(node as CersvaNode);
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
                if (node is DosnudNode)
                {
                    ToDosnud(node as DosnudNode, anaxDictionary);
                }
                else if (node is FiNode)
                {
                    // TODO: スコープ内変数の領域を確保する
                    ToFi(node as FiNode, anaxDictionary);
                    // TODO: スコープ内変数を領域を解放する
                }
                else if (node is FalNode)
                {
                    // TODO: スコープ内変数の領域を確保する
                    ToFal(node as FalNode, anaxDictionary);
                    // TODO: スコープ内変数を領域を解放する
                }
                else if (node is ElNode)
                {
                    var biop = node as ElNode;
                    ToEl(biop.Left, biop.Right, anaxDictionary);
                }
                else if (node is EksaNode)
                {
                    var biop = node as EksaNode;
                    ToEksa(biop.Left, biop.Right, anaxDictionary);
                }
                else
                {
                    throw new NotSupportedException($"Unknown node: {node}");
                }
            }
        }

        protected void OutputExpression(ExpressionNode expression, string register, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            if (expression is MonoOperatorNode)
            {
                var monoop = expression as MonoOperatorNode;

                switch (monoop.Operator)
                {
                    case TokenType.SNA:
                        ToSna(monoop.Value, anaxDictionary);
                        break;
                    case TokenType.NAC:
                        ToNac(monoop.Value, anaxDictionary);
                        break;
                    default:
                        throw new ApplicationException($"Unknown MonoOperandNode: {monoop.Operator}");
                }
            }
            else if (expression is BiOperatorNode)
            {
                var biop = expression as BiOperatorNode;
                switch (biop.Operator)
                {
                    case TokenType.ARRAY_SIGN:
                        ToArrayPos(biop.Left, biop.Right, anaxDictionary);
                        break;
                    case TokenType.ATA:
                        ToAta(biop.Left, biop.Right, anaxDictionary);
                        break;
                    case TokenType.NTA:
                        ToNta(biop.Left, biop.Right, anaxDictionary);
                        break;
                    case TokenType.LAT:
                        ToLat(biop.Left, biop.Right, anaxDictionary);
                        break;
                    case TokenType.LATSNA:
                        ToLatsna(biop.Left, biop.Right, anaxDictionary);
                        break;
                    case TokenType.KAK:
                    case TokenType.KAKSNA:
                        break;
                    case TokenType.ADA:
                        ToAda(biop.Left, biop.Right, anaxDictionary);
                        break;
                    case TokenType.EKC:
                        ToEkc(biop.Left, biop.Right, anaxDictionary);
                        break;
                    case TokenType.DAL:
                        ToDal(biop.Left, biop.Right, anaxDictionary);
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
                        ToCompare(biop.Operator, biop.Left, biop.Right, anaxDictionary);
                        break;
                    case TokenType.DTO:
                        ToDto(biop.Left, biop.Right, anaxDictionary);
                        break;
                    case TokenType.DRO:
                        ToDro(biop.Left, biop.Right, anaxDictionary);
                        break;
                    case TokenType.DTOSNA:
                        ToDtosna(biop.Left, biop.Right, anaxDictionary);
                        break;
                    default:
                        throw new ApplicationException($"Unknown BiOperandNode: {biop.Operator}");
                }
            }
            else if (expression is IdentifierNode)
            {
                ToVariable(expression as IdentifierNode, register, anaxDictionary);
            }
            else if (expression is ConstantNode)
            {
                ToConstant(expression as ConstantNode, register);
            }
            else if (expression is FenxeNode)
            {
                ToFenxe(expression as FenxeNode, register, anaxDictionary);
            }
            else
            {
                throw new ApplicationException($"Unknown ExpressionNode: {expression.GetType()?.Name}");
            }
        }
        
        protected void OutputValue(ExpressionNode expression, string register, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            if (expression is IdentifierNode)
            {
                ToVariable(expression as IdentifierNode, register, anaxDictionary);
            }
            else if (expression is ConstantNode)
            {
                ToConstant(expression as ConstantNode, register);
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
                            Type = (TokenType)Enum.Parse(typeof(TokenType), word.ToUpper()),
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
                            Type = (TokenType)Enum.Parse(typeof(TokenType), word.ToUpper()),
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
                        token = tokens[++i];
                        if(token is Identifier)
                        {
                            var ident = token as Identifier;

                            if(ident.Name[0] == '#')
                            {
                                throw new ApplicationException($"Invalid cersva name: {ident.Name}");
                            }

                            nodes.Add(new XokNode
                            {
                                FunctionName = new IdentifierNode
                                {
                                    Value = ident.Name,
                                },
                            });
                        }
                        else
                        {
                            throw new ApplicationException($"Invalid value: xok {token}");
                        }
                        break;
                    case TokenType.KUE:
                        token = tokens[++i];
                        if (token is Identifier)
                        {
                            var ident = token as Identifier;

                            if (ident.Name[0] == '#')
                            {
                                throw new ApplicationException($"Invalid cersva name: {ident.Name}");
                            }

                            nodes.Add(new KueNode
                            {
                                FunctionName = new IdentifierNode
                                {
                                    Value = ident.Name,
                                },
                            });
                        }
                        else
                        {
                            throw new ApplicationException($"Invalid value: kue {token}");
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

            if(token != null)
            {
                string name = (token as Identifier).Name;

                if (name.All(char.IsDigit) && char.IsDigit((name ?? "0").FirstOrDefault()))
                {
                    throw new ApplicationException($"Invalid identifier: cersva {name}");
                }
                else
                {
                    cersvaNode.Name = new IdentifierNode
                    {
                        Value = name
                    };

                    this.functionIdentifiers.Add(cersvaNode.Name);
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
                if (token is Identifier)
                {
                    string name = (token as Identifier).Name;

                    if (name.All(char.IsDigit) && char.IsDigit((name ?? "0").FirstOrDefault()))
                    {
                        throw new ApplicationException($"Invalid identifier: anax {name}");
                    }
                    else
                    {
                        anaxNode.Name = new IdentifierNode
                        {
                            Value = name
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
            if(token is Identifier)
            {
                string name = (token as Identifier).Name;

                if (name.All(char.IsDigit) && char.IsDigit((name ?? "0").FirstOrDefault()))
                {
                    throw new ApplicationException($"Invalid identifier: anax {name}");
                }
                else
                {
                    anaxNode.Name = new IdentifierNode
                    {
                        Value = name
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

                ExpressionNode node = GetValueNode(tokens, ref index);

                if(node is ConstantNode)
                {
                    anaxNode.Length = node as ConstantNode;
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

            controlNode.CompareExpression = GetCompareNode(tokens, ref index);
            if (controlNode.CompareExpression == null)
            {
                throw new ApplicationException("Not found compare expression");
            }

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
                SyntaxNode node = null;

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

        private SyntaxNode GetEksaNode(IList<Token> tokens, ref int index)
        {
            ExpressionNode node = GetCompareNode(tokens, ref index);
            int count = tokens.Count;
            Token token = tokens[index];

            switch (token.Type)
            {
                case TokenType.EL:
                    index++;
                    {
                        var op = new ElNode
                        {
                            Left = node as ExpressionNode,
                            Right = GetCompareNode(tokens, ref index),
                        };

                        if (op.Left == null || !(op.Left is IdentifierNode || op.Left.Operator == TokenType.ARRAY_SIGN) || op.Right == null)
                        {
                            throw new ApplicationException($"Invalid arguments: {token.Type}(Left: {op.Left}, Right: {op.Right})");
                        }
                        else
                        {
                            return op;
                        }
                    }
                case TokenType.EKSA:
                    index++;
                    {
                        var op = new EksaNode
                        {
                            Left = node,
                            Right = GetCompareNode(tokens, ref index) as ExpressionNode,
                        };

                        if (op.Left == null || !(op.Right is IdentifierNode || op.Right.Operator == TokenType.ARRAY_SIGN) || op.Right == null)
                        {
                            throw new ApplicationException($"Invalid arguments: {token.Type}(Left: {op.Left}, Right: {op.Right})");
                        }
                        else
                        {
                            return op;
                        }
                    }
                default:
                    return node;
            }
        }

        ExpressionNode GetCompareNode(IList<Token> tokens, ref int index)
        {
            ExpressionNode node = GetBitOperatorNode(tokens, ref index);
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
                            var op = new BiOperatorNode
                            {
                                Operator = token.Type,
                                Left = node,
                                Right = GetBitOperatorNode(tokens, ref index),
                            };

                            if (op.Right == null || (op.Left == null && op.Right != null))
                            {
                                throw new ApplicationException($"Invalid arguments: {token.Type}(Left: {op.Left}, Right: {op.Right})");
                            }
                            else
                            {
                                node = op;
                            }
                        }
                        break;
                    default:
                        return node;
                }
            }

            return node;
        }

        private ExpressionNode GetBitOperatorNode(IList<Token> tokens, ref int index)
        {
            ExpressionNode node = GetShiftNode(tokens, ref index);
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
                            var op = new BiOperatorNode
                            {
                                Operator = token.Type,
                                Left = node,
                                Right = GetShiftNode(tokens, ref index),
                            };

                            if (op.Right == null || (op.Left == null && op.Right != null))
                            {
                                throw new ApplicationException($"Invalid arguments: {token.Type}(Left: {op.Left}, Right: {op.Right})");
                            }
                            else
                            {
                                node = op;
                            }
                        }
                        break;
                    default:
                        return node;
                }
            }

            return node;
        }


        private ExpressionNode GetShiftNode(IList<Token> tokens, ref int index)
        {
            ExpressionNode node = GetExpressionNode(tokens, ref index);
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
                            var op = new BiOperatorNode
                            {
                                Operator = token.Type,
                                Left = node,
                                Right = GetExpressionNode(tokens, ref index),
                            };

                            if (op.Right == null || (op.Left == null && op.Right != null))
                            {
                                throw new ApplicationException($"Invalid arguments: {token.Type}(Left: {op.Left}, Right: {op.Right})");
                            }
                            else
                            {
                                node = op;
                            }
                        }
                        break;
                    default:
                        return node;
                }
            }

            return node;
        }

        private ExpressionNode GetExpressionNode(IList<Token> tokens, ref int index)
        {
            ExpressionNode node = GetTermNode(tokens, ref index);
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
                            var op = new BiOperatorNode
                            {
                                Operator = token.Type,
                                Left = node,
                                Right = GetTermNode(tokens, ref index),
                            };

                            if(op.Right == null || (op.Left == null && op.Right != null))
                            {
                                throw new ApplicationException($"Invalid arguments: {token.Type}(Left: {op.Left}, Right: {op.Right})");
                            }
                            else
                            {
                                node = op;
                            }
                        }
                        break;
                    default:
                        return node;
                }
            }

            return node;
        }

        private ExpressionNode GetTermNode(IList<Token> tokens, ref int index)
        {
            ExpressionNode node = GetMonoNode(tokens, ref index);
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
                            var op = new BiOperatorNode
                            {
                                Operator = token.Type,
                                Left = node,
                                Right = GetMonoNode(tokens, ref index),
                            };

                            if (op.Right == null || (op.Left == null && op.Right != null))
                            {
                                throw new ApplicationException($"Invalid arguments: {token.Type}(Left: {op.Left}, Right: {op.Right})");
                            }
                            else
                            {
                                node = op;
                            }
                        }
                        break;
                    default:
                        return node;
                }
            }

            return node;
        }

        private ExpressionNode GetMonoNode(IList<Token> tokens, ref int index)
        {
            ExpressionNode node = null;
            int count = tokens.Count;

            if (index < count)
            {
                Token token = tokens[index];

                switch (token.Type)
                {
                    case TokenType.SNA:
                    case TokenType.NAC:
                        {
                            var op = new MonoOperatorNode
                            {
                                Operator = token.Type,
                            };

                            index++;
                            op.Value = GetArrayPosNode(tokens, ref index);

                            if (op.Value == null)
                            {
                                throw new ApplicationException($"Invalid arguments: {token.Type}(Value: {op.Value})");
                            }
                            else
                            {
                                node = op;
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

        private ExpressionNode GetArrayPosNode(IList<Token> tokens, ref int index)
        {
            ExpressionNode node = GetValueNode(tokens, ref index);
            int count = tokens.Count;

            while (index < count)
            {
                Token token = tokens[index];

                switch (token.Type)
                {
                    case TokenType.ARRAY_SIGN:
                        index++;
                        {
                            var op = new BiOperatorNode
                            {
                                Operator = token.Type,
                                Left = node,
                                Right = GetArrayPosNode(tokens, ref index),
                            };

                            if (op.Right == null || ((op.Left == null || op.Left is ConstantNode) && op.Right != null))
                            {
                                throw new ApplicationException($"Invalid arguments: {token.Type}(Left: {op.Left}, Right: {op.Right})");
                            }
                            else
                            {
                                node = op;
                            }
                        }
                        break;
                    default:
                        return node;
                }
            }

            return node;
        }

        private ExpressionNode GetValueNode(IList<Token> tokens, ref int index)
        {
            ExpressionNode node = null;
            var count = tokens.Count;
            Token token = tokens[index];

            switch (token.Type)
            {
                case TokenType.INTEGER:
                    node = new ConstantNode
                    {
                        Value = (token as Integer).Value,
                    };
                    break;
                case TokenType.VARIABLE_SIGN:
                    token = tokens[++index];
                    if(token is Identifier)
                    {
                        node = new IdentifierNode
                        {
                            Value = (token as Identifier).Name
                        };
                    }
                    else
                    {
                        throw new ApplicationException($"Invalid token: {token}");
                    }
                    break;
                case TokenType.IDENTIFIER:
                    node = GetFenxe(token as Identifier, tokens, ref index);
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
                ExpressionNode expression = GetCompareNode(tokens, ref index);

                if(expression != null)
                {
                    fenxe.Arguments.Add(expression);
                }
                else
                {
                    throw new ApplicationException($"Invalid token: {token}");
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
