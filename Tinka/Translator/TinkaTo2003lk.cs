using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tinka.Translator
{
    public class TinkaTo2003lk : Compiler
    {
        private StreamWriter writer;

        public TinkaTo2003lk() : base()
        {
        }

        protected override void Preprocess(Stream stream)
        {
            this.writer = new StreamWriter(stream);

            var globalAnaxNode = this.nodes.Where(x => x is AnaxNode).Select(x => x as AnaxNode).ToList();
            int stackCount = globalAnaxNode.Select(x => int.Parse(x.Length.Value)).Sum();

            this.writer.WriteLine("'i'c");
            this.writer.WriteLine("nta 4 f5 krz f2 f5@ ; (global) allocate variables");
            this.writer.WriteLine("krz f5 f2");
            this.writer.WriteLine("nta {0} f5", stackCount * 4);


            for (int i = 0; i < globalAnaxNode.Count; i++)
            {
                AnaxNode anax = globalAnaxNode[i];

                if(this.globalVariables.ContainsKey(anax.Name))
                {
                    throw new ApplicationException($"Duplication variable name: {anax.Name}");
                }

                this.globalVariables.Add(anax.Name, (uint)(-stackCount * 4));
                ToAnax(anax, null);

                stackCount -= int.Parse(anax.Length.Value);
            }

            this.writer.WriteLine("inj fasal xx f5@");
            this.writer.WriteLine("krz f2 f5 ; (global) restore stack poiter");
            this.writer.WriteLine("krz f5@ f2 ata 4 f5");
            this.writer.WriteLine("krz f5@ xx ; (global) application end");
            this.writer.WriteLine();
        }

        protected override void Postprocess()
        {
            if(this.writer != null)
            {
                this.writer.Close();
            }
        }

        protected override void ToXok(XokNode node)
        {
            this.writer.WriteLine("xok {0}", node.FunctionName.Value);
        }

        protected override void ToKue(KueNode node)
        {
            this.writer.WriteLine("kue {0}", node.FunctionName.Value);
        }

        protected override void ToCersva(CersvaNode node)
        {
            var anaxList = node.Syntaxes.Where(x => x is AnaxNode)
                .Select(x => (x as AnaxNode)).ToList();
            var anaxDictionary = new Dictionary<IdentifierNode, uint>();
            int stackCount = anaxList.Select(x => int.Parse(x.Length.Value)).Sum();

            this.writer.WriteLine("nll {0} ; cersva {0}", node.Name.Value);
            this.writer.WriteLine("nta 4 f5 krz f3 f5@ ; allocate variables");
            this.writer.WriteLine("krz f5 f3");
            this.writer.WriteLine("nta {0} f5", stackCount * 4);

            // TODO: Argumentsの処理を追加する

            for (int i = 0; i < anaxList.Count; i++)
            {
                AnaxNode anax = anaxList[i];

                if (this.globalVariables.ContainsKey(anax.Name))
                {
                    throw new ApplicationException($"Duplication variable name: {anax.Name}");
                }

                anaxDictionary.Add(anax.Name, (uint)(-stackCount * 4));
                ToAnax(anax, anaxDictionary);

                stackCount -= int.Parse(anax.Length.Value);
            }

            OutputSyntax(node.Syntaxes, anaxDictionary);
            this.writer.WriteLine();
        }

        protected override void ToDosnud(DosnudNode node, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            OutputExpression(node.Expression, "f0", anaxDictionary);

            this.writer.WriteLine("krz f3 f5 ; restore stack poiter");
            this.writer.WriteLine("krz f5@ f3 ata 4 f5");
            this.writer.WriteLine("krz f5@ xx ; dosnud");
        }

        protected override void ToAnax(AnaxNode node, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            if (node.Expression != null)
            {
                if(node.Length.Value != "1")
                {
                    throw new ApplicationException($"Invalid operation: {node}");
                }

                OutputExpression(node.Expression, "f0", anaxDictionary);

                if (anaxDictionary != null && anaxDictionary.ContainsKey(node.Name))
                {
                    this.writer.WriteLine("krz f0 f3+{0}@ ; anax #{1} el f0", anaxDictionary[node.Name], node.Name.Value);
                }
                else if(this.globalVariables.ContainsKey(node.Name))
                {
                    this.writer.WriteLine("krz f0 f2+{0}@ ; anax #{1} el f0", this.globalVariables[node.Name], node.Name.Value);
                }
                else
                {
                    throw new ApplicationException($"Not found variable name : #{node.Name.Value}");
                }
            }
        }

        protected override void ToFi(FiNode node, IDictionary<IdentifierNode, uint> anaxDictionary)
        {

        }

        protected override void ToFal(FalNode node, IDictionary<IdentifierNode, uint> anaxDictionary)
        {

        }

        protected override void ToEl(ExpressionNode left, ExpressionNode right, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            OutputExpression(right, "f0", anaxDictionary);

            if (left is IdentifierNode)
            {
                var identifier = left as IdentifierNode;

                if (anaxDictionary != null && anaxDictionary.ContainsKey(identifier))
                {
                    this.writer.WriteLine("krz f0 f3+{0}@ ; #{1} el f0", anaxDictionary[identifier], identifier.Value);
                }
                else if (this.globalVariables.ContainsKey(identifier))
                {
                    this.writer.WriteLine("krz f0 f2+{0}@ ; #{1} el f0", this.globalVariables[identifier], identifier.Value);
                }
                else
                {
                    throw new ApplicationException($"Not found variable name : #{identifier.Value}");
                }
            }
            else if (left.Operator == TokenType.ARRAY_SIGN)
            {
                var biop = left as BiOperatorNode;

                if (!(biop.Left is IdentifierNode))
                {
                    throw new ApplicationException($"Invalid operation: {biop.Right}");
                }

                var identifier = biop.Left as IdentifierNode;

                OutputExpression(biop.Right, "f1", anaxDictionary);
                this.writer.WriteLine("dro 2 f1");

                if (anaxDictionary != null && anaxDictionary.ContainsKey(identifier))
                {
                    this.writer.WriteLine("ata {0} f1", anaxDictionary[identifier]);
                    this.writer.WriteLine("krz f0 f3+f1@ ; f0 eksa #{0}:#{1}", identifier.Value, biop.Right);
                }
                else if (this.globalVariables.ContainsKey(identifier))
                {
                    this.writer.WriteLine("ata {0} f1", this.globalVariables[identifier]);
                    this.writer.WriteLine("krz f0 f2+f1@ ; f0 eksa #{0}:#{1}", identifier.Value, biop.Right);
                }
                else
                {
                    throw new ApplicationException($"Not found variable name : #{identifier.Value}");
                }
            }
            else
            {
                throw new ApplicationException($"Invalid operation: {left} el {right}");
            }
        }

        protected override void ToEksa(ExpressionNode left, ExpressionNode right, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            OutputExpression(left, "f0", anaxDictionary);

            if (right is IdentifierNode)
            {
                var identifier = right as IdentifierNode;

                if (anaxDictionary != null && anaxDictionary.ContainsKey(identifier))
                {
                    this.writer.WriteLine("krz f0 f3+{0}@ ; f0 eksa #{1}", anaxDictionary[identifier], identifier.Value);
                }
                else if (this.globalVariables.ContainsKey(identifier))
                {
                    this.writer.WriteLine("krz f0 f2+{0}@ ; f0 eksa #{1}", this.globalVariables[identifier], identifier.Value);
                }
                else
                {
                    throw new ApplicationException($"Not found variable name : #{identifier.Value}");
                }
            }
            else if (right.Operator == TokenType.ARRAY_SIGN)
            {
                var biop = right as BiOperatorNode;

                if(!(biop.Right is IdentifierNode))
                {
                    throw new ApplicationException($"Invalid operation: {biop.Right}");
                }

                var identifier = biop.Left as IdentifierNode;

                OutputExpression(biop.Right, "f1", anaxDictionary);
                this.writer.WriteLine("dro 2 f1");

                if (anaxDictionary != null && anaxDictionary.ContainsKey(identifier))
                {
                    this.writer.WriteLine("ata {0} f1", anaxDictionary[identifier]);
                    this.writer.WriteLine("krz f0 f3+f1@ ; f0 eksa #{0}:#{1}", identifier.Value, biop.Right);
                }
                else if (this.globalVariables.ContainsKey(identifier))
                {
                    this.writer.WriteLine("ata {0} f1", this.globalVariables[identifier]);
                    this.writer.WriteLine("krz f0 f2+f1@ ; f0 eksa #{0}:#{1}", identifier.Value, biop.Right);
                }
                else
                {
                    throw new ApplicationException($"Not found variable name : #{identifier.Value}");
                }
            }
            else
            {
                throw new ApplicationException($"Invalid operation: {left} eksa {right}");
            }
        }

        protected override void ToAta(ExpressionNode left, ExpressionNode right, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            OutputExpression(left, "f0", anaxDictionary);
            if(right is IdentifierNode || right is ConstantNode)
            {
                OutputValue(right, "f1", anaxDictionary);
            }
            else
            {
                this.writer.WriteLine("nta 4 f5 krz f0 f5@");
                OutputExpression(right, "f0", anaxDictionary);
                this.writer.WriteLine("krz f0 f1 krz f5@ f0 ata 4 f5");
            }
            this.writer.WriteLine("ata f1 f0 ; f0 ata f1");
        }

        protected override void ToNta(ExpressionNode left, ExpressionNode right, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            OutputExpression(left, "f0", anaxDictionary);
            if (right is IdentifierNode || right is ConstantNode)
            {
                OutputValue(right, "f1", anaxDictionary);
            }
            else
            {
                this.writer.WriteLine("nta 4 f5 krz f0 f5@");
                OutputExpression(right, "f0", anaxDictionary);
                this.writer.WriteLine("krz f0 f1 krz f5@ f0 ata 4 f5");
            }
            this.writer.WriteLine("nta f1 f0 ; f0 nta f1");
        }

        protected override void ToLat(ExpressionNode left, ExpressionNode right, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            OutputExpression(left, "f0", anaxDictionary);
            if (right is IdentifierNode || right is ConstantNode)
            {
                OutputValue(right, "f1", anaxDictionary);
            }
            else
            {
                this.writer.WriteLine("nta 4 f5 krz f0 f5@");
                OutputExpression(right, "f0", anaxDictionary);
                this.writer.WriteLine("krz f0 f1 krz f5@ f0 ata 4 f5");
            }
            this.writer.WriteLine("lat f1 f0 f0 ; f0 lat f1");
        }

        protected override void ToLatsna(ExpressionNode left, ExpressionNode right, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            OutputExpression(left, "f0", anaxDictionary);
            if (right is IdentifierNode || right is ConstantNode)
            {
                OutputValue(right, "f1", anaxDictionary);
            }
            else
            {
                this.writer.WriteLine("nta 4 f5 krz f0 f5@");
                OutputExpression(right, "f0", anaxDictionary);
                this.writer.WriteLine("krz f0 f1 krz f5@ f0 ata 4 f5");
            }
            this.writer.WriteLine("latsna f1 f0 f0 ; f0 latsna f1");
        }

        protected override void ToAda(ExpressionNode left, ExpressionNode right, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            OutputExpression(left, "f0", anaxDictionary);
            if (right is IdentifierNode || right is ConstantNode)
            {
                OutputValue(right, "f1", anaxDictionary);
            }
            else
            {
                this.writer.WriteLine("nta 4 f5 krz f0 f5@");
                OutputExpression(right, "f0", anaxDictionary);
                this.writer.WriteLine("krz f0 f1 krz f5@ f0 ata 4 f5");
            }
            this.writer.WriteLine("ada f1 f0 ; f0 ada f1");
        }

        protected override void ToEkc(ExpressionNode left, ExpressionNode right, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            OutputExpression(left, "f0", anaxDictionary);
            if (right is IdentifierNode || right is ConstantNode)
            {
                OutputValue(right, "f1", anaxDictionary);
            }
            else
            {
                this.writer.WriteLine("nta 4 f5 krz f0 f5@");
                OutputExpression(right, "f0", anaxDictionary);
                this.writer.WriteLine("krz f0 f1 krz f5@ f0 ata 4 f5");
            }
            this.writer.WriteLine("ekc f1 f0 ; f0 ekc f1");
        }

        protected override void ToDal(ExpressionNode left, ExpressionNode right, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            OutputExpression(left, "f0", anaxDictionary);
            if (right is IdentifierNode || right is ConstantNode)
            {
                OutputValue(right, "f1", anaxDictionary);
            }
            else
            {
                this.writer.WriteLine("nta 4 f5 krz f0 f5@");
                OutputExpression(right, "f0", anaxDictionary);
                this.writer.WriteLine("krz f0 f1 krz f5@ f0 ata 4 f5");
            }
            this.writer.WriteLine("dal f1 f0 ; f0 dal f1");
        }

        protected override void ToSna(ExpressionNode value, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            OutputExpression(value, "f0", anaxDictionary);
            this.writer.WriteLine("dal 0 f0 ata 1 f0 ; sna f0");
        }

        protected override void ToNac(ExpressionNode value, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            OutputExpression(value, "f0", anaxDictionary);
            this.writer.WriteLine("dal 0 f0 ; nac f0");
        }

        protected override void ToDto(ExpressionNode left, ExpressionNode right, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            OutputExpression(left, "f0", anaxDictionary);
            if (right is IdentifierNode || right is ConstantNode)
            {
                OutputValue(right, "f1", anaxDictionary);
            }
            else
            {
                this.writer.WriteLine("nta 4 f5 krz f0 f5@");
                OutputExpression(right, "f0", anaxDictionary);
                this.writer.WriteLine("krz f0 f1 krz f5@ f0 ata 4 f5");
            }
            this.writer.WriteLine("dto f1 f0 ; f0 dto f1");
        }

        protected override void ToDro(ExpressionNode left, ExpressionNode right, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            OutputExpression(left, "f0", anaxDictionary);
            if (right is IdentifierNode || right is ConstantNode)
            {
                OutputValue(right, "f1", anaxDictionary);
            }
            else
            {
                this.writer.WriteLine("nta 4 f5 krz f0 f5@");
                OutputExpression(right, "f0", anaxDictionary);
                this.writer.WriteLine("krz f0 f1 krz f5@ f0 ata 4 f5");
            }
            this.writer.WriteLine("dro f1 f0 ; f0 dro f1");
        }

        protected override void ToDtosna(ExpressionNode left, ExpressionNode right, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            OutputExpression(left, "f0", anaxDictionary);
            if (right is IdentifierNode || right is ConstantNode)
            {
                OutputValue(right, "f1", anaxDictionary);
            }
            else
            {
                this.writer.WriteLine("nta 4 f5 krz f0 f5@");
                OutputExpression(right, "f0", anaxDictionary);
                this.writer.WriteLine("krz f0 f1 krz f5@ f0 ata 4 f5");
            }
            this.writer.WriteLine("dtosna f1 f0 ; f0 dtosna f1");
        }

        protected override void ToCompare(TokenType type, ExpressionNode left, ExpressionNode right, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            string op;
            switch (type)
            {
                case TokenType.XTLO:
                    op = "xtlo";
                    break;
                case TokenType.XYLO:
                    op = "xylo";
                    break;
                case TokenType.CLO:
                    op = "clo";
                    break;
                case TokenType.NIV:
                    op = "niv";
                    break;
                case TokenType.LLO:
                    op = "llo";
                    break;
                case TokenType.XOLO:
                    op = "xolo";
                    break;
                case TokenType.XTLONYS:
                    op = "xtlonys";
                    break;
                case TokenType.XYLONYS:
                    op = "xylonys";
                    break;
                case TokenType.LLONYS:
                    op = "llonys";
                    break;
                case TokenType.XOLONYS:
                    op = "xolonys";
                    break;
                default:
                    throw new ArgumentException($"Invalid compare type: {type}");
            }

            OutputExpression(left, "f0", anaxDictionary);
            if (right is IdentifierNode || right is ConstantNode)
            {
                OutputValue(right, "f1", anaxDictionary);
                this.writer.WriteLine("nta 4 f5 krz 0 f5@ ; allocate temporary");
            }
            else
            {
                this.writer.WriteLine("nta 4 f5 krz f0 f5@ ; allocate temporary");
                OutputExpression(right, "f0", anaxDictionary);
                this.writer.WriteLine("inj f5@ f0 f1 krz 0 f5@");
            }
            this.writer.WriteLine("fi f0 f1 {0} malkrz 1 f5@ ; f0 {0} f1", op);
            this.writer.WriteLine("krz f5@ f0 ata 4 f5");
        }

        protected override void ToArrayPos(ExpressionNode left, ExpressionNode right, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            OutputExpression(left, "f0", anaxDictionary);
            if (right is IdentifierNode || right is ConstantNode)
            {
                OutputValue(right, "f1", anaxDictionary);
            }
            else
            {
                this.writer.WriteLine("nta 4 f5 krz f0 f5@");
                OutputExpression(right, "f0", anaxDictionary);
                this.writer.WriteLine("krz f0 f1 krz f5@ f0 ata 4 f5");
            }
            this.writer.WriteLine("dro 2 f1");
            this.writer.WriteLine("krz f0+f1@ f0; f0:f1");
        }

        protected override void ToConstant(ConstantNode constant, string register)
        {
            this.writer.WriteLine("krz {0} {1} ; {0}", constant.Value, register);
        }

        protected override void ToVariable(IdentifierNode identifier, string register, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            if(anaxDictionary != null && anaxDictionary.ContainsKey(identifier))
            {
                this.writer.WriteLine("krz f3+{0}@ {1} ; #{2}", anaxDictionary[identifier], register, identifier.Value);
            }
            else if (this.globalVariables.ContainsKey(identifier))
            {
                this.writer.WriteLine("krz f2+{0}@ {1} ; #{2}", this.globalVariables[identifier], register, identifier.Value);
            }
            else
            {
                throw new ApplicationException($"Not found variable name : #{identifier.Value}");
            }
        }

        protected override void ToFenxe(FenxeNode fenxe, string register, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            int count = ((fenxe.Arguments?.Count ?? 0) + 1) * 4;
            // TODO: Argumentsの前処理を行う
            
            if(register != "f0")
            {
                this.writer.WriteLine("nta 4 f5 krz f0 f5@");
            }

            this.writer.WriteLine("nta 4 f5 inj {0} xx f5@ ata {1} f5", fenxe.Name.Value, count);
            if(register != "f0")
            {
                this.writer.WriteLine("krz f0 {0} krz f5@ f0 ata 4 f5", register);
            }
        }

    }
}
