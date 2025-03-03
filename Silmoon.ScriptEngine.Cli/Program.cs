// See https://aka.ms/new-console-template for more information
using Silmoon;
using Silmoon.Extension;
using Silmoon.ScriptEngine;
using System.Threading.Tasks;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Args.ParseArgs(args);
        await Entry(args);
        //await compile();
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
                default:
                    Console.WriteLine($"Unknown command: {Args.ArgsArray[0]}");
                    break;
            }
        }
    }
    static async Task compile()
    {
        var file = Args.GetParameter("--file");
        var output = Args.GetParameter("--output");
        //var file = "I:\\Git\\GitHub\\silmoonsone\\Silmoon.ScriptEngine\\AuthTradingScripts\\EAScript1.cs";
        //var output = ".\\bin.csj";
        using var engine = new EngineInstance(new EngineInstanceOptions()
        {
            ScriptFiles = [file],
        });
        engine.ProcessInstanceOptions();
        Console.Write("Checking...");
        var files = engine.CheckScriptFiles();
        if (!files.State)
        {
            Console.Write(files.Message);
            return;
        }
        Console.Write("OK");
        Console.WriteLine();
        Console.Write("Compiling...");
        var result = await engine.CompileScript();
        if (!result.Success)
        {
            Console.Write("Failed");
            Console.WriteLine();
            foreach (var item in result.Diagnostics)
            {
                Console.WriteLine(item.GetMessage());
            }
            return;
        }
        Console.Write("OK");
        Console.WriteLine();
        byte[] csjData = engine.GetEngineExecuteModelBinary(result);
        File.WriteAllBytes(output, csjData);
        Console.WriteLine($"Output to {Path.GetFullPath(output)}");
    }
    static void Help()
    {
        Console.WriteLine("Usage: Silmoon.ScriptEngine.Cli [command] [options]");
        Console.WriteLine("Commands:");
        Console.WriteLine("  compile --file [file] --output [output]");
    }
}