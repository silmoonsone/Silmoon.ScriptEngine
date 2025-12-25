using Microsoft.CodeAnalysis;
using Mono.Cecil;
using Silmoon;
using Silmoon.Extension;
using Silmoon.Secure;
using Mono.Cecil.Rocks;
using ScriptEngine.Testing;
using Silmoon.ScriptEngine;
using Silmoon.ScriptEngine.Extensions;
using Silmoon.ScriptEngine.Options;

internal class Program
{
    static string mainTypeFullName = "ScriptEngine.TestingCode.MyStorage";
    private static async Task Main(string[] args)
    {
        //byte[] assemblyBytes = File.ReadAllBytes("../../../../SourceCode.Storage/bin/Debug/net8.0/SourceCode.Storage.dll");

        Console.WriteLine("CompilerTesting test:");
        await CompilerTesting();
        Console.WriteLine("EngineCompilerTesting test:");
        await EngineCompilerTesting();
    }

    static async Task CompilerTesting()
    {
        string[] sourceCodeFiles = [
            @"../../../../ScriptEngine.TestingCode/MyStorage.cs",
            @"../../../../ScriptEngine.TestingCode/Storage.cs",
            @"../../../../ScriptEngine.TestingCode/IStorage.cs",
        ];

        Compiler compiler = new Compiler();
        var result = await compiler.CompileSourceFilesAsync("StorageAssembly", sourceCodeFiles, null, null, ["System.Console", "System.Runtime", "System.Collections", "System.Private.CoreLib"], false);

        if (result.Success)
        {
            Console.WriteLine($"Compilation success...(size: {result.Binary.Length}, hash:{result.Binary.GetSHA256Hash().ToHexString()})");
            Console.WriteLine();
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(new MemoryStream(result.Binary));
            var typeDefinition = assemblyDefinition.MainModule.GetType(mainTypeFullName);

            var context0 = new AssemblyLoadContextEx("MyAssembly", null, null, true);
            var assembly0 = context0.LoadFromStream(new MemoryStream(result.Binary));
            var type0 = assembly0.GetType(mainTypeFullName);

            if (type0 is not null)
            {
                var interfaces = type0.GetAllInterfaces();
                interfaces.Each(i => Console.WriteLine("Interfaces:" + i.FullName));
                var interfaces1 = typeDefinition.GetAllInterfaces();
                interfaces1.Each(i => Console.WriteLine("Interfaces2:" + i.InterfaceType.FullName));
                Console.WriteLine();


                var baseTypes = type0.GetAllBaseTypes();
                baseTypes.Each(b => Console.WriteLine("Base Types:" + b.FullName));
                var baseTypes1 = typeDefinition.GetAllBaseTypes();
                baseTypes1.Each(b => Console.WriteLine("Base Types2:" + b.FullName));


                Console.WriteLine();
                Console.WriteLineWithColor("=".Repeat(Console.BufferWidth), ConsoleColor.Green);
                Console.WriteLine("Running...");
                Console.WriteLine();

                context0.Unload();

                while (true)
                {
                    var context = new AssemblyLoadContextEx("MyAssembly", null, null, true);
                    using var codeStream = result.Binary.GetStream();
                    var assembly = context.LoadFromStream(codeStream);
                    Console.WriteLine("Load assembly binarycode ok");
                    Console.WriteLine();

                    var type = assembly.GetType(mainTypeFullName);
                    var instance = Activator.CreateInstance(type);


                    var eventInfo = type.GetEvent("OnSet");
                    eventInfo.AddEventHandler(instance, new Action<string, byte[]>((name, data) =>
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLineWithColor($"Event => SetKeyName: {name}, DataLength: {data.Length}");
                        Console.ResetColor();
                    }));

                    Console.WriteLine("Execute SetName()");
                    type.GetMethod("SetName")?.Invoke(instance, ["Hello World"]);

                    var test = typeDefinition.GetMethods();
                    var test2 = typeDefinition.Methods[1].Body.Instructions[0];

                    var usedTypes = TestHelper.GetUsedTypes(typeDefinition);

                    Console.WriteLine("GetName() result: " + type.GetMethod("GetName")?.Invoke(instance, null));


                    return;

                    Console.WriteLine();

                    type.GetMethod("Set")?.Invoke(instance, ["data", new byte[1024000000]]);
                    context.Unload();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Console.ReadLine();
                }
            }
            else Console.WriteLine($"Type {type0.FullName} not found in assembly");
        }
        else result.Diagnostics.Each(diagnostic => Console.WriteLine(diagnostic));
    }
    static async Task EngineCompilerTesting()
    {
        EngineCompilerOptions option = new EngineCompilerOptions()
        {
            ScriptFiles = [@"../../../../ScriptEngine.TestingCode/MyStorage.cs"],
            EntryTypeFullName = mainTypeFullName,
        };
        option.AddCoreReferrer();

        EngineCompiler compiler = new EngineCompiler(option);
        var result = await compiler.Compile();
        var executer = compiler.GetEngineExecuter(result.Data.Binary, option);
        var loadResult = executer.LoadAssembly();
        var instance = executer.CreateInstance();
        var result2 = executer.Type.GetMethod("SetName")?.Invoke(instance.Data, ["Hello World"]);
        var name = executer.Type.GetMethod("GetName")?.Invoke(instance.Data, null);
        Console.WriteLine(name);

        var interfaces = executer.Type.GetInterfaces();
        var baseType = executer.Type.BaseType;
        //Console.WriteLine(result.ToJsonString());
    }
}