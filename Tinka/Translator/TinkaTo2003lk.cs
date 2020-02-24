using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tinka.Translator
{
    public class TinkaTo2003lk : TinkaTranscompiler
    {
        private StreamWriter _writer;
        private int cersvaStackCount;
        private int blockCount;

        public TinkaTo2003lk() : base()
        {
            _writer = null!;
            cersvaStackCount = 0;
            blockCount = 0;
        }

        protected override void Preprocess(Stream stream)
        {
            _writer = new StreamWriter(stream);

            var globalAnaxNode = Nodes.Where(x => x is AnaxNode).Select(x => (x as AnaxNode)!).ToList();
            int stackCount = globalAnaxNode.Select(x => int.Parse(x!.Length.Value)).Sum();

            _writer.WriteLine("'i'c");
            _writer.WriteLine("nta 4 f5 krz f2 f5@ ; (global) allocate variables");
            _writer.WriteLine("krz f5 f2");
            _writer.WriteLine("nta {0} f5", stackCount * 4);
            
            for (int i = 0; i < globalAnaxNode.Count; i++)
            {
                AnaxNode anax = globalAnaxNode[i];

                if(GlobalVariables.ContainsKey(anax.Name))
                {
                    throw new ApplicationException($"Duplication variable name: {anax.Name}");
                }

                GlobalVariables.Add(anax.Name, (uint)(-stackCount * 4));
                ToAnax(anax, new Dictionary<IdentifierNode, uint>());

                stackCount -= int.Parse(anax.Length.Value);
            }

            _writer.WriteLine("inj fasal xx f5@");
            _writer.WriteLine("krz f2 f5 ; (global) restore stack poiter");
            _writer.WriteLine("krz f5@ f2 ata 4 f5");
            _writer.WriteLine("krz f5@ xx ; (global) application end");
            _writer.WriteLine();
        }

        protected override void Postprocess()
        {
            if(_writer != null)
            {
                _writer.Close();
            }
        }

        protected override void ToXok(XokNode node)
        {
            _writer.WriteLine("xok {0}", node.FunctionName.Value);
        }

        protected override void ToKue(KueNode node)
        {
            _writer.WriteLine("kue {0}", node.FunctionName.Value);
        }

        protected override void ToCersva(CersvaNode node)
        {
            var anaxList = node.Syntaxes.Where(x => x is AnaxNode)
                .Select(x => (x as AnaxNode)!).ToList();
            var anaxDictionary = new Dictionary<IdentifierNode, uint>();
            int stackCount = anaxList.Select(x => int.Parse(x.Length.Value)).Sum();

            cersvaStackCount = stackCount;

            _writer.WriteLine("nll {0} ; cersva {0}", node.Name.Value);
            _writer.WriteLine("nta 4 f5 krz f3 f5@ ; allocate variables");
            _writer.WriteLine("krz f5 f3");
            _writer.WriteLine("nta {0} f5", stackCount * 4);

            // 引数の設定
            int count = 0;
            if (node.Arguments is List<AnaxNode>)
            {
                count = node.Arguments.Count;
                foreach (var anax in node.Arguments)
                {
                    anaxDictionary.Add(anax.Name, (uint)((count + 1) * 4));
                    count--;
                }
            }

            // 内部変数の設定
            count = 0;
            foreach (var anax in anaxList)
            {
                count += int.Parse(anax.Length.Value);
                anaxDictionary.Add(anax.Name, (uint)(-count * 4));
                ToAnax(anax, anaxDictionary);
            }

            OutputSyntax(node.Syntaxes, anaxDictionary);
            _writer.WriteLine();

            cersvaStackCount = 0;
        }

        protected override void ToDosnud(DosnudNode node, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            if(node.Expression != null)
            {
                OutputExpression(node.Expression, "f0", anaxDictionary);
            }

            _writer.WriteLine("krz f3 f5 ; restore stack poiter");
            _writer.WriteLine("krz f5@ f3 ata 4 f5");
            _writer.WriteLine("krz f5@ xx ; dosnud");
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
                    _writer.WriteLine("krz f0 f3+{0}@ ; anax #{1} el f0", anaxDictionary[node.Name], node.Name.Value);
                }
                else if(GlobalVariables.ContainsKey(node.Name))
                {
                    _writer.WriteLine("krz f0 f2+{0}@ ; anax #{1} el f0", GlobalVariables[node.Name], node.Name.Value);
                }
                else
                {
                    throw new ApplicationException($"Not found variable name : #{node.Name.Value}");
                }
            }
        }

        protected override void ToFi(FiNode node, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            // 内部で使用する変数領域の確保
            var anaxList = node.Syntaxes.Where(x => x is AnaxNode)
                .Select(x => (x as AnaxNode)!).ToList();
            int stackCount = anaxList.Select(x => int.Parse(x.Length.Value)).Sum();
            int block = blockCount++;

            _writer.WriteLine("nta {0} f5 ; allocate variables in fi", stackCount * 4);

            // 条件式
            OutputExpression(node.CompareExpression, "f0", anaxDictionary);
            _writer.WriteLine("fi f0 0 clo malkrz --fi-{0}-- xx ; rinyv fi", block);

            int count = 0;
            for (int i = 0; i < anaxList.Count; i++)
            {
                AnaxNode anax = anaxList[i];

                if (anaxDictionary.ContainsKey(anax.Name))
                {
                    throw new ApplicationException($"Duplication variable name: {anax.Name} in fi");
                }

                count += int.Parse(anax.Length.Value);
                anaxDictionary.Add(anax.Name, (uint)(-(cersvaStackCount + count) * 4));
                ToAnax(anax, anaxDictionary);
            }

            // 文の処理
            OutputSyntax(node.Syntaxes, anaxDictionary);

            // 内部で使用した変数領域の解放
            foreach (var anax in anaxList)
            {
                anaxDictionary.Remove(anax.Name);
            }

            _writer.WriteLine("nll --fi-{0}-- ata {1} f5 ; situv fi", block, stackCount * 4);
        }

        protected override void ToFal(FalNode node, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            // 内部で使用する変数領域の確保
            var anaxList = node.Syntaxes.Where(x => x is AnaxNode)
                .Select(x => (x as AnaxNode)!).ToList();
            int stackCount = anaxList.Select(x => int.Parse(x.Length.Value)).Sum();
            int block = blockCount++;
            
            _writer.WriteLine("nta {0} f5 ; allocate variables in fal", stackCount * 4);

            // 条件式
            _writer.WriteLine("nll --fal-rinyv-{0}--", block);
            OutputExpression(node.CompareExpression, "f0", anaxDictionary);
            _writer.WriteLine("fi f0 0 clo malkrz --fal-situv-{0}-- xx ; rinyv fal", block);

            int count = 0;
            for (int i = 0; i < anaxList.Count; i++)
            {
                AnaxNode anax = anaxList[i];

                if (anaxDictionary.ContainsKey(anax.Name))
                {
                    throw new ApplicationException($"Duplication variable name: {anax.Name} in fal");
                }

                count += int.Parse(anax.Length.Value);
                anaxDictionary.Add(anax.Name, (uint)(-(cersvaStackCount + count) * 4));
                ToAnax(anax, anaxDictionary);
            }

            // 文の処理
            OutputSyntax(node.Syntaxes, anaxDictionary);

            // 内部で使用した変数領域の解放
            foreach (var anax in anaxList)
            {
                anaxDictionary.Remove(anax.Name);
            }

            _writer.WriteLine("krz --fal-rinyv-{0}-- xx", block);
            _writer.WriteLine("nll --fal-situv-{0}-- ata {1} f5 ; situv fal", block, stackCount * 4);
        }

        protected override void ToEl(ExpressionNode left, ExpressionNode right, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            OutputExpression(right, "f0", anaxDictionary);

            if (left is IdentifierNode identifierNode)
            {
                if (anaxDictionary != null && anaxDictionary.ContainsKey(identifierNode))
                {
                    _writer.WriteLine("krz f0 f3+{0}@ ; #{1} el f0", anaxDictionary[identifierNode], identifierNode.Value);
                }
                else if (GlobalVariables.ContainsKey(identifierNode))
                {
                    _writer.WriteLine("krz f0 f2+{0}@ ; #{1} el f0", GlobalVariables[identifierNode], identifierNode.Value);
                }
                else
                {
                    throw new ApplicationException($"Not found variable name : #{identifierNode.Value}");
                }
            }
            else if (left.Operator == TokenType.ARRAY_SIGN)
            {
                var biop = (left as BiOperatorNode)!;

                if (!(biop.Left is IdentifierNode))
                {
                    throw new ApplicationException($"Invalid operation: {biop.Right}");
                }

                var identifier = (biop.Left as IdentifierNode)!;

                OutputExpression(biop.Right, "f1", anaxDictionary);
                _writer.WriteLine("dro 2 f1");

                if (anaxDictionary != null && anaxDictionary.ContainsKey(identifier))
                {
                    _writer.WriteLine("ata {0} f1", anaxDictionary[identifier]);
                    _writer.WriteLine("krz f0 f3+f1@ ; f0 eksa #{0}:#{1}", identifier.Value, biop.Right);
                }
                else if (GlobalVariables.ContainsKey(identifier))
                {
                    _writer.WriteLine("ata {0} f1", GlobalVariables[identifier]);
                    _writer.WriteLine("krz f0 f2+f1@ ; f0 eksa #{0}:#{1}", identifier.Value, biop.Right);
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

            if (right is IdentifierNode identifierNode)
            {
                if (anaxDictionary != null && anaxDictionary.ContainsKey(identifierNode))
                {
                    _writer.WriteLine("krz f0 f3+{0}@ ; f0 eksa #{1}", anaxDictionary[identifierNode], identifierNode.Value);
                }
                else if (GlobalVariables.ContainsKey(identifierNode))
                {
                    _writer.WriteLine("krz f0 f2+{0}@ ; f0 eksa #{1}", GlobalVariables[identifierNode], identifierNode.Value);
                }
                else
                {
                    throw new ApplicationException($"Not found variable name : #{identifierNode.Value}");
                }
            }
            else if (right.Operator == TokenType.ARRAY_SIGN)
            {
                var biop = (right as BiOperatorNode)!;

                if(!(biop.Left is IdentifierNode))
                {
                    throw new ApplicationException($"Invalid operation: {biop.Right}");
                }

                var identifier = (biop.Left as IdentifierNode)!;

                OutputExpression(biop.Right, "f1", anaxDictionary);
                _writer.WriteLine("dro 2 f1");

                if (anaxDictionary != null && anaxDictionary.ContainsKey(identifier))
                {
                    _writer.WriteLine("ata {0} f1", anaxDictionary[identifier]);
                    _writer.WriteLine("krz f0 f3+f1@ ; f0 eksa #{0}:#{1}", identifier.Value, biop.Right);
                }
                else if (GlobalVariables.ContainsKey(identifier))
                {
                    _writer.WriteLine("ata {0} f1", GlobalVariables[identifier]);
                    _writer.WriteLine("krz f0 f2+f1@ ; f0 eksa #{0}:#{1}", identifier.Value, biop.Right);
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
                _writer.WriteLine("nta 4 f5 krz f0 f5@");
                OutputExpression(right, "f0", anaxDictionary);
                _writer.WriteLine("krz f0 f1 krz f5@ f0 ata 4 f5");
            }
            _writer.WriteLine("ata f1 f0 ; f0 ata f1");
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
                _writer.WriteLine("nta 4 f5 krz f0 f5@");
                OutputExpression(right, "f0", anaxDictionary);
                _writer.WriteLine("krz f0 f1 krz f5@ f0 ata 4 f5");
            }
            _writer.WriteLine("nta f1 f0 ; f0 nta f1");
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
                _writer.WriteLine("nta 4 f5 krz f0 f5@");
                OutputExpression(right, "f0", anaxDictionary);
                _writer.WriteLine("krz f0 f1 krz f5@ f0 ata 4 f5");
            }
            _writer.WriteLine("lat f1 f0 f0 ; f0 lat f1");
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
                _writer.WriteLine("nta 4 f5 krz f0 f5@");
                OutputExpression(right, "f0", anaxDictionary);
                _writer.WriteLine("krz f0 f1 krz f5@ f0 ata 4 f5");
            }
            _writer.WriteLine("latsna f1 f0 f0 ; f0 latsna f1");
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
                _writer.WriteLine("nta 4 f5 krz f0 f5@");
                OutputExpression(right, "f0", anaxDictionary);
                _writer.WriteLine("krz f0 f1 krz f5@ f0 ata 4 f5");
            }
            _writer.WriteLine("ada f1 f0 ; f0 ada f1");
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
                _writer.WriteLine("nta 4 f5 krz f0 f5@");
                OutputExpression(right, "f0", anaxDictionary);
                _writer.WriteLine("krz f0 f1 krz f5@ f0 ata 4 f5");
            }
            _writer.WriteLine("ekc f1 f0 ; f0 ekc f1");
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
                _writer.WriteLine("nta 4 f5 krz f0 f5@");
                OutputExpression(right, "f0", anaxDictionary);
                _writer.WriteLine("krz f0 f1 krz f5@ f0 ata 4 f5");
            }
            _writer.WriteLine("dal f1 f0 ; f0 dal f1");
        }

        protected override void ToSna(ExpressionNode value, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            OutputExpression(value, "f0", anaxDictionary);
            _writer.WriteLine("dal 0 f0 ata 1 f0 ; sna f0");
        }

        protected override void ToNac(ExpressionNode value, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            OutputExpression(value, "f0", anaxDictionary);
            _writer.WriteLine("dal 0 f0 ; nac f0");
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
                _writer.WriteLine("nta 4 f5 krz f0 f5@");
                OutputExpression(right, "f0", anaxDictionary);
                _writer.WriteLine("krz f0 f1 krz f5@ f0 ata 4 f5");
            }
            _writer.WriteLine("dto f1 f0 ; f0 dto f1");
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
                _writer.WriteLine("nta 4 f5 krz f0 f5@");
                OutputExpression(right, "f0", anaxDictionary);
                _writer.WriteLine("krz f0 f1 krz f5@ f0 ata 4 f5");
            }
            _writer.WriteLine("dro f1 f0 ; f0 dro f1");
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
                _writer.WriteLine("nta 4 f5 krz f0 f5@");
                OutputExpression(right, "f0", anaxDictionary);
                _writer.WriteLine("krz f0 f1 krz f5@ f0 ata 4 f5");
            }
            _writer.WriteLine("dtosna f1 f0 ; f0 dtosna f1");
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
                _writer.WriteLine("nta 4 f5 krz 0 f5@ ; allocate temporary at compare");
            }
            else
            {
                _writer.WriteLine("nta 4 f5 krz f0 f5@ ; allocate temporary at compare");
                OutputExpression(right, "f0", anaxDictionary);
                _writer.WriteLine("inj f5@ f0 f1 krz 0 f5@");
            }
            _writer.WriteLine("fi f0 f1 {0} malkrz 1 f5@ ; f0 {0} f1", op);
            _writer.WriteLine("krz f5@ f0 ata 4 f5");
        }

        protected override void ToArrayPos(ExpressionNode left, ExpressionNode right, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            if(left is IdentifierNode identifierNode)
            {
                ToArrayVariable(identifierNode, "f0", anaxDictionary);
            }
            else
            {
                throw new ApplicationException($"Left operand is only variable: {left}");
            }

            if (right is IdentifierNode || right is ConstantNode)
            {
                OutputValue(right, "f1", anaxDictionary);
            }
            else
            {
                _writer.WriteLine("nta 4 f5 krz f0 f5@");
                OutputExpression(right, "f0", anaxDictionary);
                _writer.WriteLine("krz f0 f1 krz f5@ f0 ata 4 f5");
            }
            _writer.WriteLine("dro 2 f1");
            _writer.WriteLine("krz f0+f1@ f0; f0:f1");
        }

        private void ToArrayVariable(IdentifierNode identifier, string register, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            if (anaxDictionary != null && anaxDictionary.ContainsKey(identifier))
            {
                _writer.WriteLine("krz f3 {0} ; #{1}", register, identifier.Value);
                _writer.WriteLine("ata {0} {1}", anaxDictionary[identifier], register);
            }
            else if (GlobalVariables.ContainsKey(identifier))
            {
                _writer.WriteLine("krz f2 {0} ; #{1}", register, identifier.Value);
                _writer.WriteLine("ata {0} {1}", GlobalVariables[identifier], register);
            }
            else
            {
                throw new ApplicationException($"Not found variable name : #{identifier.Value}");
            }
        }

        protected override void ToConstant(ConstantNode constant, string register)
        {
            _writer.WriteLine("krz {0} {1} ; {0}", constant.Value, register);
        }

        protected override void ToVariable(IdentifierNode identifier, string register, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            if(anaxDictionary != null && anaxDictionary.ContainsKey(identifier))
            {
                _writer.WriteLine("krz f3+{0}@ {1} ; #{2}", anaxDictionary[identifier], register, identifier.Value);
            }
            else if (GlobalVariables.ContainsKey(identifier))
            {
                _writer.WriteLine("krz f2+{0}@ {1} ; #{2}", GlobalVariables[identifier], register, identifier.Value);
            }
            else
            {
                throw new ApplicationException($"Not found variable name : #{identifier.Value}");
            }
        }

        protected override void ToFenxe(FenxeNode fenxe, string register, IDictionary<IdentifierNode, uint> anaxDictionary)
        {
            if (register != "f0")
            {
                _writer.WriteLine("nta 4 f5 krz f0 f5@");
            }

            int count = 4;
            if (fenxe.Arguments is IList<ExpressionNode>)
            {
                count += fenxe.Arguments.Count * 4;
                _writer.WriteLine("nta {0} f5 ; allocate arguments stack", count);

                foreach (var arg in fenxe.Arguments)
                {
                    count -= 4;
                    OutputExpression(arg, "f0", anaxDictionary);
                    _writer.WriteLine("krz f0 f5+{0}@ ; push argument", count);
                }
            }

            _writer.WriteLine("inj {0} xx f5@ ata {1} f5", fenxe.Name.Value, count);
            if(register != "f0")
            {
                _writer.WriteLine("krz f0 {0} krz f5@ f0 ata 4 f5", register);
            }
        }

    }
}
