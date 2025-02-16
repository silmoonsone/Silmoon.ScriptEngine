// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silmoon.Runtime;
using Silmoon.ScriptEngine.Services;

var builder = Host.CreateApplicationBuilder();

builder.Services.AddSingleton<EngineService>();
builder.Services.AddHostedService(p => p.GetService<EngineService>());

builder.Services.Configure<EngineServiceOptions>(options =>
{
    // if current working directory path include bin string:
    if (Directory.GetCurrentDirectory().Contains("bin"))
    {
        options.ScriptFiles.Add(@"../../../../AuthTradingScripts/ScriptProgram.cs");
        options.ScriptFiles.Add(@"../../../../AuthTradingScripts/EAScript1.cs");
        options.ReferrerAssemblyPaths.Add(@"../../../../AutoTradingFrameworks/bin/Debug/net8.0/AutoTradingFrameworks.dll");
    }
    else
    {
        options.ScriptFiles.Add(@"../AuthTradingScripts/ScriptProgram.cs");
        options.ScriptFiles.Add(@"../AuthTradingScripts/EAScript1.cs");
        options.ReferrerAssemblyPaths.Add(@"../AutoTradingFrameworks/bin/Debug/net8.0/AutoTradingFrameworks.dll");
    }

    options.ReferrerAssemblyNames.Add("System.Console");
    options.ReferrerAssemblyNames.Add("System.Runtime");
    options.ReferrerAssemblyNames.Add("System.Collections");
    options.ReferrerAssemblyNames.Add("System.Private.CoreLib");
    options.ReferrerAssemblyNames.Add("System.Linq");
    options.ReferrerAssemblyNames.Add("Silmoon.ScriptEngine");
    //options.AdditionAssemblyNames.Add("AutoTradingFrameworks");

    options.StartTypeFullName = "AuthTradingScripts.ScriptProgram";

    options.StartExecuteMethod = MethodExecuteInfo.Create("StartScript", null);
    options.StopExecuteMethod = MethodExecuteInfo.Create("StopScript", null);

});

var host = builder.Build();
host.Run();
