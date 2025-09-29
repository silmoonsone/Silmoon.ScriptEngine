// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silmoon.Runtime;
using Silmoon.ScriptEngine.ServiceHostTesting;
using Silmoon.Secure;

var builder = Host.CreateApplicationBuilder();

builder.Services.AddSingleton<EngineService>();
builder.Services.AddHostedService(p => p.GetService<EngineService>());

builder.Services.Configure<EngineServiceOptions>(options =>
{
    // if current working directory path include bin string:
    if (Directory.GetCurrentDirectory().Contains("bin"))
    {
        options.ScriptFiles.Add(@"../../../../AutoTradingScripts/EAScript1.cs");

        //options.ScriptFiles.Add(@"../../../../AutoTradingScripts/ScriptProgram.cs");
        //options.ReferrerAssemblyPaths.Add(@"../../../../AutoTradingFrameworks/bin/Debug/net9.0/AutoTradingFrameworks.dll");
    }
    else
    {
        options.ScriptFiles.Add(@"../AutoTradingScripts/EAScript1.cs");

        //options.ScriptFiles.Add(@"../AutoTradingScripts/ScriptProgram.cs");
        //options.ReferrerAssemblyPaths.Add(@"../AutoTradingFrameworks/bin/Debug/net9.0/AutoTradingFrameworks.dll");
    }

    options.AddCoreReferrer();
    options.ReferrerAssemblyNames.Add("Silmoon.ScriptEngine");
    options.ReferrerAssemblyPaths.Add("../../../../AutoTradingFrameworks/bin/Debug/net9.0/AutoTradingFrameworks.dll");

    options.AssemblyLoadContextName = HashHelper.RandomChars(8, true, false, false);
    options.EntryTypeFullName = "AutoTradingScripts.ScriptProgram";
    options.AssemblyName = "ScriptAssembly";

    //options.ReferrerAssemblyPaths.Add(@"C:\Users\silmoon\Desktop\test.dll");

    options.StartExecuteMethod = MethodExecuteInfo.Create("StartScript", null);
    options.StopExecuteMethod = MethodExecuteInfo.Create("StopScript", null);

});

var host = builder.Build();
host.Run();
