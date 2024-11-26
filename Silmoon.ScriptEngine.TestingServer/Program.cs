// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silmoon.ScriptEngine.Services;

var builder = Host.CreateApplicationBuilder();

builder.Services.AddSingleton<EngineService>();
builder.Services.AddHostedService(p => p.GetService<EngineService>());

builder.Services.Configure<EngineServiceOptions>(options =>
{
    options.ScriptFiles.Add(@"../../../../AuthTradingScripts/ScriptProgram.cs");
    options.ScriptFiles.Add(@"../../../../AuthTradingScripts/EAScript1.cs");

    options.AdditionAssemblyNames.Add("System.Console");
    options.AdditionAssemblyNames.Add("System.Runtime");
    options.AdditionAssemblyNames.Add("System.Collections");
    options.AdditionAssemblyNames.Add("System.Private.CoreLib");
    options.AdditionAssemblyNames.Add("System.Linq");
    options.AdditionAssemblyNames.Add("Silmoon.ScriptEngine");
    options.AdditionAssemblyNames.Add("AutoTradingFrameworks");

    options.MainTypeFullName = "AuthTradingScripts.ScriptProgram";
    options.StartMethod = "StartScript";
    options.StartMethodParameter = null;
    options.StopMethod = "StopScript";
});

var host = builder.Build();
host.Run();
