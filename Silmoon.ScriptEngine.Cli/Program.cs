// See https://aka.ms/new-console-template for more information
using Silmoon;
using Silmoon.Extension;
using Silmoon.ScriptEngine;
using Silmoon.ScriptEngine.Extensions;
using Silmoon.ScriptEngine.Options;
using System.Threading.Tasks;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Args.ParseArgs(args);
        await Entry(args);
        //await compile();
        //await run();
    }
    static async Task Entry(string[] args)
    {
        if (Args.ArgsArray.IsNullOrEmpty())
            Help();
        else
        {
            switch (Args.ArgsArray[0].ToLower())
            {
                case "compile":
                    await compile();
                    break;
                //case "run":
                //    await run();
                //    break;
                default:
                    Console.WriteLine($"Unknown command: {Args.ArgsArray[0]}");
                    break;
            }
        }
    }
    static async Task compile()
    {
        var file = Args.GetParameter("file");
        var output = Args.GetParameter("output");
        if (output.IsNullOrEmpty()) output = Path.GetFileNameWithoutExtension(file) + ".csj";
        //var file = "H:\\Git\\GitHub\\silmoonsone\\Silmoon.ScriptEngine\\AutoTradingScripts\\EAScript1.cs";
        //var output = ".\\bin.csj";
        var options = new EngineCompilerOptions()
        {
            ScriptFiles = [file],
        };
        options.AddCoreReferrer();
        var engine = new EngineCompiler(options);
        var result = await engine.Compile();
        if (result.State && result.Data.Success)
        {
            Console.Write("OK");
            Console.WriteLine();
            byte[] csjData = result.Data.GetEngineExecuteModelBinary(engine.Options);
            File.WriteAllBytes(output, csjData);
            Console.WriteLine($"Output to {Path.GetFullPath(output)}");
        }
        else
        {
            Console.Write("Failed");
            Console.WriteLine();
            foreach (var item in result.Data.Diagnostics)
            {
                Console.WriteLine(item.GetMessage());
            }
        }
    }
    static async Task run()
    {
        var filePath = Args.GetParameter("file");
        var fileData = File.ReadAllBytes(filePath);

        //var fileData = File.ReadAllBytes(@"C:\Users\silmoon\Desktop\main.csj");
        EngineExecuter engineExecuter = new EngineExecuter(fileData);
    }
    static void Help()
    {
        Console.WriteLine("Usage: Silmoon.ScriptEngine.Cli [command] [options]");
        Console.WriteLine("Commands:");
        Console.WriteLine("\tcompile --file [file] --output [output]");
    }
}