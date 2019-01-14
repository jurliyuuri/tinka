using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinka.Translator;

namespace Tinka
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length > 0)
                {
                    var outFileOptionIndex = Array.IndexOf(args, "-o");
                    Compiler compiler;

                    if (outFileOptionIndex == -1)
                    {
                        if((Array.IndexOf(args, "--lua64") != -1)) {
                        }
                        else
                        {
                            compiler = GetCompiler(args.ToList());
                            compiler.Output("a.lk");
                        }
                    }
                    else if (outFileOptionIndex == args.Length - 1)
                    {
                        Console.WriteLine("No set output file name");
                        Console.WriteLine("tinka.exe inFileNames");
                        Environment.Exit(1);
                    }
                    else
                    {
                        var outFileIndex = outFileOptionIndex + 1;
                        var inFiles = args.Where((x, i) => i != outFileOptionIndex && i != outFileIndex).ToList();

                        compiler = GetCompiler(inFiles);
                        compiler.Output(args[outFileIndex]);
                    }
                }
                else
                {
                    Console.WriteLine("tinka.exe inFileNames");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(ex.StackTrace);

                Environment.Exit(1);
            }
        }

        private static Compiler GetCompiler(List<string> inFileNames)
        {
            Compiler compiler = null;
            List<string> list = inFileNames.Where(x => x != "-l").ToList();

            if (inFileNames.Any(x => x == "--lua64"))
            {
            }
            else
            {
                compiler = new TinkaTo2003lk();
            }

            compiler?.Input(list);
            return compiler;
        }
    }
}
